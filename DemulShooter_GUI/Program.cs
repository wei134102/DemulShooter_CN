using System;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using DemulShooter_GUI.Properties;

namespace DemulShooter_GUI
{
    static class Program
    {
        private const string CONF_FILENAME = "config.ini";
        private const string GUI_LANGUAGE_KEY = "gui_language";

        /// <summary>
        /// Application entry point
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            bool isVerbose = false;

            if (args.Length > 0)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i].ToLower().Equals("-v") || args[i].ToLower().Equals("--verbose"))
                    {
                        isVerbose = true;
                    }
                }
            }

            // Set UI language from config before creating any form (Plan B localization)
            ApplyLanguageFromConfig();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Wnd_DemulShooterGui(isVerbose));
        }

        /// <summary>
        /// Reads gui_language from config.ini and sets CurrentUICulture + Strings.Culture.
        /// Supported values: en, en-US, zh-CN, etc. Empty or missing = use system default.
        /// </summary>
        private static void ApplyLanguageFromConfig()
        {
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CONF_FILENAME);
            if (!File.Exists(configPath))
                return;

            string cultureName = null;
            try
            {
                foreach (string line in File.ReadAllLines(configPath))
                {
                    string trimmed = line.Trim();
                    if (trimmed.StartsWith(";") || string.IsNullOrEmpty(trimmed))
                        continue;
                    int idx = trimmed.IndexOf('=');
                    if (idx <= 0) continue;
                    string key = trimmed.Substring(0, idx).Trim();
                    if (key.Equals(GUI_LANGUAGE_KEY, StringComparison.OrdinalIgnoreCase))
                    {
                        cultureName = trimmed.Substring(idx + 1).Trim();
                        break;
                    }
                }
            }
            catch { /* ignore */ }

            if (string.IsNullOrWhiteSpace(cultureName))
                return;

            try
            {
                var culture = CultureInfo.GetCultureInfo(cultureName);
                System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
                System.Threading.Thread.CurrentThread.CurrentCulture = culture;
                Strings.Culture = culture;
            }
            catch { /* invalid culture name, keep default */ }
        }
    }
}
