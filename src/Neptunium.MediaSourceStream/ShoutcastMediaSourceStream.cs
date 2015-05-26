using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.MediaProperties;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace Neptunium.MediaSourceStream
{
    public class ShoutcastMediaSourceStream
    {
        public Windows.Media.Core.MediaStreamSource MediaStreamSource { get; private set; }
        public ShoutcastStationInfo StationInfo { get; private set; }

        StreamSocket socket = null;
        DataWriter socketWriter = null;
        DataReader socketReader = null;

        Uri streamUrl = null;

        uint bitRate = 0;
        int metadataInt = 0;
        int metadataPos = 0;

        string contentType = "audio/mpeg";

        public ShoutcastMediaSourceStream(Uri url)
        {

            //var endpoint = new Windows.Networking.EndpointPair(url.Host,url.Port.ToString(),)

            StationInfo = new ShoutcastStationInfo();

            streamUrl = url;

            socket = new StreamSocket();
        }

        public async Task ConnectAsync()
        {

            await HandleConnection();


            AudioStreamDescriptor audioDescriptor = new AudioStreamDescriptor(AudioEncodingProperties.CreateMp3(32, 2, bitRate));

            MediaStreamSource = new Windows.Media.Core.MediaStreamSource(audioDescriptor);
            MediaStreamSource.SampleRequested += MediaStreamSource_SampleRequested;
        }

        private async Task HandleConnection()
        {
            //http://www.smackfu.com/stuff/programming/shoutcast.html
            try
            {
                await socket.ConnectAsync(new Windows.Networking.HostName(streamUrl.Host), streamUrl.Port.ToString());

                socketWriter = new DataWriter(socket.OutputStream);
                socketReader = new DataReader(socket.InputStream);

                socketWriter.WriteString("GET /; HTTP/1.1" + Environment.NewLine);
                socketWriter.WriteString("Icy-MetaData: 1" + Environment.NewLine);
                socketWriter.WriteString(Environment.NewLine);
                await socketWriter.StoreAsync();
                await socketWriter.FlushAsync();

                string response = string.Empty;
                while (!response.EndsWith(Environment.NewLine + Environment.NewLine))
                {
                    await socketReader.LoadAsync(1);
                    response += socketReader.ReadString(1);
                }

                ParseResponse(response);
            }
            catch (Exception)
            { }
        }

        private void ParseResponse(string response)
        {
            string[] responseSplitByLine = response.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            var headers = responseSplitByLine.Where(line => line.Contains(":")).Select(line =>
            {
                string header = line.Substring(0, line.IndexOf(":"));
                string value = line.Substring(line.IndexOf(":") + 1);

                var pair = new KeyValuePair<string, string>(header.ToUpper(), value);

                return pair;
            }).ToArray();

            StationInfo.StationName = headers.First(x => x.Key == "ICY-NAME").Value;
            StationInfo.StationGenre = headers.First(x => x.Key == "ICY-GENRE").Value;

            bitRate = uint.Parse(headers.FirstOrDefault(x => x.Key == "ICY-BR").Value);
            metadataInt = int.Parse(headers.First(x => x.Key == "ICY-METAINT").Value);
            contentType = headers.First(x => x.Key == "CONTENT-TYPE").Value;
        }

        private async void MediaStreamSource_SampleRequested(Windows.Media.Core.MediaStreamSource sender, Windows.Media.Core.MediaStreamSourceSampleRequestedEventArgs args)
        {
            var request = args.Request;
            var deferral = request.GetDeferral();
            MediaStreamSample sample = null;

            switch(contentType.ToUpper())
            {
                case "AUDIO/MPEG":
                    {
                        //mp3
                        sample = ParseMP3SampleAsync();
                        //await MediaStreamSample.CreateFromStreamAsync(socket.InputStream, bitRate, new TimeSpan(0, 0, 1));
                    }
                    break;
            }

            metadataPos += (int)sample.Buffer.Length;

            request.Sample = sample;

            deferral.Complete();

            if (metadataPos == metadataInt)
            {
                metadataPos = 0;

                await socketReader.LoadAsync(1);
                uint metaInt = socketReader.ReadByte();

                if (metaInt > 0)
                {
                    int metaDataInfo = socketReader.ReadByte() * 16;

                    await socketReader.LoadAsync((uint)metaDataInfo);

                    var metadata = socketReader.ReadString((uint)metaDataInfo);

                    var db = metadata;
                }
            }
        }

        private async Task<MediaStreamSample> ParseMP3SampleAsync()
        {
            uint frameHeader = 32;
            socketReader.LoadAsync(frameHeader);
        }
    }
}
