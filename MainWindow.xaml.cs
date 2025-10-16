using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Media;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using FuelsenseMonitorApp.Models;
using FuelsenseMonitorApp.Services;

namespace FuelsenseMonitorApp
{
    public partial class MainWindow : Window
    {
        private ObservableCollection<SensorData> sensorDataCollection = new();
        private DispatcherTimer timer = new();
        private DispatcherTimer clockTimer = new();
        private DispatcherTimer sessionTimer = new();
        private bool isMonitoring = false;
        private SerialService serialService = new();

        // Chart data lists
        private List<double> torqueData = new();
        private List<double> fuelData = new();
        private List<double> rpmData = new();
        private List<double> tempData = new();
        private List<double> mafData = new();
        private List<double> timeData = new();

        private DateTime sessionStartTime;
        
        // RPM Alert System
        private bool isRpmAlertActive = false;
        private const double RPM_WARNING_THRESHOLD = 5000;
        private DispatcherTimer buzzerTimer = new();

        public MainWindow()
        {
            try
            {
                InitializeComponent();
                InitializeApp();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing application: {ex.Message}", "Error", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InitializeApp()
        {
            InitializeChart();
            InitializeServices();
            InitializeData();
            SetupTimers();
            SetupSensorLabels();
            
            StatusText.Text = "Application initialized successfully!";
        }

        private void SetupSensorLabels()
        {
            // Update sensor labels for each speedometer
            if (RpmSpeedometer != null)
            {
                var rpmLabel = RpmSpeedometer.FindName("SensorLabel") as System.Windows.Controls.TextBlock;
                if (rpmLabel != null) rpmLabel.Text = "ENGINE ROTATION";
            }
            
            if (TorsiSpeedometer != null)
            {
                var torsiLabel = TorsiSpeedometer.FindName("SensorLabel") as System.Windows.Controls.TextBlock;
                if (torsiLabel != null) torsiLabel.Text = "ENGINE TORQUE";
            }
            
            if (MafSpeedometer != null)
            {
                var mafLabel = MafSpeedometer.FindName("SensorLabel") as System.Windows.Controls.TextBlock;
                if (mafLabel != null) mafLabel.Text = "AIR FLOW SENSOR";
            }
            
            if (TempSpeedometer != null)
            {
                var tempLabel = TempSpeedometer.FindName("SensorLabel") as System.Windows.Controls.TextBlock;
                if (tempLabel != null) tempLabel.Text = "ENGINE TEMP";
            }
            
            if (BbmSpeedometer != null)
            {
                var bbmLabel = BbmSpeedometer.FindName("SensorLabel") as System.Windows.Controls.TextBlock;
                if (bbmLabel != null) bbmLabel.Text = "FUEL CONSUMPTION";
            }
        }

        private void InitializeData()
        {
            UpdateConnectionStatus(false);
            StartButton.IsEnabled = true;
            StopButton.IsEnabled = false;
        }

        private void SetupTimers()
        {
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
            
            clockTimer.Interval = TimeSpan.FromSeconds(1);
            clockTimer.Tick += ClockTimer_Tick;
            clockTimer.Start();
            
            sessionTimer.Interval = TimeSpan.FromSeconds(1);
            sessionTimer.Tick += SessionTimer_Tick;
            sessionStartTime = DateTime.Now;
            sessionTimer.Start();
        }

        private void ClockTimer_Tick(object? sender, EventArgs e)
        {
            SessionTime.Text = DateTime.Now.ToString("HH:mm:ss");
        }

        private void SessionTimer_Tick(object? sender, EventArgs e)
        {
            var elapsed = DateTime.Now - sessionStartTime;
            SessionTime.Text = elapsed.ToString(@"hh\:mm\:ss");
            SessionTimeText.Text = elapsed.ToString(@"hh\:mm\:ss");
        }

        private void InitializeChart()
        {
            // Chart initialization removed - now handled by AnalyticsWindow
        }

        private void InitializeServices()
        {
            try
            {
                serialService.DataReceived += SerialService_DataReceived;
                serialService.ConnectionStatusChanged += SerialService_ConnectionStatusChanged;
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Services init error: {ex.Message}";
            }
        }

        private void UpdateChart()
        {
            // Chart updates now handled by AnalyticsWindow
        }

        private void OpenChartButton_Click(object sender, RoutedEventArgs e)
        {
            var chartWindow = new ChartWindow(sensorDataCollection);
            chartWindow.Show();
        }

        private void OpenAnalyticsButton_Click(object sender, RoutedEventArgs e)
        {
            var analyticsWindow = new AnalyticsWindow(sensorDataCollection);
            analyticsWindow.Show();
        }

        private void OpenDataTableButton_Click(object sender, RoutedEventArgs e)
        {
            var dataTableWindow = new DataTableWindow(sensorDataCollection);
            dataTableWindow.Show();
        }

        private void OpenSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow();
            settingsWindow.ShowDialog();
        }

        private void ClearDataButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Clear all sensor data? This action cannot be undone.", 
                                       "Clear Data", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                sensorDataCollection.Clear();
                torqueData.Clear();
                fuelData.Clear();
                rpmData.Clear();
                tempData.Clear();
                mafData.Clear();
                timeData.Clear();
                
                StatusText.Text = "Data cleared successfully";
            }
        }

