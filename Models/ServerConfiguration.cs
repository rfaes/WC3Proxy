using System.Net;

namespace Foole.WC3Proxy.Models
{
    public class ServerConfiguration
    {
        public IPAddress Host { get; set; }

        public byte Version { get; set; }

        public bool Expansion { get; set; }
    }
}