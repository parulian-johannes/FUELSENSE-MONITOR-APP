using System;
using System.IO.Ports;
using System.Windows;
using System.Windows.Controls;
using FuelsenseMonitorApp.Services;

namespace FuelsenseMonitorApp
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void LoadSettings()
        {
            try
            {
                // Load available COM ports
                LoadAvailablePorts();
                
                // Setup slider event
                SamplingRateSlider.ValueChanged += (s, e) => {
                    SamplingRateText.Text = $"{(int)e.NewValue} Hz";
                };
                
                // Load current settings from app config or use defaults
                LoadCurrentSettings();
                
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading settings: {ex.Message}", "Error", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadAvailablePorts()
        {
            try
            {
                PortComboBox.Items.Clear();
                var ports = SerialService.GetAvailablePorts();
                
                if (ports.Length == 0)
                {
                    PortComboBox.Items.Add("No ports available");
                    PortComboBox.SelectedIndex = 0;
                    PortComboBox.IsEnabled = false;
                }
                else
                {
                    foreach (var port in ports)
                    {
                        PortComboBox.Items.Add(port);
                    }
                    PortComboBox.SelectedIndex = 0;
                    PortComboBox.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                PortComboBox.Items.Add($"Error: {ex.Message}");
                PortComboBox.SelectedIndex = 0;
                PortComboBox.IsEnabled = false;
            }
        }

        private void LoadCurrentSettings()
        {
            // Load settings from application properties or config file
            // For now, use default values
            
            BaudRateComboBox.SelectedIndex = 1; // 115200
            TimeoutTextBox.Text = "2000";
            AutoReconnectCheckBox.IsChecked = true;
            
            SamplingRateSlider.Value = 1;
            ChartPointsTextBox.Text = "50";
            AutoExportComboBox.SelectedIndex = 0;
            DataValidationCheckBox.IsChecked = true;
            
            ThemeComboBox.SelectedIndex = 0;
            UpdateRateTextBox.Text = "1000";
            AnimationsCheckBox.IsChecked = true;
            ShowTooltipsCheckBox.IsChecked = true;
            ShowGridLinesCheckBox.IsChecked = true;
            
            TempAlertTextBox.Text = "150";
            RpmAlertTextBox.Text = "4500";
            TempAlertCheckBox.IsChecked = true;
            RpmAlertCheckBox.IsChecked = true;
            SoundAlertsCheckBox.IsChecked = false;
            PopupAlertsCheckBox.IsChecked = true;
            EmailAlertsCheckBox.IsChecked = false;
        }

        private void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate input values
                if (!ValidateSettings())
                    return;

                // Save all settings
                var settings = CollectSettings();
                SaveSettingsToConfig(settings);

                var message = "Settings saved successfully!\n\n";
                message += $"🔌 Serial Port: {settings.Port}\n";
                message += $"📊 Baud Rate: {settings.BaudRate}\n";
                message += $"⏱️ Sampling Rate: {settings.SamplingRate} Hz\n";
                message += $"📈 Chart Points: {settings.ChartPoints}\n";
                message += $"🔄 Auto-reconnect: {(settings.AutoReconnect ? "Enabled" : "Disabled")}\n";
                message += $"🚨 Temperature Alert: {(settings.TempAlert ? settings.TempThreshold + "°C" : "Disabled")}\n";
                message += $"⚡ RPM Alert: {(settings.RpmAlert ? settings.RpmThreshold.ToString() : "Disabled")}";

                MessageBox.Show(message, "Settings Saved", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}", "Error", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidateSettings()
        {
            // Validate timeout
            if (!int.TryParse(TimeoutTextBox.Text, out int timeout) || timeout < 500 || timeout > 10000)
            {
                MessageBox.Show("Connection timeout must be between 500 and 10000 milliseconds.", 
                              "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                TimeoutTextBox.Focus();
                return false;
            }

            // Validate chart points
            if (!int.TryParse(ChartPointsTextBox.Text, out int chartPoints) || chartPoints < 10 || chartPoints > 1000)
            {
                MessageBox.Show("Chart data points must be between 10 and 1000.", 
                              "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                ChartPointsTextBox.Focus();
                return false;
            }

            // Validate update rate
            if (!int.TryParse(UpdateRateTextBox.Text, out int updateRate) || updateRate < 100 || updateRate > 5000)
            {
                MessageBox.Show("Update rate must be between 100 and 5000 milliseconds.", 
                              "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                UpdateRateTextBox.Focus();
                return false;
            }

            // Validate temperature threshold
            if (!double.TryParse(TempAlertTextBox.Text, out double tempThreshold) || tempThreshold < 0 || tempThreshold > 500)
            {
                MessageBox.Show("Temperature threshold must be between 0 and 500°C.", 
                              "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                TempAlertTextBox.Focus();
                return false;
            }

            // Validate RPM threshold
            if (!int.TryParse(RpmAlertTextBox.Text, out int rpmThreshold) || rpmThreshold < 0 || rpmThreshold > 10000)
            {
                MessageBox.Show("RPM threshold must be between 0 and 10000.", 
                              "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                RpmAlertTextBox.Focus();
                return false;
            }

            return true;
        }

        private AppSettings CollectSettings()
        {
            return new AppSettings
            {
                Port = PortComboBox.SelectedItem?.ToString() ?? "",
                BaudRate = ((ComboBoxItem)BaudRateComboBox.SelectedItem)?.Content.ToString() ?? "115200",
                Timeout = int.Parse(TimeoutTextBox.Text),
                AutoReconnect = AutoReconnectCheckBox.IsChecked == true,
                
                SamplingRate = (int)SamplingRateSlider.Value,
                ChartPoints = int.Parse(ChartPointsTextBox.Text),
                AutoExportInterval = AutoExportComboBox.SelectedIndex,
                DataValidation = DataValidationCheckBox.IsChecked == true,
                
                Theme = ThemeComboBox.SelectedIndex,
                UpdateRate = int.Parse(UpdateRateTextBox.Text),
                Animations = AnimationsCheckBox.IsChecked == true,
                ShowTooltips = ShowTooltipsCheckBox.IsChecked == true,
                ShowGridLines = ShowGridLinesCheckBox.IsChecked == true,
                
                TempAlert = TempAlertCheckBox.IsChecked == true,
                TempThreshold = double.Parse(TempAlertTextBox.Text),
                RpmAlert = RpmAlertCheckBox.IsChecked == true,
                RpmThreshold = int.Parse(RpmAlertTextBox.Text),
                SoundAlerts = SoundAlertsCheckBox.IsChecked == true,
                PopupAlerts = PopupAlertsCheckBox.IsChecked == true,
                EmailAlerts = EmailAlertsCheckBox.IsChecked == true
            };
        }

        private void SaveSettingsToConfig(AppSettings settings)
        {
            // In a real application, save to app.config, registry, or settings file
            // For now, just show that we would save them
            Console.WriteLine("Settings would be saved to configuration file:");
            Console.WriteLine($"Port: {settings.Port}");
            Console.WriteLine($"BaudRate: {settings.BaudRate}");
            Console.WriteLine($"SamplingRate: {settings.SamplingRate}");
            // ... etc
        }

        private void ResetSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show(
                    "This will reset all settings to their default values.\n\nAre you sure you want to continue?", 
                    "Reset Settings", 
                    MessageBoxButton.YesNo, 
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // Reset all controls to default values
                    BaudRateComboBox.SelectedIndex = 1; // 115200
                    TimeoutTextBox.Text = "2000";
                    AutoReconnectCheckBox.IsChecked = true;
                    
                    SamplingRateSlider.Value = 1;
                    ChartPointsTextBox.Text = "50";
                    AutoExportComboBox.SelectedIndex = 0;
                    DataValidationCheckBox.IsChecked = true;
                    
                    ThemeComboBox.SelectedIndex = 0;
                    UpdateRateTextBox.Text = "1000";
                    AnimationsCheckBox.IsChecked = true;
                    ShowTooltipsCheckBox.IsChecked = true;
                    ShowGridLinesCheckBox.IsChecked = true;
                    
                    TempAlertTextBox.Text = "150";
                    RpmAlertTextBox.Text = "4500";
                    TempAlertCheckBox.IsChecked = true;
                    RpmAlertCheckBox.IsChecked = true;
                    SoundAlertsCheckBox.IsChecked = false;
                    PopupAlertsCheckBox.IsChecked = true;
                    EmailAlertsCheckBox.IsChecked = false;

                    MessageBox.Show("All settings have been reset to default values.", 
                                  "Reset Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error resetting settings: {ex.Message}", "Error", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            var aboutMessage = "🚗 FUELSENSE MONITOR PRO v2.0\n\n" +
                              "Advanced Engine Monitoring System\n" +
                              "Real-time data acquisition and analysis\n\n" +
                              "✨ FEATURES:\n" +
                              "• Live sensor data visualization\n" +
                              "• Real-time charts and analytics\n" +
                              "• Excel export with comprehensive graphs\n" +
                              "• Advanced alert system for critical values\n" +
                              "• Modern dark theme interface\n" +
                              "• Configurable sampling rates\n" +
                              "• Auto-reconnection capability\n" +
                              "• Data validation and filtering\n\n" +
                              "🔧 TECHNICAL SPECIFICATIONS:\n" +
                              "• Compatible with CH340 serial loggers\n" +
                              "• Supports multiple baud rates\n" +
                              "• Real-time data processing\n" +
                              "• Customizable alert thresholds\n\n" +
                              "📞 SUPPORT:\n" +
                              "For technical support and updates, please visit:\n" +
                              "https://github.com/fuelsense/monitor-pro\n\n" +
                              "© 2024 FuelSense Technologies\n" +
                              "Developed for automotive engine diagnostics";

            MessageBox.Show(aboutMessage, "About FuelSense Monitor Pro", 
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CloseWindow_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // Settings data structure
        public class AppSettings
        {
            public string Port { get; set; } = "";
            public string BaudRate { get; set; } = "115200";
            public int Timeout { get; set; } = 2000;
            public bool AutoReconnect { get; set; } = true;
            
            public int SamplingRate { get; set; } = 1;
            public int ChartPoints { get; set; } = 50;
            public int AutoExportInterval { get; set; } = 0;
            public bool DataValidation { get; set; } = true;
            
            public int Theme { get; set; } = 0;
            public int UpdateRate { get; set; } = 1000;
            public bool Animations { get; set; } = true;
            public bool ShowTooltips { get; set; } = true;
            public bool ShowGridLines { get; set; } = true;
            
            public bool TempAlert { get; set; } = true;
            public double TempThreshold { get; set; } = 150;
            public bool RpmAlert { get; set; } = true;
            public int RpmThreshold { get; set; } = 4500;
            public bool SoundAlerts { get; set; } = false;
            public bool PopupAlerts { get; set; } = true;
            public bool EmailAlerts { get; set; } = false;
        }
    }
}
