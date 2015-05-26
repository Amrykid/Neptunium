using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.MediaProperties;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;

namespace Neptunium.MediaSourceStream
{
    public class ShoutcastMediaSourceStream
    {
        public enum StreamAudioFormat
        {
            ///"audio/mpeg";
            MP3,
            AAC
        }

        public Windows.Media.Core.MediaStreamSource MediaStreamSource { get; private set; }
        public ShoutcastStationInfo StationInfo { get; private set; }

        StreamSocket socket = null;
        DataWriter socketWriter = null;
        DataReader socketReader = null;

        Uri streamUrl = null;

        uint bitRate = 0;
        uint metadataInt = 0;
        uint metadataPos = 0;

        StreamAudioFormat contentType = StreamAudioFormat.MP3;

        TimeSpan timeOffSet = new TimeSpan();
        private UInt64 byteOffset;

        #region MP3 Framesize and length for Layer II and Layer III - https://code.msdn.microsoft.com/windowsapps/MediaStreamSource-media-dfd55dff/sourcecode?fileId=111712&pathId=208523738

        UInt32 mp3_sampleSize = 1152;
        TimeSpan mp3_sampleDuration = new TimeSpan(0, 0, 0, 0, 70);
        #endregion

        public ShoutcastMediaSourceStream(Uri url)
        {
            StationInfo = new ShoutcastStationInfo();

            streamUrl = url;

            socket = new StreamSocket();
        }

        public async Task ConnectAsync()
        {
            await HandleConnection();

            AudioStreamDescriptor audioDescriptor = new AudioStreamDescriptor(AudioEncodingProperties.CreateMp3(44100, 2, bitRate));

            MediaStreamSource = new Windows.Media.Core.MediaStreamSource(audioDescriptor);
            MediaStreamSource.SampleRequested += MediaStreamSource_SampleRequested;
            MediaStreamSource.CanSeek = false;
            MediaStreamSource.Starting += MediaStreamSource_Starting;
            MediaStreamSource.Closed += MediaStreamSource_Closed;
        }

        private void MediaStreamSource_Closed(MediaStreamSource sender, MediaStreamSourceClosedEventArgs args)
        {
            MediaStreamSource.Starting -= MediaStreamSource_Starting;
            MediaStreamSource.Closed -= MediaStreamSource_Closed;
            MediaStreamSource.SampleRequested -= MediaStreamSource_SampleRequested;
        }

        private void MediaStreamSource_Starting(MediaStreamSource sender, MediaStreamSourceStartingEventArgs args)
        {
            //args.Request.SetActualStartPosition(timeOffSet);
            //args.Request.
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
            metadataInt = uint.Parse(headers.First(x => x.Key == "ICY-METAINT").Value);
            contentType = headers.First(x => x.Key == "CONTENT-TYPE").Value.ToUpper() == "AUDIO/MPEG" ? StreamAudioFormat.MP3 : StreamAudioFormat.AAC;
        }

        private async void MediaStreamSource_SampleRequested(Windows.Media.Core.MediaStreamSource sender, Windows.Media.Core.MediaStreamSourceSampleRequestedEventArgs args)
        {
            var request = args.Request;
            var deferral = request.GetDeferral();
            MediaStreamSample sample = null;

            request.ReportSampleProgress(25);

            //if metadataPos is less than mp3_sampleSize away from metadataInt
            if (metadataInt - metadataPos <= mp3_sampleSize && metadataInt - metadataPos > 0)
            {
                //parse part of the frame.

                byte[] partialmp3Frame = new byte[metadataInt - metadataPos];

                await socketReader.LoadAsync(metadataInt - metadataPos);
                socketReader.ReadBytes(partialmp3Frame);

                metadataPos += metadataInt - metadataPos;

                switch (contentType)
                {
                    case StreamAudioFormat.MP3:
                        {
                            sample = await ParseMP3SampleAsync(partial: true, partialBytes: partialmp3Frame);
                        }
                        break;
                }
            }
            else
            {
                await HandleMetadata();

                request.ReportSampleProgress(50);

                switch (contentType)
                {
                    case StreamAudioFormat.MP3:
                        {
                            //mp3
                            sample = await ParseMP3SampleAsync();
                            //await MediaStreamSample.CreateFromStreamAsync(socket.InputStream, bitRate, new TimeSpan(0, 0, 1));
                        }
                        break;
                }

                metadataPos += sample.Buffer.Length;
            }

            request.Sample = sample;

            request.ReportSampleProgress(100);

            deferral.Complete();
        }

        private async Task HandleMetadata()
        {
            if (metadataPos == metadataInt)
            {
                metadataPos = 0;

                await socketReader.LoadAsync(1);
                uint metaInt = socketReader.ReadByte();

                if (metaInt > 0)
                {
                    uint metaDataInfo = metaInt * 16;

                    await socketReader.LoadAsync((uint)metaDataInfo);

                    var metadata = socketReader.ReadString((uint)metaDataInfo);

                    ParseSongMetadata(metadata);
                }

                byteOffset = 0;
            }
        }

        private void ParseSongMetadata(string metadata)
        {
            string[] semiColonSplit = metadata.Split(';');
            var headers = semiColonSplit.Where(line => line.Contains("=")).Select(line =>
            {
                string header = line.Substring(0, line.IndexOf("="));
                string value = line.Substring(line.IndexOf("=") + 1);

                var pair = new KeyValuePair<string, string>(header.ToUpper(), value.Trim('\''));

                return pair;
            }).ToArray();

            string track = "", artist = "";
            string songInfo = headers.First(x => x.Key == "STREAMTITLE").Value;

            artist = songInfo.Split('-')[0].Trim();
            track = songInfo.Split('-')[1].Trim();

            MediaStreamSource.MusicProperties.Title = track;
            MediaStreamSource.MusicProperties.Artist = artist;

            Debug.WriteLine("Song Info: " + songInfo);

            if (MetadataChanged != null)
            {
                MetadataChanged(this, new ShoutcastMediaSourceStreamMetadataChangedEventArgs()
                {
                    Title = track,
                    Artist = artist
                });
            }
        }

        private async Task<MediaStreamSample> ParseMP3SampleAsync(bool partial = false, byte[] partialBytes = null)
        {
            //http://www.mpgedit.org/mpgedit/mpeg_format/MP3Format.html


            //uint frameHeaderCount = 32;
            //await socketReader.LoadAsync(frameHeaderCount);

            //byte[] frameHeader = new byte[4];
            //socketReader.ReadBytes(frameHeader);
            //BitArray frameHeaderArray = new BitArray(frameHeader);

            //string audioVersionID = frameHeader[1].GetBit( <<  + char.ConvertFromUtf32(frameHeaderArray.Get(19));

            //var db = audioVersionID;

            IBuffer buffer = null;
            MediaStreamSample sample = null;

            if (partial)
            {
                buffer = partialBytes.AsBuffer();
                byteOffset += mp3_sampleSize - (ulong)partialBytes.Length;
            }
            else
            {
                await socketReader.LoadAsync(mp3_sampleSize);
                buffer = socketReader.ReadBuffer(mp3_sampleSize);

                byteOffset += mp3_sampleSize;
            }

            sample = MediaStreamSample.CreateFromBuffer(buffer, timeOffSet);
            sample.Duration = mp3_sampleDuration;
            sample.KeyFrame = true;

            timeOffSet = timeOffSet.Add(mp3_sampleDuration);


            return sample;

            //return null;
        }

        public event EventHandler<ShoutcastMediaSourceStreamMetadataChangedEventArgs> MetadataChanged;
    }
}
