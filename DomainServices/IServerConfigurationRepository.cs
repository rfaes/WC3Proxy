using Foole.WC3Proxy.Models;

namespace Foole.WC3Proxy.DomainServices
{
    public interface IServerConfigurationRepository
    {
        ServerConfiguration Get();
        void Save(ServerConfiguration serverConfiguration);
    }
}