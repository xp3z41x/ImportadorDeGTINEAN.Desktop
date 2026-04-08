using System.Windows;
using System.Windows.Controls;
using ImportadorDeGTINEAN.Desktop.ViewModels;

namespace ImportadorDeGTINEAN.Desktop.Views
{
    public partial class SettingsWindow : Window
    {
        private SettingsViewModel ViewModel => (SettingsViewModel)DataContext;
        private bool _suppressPasswordSync;

        public SettingsWindow()
        {
            InitializeComponent();
            Loaded += SettingsWindow_Loaded;
        }

        private void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Sync initial password to PasswordBox
            _suppressPasswordSync = true;
            PasswordBoxField.Password = ViewModel.Password;
            _suppressPasswordSync = false;
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_suppressPasswordSync) return;
            ViewModel.Password = PasswordBoxField.Password;
        }

        private void TogglePassword_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.ShowPassword)
            {
                // Switching to TextBox - password is already bound via ViewModel.Password
            }
            else
            {
                // Switching to PasswordBox - sync from ViewModel
                _suppressPasswordSync = true;
                PasswordBoxField.Password = ViewModel.Password;
                _suppressPasswordSync = false;
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.SaveCommand.Execute(null);
            if (ViewModel.Saved)
            {
                MessageBox.Show("Configurações salvas com sucesso!", "Sucesso",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
