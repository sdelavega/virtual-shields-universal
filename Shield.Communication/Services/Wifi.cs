/*
    Copyright(c) Microsoft Open Technologies, Inc. All rights reserved.

    The MIT License(MIT)

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files(the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions :

    The above copyright notice and this permission notice shall be included in
    all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
    THE SOFTWARE.
*/
namespace Shield.Communication.Services
{
    using System;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Windows.ApplicationModel.Core;
    using Windows.Devices.Bluetooth.Rfcomm;
    using Windows.Devices.Enumeration;
    using Windows.Networking;
    using Windows.Networking.Proximity;
    using Windows.Networking.Sockets;

    public class Wifi : ServiceBase
    {
        private readonly int broadcastPort;

        private readonly DatagramSocket listener = new DatagramSocket();

        private bool beaconIsOn;

        public Wifi(int broadcastPort)
        {
            this.broadcastPort = broadcastPort;
            this.isPollingToSend = true;
        }

        public override async Task<Connections> GetConnections()
        {
            var connections = new Connections();
            foreach (var client in this.clients)
            {
                if (client.Value != null && client.Value.Source != null)
                {
                    var peer = client.Value.Source as RemotePeer;
                    if (peer != null)
                    {
                        if (DateTime.Now.Subtract(peer.Pinged).TotalSeconds > 15)
                        {
                            continue;
                        }
                    }

                    connections.Add(client.Value);
                }
            }

            await Task.FromResult(false);
            return connections;
        }

        public override async void ListenForBeacons()
        {
            if (!this.beaconIsOn)
            {
                this.beaconIsOn = true;
                this.listener.MessageReceived += this.ListenerOnMessageReceived;
                try
                {
                    await this.listener.BindServiceNameAsync(this.broadcastPort.ToString());
                }
                catch (Exception)
                {
                    // ignore dropped listen sockets
                }
            }
        }

        private async void ListenerOnMessageReceived(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args)
        {
            object outObj;
            if (CoreApplication.Properties.TryGetValue("remotePeer", out outObj))
            {
                this.EchoMessage((RemotePeer)outObj, args);
                return;
            }

            // We do not have an output stream yet so create one.
            try
            {
                var outputStream = await this.listener.GetOutputStreamAsync(args.RemoteAddress, args.RemotePort);

                // It might happen that the OnMessage was invoked more than once before the GetOutputStreamAsync completed.
                // In this case we will end up with multiple streams - make sure we have just one of it.
                RemotePeer peer;
                lock (this)
                {
                    if (CoreApplication.Properties.TryGetValue("remotePeer", out outObj))
                    {
                        peer = (RemotePeer)outObj;
                    }
                    else
                    {
                        peer = new RemotePeer(outputStream, args.RemoteAddress, args.RemotePort);
                        CoreApplication.Properties.Add("remotePeer", peer);
                    }
                }

                this.EchoMessage(peer, args);
            }
            catch (Exception exception)
            {
                // If this is an unknown status it means that the error is fatal and retry will likely fail.
                if (SocketError.GetStatus(exception.HResult) == SocketErrorStatus.Unknown)
                {
                    throw;
                }

                // NotifyUserFromAsyncThread("Connect failed with error: " + exception.Message, NotifyType.ErrorMessage);
            }
        }

