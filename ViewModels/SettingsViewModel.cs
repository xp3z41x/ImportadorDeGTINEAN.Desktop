using ImportadorDeGTINEAN.Desktop.Services;
using System.Windows.Input;

namespace ImportadorDeGTINEAN.Desktop.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        private string _host = string.Empty;
        private string _port = "5432";
        private string _database = string.Empty;
        private string _user = string.Empty;
        private string _password = string.Empty;
        private bool _showPassword;
        private string _testResult = string.Empty;
        private bool _isTesting;

        public string Host
        {
            get => _host;
            set => SetProperty(ref _host, value);
        }

        public string Port
        {
            get => _port;
            set => SetProperty(ref _port, value);
        }

        public string Database
        {
            get => _database;
            set => SetProperty(ref _database, value);
        }

        public string User
        {
            get => _user;
            set => SetProperty(ref _user, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public bool ShowPassword
        {
            get => _showPassword;
            set => SetProperty(ref _showPassword, value);
        }

        public string TestResult
        {
            get => _testResult;
            set => SetProperty(ref _testResult, value);
        }

        public bool IsTesting
        {
            get => _isTesting;
            set => SetProperty(ref _isTesting, value);
        }

        public ICommand TestConnectionCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand TogglePasswordCommand { get; }

        public bool Saved { get; private set; }

        public SettingsViewModel()
        {
            TestConnectionCommand = new RelayCommand(async _ => await TestConnectionAsync(), _ => !IsTesting);
            SaveCommand = new RelayCommand(_ => SaveSettings());
            TogglePasswordCommand = new RelayCommand(_ => ShowPassword = !ShowPassword);

            LoadSettings();
        }

        private void LoadSettings()
        {
            var settings = SettingsService.LoadConnectionSettings();
            Host = settings.Host;
            Port = string.IsNullOrEmpty(settings.Port) ? "5432" : settings.Port;
            Database = settings.Database;
            User = settings.User;
            Password = settings.Password;
        }

        private void SaveSettings()
        {
            Saved = false;
            try
            {
                SettingsService.SaveConnectionSettings(Host, Port, Database, User, Password);
                Saved = true;
            }
            catch (Exception ex)
            {
                TestResult = $"Erro ao salvar: {ex.Message}";
            }
        }

        private async Task TestConnectionAsync()
        {
            IsTesting = true;
            TestResult = string.Empty;

            try
            {
                var db = new DatabaseService(Host, Port, Database, User, Password);
                await db.TestConnectionAsync();
                TestResult = "Conexão bem-sucedida!";
            }
            catch (Exception ex)
            {
                TestResult = $"Erro: {ex.Message}";
            }
            finally
            {
                IsTesting = false;
            }
        }
    }
}
