using System.Windows;
using System.Windows.Controls;
using Wox.Plugin.Currency.ViewModels;

namespace Wox.Plugin.Currency.Views
{
    /// <summary>
    /// Interaction logic for CurrencySettings.xaml
    /// </summary>
    public partial class CurrencySettings : UserControl
    {
        private readonly SettingsViewModel _viewModel;
        private readonly Settings _settings;

        public CurrencySettings(SettingsViewModel viewModel)
        {
            _viewModel = viewModel;
            _settings = viewModel.Settings;
            DataContext = viewModel;
            InitializeComponent();
        }

        private void CurrencySettings_Loaded(object sender, RoutedEventArgs e)
        {
            cb_BaseCurrency.ItemsSource = _settings.Rates;
        }
    }

    
}