        private void EchoMessage(RemotePeer peer, DatagramSocketMessageReceivedEventArgs eventArguments)
        {
            if (!peer.IsMatching(eventArguments.RemoteAddress, eventArguments.RemotePort))
            {
                // In the sample we are communicating with just one peer. To communicate with multiple peers application
                // should cache output streams (i.e. by using a hash map), because creating an output stream for each
                // received datagram is costly. Keep in mind though, that every cache requires logic to remove old
                // or unused elements; otherwise cache turns into a memory leaking structure.
                // NotifyUserFromAsyncThread(String.Format("Got datagram from {0}:{1}, but already 'connected' to {3}", eventArguments.RemoteAddress, eventArguments.RemotePort, peer), NotifyType.ErrorMessage);
            }

            try
            {
                var reader = eventArguments.GetDataReader();
                var size = reader.UnconsumedBufferLength;
                var sb = new StringBuilder();
                while (reader.UnconsumedBufferLength > 0)
                {
                    var b = reader.ReadByte();
                    sb.Append((char)b);
                }

                var msg = sb.ToString();

                if (msg.StartsWith("VS:"))
                {
                    msg = msg.Substring(3);
                    var colon = msg.IndexOf(':');
                    var typename = msg.Substring(0, colon);
                    var equals = msg.IndexOf('=');
                    var ip = msg.Substring(colon + 1, equals - colon - 1);
                    var name = msg.Substring(equals + 1);
                    var colon2 = name.IndexOf(':');

                    if (colon2 > -1)
                    {
                        name = name.Substring(0, colon2);
                    }

                    if (string.IsNullOrWhiteSpace(name))
                    {
                        name = $"({typename}):{ip}";
                    }

                    var port = peer.Port;
                    var portColon = ip.IndexOf(':');
                    if (portColon > -1)
                    {
                        port = ip.Substring(portColon + 1);
                        ip = ip.Substring(0, portColon);
                    }

                    peer.IP = ip;
                    peer.Name = name;
                    peer.Message = msg;
                    peer.Pinged = DateTime.Now;
                    peer.OriginalPort = peer.Port;
                    peer.Port = port;

                    var connection = new Connection(name, peer);
                    if (!this.clients.ContainsKey(ip))
                    {
                        this.clients[ip] = connection;
                    }
                }
            }
            catch (Exception exception)
            {
                // If this is an unknown status it means that the error is fatal and retry will likely fail.
                if (SocketError.GetStatus(exception.HResult) == SocketErrorStatus.Unknown)
                {
                    throw;
                }
            }
        }

        private async Task<RemotePeer> GetHostNameAndService(object source)
        {
            var result = source as RemotePeer;

            if (result != null)
            {
                return result;
            }

            var peer = source as PeerInformation;
            if (peer != null)
            {
                return new RemotePeer(null, peer.HostName, "1");
            }

            var deviceInfo = source as DeviceInformation;
            if (deviceInfo != null)
            {
                var service = await RfcommDeviceService.FromIdAsync(deviceInfo.Id);
                return new RemotePeer(null, service.ConnectionHostName, service.ConnectionServiceName);
            }

            var ip = source as string;
            if (ip != null)
            {
                var iponly = ip;
                var port = this.broadcastPort.ToString();
                var colon = ip.IndexOf(':');
                if (colon > -1)
                {
                    iponly = ip.Substring(0, colon);
                    port = ip.Substring(colon + 1);
                }

                return new RemotePeer(null, new HostName(iponly), port);
            }

            return null;
        }

        public override async Task<bool> Connect(Connection newConnection)
        {
            // purposely connect to a destination
            var peer = await this.GetHostNameAndService(newConnection.Source);

            if (peer?.HostName == null)
            {
                return false;
            }

            var result = await this.Connect(peer.HostName, peer.Port);
            await base.Connect(newConnection);

            return result;
        }

        private async Task<bool> Connect(HostName deviceHostName, string remoteServiceName)
        {
            if (!this.isListening)
            {
                if (this.socket == null)
                {
                    this.socket = new StreamSocket();
                }

                if (this.socket != null)
                {
                    try
                    {
                        var cts = new CancellationTokenSource();
                        cts.CancelAfter(10000);
                        await this.socket.ConnectAsync(deviceHostName, remoteServiceName);
                        return this.InstrumentSocket(this.socket);
                    }
                    catch (Exception e)
                    {
                        this.OnThreadedException(e);
                    }
                }

                return false;
            }

            return true;
        }
    }
}