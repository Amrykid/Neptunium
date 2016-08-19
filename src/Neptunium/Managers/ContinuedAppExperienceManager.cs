using Crystal3.Core;
using Crystal3.InversionOfControl;
using Neptunium.Data;
using Neptunium.Media;
using Neptunium.Services.SnackBar;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.System;
using Windows.System.RemoteSystems;

namespace Neptunium.Managers
{
    public static class ContinuedAppExperienceManager
    {
        public const string ContinuedAppExperienceAppServiceName = "com.Amrykid.CAEAppService";
        public const string StopPlayingMusicAfterGoodHandoff = "StopPlayingMusicAfterGoodHandoff";
        public const string AppPackageName = "61121Amrykid.Neptunium_h1dcj6f978yqr";

        #region Variables and Code-Properties
        private static ObservableCollection<RemoteSystem> systemList = null;
        private static RemoteSystemWatcher remoteSystemWatcher = null;

        public static bool IsRunning { get; private set; }

        public static ReadOnlyObservableCollection<RemoteSystem> RemoteSystemsList { get; private set; }

        public static RemoteSystemAccessStatus RemoteSystemAccess { get; private set; }

        public static bool IsSupported { get; private set; }

        public static bool IsInitialized { get; private set; }
        #endregion
        public static async Task InitializeAsync()
        {
            if (IsInitialized) return;

            IsSupported = Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.System.RemoteSystems.RemoteSystem");

            if (!IsSupported) return;


            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(StopPlayingMusicAfterGoodHandoff))
                ApplicationData.Current.LocalSettings.Values.Add(StopPlayingMusicAfterGoodHandoff, true);

            StopPlayingStationOnThisDeviceAfterSuccessfulHandoff = (bool)ApplicationData.Current.LocalSettings.Values[StopPlayingMusicAfterGoodHandoff];

            RemoteSystemAccess = await RemoteSystem.RequestAccessAsync();

            if (RemoteSystemAccess == RemoteSystemAccessStatus.Allowed)
            {
                systemList = new ObservableCollection<RemoteSystem>();
                RemoteSystemsList = new ReadOnlyObservableCollection<RemoteSystem>(systemList);
                remoteSystemWatcher = RemoteSystem.CreateWatcher(new IRemoteSystemFilter[] {new RemoteSystemDiscoveryTypeFilter(RemoteSystemDiscoveryType.Proximal) });
                StartWatchingForRemoteSystems();
            }

            IsInitialized = true;
        }

        public static void StartWatchingForRemoteSystems()
        {
            if (!IsSupported) return;

            if (RemoteSystemAccess == RemoteSystemAccessStatus.Allowed && !IsRunning)
            {
                remoteSystemWatcher.RemoteSystemAdded += RemoteSystemWatcher_RemoteSystemAdded;
                remoteSystemWatcher.RemoteSystemRemoved += RemoteSystemWatcher_RemoteSystemRemoved;
                remoteSystemWatcher.Start();

                IsRunning = true;
            }
        }

        public static void StopWatchingForRemoteSystems()
        {
            if (!IsSupported) return;

            if (remoteSystemWatcher != null && IsRunning)
            {
                remoteSystemWatcher.RemoteSystemAdded -= RemoteSystemWatcher_RemoteSystemAdded;
                remoteSystemWatcher.RemoteSystemRemoved -= RemoteSystemWatcher_RemoteSystemRemoved;
                remoteSystemWatcher?.Stop();

                IsRunning = false;
            }
        }

        private static void RemoteSystemWatcher_RemoteSystemRemoved(RemoteSystemWatcher sender, RemoteSystemRemovedEventArgs args)
        {
            var systemToRemove = systemList.FirstOrDefault(x => x.Id == args.RemoteSystemId);

            if (systemToRemove != null)
            {
                systemList.Remove(systemToRemove);

                if (RemoteSystemsListUpdated != null)
                    RemoteSystemsListUpdated(null, EventArgs.Empty);
            }

        }

        private static void RemoteSystemWatcher_RemoteSystemAdded(RemoteSystemWatcher sender, RemoteSystemAddedEventArgs args)
        {
            systemList.Add(args.RemoteSystem);

            if (RemoteSystemsListUpdated != null)
                RemoteSystemsListUpdated(null, EventArgs.Empty);
        }

        public static event EventHandler RemoteSystemsListUpdated;

        #region Settings and Settings-Properties
        public static bool StopPlayingStationOnThisDeviceAfterSuccessfulHandoff { get; private set; }

        internal static void SetStopPlayingStationOnThisDeviceAfterSuccessfulHandoff(bool shouldStopPlayingAfterSuccessfulHandoff)
        {
            if (!IsInitialized) throw new InvalidOperationException();

            StopPlayingStationOnThisDeviceAfterSuccessfulHandoff = shouldStopPlayingAfterSuccessfulHandoff;

            ApplicationData.Current.LocalSettings.Values[StopPlayingMusicAfterGoodHandoff] = shouldStopPlayingAfterSuccessfulHandoff;
        }
        #endregion


