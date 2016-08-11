using Crystal3.InversionOfControl;
using Crystal3.Model;
using Neptunium.Managers;
using Neptunium.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.System.RemoteSystems;

namespace Neptunium.Fragments
{
    public class HandOffFlyoutViewFragment : ViewModelFragment
    {
        internal HandOffFlyoutViewFragment()
        {
            AvailableDevices = ContinuedAppExperienceManager.RemoteSystemsList;
            ContinuedAppExperienceManager.RemoteSystemsListUpdated += ContinuedAppExperienceManager_RemoteSystemsListUpdated;
        }

        private void ContinuedAppExperienceManager_RemoteSystemsListUpdated(object sender, EventArgs e)
        {
            App.Dispatcher.RunWhenIdleAsync(() =>
            {
                AvailableDevices = ContinuedAppExperienceManager.RemoteSystemsList;
                //RaisePropertyChanged(nameof(AvailableDevices));
            });
        }

        public override async void Invoke(ViewModelBase viewModel, object data)
        {
            var device = data as RemoteSystem;

            if (device != null && StationMediaPlayer.IsPlaying)
            {
                IsBusy = true;
                try
                {
                    var results = await ContinuedAppExperienceManager.HandoffStationToRemoteDeviceAsync(device, StationMediaPlayer.CurrentStation);

                    if (results)
                    {
                        //todo add a message box
                    }
                }
                catch (Exception)
                {

                }
                finally
                {
                    IsBusy = false;
                }
            }
        }

        public override void Dispose()
        {
            ContinuedAppExperienceManager.RemoteSystemsListUpdated -= ContinuedAppExperienceManager_RemoteSystemsListUpdated;
        }

        public ReadOnlyObservableCollection<RemoteSystem> AvailableDevices
        {
            get { return GetPropertyValue<ReadOnlyObservableCollection<RemoteSystem>>(); }
            set { SetPropertyValue<ReadOnlyObservableCollection<RemoteSystem>>(value: value); }
        }
    }
}
