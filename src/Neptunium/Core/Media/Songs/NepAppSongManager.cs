using Neptunium.Core.Media.History;
using Neptunium.Core.Media.Metadata;
using Neptunium.Core.Stations;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Storage.Streams;
using static Neptunium.NepApp;

namespace Neptunium.Media.Songs
{
    public class NepAppSongManager : INepAppFunctionManager, INotifyPropertyChanged
    {
        private SemaphoreSlim metadataLock = null;
        private Dictionary<NepAppSongMetadataBackground, Uri> artworkUriDictionary = null;

        public SongMetadata CurrentSong { get; private set; }
        public ExtendedSongMetadata CurrentSongWithAdditionalMetadata { get; private set; }

        public SongHistorian History { get; private set; }

        public event EventHandler<NepAppSongChangedEventArgs> PreSongChanged;
        public event EventHandler<NepAppSongChangedEventArgs> SongChanged;
        public event EventHandler<NepAppStationProgramStartedEventArgs> StationRadioProgramStarted;

        public event EventHandler<NepAppSongMetadataArtworkEventArgs> SongArtworkAvailable;
        public event EventHandler<NepAppSongMetadataArtworkEventArgs> NoSongArtworkAvailable;
        public event EventHandler SongArtworkProcessingComplete;

        public event PropertyChangedEventHandler PropertyChanged;

        internal NepAppSongManager()
        {
            metadataLock = new SemaphoreSlim(1);
            artworkUriDictionary = new Dictionary<NepAppSongMetadataBackground, Uri>();
            artworkUriDictionary.Add(NepAppSongMetadataBackground.Album, null);
            artworkUriDictionary.Add(NepAppSongMetadataBackground.Artist, null);

            History = new SongHistorian();
            History.InitializeAsync();
        }

        public Uri GetSongArtworkUri(NepAppSongMetadataBackground nepAppSongMetadataBackground)
        {
            return artworkUriDictionary[nepAppSongMetadataBackground];
        }

        public bool IsSongArtworkAvailable()
        {
            NepAppSongMetadataBackground type;
            return IsSongArtworkAvailable(out type);
        }
        public bool IsSongArtworkAvailable(out NepAppSongMetadataBackground nepAppSongMetadataBackgroundType)
        {
            if (artworkUriDictionary[NepAppSongMetadataBackground.Album] != null)
            {
                nepAppSongMetadataBackgroundType = NepAppSongMetadataBackground.Album;
                return true;
            }
            else if (artworkUriDictionary[NepAppSongMetadataBackground.Artist] != null)
            {
                nepAppSongMetadataBackgroundType = NepAppSongMetadataBackground.Artist;
                return true;
            }

            nepAppSongMetadataBackgroundType = NepAppSongMetadataBackground.None;
            return false;
        }

        internal async void HandleMetadata(SongMetadata songMetadata, StationStream currentStream)
        {
            await metadataLock.WaitAsync();

            try
            {
                StationProgram currentProgram = null;
                if (IsStationProgramBeginning(songMetadata, currentStream, out currentProgram))
                {
                    //we're tuning into a special radio program. this may be a DJ playing remixes, for exmaple.

                    StationRadioProgramStarted?.Invoke(this, new NepAppStationProgramStartedEventArgs()
                    {
                        RadioProgram = currentProgram,
                        Metadata = songMetadata,
                        Station = currentStream?.ParentStation?.Name
                    });


                    SetCurrentMetadataToUnknown(currentProgram.Name);
                    CurrentSongWithAdditionalMetadata = null;
                }
                else
                {
                    //we're tuned into regular programming/music

                    //filter out station messages
                    if (currentStream.ParentStation != null)
                    {
                        if (currentStream.ParentStation.StationMessages != null)
                        {
                            if (currentStream.ParentStation.StationMessages.Contains(songMetadata.Track) ||
                                currentStream.ParentStation.StationMessages.Contains(songMetadata.Artist))
                            {
                                metadataLock.Release();
                                return;
                            }
                        }
                    }

                    CurrentSong = songMetadata;
                    //this is used for the now playing bar via data binding.
                    RaisePropertyChanged(nameof(CurrentSong));

                    CurrentSongWithAdditionalMetadata = null;

                    PreSongChanged?.Invoke(this, new NepAppSongChangedEventArgs(songMetadata));

                    UpdateTransportControls(songMetadata);

                    //todo strip out "Feat." artists

                    ExtendedSongMetadata newMetadata = await MetadataFinder.FindMetadataAsync(songMetadata); //todo: cache
                    CurrentSongWithAdditionalMetadata = newMetadata;

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    History.AddSongAsync(newMetadata);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

                    SongChanged.Invoke(this, new NepAppSongChangedEventArgs(CurrentSongWithAdditionalMetadata));

                    UpdateArtworkMetadata();
                }
            }
            catch (Exception ex)
            {
                Dictionary<string, string> properties = new Dictionary<string, string>();
                properties.Add("Song-Metadata", songMetadata?.ToString());
                properties.Add("Current-Station", currentStream?.ParentStation?.ToString());
                Microsoft.HockeyApp.HockeyClient.Current.TrackException(ex, properties);
            }
            finally
            {
                metadataLock.Release();
            }
        }

