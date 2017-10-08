using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.ApplicationModel;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.System.RemoteSystems;
using System.Runtime.InteropServices.WindowsRuntime;
using System.IO;
using System.Linq;
using static Neptunium.NepApp;
using Neptunium.Core.Stations;

namespace Neptunium
{
    public class NepAppHandoffManager : INepAppFunctionManager
    {
        internal NepAppHandoffManager()
        {
        }

        public const string ContinuedAppExperienceAppServiceName = "com.Amrykid.CAEAppService";
        public const string StopPlayingMusicAfterGoodHandoff = "StopPlayingMusicAfterGoodHandoff";
        public const string AppPackageName = "61121Amrykid.Neptunium_h1dcj6f978yqr";

        #region Variables and Code-Properties
        private ObservableCollection<RemoteSystem> systemList = null;
        private RemoteSystemWatcher remoteSystemWatcher = null;
        private AutoResetEvent watcherLock = null;

        public ReadOnlyObservableCollection<RemoteSystem> RemoteSystemsList { get; private set; }

        public RemoteSystemAccessStatus RemoteSystemAccess { get; private set; }

        public bool IsSupported { get; private set; }

        public bool IsInitialized { get; private set; }
        #endregion
        public async Task InitializeAsync()
        {
            if (IsInitialized) return;

            IsSupported = Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.System.RemoteSystems.RemoteSystem");

            if (!IsSupported) return;

            if (!await CheckRemoteAppServiceEnabledAsync())
            {
                //app service listing got removed while building for store.
                IsSupported = false;

                return;
            }


            RemoteSystemAccess = await await App.Dispatcher.RunAsync(() => RemoteSystem.RequestAccessAsync());

            systemList = new ObservableCollection<RemoteSystem>();
            RemoteSystemsList = new ReadOnlyObservableCollection<RemoteSystem>(systemList);

            IsInitialized = true;

            if (RemoteSystemAccess == RemoteSystemAccessStatus.Allowed)
            {
                remoteSystemWatcher = RemoteSystem.CreateWatcher(new IRemoteSystemFilter[] { new RemoteSystemDiscoveryTypeFilter(RemoteSystemDiscoveryType.Proximal) });
                remoteSystemWatcher.RemoteSystemAdded += RemoteSystemWatcher_RemoteSystemAdded;
                remoteSystemWatcher.RemoteSystemRemoved += RemoteSystemWatcher_RemoteSystemRemoved;
                remoteSystemWatcher.RemoteSystemUpdated += RemoteSystemWatcher_RemoteSystemUpdated;

                App.Current.EnteredBackground += Current_EnteredBackground;
                App.Current.LeavingBackground += Current_LeavingBackground;

                remoteSystemWatcher.Start(); //auto runs for 30 seconds. stops when app is suspended - https://docs.microsoft.com/en-us/uwp/api/windows.system.remotesystems.remotesystemwatcher
            }
        }

        private void Current_LeavingBackground(object sender, LeavingBackgroundEventArgs e)
        {
            remoteSystemWatcher.Start();
        }

        private void Current_EnteredBackground(object sender, EnteredBackgroundEventArgs e)
        {

        }

        private void RemoteSystemWatcher_RemoteSystemUpdated(RemoteSystemWatcher sender, RemoteSystemUpdatedEventArgs args)
        {
            var system = systemList.FirstOrDefault(x => x.Id == args.RemoteSystem.Id);
            if (system != null)
            {
                systemList[systemList.IndexOf(system)] = args.RemoteSystem;
                RemoteSystemsListUpdated?.Invoke(this, EventArgs.Empty);
            }
        }

        private void RemoteSystemWatcher_RemoteSystemRemoved(RemoteSystemWatcher sender, RemoteSystemRemovedEventArgs args)
        {
            systemList.Remove(systemList.First(x => x.Id == args.RemoteSystemId));
            RemoteSystemsListUpdated?.Invoke(this, EventArgs.Empty);
        }

