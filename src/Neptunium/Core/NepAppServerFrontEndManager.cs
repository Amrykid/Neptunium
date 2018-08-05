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
using Windows.System.Threading;
using static Neptunium.NepApp;

namespace Neptunium.Core
{
    public class NepAppServerFrontEndManager : INotifyPropertyChanged, INepAppFunctionManager
    {
        public const int ServerPortNumber = 8806;

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
        public EndPoint LocalEndPoint { get; private set; }

        public event EventHandler<NepAppServerFrontEndManagerDataReceivedEventArgs> DataReceived;


        private Task serverRunningTask = null;
        private CancellationTokenSource serverTaskCancelTokenSrc = null;

        public async Task InitializeAsync()
        {
            if (IsInitialized) return;

            serverTaskCancelTokenSrc = new CancellationTokenSource();
            serverRunningTask = new Task(HandleServer, serverTaskCancelTokenSrc.Token);

            serverRunningTask.Start();

            IsInitialized = true;
        }

        private async void HandleServer()
        {
            List<Socket> connections = new List<Socket>();

            TcpListener listener = new TcpListener(IPAddress.Any, ServerPortNumber);
            listener.Start();

            LocalEndPoint = listener.LocalEndpoint;
            RaisePropertyChanged(nameof(LocalEndPoint));

            while (!serverTaskCancelTokenSrc.IsCancellationRequested)
            {
                if (listener.Pending())
                {
                    connections.Add(await listener.AcceptSocketAsync());
                }

                List<Socket> sockets = connections.ToList();
                Socket.Select(sockets, null, null, 20); //figure out which ones have data available to read.

                foreach(var socket in sockets)
                {
                    byte[] data = new byte[2048];
                    socket.Receive(data);

                    var dataString = Encoding.Unicode.GetString(data);

                    await ThreadPool.RunAsync(new WorkItemHandler(x =>
                    {
                        DataReceived?.Invoke(this, new NepAppServerFrontEndManagerDataReceivedEventArgs(dataString));
                    }));
                }
            }

            //clean up
            listener.Stop();

            foreach(Socket client in connections)
            {
                client.Dispose();
            }

            connections.Clear();

            LocalEndPoint = null;
            RaisePropertyChanged(nameof(LocalEndPoint));
        }
    }
}
