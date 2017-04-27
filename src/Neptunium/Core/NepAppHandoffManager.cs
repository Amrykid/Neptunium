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

namespace Neptunium
{
    public class NepAppHandoffManager: INepAppFunctionManager
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

            RemoteSystemAccess = await await App.Dispatcher.RunAsync(() => RemoteSystem.RequestAccessAsync());

            systemList = new ObservableCollection<RemoteSystem>();
            RemoteSystemsList = new ReadOnlyObservableCollection<RemoteSystem>(systemList);

            IsInitialized = true;

            if (RemoteSystemAccess == RemoteSystemAccessStatus.Allowed)
            {
                remoteSystemWatcher = RemoteSystem.CreateWatcher(new IRemoteSystemFilter[] { new RemoteSystemDiscoveryTypeFilter(RemoteSystemDiscoveryType.Proximal) });              
            }
        }

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
    }
}