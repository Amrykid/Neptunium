using Crystal3.InversionOfControl;
using Crystal3.Model;
using Neptunium.Media;
using Neptunium.Services.SnackBar;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media.Playback;
using Windows.UI.Xaml;

namespace Neptunium.Fragments
{
    public class SleepTimerFlyoutViewFragment : ViewModelFragment
    {
        public class SleepTimerFlyoutViewFragmentSleepItem
        {
            public string DisplayName { get; set; }
            public TimeSpan TimeToWait { get; set; }
        }

        private DispatcherTimer sleepTimer = new DispatcherTimer();

        public SleepTimerFlyoutViewFragment()
        {
            AvailableSleepItems = new ObservableCollection<SleepTimerFlyoutViewFragmentSleepItem>(
                new SleepTimerFlyoutViewFragmentSleepItem[] {
                    new SleepTimerFlyoutViewFragmentSleepItem() {DisplayName = "Disabled", TimeToWait=TimeSpan.MinValue },
                    new SleepTimerFlyoutViewFragmentSleepItem() {DisplayName = "5 Minutes", TimeToWait=TimeSpan.FromMinutes(5) },
                    new SleepTimerFlyoutViewFragmentSleepItem() {DisplayName = "10 Minutes", TimeToWait=TimeSpan.FromMinutes(10) },
                    new SleepTimerFlyoutViewFragmentSleepItem() {DisplayName = "15 Minutes", TimeToWait=TimeSpan.FromMinutes(15) },
                    new SleepTimerFlyoutViewFragmentSleepItem() {DisplayName = "30 Minutes", TimeToWait=TimeSpan.FromMinutes(30) },
                    new SleepTimerFlyoutViewFragmentSleepItem() {DisplayName = "1 Hour", TimeToWait=TimeSpan.FromHours(1) },
                    new SleepTimerFlyoutViewFragmentSleepItem() {DisplayName = "2 Hours", TimeToWait=TimeSpan.FromHours(2) },
            });

            SelectedSleepItem = AvailableSleepItems.First(x => x.TimeToWait == TimeSpan.MinValue);

            IsViewEnabled = false;

            BackgroundMediaPlayer.Current.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;

            this.PropertyChanged += SleepTimerFlyoutViewFragment_PropertyChanged;

            sleepTimer.Tick += SleepTimer_Tick;
        }

        private async void PlaybackSession_PlaybackStateChanged(MediaPlaybackSession sender, object args)
        {
            await App.Dispatcher.RunWhenIdleAsync(() =>
            {
                IsViewEnabled = StationMediaPlayer.IsPlaying;

                if (!IsViewEnabled)
                {
                    SelectedSleepItem = AvailableSleepItems.First(x => x.TimeToWait == TimeSpan.MinValue);
                }
            });
        }

        private async void SleepTimer_Tick(object sender, object e)
        {
            if (StationMediaPlayer.IsPlaying)
            {
                StationMediaPlayer.Stop();

                SelectedSleepItem = AvailableSleepItems.First(x => x.TimeToWait == TimeSpan.MinValue);

                if (await App.GetIfPrimaryWindowVisibleAsync())
                    await IoC.Current.Resolve<ISnackBarService>().ShowSnackAsync("We've stopped your music as per your sleep timer.", 5000);
            }
        }

        private void SleepTimerFlyoutViewFragment_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SelectedSleepItem))
            {
                if (SelectedSleepItem.TimeToWait == TimeSpan.MinValue)
                {
                    //disable timer

                    if (sleepTimer.IsEnabled)
                        sleepTimer.Stop();
                }
                else
                {
                    //set/reset timer

                    if (sleepTimer.IsEnabled)
                        sleepTimer.Stop();

                    sleepTimer.Interval = SelectedSleepItem.TimeToWait;

                    sleepTimer.Start();
                }
            }
        }

        public override void Dispose()
        {
            this.PropertyChanged -= SleepTimerFlyoutViewFragment_PropertyChanged;
            sleepTimer.Tick -= SleepTimer_Tick;
        }

        public override void Invoke(ViewModelBase viewModel, object data)
        {

        }

        public bool IsViewEnabled { get { return GetPropertyValue<bool>(); } set { SetPropertyValue<bool>(value: value); } }

        public ObservableCollection<SleepTimerFlyoutViewFragmentSleepItem> AvailableSleepItems { get; set; }
        public SleepTimerFlyoutViewFragmentSleepItem SelectedSleepItem
        {
            get { return GetPropertyValue<SleepTimerFlyoutViewFragmentSleepItem>(); }
            set { SetPropertyValue<SleepTimerFlyoutViewFragmentSleepItem>(value: value); }
        }
    }
}
