﻿using Crystal3.UI.Commands;
using Neptunium.Core.Stations;
using Neptunium.Core.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.System;

namespace Neptunium.ViewModel.Dialog
{
    public class StationInfoDialogFragment : NepAppUIDialogFragment
    {
        private volatile bool dialogAnswered = false;

        public StationInfoDialogFragment()
        {
            ResultTaskCompletionSource = new TaskCompletionSource<NepAppUIManagerDialogResult>();
        }

        public StationItem Station { get { return GetPropertyValue<StationItem>(); } private set { SetPropertyValue<StationItem>(value: value); } }

        public RelayCommand CancelCommand => new RelayCommand(x =>
        {
            if (dialogAnswered) return;
            ResultTaskCompletionSource.SetResult(NepAppUIManagerDialogResult.Declined);
            dialogAnswered = true;
            NepApp.UI.Notifier.VibrateClick();
        });
        public RelayCommand PlayCommand => new RelayCommand(x =>
        {
            if (dialogAnswered) return;
            ResultTaskCompletionSource.SetResult(new NepAppUIManagerDialogResult() { ResultType = NepAppUIManagerDialogResult.NepAppUIManagerDialogResultType.Positive });
            dialogAnswered = true;
            NepApp.UI.Notifier.VibrateClick();
        });
        public RelayCommand PlayStreamCommand => new RelayCommand(x =>
        {
            if (dialogAnswered) return;
            ResultTaskCompletionSource.SetResult(new NepAppUIManagerDialogResult() { ResultType = NepAppUIManagerDialogResult.NepAppUIManagerDialogResultType.Positive, Selection = x });
            dialogAnswered = true;
        });


        public RelayCommand OpenStationWebsiteCommand => new RelayCommand(async x =>
        {
            if (!string.IsNullOrWhiteSpace(Station.Site))
            {
                NepApp.UI.Notifier.VibrateClick();
                await Launcher.LaunchUriAsync(new Uri(Station.Site));
            }
        });

        public RelayCommand PinStationCommand => new RelayCommand(async x =>
        {
            NepApp.UI.Notifier.VibrateClick();
            if (!NepApp.UI.Notifier.CheckIfStationTilePinned(Station))
            {
                try
                {
                    bool result = await NepApp.UI.Notifier.PinStationAsTileAsync(Station);
                }
                catch (Exception)
                {
                    await NepApp.UI.ShowInfoDialogAsync("Uh-oh!", "Wasn't able to pin the station.");
                }
            }
            else
            {
                await NepApp.UI.ShowInfoDialogAsync("Can't do that!", "This station is already pinned!");
            }
        });

        public override Task<NepAppUIManagerDialogResult> InvokeAsync(object parameter)
        {
            if (parameter == null || !(parameter is StationItem))
            {
                dialogAnswered = true;
                ResultTaskCompletionSource.SetCanceled();
                return Task.FromResult<NepAppUIManagerDialogResult>(NepAppUIManagerDialogResult.Declined);
            }

            Station = parameter as StationItem;
            return ResultTaskCompletionSource.Task;
        }
    }
}
