using Crystal3.Model;
using Crystal3.UI.Commands;
using Neptunium.Core.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Neptunium.ViewModel.Fragments
{
    public class SleepTimerContextFragment : ViewModelFragment
    {
        public class SleepTimerFlyoutViewFragmentSleepItem
        {
            public string DisplayName { get; set; }
            public TimeSpan TimeToWait { get; set; }
        }

        public SleepTimerContextFragment()
        {
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
            EstimatedTime = "None";

            this.PropertyChanged += SleepTimerContextFragment_PropertyChanged;

        }

        private void SleepTimerContextFragment_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SelectedSleepItem))
            {
                if (SelectedSleepItem != null)
                {
                    if (SelectedSleepItem.TimeToWait == TimeSpan.MinValue)
                        NepApp.MediaPlayer.SleepTimer.ClearSleepTimer();
                    else
                        NepApp.MediaPlayer.SleepTimer.SetSleepTimer(SelectedSleepItem.TimeToWait);

                    EstimatedTime = SelectedSleepItem.TimeToWait == TimeSpan.MinValue ? "None" : DateTime.Now.Add(SelectedSleepItem.TimeToWait).ToString("t");
                }
            }
        }

        public ObservableCollection<SleepTimerFlyoutViewFragmentSleepItem> AvailableSleepItems { get; set; }
        public SleepTimerFlyoutViewFragmentSleepItem SelectedSleepItem
        {
            get { return GetPropertyValue<SleepTimerFlyoutViewFragmentSleepItem>(); }
            set { SetPropertyValue<SleepTimerFlyoutViewFragmentSleepItem>(value: value); }
        }

        public string EstimatedTime
        {
            get { return GetPropertyValue<string>(); }
            private set { SetPropertyValue<string>(value: value); }
        }
    }
}
