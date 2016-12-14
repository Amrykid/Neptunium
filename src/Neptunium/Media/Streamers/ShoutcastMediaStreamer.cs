﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neptunium.Data;
using Windows.Media.Core;
using System.Reactive.Subjects;
using Neptunium.MediaSourceStream;

namespace Neptunium.Media.Streamers
{
    public class ShoutcastMediaStreamer : BasicMediaStreamer
    {
        private MediaSourceStream.ShoutcastMediaSourceStream shoutcastStream = null;

        internal ShoutcastMediaStreamer() : base()
        {
        }

        public override async Task ConnectAsync(StationModel station, StationModelStream stream, IEnumerable<KeyValuePair<string, object>> props = null)
        {
            shoutcastStream = new MediaSourceStream.ShoutcastMediaSourceStream(new Uri(stream.Url), ConvertServerTypeToMediaServerType(stream.ServerType));

            shoutcastStream.MetadataChanged += ShoutcastStream_MetadataChanged;

            try
            {
                await shoutcastStream.ConnectAsync();

                Source = MediaSource.CreateFromMediaStreamSource(shoutcastStream.MediaStreamSource);
                Source.StateChanged += Source_StateChanged;
                Player.Source = Source;

                CurrentStation = station;
                CurrentStream = stream;

                IsConnected = true;
            }
            catch (Exception ex)
            {
                shoutcastStream.MetadataChanged -= ShoutcastStream_MetadataChanged;

                ((Subject<Exception>)ErrorOccurred).OnNext(ex);

                IsConnected = false;
            }
        }

        private void Source_StateChanged(MediaSource sender, MediaSourceStateChangedEventArgs args)
        {
            IsConnected = args.NewState == MediaSourceState.Opened;
        }

        private void ShoutcastStream_MetadataChanged(object sender, ShoutcastMediaSourceStreamMetadataChangedEventArgs e)
        {
            CurrentTrack = e.Title;
            CurrentArtist = e.Artist;

            metadataSubject.OnNext(new BasicSongInfo() { Track = CurrentTrack, Artist = CurrentArtist });
        }

        public override Task DisconnectAsync()
        {
            if (shoutcastStream != null)
            {
                shoutcastStream.MetadataChanged -= ShoutcastStream_MetadataChanged;
                shoutcastStream.Disconnect();
            }

            if (Source != null)
            {
                Source.StateChanged -= Source_StateChanged;
            }

            return Task.CompletedTask;
        }

        private static ShoutcastServerType ConvertServerTypeToMediaServerType(StationModelStreamServerType currentStationServerType)
        {
            switch (currentStationServerType)
            {
                case StationModelStreamServerType.Shoutcast:
                    return ShoutcastServerType.Shoutcast;
                case StationModelStreamServerType.Radionomy:
                    return ShoutcastServerType.Radionomy;
                default:
                    return ShoutcastServerType.Shoutcast;
            }
        }
    }
}