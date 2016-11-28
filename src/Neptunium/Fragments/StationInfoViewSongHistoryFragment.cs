using Crystal3.Model;
using Neptunium.Data;
using Neptunium.Data.History;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Neptunium.Fragments
{
    public class StationInfoViewSongHistoryFragment : ViewModelFragment
    {
        public override void Dispose()
        {

        }

        public override async void Invoke(ViewModelBase viewModel, object data)
        {
            if (data != null)
            {
                var station = data as StationModel;

                if (station != null)
                {
                    if (App.IsInternetConnected())
                    {
                        if (station.Streams.Any())
                        {
                            IsBusy = true;
                            if (station.Streams.First().ServerType == StationModelStreamServerType.Shoutcast)
                            {
                                try
                                {
                                    UI.SendMessageToUI("show");

                                    var items = await ShoutcastService.GetShoutcastStationSongHistoryAsync(station);
                                    HistoryItems = new ObservableCollection<HistoryItemModel>(items.Select(item =>
                                    {
                                        var newItem = new HistoryItemModel();
                                        newItem.Song = item.Song;
                                        newItem.Time = item.LocalizedTime;
                                        return newItem;
                                    }));

                                    IsBusy = false;
                                    return;
                                }
                                catch (HttpRequestException)
                                {

                                }
                            }
                            else
                            {
                                UI.SendMessageToUI("hide");
                            }
                        }
                    }
                }
            }


            UI.SendMessageToUI("hide");
            IsBusy = false;
        }

        public ObservableCollection<HistoryItemModel> HistoryItems
        {
            get { return GetPropertyValue<ObservableCollection<HistoryItemModel>>(); }
            set { SetPropertyValue<ObservableCollection<HistoryItemModel>>(value: value); }
        }
    }
}
