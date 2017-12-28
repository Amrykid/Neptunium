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
using Windows.System.Threading;
using Windows.UI.Xaml;
using static Neptunium.NepApp;

namespace Neptunium.Media.Songs
{
    public class NepAppSongManager : INepAppFunctionManager, INotifyPropertyChanged
    {
        private SemaphoreSlim metadataLock = null;
        private SemaphoreSlim blockProgrammingLock = null;
        private Regex featuredArtistRegex = new Regex(@"(?:(?:f|F)(?:ea)*t(?:uring)*\.?\s*(.+)(?:\n|$))");
        private Dictionary<NepAppSongMetadataBackground, Uri> artworkUriDictionary = null;
        private ThreadPoolTimer blockStationProgramTimer = null;

        public SongMetadata CurrentSong { get; private set; }
        public StationItem CurrentStation { get; set; }
        private StationProgram CurrentProgram { get; set; }
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
            blockProgrammingLock = new SemaphoreSlim(1);
            artworkUriDictionary = new Dictionary<NepAppSongMetadataBackground, Uri>();
            artworkUriDictionary.Add(NepAppSongMetadataBackground.Album, null);
            artworkUriDictionary.Add(NepAppSongMetadataBackground.Artist, null);

            History = new SongHistorian();
            History.InitializeAsync();
        }
        

        private void DeactivateProgramBlockTimer()
        {
            if (blockStationProgramTimer != null)
            {
                blockStationProgramTimer.Cancel();
                blockStationProgramTimer = null;
            }

            if (CurrentProgram != null)
            {
                if (CurrentProgram.Style == StationProgramStyle.Block)
                {
                    CurrentProgram = null;
                }
            }
        }

        private void ActivateProgramBlockTimer()
        {
            if (CurrentStation == null) return;
            if (CurrentStation.Programs == null) return;
            if (CurrentStation.Programs.Length == 0) return;

            CheckForStationBlockRightNow();

            if (blockStationProgramTimer != null) return;

            blockStationProgramTimer = ThreadPoolTimer.CreatePeriodicTimer(timer =>
            {
                CheckForStationBlockRightNow();
            }, TimeSpan.FromMinutes(5));
        }

        private async void CheckForStationBlockRightNow()
        {
            await blockProgrammingLock.WaitAsync();
            if (CurrentStation.Programs.Any(FilterStationBlockPrograms))
            {
                var currentBlock = CurrentStation.Programs.First(FilterStationBlockPrograms);

                if (CurrentProgram == currentBlock) return; //prevent duplicate events.

                CurrentProgram = currentBlock;

                StationRadioProgramStarted?.Invoke(this, new NepAppStationProgramStartedEventArgs()
                {
                    RadioProgram = currentBlock,
                    Metadata = CurrentSong,
                    Station = CurrentStation.Name
                });
            }
            blockProgrammingLock.Release();
        }

        private bool FilterStationBlockPrograms(StationProgram program)
        {
            if (program.Style != StationProgramStyle.Block)
            {
                return false;
            }

            return program.TimeListings.Any(listing =>
            {
                return listing.Time.TimeOfDay < DateTime.Now.TimeOfDay && DateTime.Now.TimeOfDay < listing.EndTime.TimeOfDay && listing.Day == DateTime.Now.DayOfWeek;
            });
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
                CurrentStation = currentStream.ParentStation;

                StationProgram currentProgram = null;
                if (IsHostedStationProgramBeginning(songMetadata, currentStream, out currentProgram))
                {
                    //we're tuning into a hosted radio program. this may be a DJ playing remixes, for example.

                    ActivateStationProgrammingMode(songMetadata, currentStream, currentProgram);

                    //block programs are handled differently.
                }
                else
                {
                    //we're tuned into regular programming/music

                    if (CurrentProgram != null)
                    {
                        if (CurrentProgram.Style == StationProgramStyle.Hosted)
                        {
                            CurrentProgram = null; //hosted program ended.
                        }
                    }

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


                    string originalArtistString = songMetadata.Artist;
                    if (featuredArtistRegex.IsMatch(songMetadata.Artist))
                        songMetadata.Artist = featuredArtistRegex.Replace(songMetadata.Artist, "").Trim();

                    CurrentSong = songMetadata;
                    //this is used for the now playing bar via data binding.
                    RaisePropertyChanged(nameof(CurrentSong));

                    CurrentSongWithAdditionalMetadata = null;

                    PreSongChanged?.Invoke(this, new NepAppSongChangedEventArgs(songMetadata));

                    UpdateTransportControls(songMetadata);

                    //todo strip out "Feat." artists

                    ExtendedSongMetadata newMetadata = await MetadataFinder.FindMetadataAsync(songMetadata); //todo: cache

                    if (featuredArtistRegex.IsMatch(originalArtistString))
                    {
                        try
                        {
                            var artistsMatch = featuredArtistRegex.Match(originalArtistString);
                            var artists = artistsMatch.Groups[1].Value.Split(',').Select(x => x.Trim());
                            newMetadata.FeaturedArtists = artists.ToArray();
                        }
                        catch (Exception)
                        {

                        }
                    }

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
                properties.Add("Current-Station", currentStream?.ParentStation?.Name);
                Microsoft.HockeyApp.HockeyClient.Current.TrackException(ex, properties);
            }
            finally
            {
                metadataLock.Release();
            }
        }

