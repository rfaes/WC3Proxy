/*
Copyright (c) 2008 Foole

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
 */

using System;
using System.Net;
using System.Net.Sockets;
using Foole.WC3Proxy.DomainServices;

namespace Foole.WC3Proxy.ApplicationServices
{
    public class Listener : IListener
    {
        private readonly AsyncCallback _acceptCallback;
        private readonly GotConnectionDelegate _gotConnectionDelegate;
        private readonly IPAddress _localIpAddress;

        private Socket _localSocket;
        private int _localListeningPort;
        private bool _stopping;

        public Listener(GotConnectionDelegate gotConnectionDelegate) : this(0, gotConnectionDelegate) { }

        public Listener(int localListeningPort, GotConnectionDelegate gotConnectionDelegate) : this(IPAddress.Any, localListeningPort, gotConnectionDelegate) { }

        public Listener(IPAddress localIpAddress, int localListeningPort, GotConnectionDelegate gotConnectionDelegate)
        {
            _localIpAddress = localIpAddress;
            _localListeningPort = localListeningPort;
            _gotConnectionDelegate = gotConnectionDelegate;
            _acceptCallback = EndAccept;
        }

        public void Start()
        {
            _stopping = false;

            var localEndPoint = new IPEndPoint(_localIpAddress, _localListeningPort);

            _localSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            _localSocket.Bind(localEndPoint);

            _localListeningPort = LocalEndPoint.Port;
            _localSocket.Listen(20);

            BeginAccept();
        }

        public void Stop()
        {
            _stopping = true;
            _localSocket.Close();
            _localSocket = null;
        }

        public IPEndPoint LocalEndPoint
        {
            get
            {
                return (IPEndPoint)_localSocket.LocalEndPoint;
            }
        }

        private void BeginAccept()
        {
            if (_stopping) return;
            _localSocket.BeginAccept(_acceptCallback, null);
        }

        private void EndAccept(IAsyncResult ar)
        {
            if (_stopping) return;

            try
            {
                Socket Client = _localSocket.EndAccept(ar);
                _gotConnectionDelegate(Client);
            }
            catch (ObjectDisposedException)
            {
                // Occasionally throws: System.ObjectDisposedException: Cannot access a disposed object.
                // Do nothing
            }
            BeginAccept();
        }
    }
}
