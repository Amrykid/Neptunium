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
using System.Threading;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.ApplicationModel;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.System;
using Windows.System.RemoteSystems;
using Crystal3.Navigation;
using Crystal3;

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
        private static AutoResetEvent watcherLock = null;

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

            if (!await CheckRemoteAppServiceEnabledAsync())
            {
                //app service listing got removed while building for store.

                App.MessageQueue.Enqueue("Handoff functionality is disabled.");
                
                return;
            }

            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(StopPlayingMusicAfterGoodHandoff))
                ApplicationData.Current.LocalSettings.Values.Add(StopPlayingMusicAfterGoodHandoff, true);

            StopPlayingStationOnThisDeviceAfterSuccessfulHandoff = (bool)ApplicationData.Current.LocalSettings.Values[StopPlayingMusicAfterGoodHandoff];

            RemoteSystemAccess = await await App.Dispatcher.RunAsync(() => RemoteSystem.RequestAccessAsync());

            if (RemoteSystemAccess == RemoteSystemAccessStatus.Allowed)
            {
                watcherLock = new AutoResetEvent(true);

                systemList = new ObservableCollection<RemoteSystem>();
                RemoteSystemsList = new ReadOnlyObservableCollection<RemoteSystem>(systemList);
                remoteSystemWatcher = RemoteSystem.CreateWatcher(new IRemoteSystemFilter[] { new RemoteSystemDiscoveryTypeFilter(RemoteSystemDiscoveryType.Proximal) });
                StartWatchingForRemoteSystems();
            }

            IsInitialized = true;
        }

        private static async Task<bool> CheckRemoteAppServiceEnabledAsync()
        {
            //Package.appxmanifest -> AppxManifest.xml upon installation
            var manifestFile = await Package.Current.InstalledLocation.GetFileAsync("AppxManifest.xml");
            var winrtStream = await manifestFile.OpenReadAsync();
            var realStream = winrtStream.AsStreamForRead();
            XDocument document = XDocument.Load(realStream);
            XNamespace packageNamespace = "http://schemas.microsoft.com/appx/manifest/foundation/windows10";
            var packageElement = document.Element(packageNamespace + "Package");
            var applicationsElement = packageElement.Element(packageNamespace + "Applications");
            var applicationElement = applicationsElement.Element(packageNamespace + "Application");
            var extensions = applicationElement.Element(packageNamespace + "Extensions").Elements();

            bool result = extensions.Any(x =>
            {
                if (x.HasAttributes)
                {
                    XAttribute attr = x.Attribute("Category");
                    if (attr?.Value == "windows.appService")
                    {
                        return ((XElement)x.FirstNode).Attribute("SupportsRemoteSystems") != null;
                    }
                }

                return false;
            });

            realStream.Dispose();
            winrtStream.Dispose();

            return result;
        }

        public static async void StartWatchingForRemoteSystems()
        {
            if (!IsSupported) return;

            if (RemoteSystemAccess == RemoteSystemAccessStatus.Allowed && !IsRunning)
            {
                watcherLock.Reset();

                remoteSystemWatcher.RemoteSystemAdded += RemoteSystemWatcher_RemoteSystemAdded;
                remoteSystemWatcher.RemoteSystemRemoved += RemoteSystemWatcher_RemoteSystemRemoved;
                remoteSystemWatcher.RemoteSystemUpdated += RemoteSystemWatcher_RemoteSystemUpdated;
                remoteSystemWatcher.Start();

                IsRunning = true;

                await Task.Delay(5000);

                watcherLock.Set();
            }
        }

        public static void StopWatchingForRemoteSystems()
        {
            if (!IsSupported) return;

            if (remoteSystemWatcher != null && IsRunning)
            {
                remoteSystemWatcher.RemoteSystemAdded -= RemoteSystemWatcher_RemoteSystemAdded;
                remoteSystemWatcher.RemoteSystemRemoved -= RemoteSystemWatcher_RemoteSystemRemoved;
                remoteSystemWatcher.RemoteSystemUpdated -= RemoteSystemWatcher_RemoteSystemUpdated;
                remoteSystemWatcher?.Stop();

                IsRunning = false;
            }
        }

        private static void RemoteSystemWatcher_RemoteSystemUpdated(RemoteSystemWatcher sender, RemoteSystemUpdatedEventArgs args)
        {
            if (!systemList.Any(x => x.Id == args.RemoteSystem.Id))
            {
                systemList.Add(args.RemoteSystem);
            }
            else
            {
                int index = systemList.IndexOf(systemList.First(x => x.Id == args.RemoteSystem.Id));
                systemList[index] = args.RemoteSystem;
            }

            if (RemoteSystemsListUpdated != null)
                RemoteSystemsListUpdated(null, EventArgs.Empty);
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
            if (!systemList.Any(x => x.Id == args.RemoteSystem.Id))
                systemList.Add(args.RemoteSystem);

            if (RemoteSystemsListUpdated != null)
                RemoteSystemsListUpdated(null, EventArgs.Empty);
        }

        public static event EventHandler RemoteSystemsListUpdated;

        #region Settings and Settings-Properties
        public static bool StopPlayingStationOnThisDeviceAfterSuccessfulHandoff { get; private set; }

        internal static void SetStopPlayingStationOnThisDeviceAfterSuccessfulHandoff(bool shouldStopPlayingAfterSuccessfulHandoff)
        {
            StopPlayingStationOnThisDeviceAfterSuccessfulHandoff = shouldStopPlayingAfterSuccessfulHandoff;

            ApplicationData.Current.LocalSettings.Values[StopPlayingMusicAfterGoodHandoff] = shouldStopPlayingAfterSuccessfulHandoff;
        }
        #endregion


        private static async Task<Tuple<AppServiceResponse, AppServiceConnection>> SendDataToDeviceAsync(RemoteSystem device, ValueSet data, bool keepAlive = false)
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

                    if (!keepAlive)
                        connection.Dispose();

                    return new Tuple<AppServiceResponse, AppServiceConnection>(response, keepAlive ? connection : null);
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

        private static async Task<AppServiceResponse> SendDataToDeviceOverExistingConnectionAsync(AppServiceConnection connection, ValueSet data, bool keepAlive = true)
        {
            if (!IsSupported) return null;

            var response = await connection.SendMessageAsync(data);

            if (!keepAlive)
                connection.Dispose();

            return response;

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
                                cmd.Add("Message", "CAE:StopPlayback");
                                var stopMsgTask = SendDataToDeviceOverExistingConnectionAsync(streamingDevice.Item3, cmd, keepAlive: false);

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

        private static async Task<List<Tuple<RemoteSystem, StationModel, AppServiceConnection>>> DetectStreamingDevicesAsync()
        {
            if (!IsInitialized)
                await InitializeAsync();

            if (!StationDataManager.IsInitialized)
                await StationDataManager.InitializeAsync();

            if (RemoteSystemAccess != RemoteSystemAccessStatus.Allowed) return null;

            await Task.Run(() => watcherLock.WaitOne());

            List<Tuple<RemoteSystem, StationModel, AppServiceConnection>> devices = new List<Tuple<RemoteSystem, StationModel, AppServiceConnection>>();

            var packet = new ValueSet();
            packet.Add("Message", "Status");

            foreach (var system in systemList.ToArray())
            {
                try
                {
                    var result = await SendDataToDeviceAsync(system, packet, keepAlive: true);

                    if (result != null)
                    {
                        var response = result.Item1;
                        var connection = result.Item2;
                        if (response != null)
                        {
                            if (response.Status == AppServiceResponseStatus.Success)
                            {
                                var responseMsg = response.Message;

                                if (responseMsg["Status"].ToString() == "OK")
                                {
                                    if (bool.Parse(responseMsg["IsPlaying"].ToString()) == true)
                                    {
                                        var station = StationDataManager.Stations.FirstOrDefault(x => x.Name == responseMsg["CurrentStation"].ToString());

                                        devices.Add(new Tuple<RemoteSystem, StationModel, AppServiceConnection>(system, station, connection));
                                    }
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

                appServiceTriggerDetails.AppServiceConnection.ServiceClosed += AppServiceConnection_ServiceClosed;
            }
        }

        private static void AppServiceConnection_ServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args)
        {
            sender.RequestReceived -= AppServiceConnection_RequestReceived;
            sender.ServiceClosed -= AppServiceConnection_ServiceClosed;
        }

        private static async void AppServiceConnection_RequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
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
                case "CAE:StopPlayback":
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
