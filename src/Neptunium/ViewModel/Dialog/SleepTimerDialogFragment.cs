using Crystal3.UI.Commands;
using Neptunium.Core.UI;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Neptunium.ViewModel.Dialog
{
    public class SleepTimerDialogFragment : NepAppUIDialogFragment
    {
        public SleepTimerDialogFragment()
        {
            ResultTaskCompletionSource = new TaskCompletionSource<NepAppUIManagerDialogResult>();

            AvailableSleepItems = new ObservableCollection<SleepTimerFlyoutViewFragmentSleepItem>(
                new SleepTimerFlyoutViewFragmentSleepItem[] {
                    new SleepTimerFlyoutViewFragmentSleepItem() {DisplayName = "Disabled/Cancel Timer", TimeToWait=TimeSpan.MinValue },
                    new SleepTimerFlyoutViewFragmentSleepItem() {DisplayName = "5 Minutes", TimeToWait=TimeSpan.FromMinutes(5) },
                    new SleepTimerFlyoutViewFragmentSleepItem() {DisplayName = "10 Minutes", TimeToWait=TimeSpan.FromMinutes(10) },
                    new SleepTimerFlyoutViewFragmentSleepItem() {DisplayName = "15 Minutes", TimeToWait=TimeSpan.FromMinutes(15) },
                    new SleepTimerFlyoutViewFragmentSleepItem() {DisplayName = "30 Minutes", TimeToWait=TimeSpan.FromMinutes(30) },
                    new SleepTimerFlyoutViewFragmentSleepItem() {DisplayName = "1 Hour", TimeToWait=TimeSpan.FromHours(1) },
                    new SleepTimerFlyoutViewFragmentSleepItem() {DisplayName = "2 Hours", TimeToWait=TimeSpan.FromHours(2) },
            });

            SelectedSleepItem = AvailableSleepItems.First(x => x.TimeToWait == TimeSpan.MinValue);
            EstimatedTime = NepApp.MediaPlayer.SleepTimer.IsSleepTimerRunning ? NepApp.MediaPlayer.SleepTimer.EstimateTimeToElapse.Value.ToString("t") : "None";
        }

        public override Task<NepAppUIManagerDialogResult> InvokeAsync(object parameter)
        {
            return ResultTaskCompletionSource.Task;
        }

        public ObservableCollection<SleepTimerFlyoutViewFragmentSleepItem> AvailableSleepItems { get; set; }
        public SleepTimerFlyoutViewFragmentSleepItem SelectedSleepItem
        {
            get { return GetPropertyValue<SleepTimerFlyoutViewFragmentSleepItem>(); }
            set
            {
                SetPropertyValue<SleepTimerFlyoutViewFragmentSleepItem>(value: value);

                EstimatedTime = value.TimeToWait == TimeSpan.MinValue ? "None" : DateTime.Now.Add(value.TimeToWait).ToString("t");
            }
        }

        public string EstimatedTime
        {
            get { return GetPropertyValue<string>(); }
            private set { SetPropertyValue<string>(value: value); }
        }

        public RelayCommand CancelCommand => new RelayCommand(x => ResultTaskCompletionSource.SetResult(NepAppUIManagerDialogResult.Declined));
        public RelayCommand OKCommand => new RelayCommand(x =>
        {
            ResultTaskCompletionSource.SetResult(new NepAppUIManagerDialogResult() { ResultType = NepAppUIManagerDialogResult.NepAppUIManagerDialogResultType.Positive });

            if (SelectedSleepItem != null)
            {
                if (SelectedSleepItem.TimeToWait == TimeSpan.MinValue)
                    NepApp.MediaPlayer.SleepTimer.ClearSleepTimer();
                else
                    NepApp.MediaPlayer.SleepTimer.SetSleepTimer(SelectedSleepItem.TimeToWait);
            }
        });
    }

    public class SleepTimerFlyoutViewFragmentSleepItem
    {
        public string DisplayName { get; set; }
        public TimeSpan TimeToWait { get; set; }
    }
}
