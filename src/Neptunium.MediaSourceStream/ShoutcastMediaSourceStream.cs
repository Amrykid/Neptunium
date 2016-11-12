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

        public bool ShouldGetMetadata { get; private set; }

        StreamSocket socket = null;
        DataWriter socketWriter = null;
        DataReader socketReader = null;
        private volatile bool connected = false;
        private string _relativePath = ";";
        private uint _sampleRate = 44100;
        private ShoutcastServerType serverType = ShoutcastServerType.Shoutcast;

        Uri streamUrl = null;

        uint bitRate = 0;
        uint metadataInt = 0;
        uint metadataPos = 0;

        StreamAudioFormat contentType = StreamAudioFormat.MP3;

        public void Disconnect()
        {
            try
            {
                MediaStreamSource.SampleRequested -= MediaStreamSource_SampleRequested;
                MediaStreamSource.Starting -= MediaStreamSource_Starting;
                MediaStreamSource.Closed -= MediaStreamSource_Closed;
            }
            catch (Exception) { }

            try
            {
                socketWriter.Dispose();
                socketReader.Dispose();
                socket.Dispose();
            }
            catch (Exception) { }

            connected = false;
        }

        //http://stackoverflow.com/questions/6294807/calculate-mpeg-frame-length-ms

        TimeSpan timeOffSet = new TimeSpan();
        private UInt64 byteOffset;

        #region MP3 Framesize and length for Layer II and Layer III - https://code.msdn.microsoft.com/windowsapps/MediaStreamSource-media-dfd55dff/sourcecode?fileId=111712&pathId=208523738

        UInt32 mp3_sampleSize = 1152;
        TimeSpan mp3_sampleDuration = new TimeSpan(0, 0, 0, 0, 70);
        #endregion

        //TODO
        UInt32 aac_sampleSize = 16;
        TimeSpan aac_sampleDuration = new TimeSpan(0, 0, 0, 0, 70);

        public ShoutcastMediaSourceStream(Uri url, ShoutcastServerType stationServerType = ShoutcastServerType.Shoutcast)
        {
            StationInfo = new ShoutcastStationInfo();

            streamUrl = url;

            serverType = stationServerType;

            socket = new StreamSocket();
        }


        public async Task ReconnectAsync()
        {
            if (MediaStreamSource == null) throw new InvalidOperationException();

            metadataPos = 0;

            await HandleConnection(_relativePath);
        }
        public async Task<MediaStreamSource> ConnectAsync(uint sampleRate = 44100, string relativePath = ";", bool getMetadata = true)
        {
            try
            {
                ShouldGetMetadata = getMetadata;

                await HandleConnection(relativePath);
                //AudioEncodingProperties obtainedProperties = await GetEncodingPropertiesAsync();

                switch (contentType)
                {
                    case StreamAudioFormat.MP3:
                        {
                            MediaStreamSource = new Windows.Media.Core.MediaStreamSource(new AudioStreamDescriptor(AudioEncodingProperties.CreateMp3(sampleRate, 2, (uint)bitRate)));
                            //MediaStreamSource.AddStreamDescriptor(new AudioStreamDescriptor(AudioEncodingProperties.CreateMp3(48000, 2, (uint)bitRate)));
                            //MediaStreamSource.AddStreamDescriptor(new AudioStreamDescriptor(AudioEncodingProperties.CreateMp3(32000, 2, (uint)bitRate)));
                            //MediaStreamSource.AddStreamDescriptor(new AudioStreamDescriptor(AudioEncodingProperties.CreateMp3(24000, 2, (uint)bitRate)));
                            //MediaStreamSource.AddStreamDescriptor(new AudioStreamDescriptor(AudioEncodingProperties.CreateMp3(22050, 2, (uint)bitRate)));
                        }
                        break;
                    case StreamAudioFormat.AAC:
                        {
                            MediaStreamSource = new MediaStreamSource(new AudioStreamDescriptor(AudioEncodingProperties.CreateAac(sampleRate, 2, (uint)bitRate)));
                        }
                        break;
                }

                MediaStreamSource.SampleRequested += MediaStreamSource_SampleRequested;
                MediaStreamSource.CanSeek = false;
                MediaStreamSource.Starting += MediaStreamSource_Starting;
                MediaStreamSource.Closed += MediaStreamSource_Closed;

                connected = true;
                _relativePath = relativePath;
                _sampleRate = sampleRate;

                return MediaStreamSource;
            }
            catch (Exception ex)
            {
                throw new Exception("Connection error.", ex);
            }
        }

        private async Task<AudioEncodingProperties> GetEncodingPropertiesAsync()
        {
            //grab the first frame and strip it for information

            AudioEncodingProperties obtainedProperties = null;
            IBuffer buffer = null;
            int sampleRate = 0;

            switch (contentType)
            {
                case StreamAudioFormat.MP3:
                    {
                        await socketReader.LoadAsync(mp3_sampleSize);
                        buffer = socketReader.ReadBuffer(mp3_sampleSize);
                        byteOffset += mp3_sampleSize;

                        byte[] bytesHeader = buffer.ToArray(0, 5); //first four bytes

                        #region Modified version of http://sahanganepola.blogspot.com/2010/07/c-class-to-get-mp3-header-details.html
                        //I don't like copying code without understanding it but this is a case where i dont fully understand everything going on.
                        //I need to read up on bitmasking and such.

                        //EDIT FROM FUTURE: Found this -> http://www.cprogramming.com/tutorial/bitwise_operators.html

                        var bithdr = (ulong)(((bytesHeader[0] & 255) << 24) | ((bytesHeader[1] & 255) << 16) | ((bytesHeader[2] & 255) << 8) | ((bytesHeader[3] & 255)));

                        var bitrateIndex = (int)((bithdr >> 12) & 15);
                        var versionIndex = (int)((bithdr >> 19) & 3);

                        var frequencyIndex = (int)((bithdr >> 10) & 3); //sampleRate

                        int[,] frequencyTable =    {
                             {32000, 16000,  8000}, // MPEG 2.5
                             {    0,     0,     0}, // reserved
                             {22050, 24000, 16000}, // MPEG 2
                             {44100, 48000, 32000}  // MPEG 1
                         };
                        #endregion

                        sampleRate = frequencyTable[versionIndex, frequencyIndex];

                        obtainedProperties = AudioEncodingProperties.CreateMp3((uint)sampleRate, 2, bitRate);
                        break;
                    }
                case StreamAudioFormat.AAC:
                    {
                        obtainedProperties = AudioEncodingProperties.CreateAac(0, 2, 0);
                        throw new Exception();
                    }
                default:
                    break;
            }

            metadataPos += buffer.Length; //very important or it will throw everything off!

            return obtainedProperties;
        }

        private void MediaStreamSource_Closed(MediaStreamSource sender, MediaStreamSourceClosedEventArgs args)
        {
            MediaStreamSource.Starting -= MediaStreamSource_Starting;
            MediaStreamSource.Closed -= MediaStreamSource_Closed;
            MediaStreamSource.SampleRequested -= MediaStreamSource_SampleRequested;

            try
            {
                Disconnect();
            }
            catch (Exception) { }
        }

        private void MediaStreamSource_Starting(MediaStreamSource sender, MediaStreamSourceStartingEventArgs args)
        {
            //args.Request.SetActualStartPosition(timeOffSet);
            //args.Request.
        }

        private async Task HandleConnection(string relativePath)
        {
            //http://www.smackfu.com/stuff/programming/shoutcast.html
            try
            {
                await socket.ConnectAsync(new Windows.Networking.HostName(streamUrl.Host), streamUrl.Port.ToString());

                socketWriter = new DataWriter(socket.OutputStream);
                socketReader = new DataReader(socket.InputStream);

                //todo figure out how to resolve http requests better to get rid of this hack.
                String httpPath = "";

                if (streamUrl.Host.Contains("radionomy.com") || serverType == ShoutcastServerType.Radionomy)
                {
                    httpPath = streamUrl.LocalPath;
                    serverType = ShoutcastServerType.Radionomy;
                }
                else
                {
                    httpPath = "/" + relativePath;
                }

                socketWriter.WriteString("GET " + httpPath + " HTTP/1.1" + Environment.NewLine);

                if (ShouldGetMetadata)
                    socketWriter.WriteString("Icy-MetaData: 1" + Environment.NewLine);

                socketWriter.WriteString("Connection: Keep-Alive" + Environment.NewLine);
                socketWriter.WriteString("User-Agent: Neptunium (http://github.com/Amrykid)" + Environment.NewLine);
                socketWriter.WriteString(Environment.NewLine);
                await socketWriter.StoreAsync();
                await socketWriter.FlushAsync();

                string response = string.Empty;
                while (!response.EndsWith(Environment.NewLine + Environment.NewLine))
                {
                    await socketReader.LoadAsync(1);
                    response += socketReader.ReadString(1);
                }

                if (response.StartsWith("HTTP/1.0 302"))
                {
                    socketReader.Dispose();
                    socketWriter.Dispose();
                    socket.Dispose();

                    var parsedResponse = ParseHttpResponseToKeyPairArray(response.Split(new string[] { "\r\n" }, StringSplitOptions.None).Skip(1).ToArray());

                    socket = new StreamSocket();
                    streamUrl = new Uri(parsedResponse.First(x => x.Key.ToLower() == "location").Value);

                    await HandleConnection(relativePath);

                    return;
                }

                ParseResponse(response);
            }
            catch (Exception ex)
            {
                connected = false;

                if (MediaStreamSource != null)
                    MediaStreamSource.NotifyError(MediaStreamSourceErrorStatus.FailedToConnectToServer);
                else
                    throw new Exception("Inner exception", ex);
            }
        }

        private void ParseResponse(string response)
        {
            string[] responseSplitByLine = response.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            KeyValuePair<string, string>[] headers = ParseHttpResponseToKeyPairArray(responseSplitByLine);

            StationInfo.StationName = headers.First(x => x.Key == "ICY-NAME").Value;
            StationInfo.StationGenre = headers.First(x => x.Key == "ICY-GENRE").Value;

            bitRate = uint.Parse(headers.FirstOrDefault(x => x.Key == "ICY-BR").Value);
            metadataInt = uint.Parse(headers.First(x => x.Key == "ICY-METAINT").Value);
            contentType = headers.First(x => x.Key == "CONTENT-TYPE").Value.ToUpper().Trim() == "AUDIO/MPEG" ? StreamAudioFormat.MP3 : StreamAudioFormat.AAC;
        }

        private static KeyValuePair<string, string>[] ParseHttpResponseToKeyPairArray(string[] responseSplitByLine)
        {
            return responseSplitByLine.Where(line => line.Contains(":")).Select(line =>
            {
                string header = line.Substring(0, line.IndexOf(":"));
                string value = line.Substring(line.IndexOf(":") + 1);

                var pair = new KeyValuePair<string, string>(header.ToUpper(), value);

                return pair;
            }).ToArray();
        }

        private async void MediaStreamSource_SampleRequested(Windows.Media.Core.MediaStreamSource sender, Windows.Media.Core.MediaStreamSourceSampleRequestedEventArgs args)
        {
            var request = args.Request;
            var deferral = request.GetDeferral();

            if (!connected)
            {
                await ReconnectAsync();
            }

            try
            {
                MediaStreamSample sample = null;
                uint sampleLength = 0;

                request.ReportSampleProgress(25);

                //if metadataPos is less than mp3_sampleSize away from metadataInt
                if (metadataInt - metadataPos <= (contentType == StreamAudioFormat.MP3 ? mp3_sampleSize : aac_sampleSize) && metadataInt - metadataPos > 0)
                {
                    //parse part of the frame.

                    byte[] partialFrame = new byte[metadataInt - metadataPos];

                    var read = await socketReader.LoadAsync(metadataInt - metadataPos);

                    if (read == 0)
                    {
                        Disconnect();
                        MediaStreamSource.NotifyError(MediaStreamSourceErrorStatus.ConnectionToServerLost);
                        return;
                    }

                    socketReader.ReadBytes(partialFrame);

                    metadataPos += metadataInt - metadataPos;

                    switch (contentType)
                    {
                        case StreamAudioFormat.MP3:
                            {
                                Tuple<MediaStreamSample, uint> result = await ParseMP3SampleAsync(partial: true, partialBytes: partialFrame);
                                sample = result.Item1;
                                sampleLength = result.Item2;
                            }
                            break;
                        case StreamAudioFormat.AAC:
                            {
                                Tuple<MediaStreamSample, uint> result = await ParseAACSampleAsync(partial: true, partialBytes: partialFrame);
                                sample = result.Item1;
                                sampleLength = result.Item2;
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
                                Tuple<MediaStreamSample, uint> result = await ParseMP3SampleAsync();
                                sample = result.Item1;
                                sampleLength = result.Item2;
                                //await MediaStreamSample.CreateFromStreamAsync(socket.InputStream, bitRate, new TimeSpan(0, 0, 1));
                            }
                            break;
                        case StreamAudioFormat.AAC:
                            {
                                Tuple<MediaStreamSample, uint> result = await ParseAACSampleAsync();
                                sample = result.Item1;
                                sampleLength = result.Item2;
                            }
                            break;
                    }

                    try
                    {
                        if (sample == null || sampleLength == 0) //bug: on RELEASE builds, sample.Buffer causes the app to die due to a possible .NET Native bug
                        {
                            MediaStreamSource.NotifyError(MediaStreamSourceErrorStatus.DecodeError);
                            deferral.Complete();
                            return;
                        }
                        else
                            metadataPos += sampleLength;
                    }
                    catch (Exception) { }
                }

                if (sample != null)
                    request.Sample = sample;

                request.ReportSampleProgress(100);
            }
            catch (Exception)
            {
                MediaStreamSource.NotifyError(MediaStreamSourceErrorStatus.Other);
            }

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

                    if (ShouldGetMetadata)
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

                var pair = new KeyValuePair<string, string>(header.ToUpper(), value.Trim('\'').Trim());

                return pair;
            }).ToArray();

            string track = "", artist = "";
            string songInfo = headers.First(x => x.Key == "STREAMTITLE").Value;

            if (songInfo.Split('-').Count() >= 2)
            {
                artist = songInfo.Split(new string[] { " - " }, StringSplitOptions.None)[0].Trim();
                track = songInfo.Split(new string[] { " - " }, StringSplitOptions.None)[1].Trim();

                MediaStreamSource.MusicProperties.Title = track;
                MediaStreamSource.MusicProperties.Artist = artist;
            }
            else
            {
                track = songInfo.Trim();
                artist = "Unknown";
            }

            if (MetadataChanged != null)
            {
                MetadataChanged(this, new ShoutcastMediaSourceStreamMetadataChangedEventArgs()
                {
                    Title = track,
                    Artist = artist
                });
            }
        }

        private async Task<Tuple<MediaStreamSample, uint>> ParseMP3SampleAsync(bool partial = false, byte[] partialBytes = null)
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
            uint sampleLength = 0;

            if (partial)
            {
                buffer = partialBytes.AsBuffer();
                sampleLength = mp3_sampleSize - (uint)partialBytes.Length;
                byteOffset += sampleLength;
            }
            else
            {
                var read = await socketReader.LoadAsync(mp3_sampleSize);

                if (read == 0)
                {
                    Disconnect();
                    MediaStreamSource.NotifyError(MediaStreamSourceErrorStatus.ConnectionToServerLost);
                    return new Tuple<MediaStreamSample, uint>(null, 0);
                }
                else if (read < mp3_sampleSize)
                {
                    buffer = socketReader.ReadBuffer(read);

                    byteOffset += mp3_sampleSize;
                }
                else
                {
                    buffer = socketReader.ReadBuffer(mp3_sampleSize);

                    byteOffset += mp3_sampleSize;
                }

                sampleLength = mp3_sampleSize;
            }

            sample = MediaStreamSample.CreateFromBuffer(buffer, timeOffSet);
            sample.Duration = mp3_sampleDuration;
            sample.KeyFrame = true;

            timeOffSet = timeOffSet.Add(mp3_sampleDuration);


            return new Tuple<MediaStreamSample,uint>(sample, sampleLength);

            //return null;
        }

        private async Task<Tuple<MediaStreamSample, uint>> ParseAACSampleAsync(bool partial = false, byte[] partialBytes = null)
        {

            IBuffer buffer = null;
            MediaStreamSample sample = null;
            uint sampleLength = 0;

            if (partial)
            {
                buffer = partialBytes.AsBuffer();
                sampleLength = aac_sampleSize - (uint)partialBytes.Length;
                byteOffset += sampleLength;
            }
            else
            {
                await socketReader.LoadAsync(aac_sampleSize);
                buffer = socketReader.ReadBuffer(aac_sampleSize);

                byteOffset += aac_sampleSize;
                sampleLength = aac_sampleSize;
            }

            sample = MediaStreamSample.CreateFromBuffer(buffer, timeOffSet);
            sample.Duration = aac_sampleDuration;
            sample.KeyFrame = true;

            timeOffSet = timeOffSet.Add(aac_sampleDuration);


            return new Tuple<MediaStreamSample, uint>(sample, sampleLength);
        }

        public event EventHandler<ShoutcastMediaSourceStreamMetadataChangedEventArgs> MetadataChanged;
    }
}
