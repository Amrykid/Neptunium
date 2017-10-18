using Crystal3.Model;
using Crystal3.UI.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.System.RemoteSystems;

namespace Neptunium.ViewModel.Fragments
{
    public class HandoffContextFragment: ViewModelFragment
    {
        public HandoffContextFragment()
        {
            if (NepApp.Handoff.IsSupported)
            {
                NepApp.Handoff.RemoteSystemsListUpdated += Handoff_RemoteSystemsListUpdated;
            }
        }

        private void Handoff_RemoteSystemsListUpdated(object sender, EventArgs e)
        {
           RaisePropertyChanged(nameof(AvailableSystems));
        }

        public RelayCommand HandOffCommand => new RelayCommand(async system =>
        {
            if (system is RemoteSystem)
            {
                if (NepApp.MediaPlayer.IsPlaying)
                {
                    var device = (RemoteSystem)system;

                    var station = NepApp.MediaPlayer.CurrentStream.ParentStation;
                    if (await NepApp.Handoff.HandoffStationToRemoteDeviceAsync(device, station))
                    {
                        await NepApp.UI.ShowInfoDialogAsync("Handoff to " + device.DisplayName + " was successful.", "We were able to start playback on the device.");
                    }
                }
                else
                {
                    await NepApp.UI.ShowInfoDialogAsync("Can't do that!", "You must be listening to something before you can hand it off.");
                }
            }
        });

        public ReadOnlyObservableCollection<RemoteSystem> AvailableSystems => NepApp.Handoff.RemoteSystemsList;
    }
}
