using Crystal3.UI.Commands;
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
    public class StationInfoDialogFragment: NepAppUIDialogFragment
    {
        public StationInfoDialogFragment()
        {
            ResultTaskCompletionSource = new TaskCompletionSource<NepAppUIManagerDialogResult>();
        }

        public StationItem Station { get { return GetPropertyValue<StationItem>(); } private set { SetPropertyValue<StationItem>(value: value); } }

        public RelayCommand CancelCommand => new RelayCommand(x => ResultTaskCompletionSource.SetResult(NepAppUIManagerDialogResult.Declined));
        public RelayCommand PlayCommand => new RelayCommand(x =>
            ResultTaskCompletionSource.SetResult(new NepAppUIManagerDialogResult() { ResultType = NepAppUIManagerDialogResult.NepAppUIManagerDialogResultType.Positive }));

        public RelayCommand OpenStationWebsiteCommand => new RelayCommand(async x =>
        {
            if (!string.IsNullOrWhiteSpace(Station.Site))
            {
                await Launcher.LaunchUriAsync(new Uri(Station.Site));
            }
        });

        public override Task<NepAppUIManagerDialogResult> InvokeAsync(object parameter)
        {
            if (parameter == null || !(parameter is StationItem)) Task.FromResult<NepAppUIManagerDialogResult>(NepAppUIManagerDialogResult.Declined);

            Station = parameter as StationItem;
            return ResultTaskCompletionSource.Task;
        }
    }
}
