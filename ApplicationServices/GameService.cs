using System;
using System.Diagnostics;
using System.IO;
using Foole.WC3Proxy.DomainServices;
using Microsoft.Win32;

namespace Foole.WC3Proxy.ApplicationServices
{
    public class GameService : IGameService
    {
        private const string Wc3RegistryPath= @"HKEY_CURRENT_USER\Software\Blizzard Entertainment\Warcraft III";

        private readonly Configuration configuration;

        public GameService(Configuration configuration)
        {
            this.configuration = configuration;
        }

        public bool TryToStartGame(bool isExpansion)
        {
            try
            {
                string programkey = isExpansion ? "ProgramX" : "Program";
                var program = (string)Registry.GetValue(Wc3RegistryPath, programkey, null);

                if (File.Exists(program))
                {
                    Process.Start(program);
                    return true;
                }

                if (isExpansion)
                {
                    program = configuration.WC3ExpansionExecutableName;
                }
                else
                {
                    program = configuration.WC3ExecutableName;
                }

                if (File.Exists(program))
                {
                    Process.Start(program);
                    return true;
                }
            }
            catch (Exception)
            {
                // ignored
            }

            return false;
        }
    }
}
