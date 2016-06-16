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
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;


using Foole.Net;
using Foole.WC3Proxy.ApplicationServices;
using Foole.WC3Proxy.DomainServices;
using Foole.WC3Proxy.Models;

// For Listener

namespace Foole.WC3Proxy
{
    public partial class MainForm : Form
    {
        private Listener mListener; // This waits for proxy connections
        private List<TcpProxy> mProxies; // A collection of game proxies.  Usually we would only need 1 proxy.
        private Browser mBrowser; // This sends game info queries to the server and forwards the responses to the client

        private IPAddress _serverHost;
        private IPEndPoint mServerEP;
        private byte mVersion;
        private bool mExpansion;

        // TODO: Possibly move these (and associated code) into the Browser class
        private bool mFoundGame;
        private DateTime mLastFoundServer;
        private GameInfo mGameInfo;

        private readonly string mCaption = "WC3 Proxy";
        private readonly int mBalloonTipTimeout = 1000;

        private delegate void SimpleDelegate();

        private static readonly ServerConfigurationRepository _serverConfigurationRepository = new ServerConfigurationRepository();

        private readonly ServerConfiguration _serverConfiguration;

        private readonly IGameService _gameService;

        // TODO: Configurable command line arguments for war3?
        // window       Windowed mode
        // fullscreen   (Default)
        // gametype     ?
        // loadfile     Loads a map or replay
        // datadir      ?
        // classic      This will load in RoC mode even if you have TFT installed.
        // swtnl        Software Transform & Lighting
        // opengl
        // d3d          (Default)

        static void Main(string[] args)
        {
            var serverConfiguration = _serverConfigurationRepository.Get();

            if (serverConfiguration == null)
            {
                serverConfiguration = new ServerConfiguration();
                if (ShowInfoDialog(serverConfiguration) == false)
                {
                    return;
                }
            }

            MainForm mainform = new MainForm(serverConfiguration, new GameService(new Configuration()));

            Application.Run(mainform);
        }

        private static bool ShowInfoDialog(ServerConfiguration serverConfiguration)
        {
            ServerInfoDlg dlg = new ServerInfoDlg();

            if (serverConfiguration != null)
            {
                dlg.Host = serverConfiguration.Host;
                dlg.Expansion = serverConfiguration.Expansion;
                dlg.Version = serverConfiguration.Version;
            }
            if (dlg.ShowDialog() == DialogResult.Cancel)
                return false;

            serverConfiguration.Host = dlg.Host;
            serverConfiguration.Version = dlg.Version;
            serverConfiguration.Expansion = dlg.Expansion;
            dlg.Dispose();

            return true;
        }

        public MainForm(ServerConfiguration serverConfiguration, IGameService gameService)
        {
            InitializeComponent();
            _serverConfiguration = serverConfiguration;
            _gameService = gameService;

            ServerHost = serverConfiguration.Host;
            Version = serverConfiguration.Version;
            Expansion = serverConfiguration.Expansion;
        }

        public IPAddress ServerHost
        {
            get { return _serverHost; }
            set
            {
                OnLostGame();

                _serverHost = value;
                mServerEP = new IPEndPoint(_serverHost, 0);

                lblServerAddress.Text = _serverHost.ToString();

                if (mBrowser != null) mBrowser.ServerAddress = _serverHost;
            }
        }

        public bool Expansion
        {
            get { return mExpansion; }
            set
            {
                mExpansion = value;
                if (mBrowser != null) mBrowser.Expansion = value;
            }
        }

        public byte Version
        {
            get { return mVersion; }
            set
            {
                mVersion = value;
                if (mBrowser != null) mBrowser.Version = value;
            }
        }

        private void ResetGameInfo()
        {
            mIcon.ShowBalloonTip(mBalloonTipTimeout, mCaption, "Lost game", ToolTipIcon.Info);

            lblGameName.Text = "(None found)";
            lblMap.Text = "(N/A)";
            lblGamePort.Text = "(N/A)";
            lblPlayers.Text = "(N/A)";

            mServerEP.Port = 0;

            mFoundGame = false;
        }

