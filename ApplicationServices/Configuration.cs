using System.Configuration;

namespace Foole.WC3Proxy.ApplicationServices
{
    public class Configuration
    {
        public string WC3ExecutableName
        {
            get
            {
                return ConfigurationManager.AppSettings["WC3ExecutableName"];
            }
        }

        public string WC3ExpansionExecutableName
        {
            get
            {
                return ConfigurationManager.AppSettings["WC3ExpansionExecutableName"];
            }
        }
    }
}