        private static async Task<AppServiceResponse> SendDataToDeviceAsync(RemoteSystem device, ValueSet data)
        {
            if (!IsSupported) return null;

            try
            {
                string ver = string.Format("{0}.{1}.{2}.{3}", Package.Current.Id.Version.Major, Package.Current.Id.Version.Minor, Package.Current.Id.Version.Build, Package.Current.Id.Version.Revision);
                AppServiceConnection connection = new AppServiceConnection();
                connection.AppServiceName = ContinuedAppExperienceAppServiceName;
                connection.PackageFamilyName = AppPackageName;
                var status = await connection.OpenRemoteAsync(new RemoteSystemConnectionRequest(device));

                if (status == AppServiceConnectionStatus.Success)
                {
                    var response = await connection.SendMessageAsync(data);
                    connection.Dispose();

                    return response;
                }
                else
                {
                    connection.Dispose();

                    return null;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static async Task<RemoteLaunchUriStatus> LaunchAppOnDeviceAsync(RemoteSystem device, string args)
        {
            if (!IsSupported) throw new Exception("Not supported.");

            RemoteSystemConnectionRequest request = new RemoteSystemConnectionRequest(device);
            return await RemoteLauncher.LaunchUriAsync(request, new Uri("nep:" + args));
        }

        public static async Task<bool> HandoffStationToRemoteDeviceAsync(RemoteSystem device, StationModel station)
        {
            if (!IsSupported) throw new Exception("Not supported.");

            try
            {
                var data = new ValueSet();
                data.Add("Command", "Play-Station");
                data.Add("Station", station.Name);

                var status = await LaunchAppOnDeviceAsync(device, "play-station?station=" + station.Name);

                if ((status == RemoteLaunchUriStatus.Success) && StopPlayingStationOnThisDeviceAfterSuccessfulHandoff)
                {
                    StationMediaPlayer.Stop();
                }

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public static async void CheckForReverseHandoffOpportunities()
        {
            if (IsSupported)
            {
                var streamingDevices = await DetectStreamingDevicesAsync();

                if (streamingDevices.Count > 0)
                {
                    if (streamingDevices.Count == 1)
                    {
                        var streamingDevice = streamingDevices.First();
                        await IoC.Current.Resolve<ISnackBarService>().ShowActionableSnackAsync(
                            "You're streaming on \'" + streamingDevice.Item1.DisplayName + "\'.",
                            "Transfer playback",
                            async x =>
                            {
                                //do reverse handoff

                                var cmd = new ValueSet();
                                cmd.Add("Message", "StopPlayback");
                                var stopMsgTask = SendDataToDeviceAsync(streamingDevice.Item1, cmd);

                                var playStationTask = StationMediaPlayer.PlayStationAsync(streamingDevice.Item2);

                                await Task.WhenAny(stopMsgTask, playStationTask);
                            },
                            10000);
                    }
                    else
                    {
                        await IoC.Current.Resolve<ISnackBarService>().ShowSnackAsync("You have multiple devices streaming.", 3000);
                    }
                }
            }
        }

        private static async Task<List<Tuple<RemoteSystem, StationModel>>> DetectStreamingDevicesAsync()
        {
            if (!StationDataManager.IsInitialized)
                await StationDataManager.InitializeAsync();

            List<Tuple<RemoteSystem, StationModel>> devices = new List<Tuple<RemoteSystem, StationModel>>();

            var packet = new ValueSet();
            packet.Add("Message", "Status");

            foreach(var system in systemList.ToArray())
            {
                try
                {
                    var response = await SendDataToDeviceAsync(system, packet);

                    if (response != null)
                    {
                        if (response?.Status == AppServiceResponseStatus.Success)
                        {
                            var responseMsg = response.Message;

                            if (responseMsg["Status"].ToString() == "OK")
                            {
                                if (bool.Parse(responseMsg["IsPlaying"].ToString()) == true)
                                {
                                    var station = StationDataManager.Stations.FirstOrDefault(x => x.Name == responseMsg["CurrentStation"].ToString());

                                    devices.Add(new Tuple<RemoteSystem, StationModel>(system, station));
                                }
                            }
                        }
                    }
                }
                catch (Exception) { }
            }

            return devices;
        }

        #region Handling app service requests
        internal static void HandleBackgroundActivation(AppServiceTriggerDetails appServiceTriggerDetails)
        {
            if (appServiceTriggerDetails != null)
            {
                appServiceTriggerDetails.AppServiceConnection.RequestReceived += AppServiceConnection_RequestReceived;
            }
        }

        private static async void AppServiceConnection_RequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            sender.RequestReceived -= AppServiceConnection_RequestReceived;

            var deferral = args.GetDeferral();

            var message = args.Request.Message;

            var response = new ValueSet();

            switch (message["Message"].ToString())
            {
                case "Status":
                    {
                        response.Add("Status", "OK");
                        response.Add("IsPlaying", StationMediaPlayer.IsPlaying);

                        if (StationMediaPlayer.IsPlaying && StationMediaPlayer.CurrentStation != null)
                            response.Add("CurrentStation", StationMediaPlayer.CurrentStation.Name);
                        
                        break;
                    }
                case "StopPlayback":
                    {
                        if (StationMediaPlayer.IsPlaying)
                            StationMediaPlayer.Stop();
                        response.Add("Status", "OK");
                        break;
                    }
                default:
                    {
                        response.Add("Status", "Unknown");
                        break;
                    }
            }

            await args.Request.SendResponseAsync(response);

            deferral.Complete();
        }
        #endregion
    }
}
