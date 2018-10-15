using Neptunium.Core.Media;
using Neptunium.Core.Media.History;
using Neptunium.Core.Media.Metadata;
using Neptunium.Core.Stations;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Windows.System.Threading;
using static Neptunium.NepApp;

namespace Neptunium.Media.Songs
{
    public class NepAppSongManager : INepAppFunctionManager, INotifyPropertyChanged
    {
        private SemaphoreSlim metadataLock = null;
        private SemaphoreSlim blockProgrammingLock = null;
        private Regex featuredArtistRegex = new Regex(@"(?:(?:f|F)(?:ea)*t(?:uring)*\.?\s*(.+)(?:\n|$))");
        private ThreadPoolTimer blockStationProgramTimer = null;

        public SongMetadata CurrentSong { get; private set; }
        public StationItem CurrentStation { get; set; }
        private StationProgram CurrentProgram { get; set; }

        public SongHistorian History { get; private set; }
        public NepAppSongManagerMediaTransportUpdater MediaTransportUpdater { get; private set; }
        public NepAppSongManagerArtworkProcessor ArtworkProcessor { get; private set; }

        public event EventHandler<NepAppSongChangedEventArgs> PreSongChanged;
        public event EventHandler<NepAppSongChangedEventArgs> SongChanged;
        public event EventHandler<NepAppStationProgramStartedEventArgs> StationRadioProgramStarted;

        public event PropertyChangedEventHandler PropertyChanged;

        internal NepAppSongManager()
        {
            metadataLock = new SemaphoreSlim(1);
            blockProgrammingLock = new SemaphoreSlim(1);
            ArtworkProcessor = new NepAppSongManagerArtworkProcessor(this);

            History = new SongHistorian();
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            History.InitializeAsync();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

            MediaTransportUpdater = new NepAppSongManagerMediaTransportUpdater(this);

            VoiceUtility.SongAnnouncementFinished += VoiceUtility_SongAnnouncementFinished;
        }

        private void VoiceUtility_SongAnnouncementFinished(object sender, EventArgs e)
        {

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

        internal void RefreshMetadata()
        {
            if (CurrentSong == null || CurrentStation == null)
            {
                SetCurrentMetadataToUnknown();
            }
            else
            {
                //this is used for the now playing bar via data binding.
                RaisePropertyChanged(nameof(CurrentSong));

                PreSongChanged?.Invoke(this, new NepAppSongChangedEventArgs(CurrentSong));
            }
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

        internal async void HandleMetadata(SongMetadata songMetadata, StationStream currentStream)
        {
            if (songMetadata == null || songMetadata.IsUnknownMetadata)
            {
                SetCurrentMetadataToUnknown();
                return;
            }

            await metadataLock.WaitAsync();
            try
            {
                CurrentStation = await NepApp.Stations.GetStationByNameAsync(currentStream.ParentStation);

                StationProgram currentProgram = null;
                if (IsHostedStationProgramBeginning(songMetadata, CurrentStation, out currentProgram))
                {
                    //we're tuning into a hosted radio program. this may be a DJ playing remixes, for example.

                    ActivateStationProgrammingMode(songMetadata, currentStream, currentProgram);

                    //block programs are handled differently.
                }
                else
                {
                    //we're tuned into regular programming/music

                    HandleStationSongMetadata(songMetadata, currentStream, CurrentStation);
                }
            }
            catch (Exception ex)
            {
                if (!Debugger.IsAttached)
                {
                    Dictionary<string, string> properties = new Dictionary<string, string>();
                    properties.Add("Song-Metadata", songMetadata?.ToString());
                    properties.Add("Current-Station", currentStream?.ParentStation);
                    Microsoft.HockeyApp.HockeyClient.Current.TrackException(ex, properties);
                }
                else
                {
                    Debugger.Break();
                }
            }
            finally
            {
                metadataLock.Release();
            }
        }

        private async void HandleStationSongMetadata(SongMetadata songMetadata, StationStream currentStream, StationItem currentStation)
        {
            if (CurrentProgram != null)
            {
                if (CurrentProgram.Style == StationProgramStyle.Hosted)
                {
                    CurrentProgram = null; //hosted program ended.
                }
            }

            //filter out station messages
            if (!string.IsNullOrWhiteSpace(currentStream.ParentStation))
            {
                if (currentStation.StationMessages != null)
                {
                    if (currentStation.StationMessages.Contains(songMetadata.Track) ||
                        currentStation.StationMessages.Contains(songMetadata.Artist))
                    {
                        metadataLock.Release();
                        return;
                    }
                }
            }


            string originalArtistString = songMetadata.Artist;
            if (featuredArtistRegex.IsMatch(songMetadata.Artist))
                songMetadata.Artist = featuredArtistRegex.Replace(songMetadata.Artist, "").Trim();

            if (CurrentSong.ToString().Equals(songMetadata.ToString())) return;

            CurrentSong = songMetadata;
            //this is used for the now playing bar via data binding.
            RaisePropertyChanged(nameof(CurrentSong));

            PreSongChanged?.Invoke(this, new NepAppSongChangedEventArgs(songMetadata));

            ExtendedSongMetadata newMetadata = await MetadataFinder.FindMetadataAsync(songMetadata); //todo: cache

            if (newMetadata != null)
            {

                //todo strip out "Feat." artists
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

                CurrentSong = newMetadata;
                CurrentSong.SongLength = newMetadata.SongLength;
                RaisePropertyChanged(nameof(CurrentSong));
            }

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            History.AddSongAsync(CurrentSong);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

            SongChanged.Invoke(this, new NepAppSongChangedEventArgs(CurrentSong));

            ArtworkProcessor.UpdateArtworkMetadata();
        }

        internal SongMetadata GetCurrentSongOrUnknown()
        {
            if (CurrentStation == null) return null;

            if (CurrentSong != null) return CurrentSong;

            return GetUnknownSongMetadata();
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
                Station = currentStream?.ParentStation
            });

            CurrentProgram = currentProgram;
            SetCurrentMetadataToUnknown(currentProgram.Name);
        }