        private void DisplayGameInfo()
        {
            if (InvokeRequired)
            {
                Invoke(new SimpleDelegate(DisplayGameInfo));
                return;
            }

            if (mFoundGame == false) mIcon.ShowBalloonTip(mBalloonTipTimeout, mCaption, "Found game: " + mGameInfo.Name, ToolTipIcon.Info);

            lblGameName.Text = mGameInfo.Name;
            lblMap.Text = mGameInfo.Map;
            lblGamePort.Text = mGameInfo.Port.ToString();
            lblPlayers.Text = String.Format("{0} / {1} / {2}", mGameInfo.CurrentPlayers, mGameInfo.PlayerSlots, mGameInfo.SlotCount);

            mServerEP.Port = mGameInfo.Port;
        }

        private void ExecuteWC3(bool Expansion)
        {
            if (!_gameService.TryToStartGame(Expansion))
            {
                MessageBox.Show("Unable to launch or find warcraft executable", mCaption, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void mnuFileExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void mnuLaunchWarcraft_Click(object sender, EventArgs e)
        {
            ExecuteWC3(Expansion);
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            StartTcpProxy();
            StartBrowser();
        }

        private void StartBrowser()
        {
            mBrowser = new Browser(ServerHost, mListener.LocalEndPoint.Port, Version, Expansion);
            mBrowser.QuerySent += new QuerySentHandler(mBrowser_QuerySent);
            mBrowser.FoundServer += new FoundServerHandler(mBrowser_FoundServer);
            mBrowser.Run();
        }

        void mBrowser_FoundServer(GameInfo Game)
        {
            mGameInfo = Game;
            DisplayGameInfo();

            mFoundGame = true;
            mLastFoundServer = DateTime.Now;
        }

        void mBrowser_QuerySent()
        {
            // TODO: show an activity indicator?

            // We don't receive the "server cancelled" messages
            // because they are only ever broadcast to the host's LAN.
            if (mFoundGame == true)
            {
                TimeSpan interval = DateTime.Now - mLastFoundServer;
                if (interval.TotalSeconds > 3)
                    OnLostGame();
            }
        }

        private void OnLostGame()
        {
            if (mBrowser != null) mBrowser.SendGameCancelled(mGameInfo.GameId);
            if (mFoundGame) Invoke(new SimpleDelegate(ResetGameInfo));
        }

        private void StartTcpProxy()
        {
            mProxies = new List<TcpProxy>();

            mListener = new Listener(new GotConnectionDelegate(GotConnection));
            try
            {
                mListener.Run();
            }
            catch (SocketException ex)
            {
                MessageBox.Show("Unable to start listener\n" + ex.Message);
            }
        }

        private void GotConnection(Socket ClientSocket)
        {
            string message = String.Format("Got a connection from {0}", ClientSocket.RemoteEndPoint.ToString());
            mIcon.ShowBalloonTip(mBalloonTipTimeout, mCaption, message, ToolTipIcon.Info);

            TcpProxy proxy = new TcpProxy(ClientSocket, mServerEP);
            proxy.ProxyDisconnected += new ProxyDisconnectedHandler(ProxyDisconnected);
            lock (mProxies) mProxies.Add(proxy);

            proxy.Run();

            UpdateClientCount();
        }

        private void UpdateClientCount()
        {
            if (InvokeRequired)
            {
                Invoke(new SimpleDelegate(UpdateClientCount));
                return;
            }
            lblClientCount.Text = mProxies.Count.ToString();
        }

        private void ProxyDisconnected(TcpProxy p)
        {
            mIcon.ShowBalloonTip(mBalloonTipTimeout, mCaption, "Client disconnected", ToolTipIcon.Info);

            lock (mProxies)
                if (mProxies.Contains(p)) mProxies.Remove(p);

            UpdateClientCount();
        }

        private void StopTcpProxy()
        {
            mListener.Stop();
            foreach (TcpProxy p in mProxies)
                p.Stop();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopTcpProxy();
            if (mBrowser != null)
            {
                if (mFoundGame)
                {
                    mBrowser.SendGameCancelled(mGameInfo.GameId);
                }
                mBrowser.Stop();
            }
        }

        private void mIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            WindowState = FormWindowState.Normal;
            Focus();
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            ShowInTaskbar = (WindowState != FormWindowState.Minimized);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void mnuChangeServer_Click(object sender, EventArgs e)
        {
            if (ShowInfoDialog(_serverConfiguration))
            {
                ServerHost = _serverConfiguration.Host;
                Version = _serverConfiguration.Version;
                Expansion = _serverConfiguration.Expansion;
            }
        }

        private void mnuHelpAbout_Click(object sender, EventArgs e)
        {
            new AboutBox().ShowDialog();
        }
    }
}