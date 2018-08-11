using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Connectivity;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.System.Threading;
using static Neptunium.NepApp;

namespace Neptunium.Core
{
    public class NepAppServerFrontEndManager : INotifyPropertyChanged, INepAppFunctionManager
    {
        public const int ServerPortNumber = 8806; //netsh advfirewall firewall add rule name="Open port 8806" dir=in action=allow protocol=TCP localport=8806
        public const int BroadcastPortNumber = 8807; //netsh advfirewall firewall add rule name="Open port 8807" dir=out action=allow protocol=UDP localport=8807

        /// <summary>
        /// From INotifyPropertyChanged
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (string.IsNullOrWhiteSpace(propertyName)) throw new ArgumentNullException("propertyName");

            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public class NepAppServerFrontEndManagerDataReceivedEventArgs : EventArgs
        {
            public string Data { get; private set; }
            internal NepAppServerFrontEndManagerDataReceivedEventArgs(string data)
            {
                Data = data;
            }
        }

        public bool IsInitialized { get; private set; }
        public IEnumerable<IPAddress> LocalEndPoints { get; private set; }

        public event EventHandler<NepAppServerFrontEndManagerDataReceivedEventArgs> DataReceived;

        private List<Tuple<StreamSocket, DataReader, DataWriter>> connections = new List<Tuple<StreamSocket, DataReader, DataWriter>>();
        private StreamSocketListener listener = null; //TCP
        private DatagramSocket serverBroadcaster = null; //UDP

        public async Task InitializeAsync()
        {
            if (IsInitialized) return;

            NepApp.SongManager.PreSongChanged += SongManager_PreSongChanged;

            //set up the tcp listener to listen for connections
            listener = new StreamSocketListener();
            listener.ConnectionReceived += Listener_ConnectionReceived;
            await listener.BindServiceNameAsync(ServerPortNumber.ToString());

            //set up the broadcaster that will broadcast the server's prescence over the network.
            serverBroadcaster = new DatagramSocket();
            await serverBroadcaster.BindServiceNameAsync(BroadcastPortNumber.ToString());

            LocalEndPoints = NepApp.Network.GetLocalIPAddresses();
            RaisePropertyChanged(nameof(LocalEndPoints));

            var broadcastAddr = NepApp.Network.GetBroadastAddress(LocalEndPoints.Last());
            var broadcastDataWriter = new DataWriter(await serverBroadcaster.GetOutputStreamAsync(new HostName(broadcastAddr.ToString()), BroadcastPortNumber.ToString()));

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Task.Run(async () =>
            {
                //todo make a way to stop.
                while (true)
                {
                    broadcastDataWriter.WriteString("NEP" + NepAppServerClient.MessageTypeSeperator + LocalEndPoints.First().ToString());
                    await broadcastDataWriter.StoreAsync();

                    await Task.Delay(30000); //wait 30 seconds
                }
            });
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

            IsInitialized = true;
        }

        private async void SongManager_PreSongChanged(object sender, Neptunium.Media.Songs.NepAppSongChangedEventArgs e)
        {
            List<Tuple<StreamSocket, DataReader, DataWriter>> connectionsToRemove = new List<Tuple<StreamSocket, DataReader, DataWriter>>();

            //todo, make an object for this
            foreach (Tuple<StreamSocket, DataReader, DataWriter> tup in connections)
            {
                try
                {
                    tup.Item3.WriteString("MEDIA" + NepAppServerClient.MessageTypeSeperator + e.Metadata.ToString());
                    await tup.Item3.StoreAsync();
                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode == System.Net.Sockets.SocketError.ConnectionReset
                        || ex.SocketErrorCode == System.Net.Sockets.SocketError.ConnectionAborted)
                    {
                        connectionsToRemove.Add(tup);
                    }
                }
            }

            foreach (Tuple<StreamSocket, DataReader, DataWriter> tup in connectionsToRemove)
            {
                try
                {
                    tup.Item3.Dispose();
                    tup.Item2.Dispose();
                    tup.Item1.Dispose();
                }
                catch (Exception) { }

                connections.Remove(tup);
            }

            connectionsToRemove.Clear();
        }

        private async void Listener_ConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            DataReader reader = new DataReader(args.Socket.InputStream);
            DataWriter writer = new DataWriter(args.Socket.OutputStream);

            reader.InputStreamOptions = InputStreamOptions.Partial;

            var socketTup = new Tuple<StreamSocket, DataReader, DataWriter>(args.Socket, reader, writer);
            connections.Add(socketTup);

