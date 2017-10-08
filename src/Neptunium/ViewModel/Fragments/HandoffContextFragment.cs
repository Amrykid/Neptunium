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
                if (NepApp.Media.IsPlaying)
                {
                    var station = NepApp.Media.CurrentStream.ParentStation;
                    await NepApp.Handoff.HandoffStationToRemoteDeviceAsync((RemoteSystem)system, station);
                }
            }
        });

        public ReadOnlyObservableCollection<RemoteSystem> AvailableSystems => NepApp.Handoff.RemoteSystemsList;
    }
}