        internal void SetCurrentMetadataToUnknown(string program = null)
        {
            //todo make a readonly field
            SongMetadata unknown = new SongMetadata()
            {
                Track = "Unknown Song",
                Artist = "Unknown Artist",
                RadioProgram = program,
                StationPlayedOn = NepApp.MediaPlayer.CurrentStream?.ParentStation?.Name,
                StationLogo = NepApp.MediaPlayer.CurrentStream?.ParentStation?.StationLogoUrl,
                IsUnknownMetadata = true
            };

            CurrentSong = unknown;
            //this is used for the now playing bar via data binding.
            RaisePropertyChanged(nameof(CurrentSong));

            PreSongChanged?.Invoke(this, new NepAppSongChangedEventArgs(unknown));
            //SongChanged?.Invoke(this, new NepAppSongChangedEventArgs(unknown));

            UpdateTransportControls(unknown);

            UpdateArtworkMetadata();
        }


        internal void ResetMetadata()
        {
            CurrentSong = null;
            CurrentSongWithAdditionalMetadata = null;

            RaisePropertyChanged(nameof(CurrentSong));
            PreSongChanged?.Invoke(this, new NepAppSongChangedEventArgs(null));
        }

        private void UpdateArtworkMetadata()
        {
            //Handle backgrounds
            if (CurrentSongWithAdditionalMetadata != null)
            {
                //album artwork
                Uri albumArtUri = null;
                if (CurrentSongWithAdditionalMetadata.Album != null)
                {
                    if (!string.IsNullOrWhiteSpace(CurrentSongWithAdditionalMetadata.Album?.AlbumCoverUrl))
                    {
                        albumArtUri = new Uri(CurrentSongWithAdditionalMetadata.Album?.AlbumCoverUrl);
                    }
                }
                artworkUriDictionary[NepAppSongMetadataBackground.Album] = albumArtUri;
                if (albumArtUri != null)
                {
                    SongArtworkAvailable?.Invoke(this, new NepAppSongMetadataArtworkEventArgs(NepAppSongMetadataBackground.Album, albumArtUri));
                }
                else
                {
                    NoSongArtworkAvailable?.Invoke(this, new NepAppSongMetadataArtworkEventArgs(NepAppSongMetadataBackground.Album, null));
                }


                //artist artwork
                Uri artistArtUri = null;
                if (!string.IsNullOrWhiteSpace(CurrentSongWithAdditionalMetadata.ArtistInfo?.ArtistImage))
                {
                    artistArtUri = new Uri(CurrentSongWithAdditionalMetadata.ArtistInfo?.ArtistImage);
                }
                else if (CurrentSongWithAdditionalMetadata.JPopAsiaArtistInfo != null)
                {
                    //from JPopAsia
                    if (CurrentSongWithAdditionalMetadata.JPopAsiaArtistInfo.ArtistImageUrl != null)
                    {
                        artistArtUri = CurrentSongWithAdditionalMetadata.JPopAsiaArtistInfo.ArtistImageUrl;
                    }
                }
                artworkUriDictionary[NepAppSongMetadataBackground.Artist] = artistArtUri;
                if (artistArtUri != null)
                {
                    SongArtworkAvailable?.Invoke(this, new NepAppSongMetadataArtworkEventArgs(NepAppSongMetadataBackground.Artist, artistArtUri));
                }
                else
                {
                    NoSongArtworkAvailable?.Invoke(this, new NepAppSongMetadataArtworkEventArgs(NepAppSongMetadataBackground.Artist, null));
                }
            }
            else
            {
                //reset all artwork
                artworkUriDictionary[NepAppSongMetadataBackground.Album] = null;
                artworkUriDictionary[NepAppSongMetadataBackground.Artist] = null;

                NoSongArtworkAvailable?.Invoke(this, new NepAppSongMetadataArtworkEventArgs(NepAppSongMetadataBackground.Album, null));
                NoSongArtworkAvailable?.Invoke(this, new NepAppSongMetadataArtworkEventArgs(NepAppSongMetadataBackground.Artist, null));
            }

            SongArtworkProcessingComplete?.Invoke(this, EventArgs.Empty);
        }


        private bool IsStationProgramBeginning(SongMetadata songMetadata, StationStream currentStream, out StationProgram stationProgram)
        {
            Func<StationProgram, bool> getStationProgram = x =>
            {
                if (x.Host.ToLower().Equals(songMetadata.Artist.Trim().ToLower())) return true;
                if (!string.IsNullOrWhiteSpace(x.HostRegexExpression))
                {
                    if (Regex.IsMatch(songMetadata.Artist, x.HostRegexExpression))
                        return true;
                }

                return false;
            };

            if (currentStream.ParentStation?.Programs != null)
            {
                if (currentStream.ParentStation.Programs.Any(getStationProgram))
                {
                    //we're tuning into a special radio program. this may be a DJ playing remixes, for exmaple.
                    stationProgram = currentStream.ParentStation.Programs?.First(getStationProgram);

                    return true;
                }
            }

            stationProgram = null;
            return false;
        }

        private void RaisePropertyChanged(string propertyName)
        {
            App.Dispatcher.RunWhenIdleAsync(() =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            });
        }

        private void UpdateTransportControls(SongMetadata songMetadata)
        {
            try
            {
                var updater = NepApp.MediaPlayer.MediaTransportControls.DisplayUpdater;
                updater.Type = MediaPlaybackType.Music;
                updater.MusicProperties.Title = songMetadata.Track;
                updater.MusicProperties.Artist = songMetadata.Artist;
                updater.AppMediaId = songMetadata.StationPlayedOn.GetHashCode().ToString();
                updater.Thumbnail = RandomAccessStreamReference.CreateFromUri(songMetadata.StationLogo);
                updater.Update();
            }
            catch (COMException) { }
            catch (Exception ex)
            {

            }
        }
    }
}
