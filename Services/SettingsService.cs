using System.Configuration;

namespace ImportadorDeGTINEAN.Desktop.Services
{
    public static class SettingsService
    {
        public static string GetSetting(string key)
        {
            return ConfigurationManager.AppSettings[key] ?? string.Empty;
        }

        public static void SaveSetting(string key, string value)
        {
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var settings = config.AppSettings.Settings;

            if (settings[key] != null)
                settings[key].Value = value;
            else
                settings.Add(key, value);

            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }

        public static (string Host, string Port, string Database, string User, string Password) LoadConnectionSettings()
        {
            return (
                GetSetting("PgHost"),
                GetSetting("PgPort"),
                GetSetting("PgDatabase"),
                GetSetting("PgUser"),
                GetSetting("PgPassword")
            );
        }

        public static void SaveConnectionSettings(string host, string port, string database, string user, string password)
        {
            SaveSetting("PgHost", host);
            SaveSetting("PgPort", port);
            SaveSetting("PgDatabase", database);
            SaveSetting("PgUser", user);
            SaveSetting("PgPassword", password);
        }
    }
}
