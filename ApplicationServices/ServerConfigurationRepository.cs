using System.Net;
using Foole.WC3Proxy.DomainServices;
using Foole.WC3Proxy.Models;
using Microsoft.Win32;

namespace Foole.WC3Proxy.ApplicationServices
{
    public class ServerConfigurationRepository : IServerConfigurationRepository
    {
        private static readonly string mRegPath = @"HKEY_CURRENT_USER\Software\Foole\WC3 Proxy";
        private string _wc3VersionRegistryName = "WC3Version";
        private string _expansionRegistryName = "Expansion";
        private string _serverIpRegsitryName = "ServerIp";

        public ServerConfiguration Get()
        {
            ServerConfiguration serverConfiguration = new ServerConfiguration();
            string serverIp = (string)Registry.GetValue(mRegPath, _serverIpRegsitryName, null);

            if (serverIp == null)
            {
                return null;
            }

            serverConfiguration.Expansion = ((int)Registry.GetValue(mRegPath, _expansionRegistryName, 0)) != 0;
            serverConfiguration.Host = IPAddress.Parse(serverIp);
            serverConfiguration.Version = (byte)(int)Registry.GetValue(mRegPath, _wc3VersionRegistryName, 0);

            return serverConfiguration;
        }

        public void Save(ServerConfiguration serverConfiguration)
        {
            Registry.SetValue(mRegPath, _serverIpRegsitryName, serverConfiguration.Host.ToString(), RegistryValueKind.String);
            Registry.SetValue(mRegPath, _expansionRegistryName, serverConfiguration.Expansion ? 1 : 0, RegistryValueKind.DWord);
            Registry.SetValue(mRegPath, _wc3VersionRegistryName, serverConfiguration.Version, RegistryValueKind.DWord);
        }
    }
}
