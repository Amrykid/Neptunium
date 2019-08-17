using Neptunium.Core.Stations;
using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage.Streams;
using Windows.Web.Http;

namespace Neptunium.Media
{
    internal class DirectStationMediaStreamer : BasicNepAppMediaStreamer
    {
        HttpClient httpClient = new HttpClient();
        public override void InitializePlayback(MediaPlayer player)
        {
            Player = player;
            Player.Source = StreamMediaSource;
        }

        public override async Task TryConnectAsync(StationStream stream)
        {
            var httpRequest = new HttpRequestMessage(HttpMethod.Get, stream.StreamUrl); //some servers don't support head. we need a better way to poke the server
            var httpResponse = await httpClient.SendRequestAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead);

            if (!httpResponse.IsSuccessStatusCode) throw new Neptunium.Core.NeptuniumStreamConnectionFailedException(stream);

            //collect header information here

            httpRequest.Dispose();
            httpResponse.Dispose();
            httpClient.Dispose();

            await Task.Delay(500);

            StreamMediaSource = MediaSource.CreateFromUri(stream.StreamUrl);

            var station = await NepApp.Stations.GetStationByNameAsync(stream.ParentStation);

            RaiseMetadataChanged(new Core.Media.Metadata.SongMetadata() { Artist = "Unknown Artist", Track = "Unknown Song",
                StationPlayedOn = stream.ParentStation, StationLogo = station.StationLogoUrl, IsUnknownMetadata = true });
        }
    }
}