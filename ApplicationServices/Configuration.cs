using System.Configuration;
using System.Linq;

namespace Foole.WC3Proxy.ApplicationServices
{
    public class Configuration
    {
        // App settings keys
        private const string WC3ExecutablenameAppSettingsKey = "WC3ExecutableName";
        private const string WC3ExpansionExecutablenameAppSettingsKey = "WC3ExpansionExecutableName";

        // default values if app.config doesn't exist
        private const string DefaultWC3ExecutableName = "War3.exe";
        private const string DefaultWC3ExpansionExecutableName = "Frozen throne.exe";

        public string WC3ExecutableName
        {
            get
            {
                return ConfigurationManager.AppSettings.AllKeys.Contains(WC3ExecutablenameAppSettingsKey) ? ConfigurationManager.AppSettings[WC3ExecutablenameAppSettingsKey] : DefaultWC3ExecutableName;
            }
        }

        public string WC3ExpansionExecutableName
        {
            get
            {
                return ConfigurationManager.AppSettings.AllKeys.Contains(WC3ExpansionExecutablenameAppSettingsKey) ? ConfigurationManager.AppSettings[WC3ExpansionExecutablenameAppSettingsKey] : DefaultWC3ExpansionExecutableName;
            }
        }
    }
}
