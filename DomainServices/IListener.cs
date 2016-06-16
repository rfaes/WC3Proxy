using System.Net;

namespace Foole.WC3Proxy.DomainServices
{
    public interface IListener
    {
        void Start();
        void Stop();
        IPEndPoint LocalEndPoint { get; }
    }
}