using Crystal3.Core;
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
            ContinuedAppExperienceManager.RemoteSystemsListUpdated += ContinuedAppExperienceManager_RemoteSystemsListUpdated;

            IsBusy = true;

            if (ContinuedAppExperienceManager.RemoteSystemsList != null)
                AvailableDevices = new ObservableCollection<RemoteSystem>(ContinuedAppExperienceManager.RemoteSystemsList);
        }

        private void ContinuedAppExperienceManager_RemoteSystemsListUpdated(object sender, EventArgs e)
        {
            App.Dispatcher.RunWhenIdleAsync(() =>
            {
                AvailableDevices = new ObservableCollection<RemoteSystem>(ContinuedAppExperienceManager.RemoteSystemsList);
                //RaisePropertyChanged(nameof(AvailableDevices));

                IsBusy = false;
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
                        await IoC.Current.Resolve<IMessageDialogService>()
                            .ShowAsync("YAY!", 
                                string.Format("Hand off was successful. '{0}' should begin playing on '{1}' shortly.",
                                    StationMediaPlayer.CurrentStation.Name, device.DisplayName));
                    }
                    else
                    {
                        await IoC.Current.Resolve<IMessageDialogService>()
                            .ShowAsync("Uh-oh!",
                                string.Format("Hand off failed. Unable to get '{0}' playing on '{1}'.",
                                    StationMediaPlayer.CurrentStation.Name, device.DisplayName));
                    }
                }
                catch (Exception ex)
                {
                    await IoC.Current.Resolve<IMessageDialogService>()
                          .ShowAsync("Uh-oh!", "We weren't able to hand off.");
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

        public ObservableCollection<RemoteSystem> AvailableDevices
        {
            get { return GetPropertyValue<ObservableCollection<RemoteSystem>>(); }
            set { SetPropertyValue<ObservableCollection<RemoteSystem>>(value: value); }
        }
    }
}