        private SongMetadata GetUnknownSongMetadata(StationItem stationPlaying = null, string radioProgram = null)
        {
            SongMetadata unknown = new SongMetadata()
            {
                Track = "Unknown Song",
                Artist = "Unknown Artist",
                RadioProgram = radioProgram,
                StationPlayedOn = (stationPlaying ?? CurrentStation)?.Name,
                StationLogo = (stationPlaying ?? CurrentStation)?.StationLogoUrl,
                IsUnknownMetadata = true
            };

            return unknown;
        }

        internal async void SetCurrentMetadataToUnknown(string program = null)
        {
            await metadataLock.WaitAsync();
            //todo make a readonly field
            SongMetadata unknown = GetUnknownSongMetadata();

            CurrentSong = unknown;
            //this is used for the now playing bar via data binding.
            RaisePropertyChanged(nameof(CurrentSong));

            PreSongChanged?.Invoke(this, new NepAppSongChangedEventArgs(unknown));

            ArtworkProcessor.UpdateArtworkMetadata();
            metadataLock.Release();
        }


        internal void ResetMetadata()
        {
            DeactivateProgramBlockTimer();

            CurrentSong = null;
            CurrentStation = null;

            RaisePropertyChanged(nameof(CurrentSong));
            PreSongChanged?.Invoke(this, new NepAppSongChangedEventArgs(null));
        }




        private bool IsHostedStationProgramBeginning(SongMetadata songMetadata, StationItem currentStation, out StationProgram stationProgram)
        {
            try
            {
                //this function checkes for "hosted" programs which rely on metadata matching to activate.
                Func<StationProgram, bool> getStationProgram = x =>
                {
                    if (x.Style != StationProgramStyle.Hosted) return false;

                    if (!string.IsNullOrWhiteSpace(x.Host))
                    {
                        if (x.Host.ToLower().Equals(songMetadata.Artist.Trim().ToLower())) return true;
                    }

                    if (!string.IsNullOrWhiteSpace(x.HostRegexExpression))
                    {
                        if (Regex.IsMatch(songMetadata.Artist, x.HostRegexExpression))
                            return true;
                    }

                    return false;
                };

                if (currentStation?.Programs != null)
                {
                    if (currentStation.Programs.Any(getStationProgram))
                    {
                        //we're tuning into a special radio program. this may be a DJ playing remixes, for exmaple.
                        stationProgram = currentStation.Programs?.First(getStationProgram);

                        return true;
                    }
                }
            }
            catch (Exception)
            {
#if DEBUG
                if (Debugger.IsAttached)
                    Debugger.Break();
#endif
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
    }
}
