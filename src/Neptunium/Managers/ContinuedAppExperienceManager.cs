using Neptunium.Data;
using Neptunium.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.Foundation.Collections;
using Windows.System.RemoteSystems;

namespace Neptunium.Managers
{
    public static class ContinuedAppExperienceManager
    {
        public const string ContinuedAppExperienceAppServiceName = "ContinuedAppExperienceAppService";

        #region Variables and Code-Properties
        private static List<RemoteSystem> systemList = null;
        private static RemoteSystemWatcher remoteSystemWatcher = null;

        public static bool IsRunning { get; private set; }

        public static IEnumerable<RemoteSystem> RemoteSystemsList { get { return systemList.AsEnumerable(); } }

        public static RemoteSystemAccessStatus RemoteSystemAccess { get; private set; }

        public static bool IsSupported { get; private set; }

        public static bool IsInitialized { get; private set; }
        #endregion
        public static async Task InitializeAsync()
        {
            if (IsInitialized) return;

            IsSupported = Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.System.RemoteSystems.RemoteSystem");

            if (!IsSupported) return;

            var result = await RemoteSystem.RequestAccessAsync();

            RemoteSystemAccess = result;

            if (result == RemoteSystemAccessStatus.Allowed)
            {
                systemList = new List<RemoteSystem>();
                remoteSystemWatcher = RemoteSystem.CreateWatcher();
                StartWatchingForRemoteSystems();

                //if (App.BackgroundAccess == BackgroundAccessStatus.AllowedMayUseActiveRealTimeConnectivity
                //    || App.BackgroundAccess == BackgroundAccessStatus.AllowedWithAlwaysOnRealTimeConnectivity
                //    || App.BackgroundAccess == BackgroundAccessStatus.AllowedSubjectToSystemPolicy)
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
                systemList.Remove(systemToRemove);
        }

        private static void RemoteSystemWatcher_RemoteSystemAdded(RemoteSystemWatcher sender, RemoteSystemAddedEventArgs args)
        {
            systemList.Add(args.RemoteSystem);
        }


        #region Settings and Settings-Properties
        public static bool StopPlayingStationOnThisDeviceAfterSuccessfulHandoff { get; private set; }
        #endregion


        private static async Task<AppServiceResponse> SendDataToDeviceAsync(RemoteSystem device, ValueSet data)
        {
            if (!IsSupported) return null;

            try
            {

                string ver = string.Format("{0}.{1}.{2}.{3}", Package.Current.Id.Version.Major, Package.Current.Id.Version.Minor, Package.Current.Id.Version.Build, Package.Current.Id.Version.Revision);
                AppServiceConnection connection = new AppServiceConnection();
                connection.AppServiceName = ContinuedAppExperienceAppServiceName;
                connection.PackageFamilyName = "61121Amrykid.Neptunium_h1dcj6f978yqr";
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

        public static async Task HandoffStationToRemoteDeviceAsync(RemoteSystem device, StationModel station)
        {
            try
            {
                var data = new ValueSet();
                data.Add("Command", "Play-Station");
                data.Add("Station", station.Name);

                var status = await SendDataToDeviceAsync(device, data);

                if ((status != null) && StopPlayingStationOnThisDeviceAfterSuccessfulHandoff)
                {
                    if (status.Message["Status"].ToString() == "OK")
                        StationMediaPlayer.Stop();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Unable to hand off station to remote device.", ex);
            }
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

            switch (message["Command"].ToString())
            {
                case "Play-Station":
                    {
                        if (Windows.Networking.Connectivity.NetworkInformation.GetInternetConnectionProfile() != null)
                        {
                            var stationName = message["Station"].ToString();

                            if (!StationDataManager.IsInitialized)
                                await StationDataManager.InitializeAsync();

                            var stationModel = StationDataManager.Stations.FirstOrDefault(x => x.Name == stationName);

                            if (stationModel != null)
                            {
                                if (StationMediaPlayer.IsPlaying)
                                    StationMediaPlayer.Stop();

                                var result = await StationMediaPlayer.PlayStationAsync(stationModel);

                                if (result == true)
                                {
                                    response.Add("Status", "OK");
                                }
                                else
                                {
                                    response.Add("Status", "CantPlay");
                                }
                            }
                            else
                            {
                                response.Add("Status", "NotFound");
                            }

                            await args.Request.SendResponseAsync(response);
                        }

                        break;
                    }
                default:
                    {
                        response.Add("Status", "Unknown");
                        await args.Request.SendResponseAsync(response);
                        break;
                    }
            }

            deferral.Complete();
        }
        #endregion
    }
}
