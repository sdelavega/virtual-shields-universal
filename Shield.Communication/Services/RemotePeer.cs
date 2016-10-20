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

    using Windows.Networking;
    using Windows.Storage.Streams;

    public class RemotePeer
    {
        public RemotePeer(IOutputStream outputStream, HostName hostName, string port)
        {
            this.OutputStream = outputStream;
            this.HostName = hostName;
            this.Port = port;
        }

        public HostName HostName { get; set; }

        public string IP { get; set; }

        public string Name { get; set; }

        public string Key { get; set; }

        public string Port { get; set; }

        public IOutputStream OutputStream { get; }

        public string Message { get; set; }

        public DateTime Pinged { get; set; }

        public string OriginalPort { get; set; }

        public bool IsMatching(HostName hostName, string port)
        {
            return this.HostName == hostName && this.Port == port;
        }

        public override string ToString()
        {
            return this.HostName + this.Port;
        }
    }
}