            while (true)
            {
                uint available = await reader.LoadAsync(50);

                if (available > 0)
                {
                    var data = reader.ReadString(available);
                    data = data.Trim();

                    DataReceived?.Invoke(this, new NepAppServerFrontEndManagerDataReceivedEventArgs(data));
                }
                else
                {
                    lock (connections)
                    {
                        connections.Remove(socketTup);
                    }

                    break;
                }
            }
        }

        private void CleanUp()
        {
            if (!IsInitialized) return;

            //clean up
            listener.Dispose();

            LocalEndPoints = null;
            RaisePropertyChanged(nameof(LocalEndPoints));

            NepApp.SongManager.PreSongChanged -= SongManager_PreSongChanged;

            IsInitialized = false;
        }

        public class NepAppServerClient : IDisposable, INotifyPropertyChanged
        {
            public const char MessageTypeSeperator = '|';

            private StreamSocket tcpClient = null;
            private DataWriter dataWriter = null;
            private DataReader dataReader = null;
            private CancellationTokenSource readerTaskCancellationSource = null;

            public bool IsConnected { get; private set; }

            public NepAppServerClient()
            {
                tcpClient = new StreamSocket();
            }

            public async Task TryConnectAsync(IPAddress address)
            {
                await tcpClient.ConnectAsync(new Windows.Networking.HostName(address.ToString()), ServerPortNumber.ToString());
                dataWriter = new DataWriter(tcpClient.OutputStream);
                dataReader = new DataReader(tcpClient.InputStream);
                readerTaskCancellationSource = new CancellationTokenSource();

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                Task.Run(action: ReadDataFromServer, cancellationToken: readerTaskCancellationSource.Token);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

                IsConnected = true;
                RaisePropertyChanged(nameof(IsConnected));
            }

            private async void ReadDataFromServer()
            {
                dataReader.InputStreamOptions = InputStreamOptions.Partial;
                while (!readerTaskCancellationSource.IsCancellationRequested)
                {
                    await dataReader.LoadAsync(100);
                    var data = dataReader.ReadString(100);

                    var x = data;
                }
            }

            public void AskServerToStreamStation(Stations.StationItem station)
            {
                if (IsConnected)
                {
                    dataWriter.WriteString("PLAY" + MessageTypeSeperator + station.Name);
                }
            }

            public void AskServerToStop()
            {
                if (IsConnected)
                {
                    dataWriter.WriteString("STOP" + MessageTypeSeperator);
                }
            }


            #region IDisposable Support
            private bool disposedValue = false; // To detect redundant calls

            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        // TODO: dispose managed state (managed objects).
                        dataWriter.Dispose();
                        dataReader.Dispose();
                        tcpClient.Dispose();

                        IsConnected = false;
                    }

                    // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                    // TODO: set large fields to null.

                    disposedValue = true;
                }
            }

            // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
            // ~NepAppServerClient() {
            //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            //   Dispose(false);
            // }

            // This code added to correctly implement the disposable pattern.
            public void Dispose()
            {
                // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
                Dispose(true);
                // TODO: uncomment the following line if the finalizer is overridden above.
                // GC.SuppressFinalize(this);
            }
            #endregion


            /// <summary>
            /// From INotifyPropertyChanged
            /// </summary>
            public event PropertyChangedEventHandler PropertyChanged;

            private void RaisePropertyChanged([CallerMemberName] string propertyName = "")
            {
                if (string.IsNullOrWhiteSpace(propertyName)) throw new ArgumentNullException("propertyName");

                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public class NepAppServerDiscoverer : IDisposable, INotifyPropertyChanged
        {
            private DatagramSocket udpSocket = new DatagramSocket();

            //todo an event for alerting users of a discovered server. methods for starting and stopping this object.

            #region IDisposable Support
            private bool disposedValue = false; // To detect redundant calls

            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        // TODO: dispose managed state (managed objects).
                    }

                    // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                    // TODO: set large fields to null.

                    disposedValue = true;
                }
            }

            // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
            // ~NepAppServerDiscoverer() {
            //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            //   Dispose(false);
            // }

            // This code added to correctly implement the disposable pattern.
            public void Dispose()
            {
                // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
                Dispose(true);
                // TODO: uncomment the following line if the finalizer is overridden above.
                // GC.SuppressFinalize(this);
            }
            #endregion
            #region INotifyPropertyChanged Support

            public event PropertyChangedEventHandler PropertyChanged;

            private void RaisePropertyChanged([CallerMemberName] string propertyName = "")
            {
                if (string.IsNullOrWhiteSpace(propertyName)) throw new ArgumentNullException("propertyName");

                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
            #endregion
        }
    }
}
