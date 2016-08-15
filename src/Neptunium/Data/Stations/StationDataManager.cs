﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Neptunium.Data
{
    public static class StationDataManager
    {
        public static bool IsInitialized { get; private set; }

        public static IEnumerable<StationModel> Stations { get; private set; }

        private static TaskCompletionSource<object> initializeTask = null;
        public static async Task InitializeAsync()
        {
            if (IsInitialized) return;

            if (initializeTask != null)
            {
                await initializeTask.Task; //prevent multiple tasks from trying to initialize this.
            }
            else
            {
                initializeTask = new TaskCompletionSource<object>();

                var file = await Windows.ApplicationModel.Package.Current.InstalledLocation.GetFileAsync(@"Data\Stations\Stations.xml");
                var reader = await file.OpenReadAsync();
                XDocument xmlDoc = XDocument.Load(reader.AsStream());

                List<StationModel> stationList = new List<StationModel>();

                foreach (var stationElement in xmlDoc.Element("Stations").Elements("Station"))
                {
                    var station = new StationModel();

                    station.Name = stationElement.Element("Name").Value;
                    station.Description = stationElement.Element("Description").Value;
                    station.Logo = stationElement.Element("Logo").Value;
                    station.Site = stationElement.Element("Site").Value;
                    station.Genres = stationElement.Element("Genres").Value.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                    station.Streams = stationElement.Element("Streams").Elements("Stream").Select<XElement, StationModelStream>(x =>
                    {
                        var stream = new StationModelStream();

                        stream.ContentType = x.Attribute("ContentType")?.Value;
                        stream.Bitrate = int.Parse(x.Attribute("Bitrate")?.Value);
                        stream.SampleRate = uint.Parse(x.Attribute("SampleRate")?.Value);
                        stream.Url = x.Value;
                        stream.RelativePath = x.Attribute("RelativePath")?.Value;

                        try
                        {
                            if (x.Attribute("ServerType") != null)
                            {
                                stream.ServerType = (StationModelStreamServerType)Enum.Parse(typeof(StationModelStreamServerType), x.Attribute("ServerType").Value);

                                stream.HistoryPath = x.Attribute("HistoryPath")?.Value;
                            }
                        }
                        catch (Exception) { }

                        return stream;
                    }).ToArray();

                    try
                    {
                        if (stationElement.Element("StationMessages") == null)
                        {
                            station.StationMessages = new string[] { };
                        }
                        else
                        {
                            var stationMsgElement = stationElement.Element("StationMessages");

                            var messages = stationMsgElement.Elements("Message").Select(x => x.Value.ToString()).ToArray();
                            station.StationMessages = messages;
                        }
                    }
                    catch (Exception) { }

                    stationList.Add(station);
                }

                Stations = stationList.ToArray();

                IsInitialized = true;

                initializeTask.SetResult(true);

                stationList = null;

                xmlDoc = null;

                reader.Dispose();
            }
        }

        internal static Task DeinitializeAsync()
        {
            if (!IsInitialized) return Task.CompletedTask;

            Stations = null;

            IsInitialized = false;

            return Task.CompletedTask;
        }
    }
}