        private void RemoteSystemWatcher_RemoteSystemAdded(RemoteSystemWatcher sender, RemoteSystemAddedEventArgs args)
        {
            systemList.Add(args.RemoteSystem);
            RemoteSystemsListUpdated?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler RemoteSystemsListUpdated;

        private async Task<bool> CheckRemoteAppServiceEnabledAsync()
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

        private async Task<Tuple<AppServiceResponse, AppServiceConnection>> SendDataToDeviceAsync(RemoteSystem device, ValueSet data, bool keepAlive = false)
        {
            if (!IsSupported) return null;

            //this method establishes a connection and sends a message to a remote device. optionally keeps the connection alive for further communication.

            string ver = string.Format("{0}.{1}.{2}.{3}",
                Package.Current.Id.Version.Major,
                Package.Current.Id.Version.Minor,
                Package.Current.Id.Version.Build,
                Package.Current.Id.Version.Revision);
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

        private async Task<AppServiceResponse> SendDataToDeviceOverExistingConnectionAsync(AppServiceConnection connection, ValueSet data, bool keepAlive = true)
        {
            if (!IsSupported) return null;
            if (!IsInitialized) return null;

            var response = await connection.SendMessageAsync(data);

            if (!keepAlive)
                connection.Dispose();

            return response;

        }

        private async Task<RemoteLaunchUriStatus> LaunchAppOnDeviceAsync(RemoteSystem device, string args)
        {
            if (!IsSupported) return RemoteLaunchUriStatus.Unknown;

            RemoteSystemConnectionRequest request = new RemoteSystemConnectionRequest(device);
            return await RemoteLauncher.LaunchUriAsync(request, new Uri("nep:" + args));
        }

        public async Task<bool> HandoffStationToRemoteDeviceAsync(RemoteSystem device, StationItem station)
        {
            if (!IsSupported) return false;

            var data = new ValueSet();
            data.Add("Command", "Play-Station");
            data.Add("Station", station.Name);

            var status = await LaunchAppOnDeviceAsync(device, "play-station?station=" + station.Name);

            if (status == RemoteLaunchUriStatus.Success)
            {
                NepApp.Media.Pause();

                return true;
            }

            return false;
        }

        private async Task DoReverseHandoff(Tuple<RemoteSystem, StationItem, AppServiceConnection> streamingDevice)
        {
            var cmd = new ValueSet();
            cmd.Add("Message", "CAE:StopPlayback");
            var stopMsgTask = SendDataToDeviceOverExistingConnectionAsync(streamingDevice.Item3, cmd, keepAlive: false);

            var playStationTask = NepApp.Media.TryStreamStationAsync(streamingDevice.Item2.Streams.First());

            await Task.WhenAny(stopMsgTask, playStationTask);
        }

        public async Task<List<Tuple<RemoteSystem, StationItem, AppServiceConnection>>> DetectStreamingDevicesAsync()
        {
            //this method detects if Neptunium is running on the user's other connected devices.

            if (!IsInitialized)
                await InitializeAsync();

            if (!IsSupported) return null;

            if (RemoteSystemAccess != RemoteSystemAccessStatus.Allowed) return null;

            await Task.Run(() => watcherLock.WaitOne());

            List<Tuple<RemoteSystem, StationItem, AppServiceConnection>> devices = new List<Tuple<RemoteSystem, StationItem, AppServiceConnection>>();

            var packet = new ValueSet();
            packet.Add("Message", "Status");

            foreach (var system in systemList.ToArray())
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
                                    var station = await NepApp.Stations.GetStationByNameAsync(responseMsg["CurrentStation"].ToString());

                                    devices.Add(new Tuple<RemoteSystem, StationItem, AppServiceConnection>(system, station, connection));
                                }
                            }
                        }
                    }
                }
            }

            return devices;
        }

        #region Handling app service requests
        internal void HandleBackgroundActivation(AppServiceTriggerDetails appServiceTriggerDetails)
        {
            if (appServiceTriggerDetails != null)
            {
                appServiceTriggerDetails.AppServiceConnection.RequestReceived += AppServiceConnection_RequestReceived;

                appServiceTriggerDetails.AppServiceConnection.ServiceClosed += AppServiceConnection_ServiceClosed;
            }
        }

        private void AppServiceConnection_ServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args)
        {
            sender.RequestReceived -= AppServiceConnection_RequestReceived;
            sender.ServiceClosed -= AppServiceConnection_ServiceClosed;
        }

        private async void AppServiceConnection_RequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            //this messages handles creating replies to messages since from other instances of Neptunium running on remote devices.

            var deferral = args.GetDeferral();

            var message = args.Request.Message;

            var response = new ValueSet();

            switch (message["Message"].ToString())
            {
                case "Status":
                    {
                        response.Add("Status", "OK");
                        response.Add("IsPlaying", NepApp.Media.IsPlaying);

                        if (NepApp.Media.IsPlaying && NepApp.Media.CurrentStream != null)
                            response.Add("CurrentStation", NepApp.Media.CurrentStream.ParentStation.Name);

                        break;
                    }
                case "CAE:StopPlayback":
                    {
                        if (NepApp.Media.IsPlaying)
                            NepApp.Media.Pause();
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