        internal void SetCurrentStation(StationItem parentStation)
        {
            CurrentStation = parentStation;

            ActivateProgramBlockTimer();
        }

        private void ActivateStationProgrammingMode(SongMetadata songMetadata, StationStream currentStream, StationProgram currentProgram)
        {
            StationRadioProgramStarted?.Invoke(this, new NepAppStationProgramStartedEventArgs()
            {
                RadioProgram = currentProgram,
                Metadata = songMetadata,
                Station = currentStream?.ParentStation?.Name
            });


            CurrentProgram = currentProgram;
            SetCurrentMetadataToUnknown(currentProgram.Name);
            CurrentSongWithAdditionalMetadata = null;
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
            DeactivateProgramBlockTimer();

            CurrentSong = null;
            CurrentSongWithAdditionalMetadata = null;
            CurrentStation = null;

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

                if (CurrentSongWithAdditionalMetadata.FanArtTVBackgroundUrl != null)
                {
                    albumArtUri = CurrentSongWithAdditionalMetadata.FanArtTVBackgroundUrl;
                }
                if (CurrentSongWithAdditionalMetadata.Album != null && albumArtUri == null)
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

            if (artworkUriDictionary[NepAppSongMetadataBackground.Album] != null)
            {
                UpdateTransportControls(CurrentSongWithAdditionalMetadata);
            }
        }


        private bool IsHostedStationProgramBeginning(SongMetadata songMetadata, StationStream currentStream, out StationProgram stationProgram)
        {
            //this function checkes for "hosted" programs which rely on metadata matching to activate.
            Func<StationProgram, bool> getStationProgram = x =>
            {
                if (x.Style != StationProgramStyle.Hosted) return false;
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
            if (songMetadata == null) return;

            try
            {
                var updater = NepApp.MediaPlayer.MediaTransportControls.DisplayUpdater;
                updater.Type = MediaPlaybackType.Music;
                updater.MusicProperties.Title = songMetadata.Track;
                updater.MusicProperties.Artist = songMetadata.Artist;
                updater.AppMediaId = songMetadata.StationPlayedOn.GetHashCode().ToString();


                if (songMetadata is ExtendedSongMetadata)
                {
                    var extended = (ExtendedSongMetadata)songMetadata;
                    updater.Thumbnail = RandomAccessStreamReference.CreateFromUri(artworkUriDictionary[NepAppSongMetadataBackground.Album] ?? songMetadata.StationLogo);

                    if (extended.Album != null)
                    {
                        updater.MusicProperties.AlbumTitle = extended.Album?.Album ?? "";
                        updater.MusicProperties.AlbumArtist = extended.Album?.Artist ?? "";
                    }
                }
                else
                {
                    updater.Thumbnail = RandomAccessStreamReference.CreateFromUri(songMetadata.StationLogo);
                    updater.MusicProperties.AlbumTitle = "";
                    updater.MusicProperties.AlbumArtist = "";
                }

                updater.Update();
            }
            catch (COMException) { }
            catch (Exception ex)
            {

            }
        }
    }
}