        private void ResetSpeedometersButton_Click(object sender, RoutedEventArgs e)
        {
            RpmSpeedometer.Value = 0;
            TorsiSpeedometer.Value = 0;
            MafSpeedometer.Value = 0;
            TempSpeedometer.Value = 0;
            BbmSpeedometer.Value = 0;
            
            RpmValue.Text = "0 rpm";
            TorqueValue.Text = "0.0 Nm";
            MafValue.Text = "0.0 g/s";
            TempValue.Text = "0.0°C";
            FuelValue.Text = "0.0 L/h";
            
            StatusText.Text = "Speedometers reset to zero";
        }

        private void ExportDataButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var excelService = new ExcelService();
                if (excelService.ExportToExcel(sensorDataCollection))
                {
                    StatusText.Text = "Data exported successfully";
                }
                else
                {
                    StatusText.Text = "Export cancelled";
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Export error: {ex.Message}";
                MessageBox.Show($"Error exporting data: {ex.Message}", "Export Error", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            RecordCountStatus.Text = sensorDataCollection.Count.ToString();
            RecordCountStatus.Text = sensorDataCollection.Count.ToString();
            
            // Update session time
            if (isMonitoring)
            {
                var sessionDuration = DateTime.Now - sessionStartTime;
                SessionTimeText.Text = sessionDuration.ToString(@"hh\:mm\:ss");
            }

            if (sensorDataCollection.Count > 0)
            {
                UpdateChart();
            }
        }

        private void SerialService_DataReceived(object? sender, SensorData data)
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    sensorDataCollection.Add(data);
                    
                    // Update live values display
                    TorqueValue.Text = $"{data.Torque:F1} Nm";
                    FuelValue.Text = $"{data.Fuel:F1} L/h";
                    RpmValue.Text = $"{data.RPM} rpm";
                    TempValue.Text = $"{data.Temperature:F1}°C";
                    MafValue.Text = $"{data.MAF:F1} g/s";
                    
                    // Update modern speedometers
                    UpdateSpeedometers(data);
                    
                    // Check RPM Alert
                    CheckRpmAlert(data.RPM);
                    
                    // Add to chart data
                    var timePoint = (DateTime.Now - sessionStartTime).TotalSeconds;
                    timeData.Add(timePoint);
                    torqueData.Add(data.Torque);
                    fuelData.Add(data.Fuel);
                    rpmData.Add(data.RPM / 100.0);
                    tempData.Add(data.Temperature);
                    mafData.Add(data.MAF);
                    
                    // Limit data points for performance
                    if (timeData.Count > 100)
                    {
                        timeData.RemoveAt(0);
                        torqueData.RemoveAt(0);
                        fuelData.RemoveAt(0);
                        rpmData.RemoveAt(0);
                        tempData.RemoveAt(0);
                        mafData.RemoveAt(0);
                    }
                    
                    StatusText.Text = $"Latest: RPM={data.RPM}, Torque={data.Torque:F1}Nm, Temp={data.Temperature:F1}°C";
                }
                catch (Exception ex)
                {
                    StatusText.Text = $"Error processing data: {ex.Message}";
                }
            });
        }

        private void UpdateSpeedometers(SensorData data)
        {
            try
            {
                if (RpmSpeedometer != null) RpmSpeedometer.Value = data.RPM;
                if (TorsiSpeedometer != null) TorsiSpeedometer.Value = data.Torque;
                if (MafSpeedometer != null) MafSpeedometer.Value = data.MAF;
                if (TempSpeedometer != null) TempSpeedometer.Value = data.Temperature;
                if (BbmSpeedometer != null) BbmSpeedometer.Value = data.Fuel;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating speedometers: {ex.Message}");
            }
        }

        private void UpdateConnectionStatus(bool connected)
        {
            if (connected)
            {
                ConnectionStatus.Fill = new RadialGradientBrush(
                    Color.FromRgb(0, 255, 136), Color.FromRgb(0, 204, 106));
                ConnectionText.Text = "Connected";
                if (PortInfo != null) PortInfo.Text = "COM3";
                
                ConnectionStatus.Effect = new DropShadowEffect
                {
                    Color = Color.FromRgb(0, 255, 136),
                    ShadowDepth = 0,
                    BlurRadius = 8,
                    Opacity = 0.8
                };
            }
            else
            {
                ConnectionStatus.Fill = new RadialGradientBrush(
                    Color.FromRgb(255, 68, 68), Color.FromRgb(204, 0, 0));
                ConnectionText.Text = "Disconnected";
                if (PortInfo != null) PortInfo.Text = "COM--";
                
                ConnectionStatus.Effect = new DropShadowEffect
                {
                    Color = Color.FromRgb(255, 68, 68),
                    ShadowDepth = 0,
                    BlurRadius = 8,
                    Opacity = 0.8
                };
            }
        }

        private void SerialService_ConnectionStatusChanged(object? sender, bool connected)
        {
            Dispatcher.Invoke(() => UpdateConnectionStatus(connected));
        }

        private void CheckRpmAlert(double rpm)
        {
            if (rpm >= RPM_WARNING_THRESHOLD && !isRpmAlertActive)
            {
                // Show RPM Alert
                isRpmAlertActive = true;
                RpmAlertPanel.Visibility = Visibility.Visible;
                RpmAlertText.Text = $"🚨 HIGH RPM WARNING: {rpm:F0} RPM - REDUCE THROTTLE! 🚨";
                
                // Modern notification sound sequence
                PlayModernAlertSequence();
                
                // Start intelligent alert timer - adaptive frequency
                buzzerTimer.Interval = TimeSpan.FromSeconds(3);
                buzzerTimer.Tick += BuzzerTimer_Tick;
                buzzerTimer.Start();
                
                // Smooth modern pulsing animation
                var pulseAnimation = new System.Windows.Media.Animation.DoubleAnimation
                {
                    From = 0.2,
                    To = 1.0,
                    Duration = TimeSpan.FromMilliseconds(600),
                    AutoReverse = true,
                    RepeatBehavior = System.Windows.Media.Animation.RepeatBehavior.Forever,
                    EasingFunction = new System.Windows.Media.Animation.QuadraticEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseInOut }
                };
                
                RpmAlertPanel.BeginAnimation(UIElement.OpacityProperty, pulseAnimation);
            }
            else if (rpm < RPM_WARNING_THRESHOLD && isRpmAlertActive)
            {
                // Auto-hide alert when RPM drops below threshold
                HideRpmAlert();
            }
        }

        private async void PlayModernAlertSequence()
        {
            // Layer 1: Multiple Windows notification sounds for impact
            SystemSounds.Hand.Play();
            await Task.Delay(200);
            SystemSounds.Exclamation.Play();
            
            // Layer 2: Sophisticated but intense beep pattern
            await Task.Run(async () =>
            {
                // Urgent ascending alarm sequence (more intense)
                int[] alertSequence = { 750, 900, 1050, 1200, 1350, 1200, 1050, 900, 750 };
                for (int i = 0; i < alertSequence.Length; i++)
                {
                    Console.Beep(alertSequence[i], 180);
                    await Task.Delay(120);
                }
                
                await Task.Delay(300);
                
                // Professional attention pattern (rapid but controlled)
                for (int i = 0; i < 5; i++)
                {
                    Console.Beep(1000, 200);
                    await Task.Delay(150);
                }
                
                await Task.Delay(250);
                
                // Final emphasis sequence
                Console.Beep(800, 300);   // Low emphasis
                await Task.Delay(100);
                Console.Beep(1200, 300);  // High emphasis
                await Task.Delay(100);
                Console.Beep(1000, 400);  // Final sustained note
            });
        }

        private void BuzzerTimer_Tick(object? sender, EventArgs e)
        {
            // Intensified continuous alert - more berisik but professional
            Task.Run(async () =>
            {
                // Rapid professional warning sequence
                Console.Beep(850, 250);   // Low warning tone
                await Task.Delay(80);
                Console.Beep(1150, 250);  // High warning tone
                await Task.Delay(80);
                Console.Beep(1000, 300);  // Emphasis tone
                await Task.Delay(200);
                
                // Insistent reminder pattern
                for (int i = 0; i < 4; i++)
                {
                    Console.Beep(1100, 180);
                    await Task.Delay(120);
                }
                
                await Task.Delay(150);
                
                // Final attention getter
                Console.Beep(900, 200);
                await Task.Delay(50);
                Console.Beep(1250, 200);
            });
        }

        private void HideRpmAlert()
        {
            isRpmAlertActive = false;
            
            // Stop buzzer timer
            buzzerTimer.Stop();
            buzzerTimer.Tick -= BuzzerTimer_Tick;
            
            RpmAlertPanel.BeginAnimation(UIElement.OpacityProperty, null); // Stop animation
            RpmAlertPanel.Visibility = Visibility.Collapsed;
        }

        private void CloseRpmAlert_Click(object sender, RoutedEventArgs e)
        {
            HideRpmAlert();
        }

        private void TestRpmAlert_Click(object sender, RoutedEventArgs e)
        {
            // Simulate high RPM for testing alert system
            CheckRpmAlert(5500); // Simulate RPM above threshold
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!isMonitoring)
                {
                    // Auto-detect available COM ports
                    string[] availablePorts = SerialService.GetAvailablePorts();
                    
                    if (availablePorts.Length == 0)
                    {
                        StatusText.Text = "No COM ports available! Please check device connection.";
                        MessageBox.Show("No COM ports found. Please:\n1. Check if device is connected\n2. Install CH340 driver\n3. Check Device Manager", 
                                       "Connection Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    
                    bool connected = false;
                    string connectedPort = "";
                    
                    // Try different baud rates and ports
                    int[] baudRates = { 9600, 115200, 57600, 38400, 19200, 4800 };
                    
                    foreach (string port in availablePorts)
                    {
                        StatusText.Text = $"Trying to connect to {port}...";
                        
                        foreach (int baudRate in baudRates)
                        {
                            Console.WriteLine($"Attempting connection to {port} at {baudRate} baud...");
                            
                            if (serialService.ConnectToCH340(port, baudRate))
                            {
                                connected = true;
                                connectedPort = port;
                                
                                // Wait a bit to see if we get data
                                await Task.Delay(2000);
                                
                                // Check if we're actually receiving data
                                if (sensorDataCollection.Count > 0 || serialService.IsConnected)
                                {
                                    Console.WriteLine($"Successfully connected to {port} at {baudRate} baud with data flow");
                                    break;
                                }
                                else
                                {
                                    Console.WriteLine($"Connected to {port} at {baudRate} baud but no data received, trying next...");
                                    serialService.ForceDisconnect();
                                    connected = false;
                                }
                            }
                        }
                        
                        if (connected) break;
                    }
                    
                    if (connected)
                    {
                        timer.Start();
                        isMonitoring = true;
                        
                        StartButton.IsEnabled = false;
                        StopButton.IsEnabled = true;
                        
                        StatusText.Text = $"Connected to {connectedPort} - Monitoring started!";
                        sessionStartTime = DateTime.Now;
                        
                        Console.WriteLine($"Monitoring session started on {connectedPort}");
                    }
                    else
                    {
                        StatusText.Text = "Failed to connect to any COM port!";
                        MessageBox.Show($"Unable to connect to logger. Available ports: {string.Join(", ", availablePorts)}\n\nPlease check:\n1. Device connection\n2. CH340 driver installation\n3. Correct baud rate\n4. Device is sending data", 
                                       "Connection Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error starting monitoring: {ex.Message}";
                MessageBox.Show($"Error starting monitoring: {ex.Message}", "Error", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (isMonitoring)
                {
                    serialService.ForceDisconnect();
                    timer.Stop();
                    isMonitoring = false;
                    
                    StartButton.IsEnabled = true;
                    StopButton.IsEnabled = false;
                    
                    StatusText.Text = "Monitoring stopped. Data saved to collection.";
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error stopping monitoring: {ex.Message}";
                MessageBox.Show($"Error stopping monitoring: {ex.Message}", "Error", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Window Control Event Handlers
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                if (isMonitoring)
                {
                    serialService.ForceDisconnect();
                }
                
                timer?.Stop();
                clockTimer?.Stop();
                sessionTimer?.Stop();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during cleanup: {ex.Message}");
            }
            
            base.OnClosed(e);
        }

        // Enable window dragging for custom title bar
        protected override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            DragMove();
        }
    }
}