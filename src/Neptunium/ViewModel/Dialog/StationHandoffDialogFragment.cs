using Crystal3.UI.Commands;
using Neptunium.Core.UI;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.System.RemoteSystems;

namespace Neptunium.ViewModel.Dialog
{
    public class StationHandoffDialogFragment : NepAppUIDialogFragment
    {
        public StationHandoffDialogFragment()
        {
            ResultTaskCompletionSource = new TaskCompletionSource<NepAppUIManagerDialogResult>();

            if (NepApp.Handoff.IsSupported)
            {
                NepApp.Handoff.RemoteSystemsListUpdated += Handoff_RemoteSystemsListUpdated;
                IsHandoffSupported = true;
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
                var device = (RemoteSystem)system;

                var station = NepApp.MediaPlayer.CurrentStream.ParentStation;

                ResultTaskCompletionSource.SetResult(new NepAppUIManagerDialogResult() { ResultType = NepAppUIManagerDialogResult.NepAppUIManagerDialogResultType.Positive });
                NepApp.UI.Notifier.VibrateClick();

                var controller = await NepApp.UI.Overlay.ShowProgressDialogAsync("Transferring playback...", "Please wait...");
                controller.SetIndeterminate();

                if (await NepApp.Handoff.HandoffStationToRemoteDeviceAsync(device, station))
                {
                    await controller.CloseAsync();
                    await NepApp.UI.ShowInfoDialogAsync("Handoff to " + device.DisplayName + " was successful.", "We were able to start playback on the device.");
                }
                else
                {
                    await controller.CloseAsync();
                    await NepApp.UI.ShowInfoDialogAsync("Handoff to " + device.DisplayName + " was unsuccessful.", "We weren't able to start playback on the device.");
                }

            }
        });

        public RelayCommand CancelCommand => new RelayCommand(x =>
        {
            ResultTaskCompletionSource.SetResult(NepAppUIManagerDialogResult.Declined);
            NepApp.UI.Notifier.VibrateClick();
        });

        public ReadOnlyObservableCollection<RemoteSystem> AvailableSystems => NepApp.Handoff.RemoteSystemsList;
        public bool IsHandoffSupported { get { return GetPropertyValue<bool>(); } private set { SetPropertyValue<bool>(value: value); } }

        public override Task<NepAppUIManagerDialogResult> InvokeAsync(object parameter)
        {
            return ResultTaskCompletionSource.Task;
        }
    }
}
