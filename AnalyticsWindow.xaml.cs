using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using EngineMonitoring.Models;
using ScottPlot;

namespace EngineMonitoring
{
    public partial class AnalyticsWindow : Window
    {
        private readonly ObservableCollection<SensorData> sensorDataCollection;
        private DispatcherTimer refreshTimer;
        private DispatcherTimer sessionTimer;
        private DateTime sessionStartTime;

        public AnalyticsWindow(ObservableCollection<SensorData> data)
        {
            try
            {
                InitializeComponent();
                
                if (data == null)
                {
                    MessageBox.Show("No data provided to Analytics Window", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                    return;
                }
                
                sensorDataCollection = data;
                sessionStartTime = DateTime.Now;
                
                InitializeChart();
                InitializeTimers();
                UpdateAnalytics();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing Analytics Window: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}", 
                               "Analytics Window Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Console.WriteLine($"AnalyticsWindow Constructor Error: {ex}");
            }
        }

        private void InitializeChart()
        {
            try
            {
                SensorChart.Plot.Palette = ScottPlot.Palette.Dark;
                SensorChart.Plot.YLabel("Sensor Values");
                SensorChart.Plot.XLabel("Time");
                SensorChart.Plot.Title("Real-Time Sensor Data Analytics");
                SensorChart.Refresh();
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Chart initialization error: {ex.Message}";
            }
        }

        private void InitializeTimers()
        {
            // Refresh timer for updating analytics
            refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            refreshTimer.Tick += (s, e) => UpdateAnalytics();
            refreshTimer.Start();

            // Session timer for tracking duration
            sessionTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            sessionTimer.Tick += (s, e) => UpdateSessionDuration();
            sessionTimer.Start();
        }

        private void UpdateSessionDuration()
        {
            var duration = DateTime.Now - sessionStartTime;
            SessionDurationText.Text = duration.ToString(@"hh\:mm\:ss");
        }

        private void UpdateAnalytics()
        {
            try
            {
                if (sensorDataCollection == null) return;
                
                // Make sure we're on UI thread
                if (!Dispatcher.CheckAccess())
                {
                    Dispatcher.Invoke(() => UpdateAnalytics());
                    return;
                }
                
                if (sensorDataCollection.Count > 0)
                {
                    // Filter out invalid data for calculations
                    var validTorque = sensorDataCollection.Where(d => !double.IsNaN(d.Torque) && !double.IsInfinity(d.Torque)).ToList();
                    var validRpm = sensorDataCollection.Where(d => d.RPM > 0).ToList();
                    var validMaf = sensorDataCollection.Where(d => !double.IsNaN(d.MAF) && !double.IsInfinity(d.MAF)).ToList();
                    var validFuel = sensorDataCollection.Where(d => !double.IsNaN(d.Fuel) && !double.IsInfinity(d.Fuel)).ToList();
                    var validTemp = sensorDataCollection.Where(d => !double.IsNaN(d.Temperature) && !double.IsInfinity(d.Temperature)).ToList();
                    
                    // Update analytics cards - check each control is not null
                    if (TotalRecordsText != null) TotalRecordsText.Text = sensorDataCollection.Count.ToString();
                    if (AvgTorqueText != null) AvgTorqueText.Text = validTorque.Count > 0 ? $"{validTorque.Average(d => d.Torque):F1} Nm" : "0.0 Nm";
                    if (MaxRpmText != null) MaxRpmText.Text = validRpm.Count > 0 ? validRpm.Max(d => d.RPM).ToString() : "0";
                    if (MaxMafText != null) MaxMafText.Text = validMaf.Count > 0 ? $"{validMaf.Max(d => d.MAF):F1} m/s" : "0.0 m/s";
                    if (AvgFuelText != null) AvgFuelText.Text = validFuel.Count > 0 ? $"{validFuel.Average(d => d.Fuel):F1} g" : "0.0 g";
                    if (MaxTempText != null) MaxTempText.Text = validTemp.Count > 0 ? $"{validTemp.Max(d => d.Temperature):F1}°C" : "0°C";
                    
                    // Update data points count
                    if (DataPointsCount != null) DataPointsCount.Text = sensorDataCollection.Count.ToString();
                    
                    UpdateChart();
                }
                else
                {
                    // Reset values when no data
                    if (TotalRecordsText != null) TotalRecordsText.Text = "0";
                    if (AvgTorqueText != null) AvgTorqueText.Text = "0.0 Nm";
                    if (MaxRpmText != null) MaxRpmText.Text = "0";
                    if (MaxMafText != null) MaxMafText.Text = "0.0 m/s";
                    if (AvgFuelText != null) AvgFuelText.Text = "0.0 g";
                    if (MaxTempText != null) MaxTempText.Text = "0°C";
                    if (DataPointsCount != null) DataPointsCount.Text = "0";
                }
            }
            catch (Exception ex)
            {
                if (StatusText != null)
                    StatusText.Text = $"Analytics update error: {ex.Message}";
                Console.WriteLine($"UpdateAnalytics Error: {ex}");
            }
        }

        private void UpdateChart()
        {
            try
            {
                if (sensorDataCollection == null || SensorChart == null) return;
                
                // Make sure we're on UI thread
                if (!Dispatcher.CheckAccess())
                {
                    Dispatcher.Invoke(() => UpdateChart());
                    return;
                }

#pragma warning disable CA1416
                SensorChart.Plot.Clear();

                if (sensorDataCollection.Count > 0)
                {
                    // Filter out invalid data (NaN, Infinity)
                    var validData = sensorDataCollection
                        .Where(d => !double.IsNaN(d.Torque) && !double.IsInfinity(d.Torque) &&
                                   !double.IsNaN(d.Temperature) && !double.IsInfinity(d.Temperature) &&
                                   !double.IsNaN(d.MAF) && !double.IsInfinity(d.MAF) &&
                                   !double.IsNaN(d.Fuel) && !double.IsInfinity(d.Fuel))
                        .ToArray();
                    
                    if (validData.Length > 1)
                    {
                        var timePoints = validData.Select((d, i) => (double)i).ToArray();
                        
                        // Plot different sensor values with valid data only
                        var torqueValues = validData.Select(d => d.Torque).ToArray();
                        var rpmValues = validData.Select(d => (double)d.RPM / 10.0).ToArray(); // Scale down RPM
                        var tempValues = validData.Select(d => d.Temperature).ToArray();
                        var mafValues = validData.Select(d => d.MAF * 10.0).ToArray(); // Scale up MAF
                        var fuelValues = validData.Select(d => d.Fuel / 10.0).ToArray(); // Scale down Fuel

                        SensorChart.Plot.AddScatter(timePoints, torqueValues, label: "Torque (Nm)", lineWidth: 2);
                        SensorChart.Plot.AddScatter(timePoints, rpmValues, label: "RPM (/10)", lineWidth: 2);
                        SensorChart.Plot.AddScatter(timePoints, tempValues, label: "Temperature (°C)", lineWidth: 2);
                        SensorChart.Plot.AddScatter(timePoints, mafValues, label: "MAF (×10 m/s)", lineWidth: 2);
                        SensorChart.Plot.AddScatter(timePoints, fuelValues, label: "Fuel (/10 g)", lineWidth: 2);

                        SensorChart.Plot.Legend();
                    }
                }

                SensorChart.Refresh();
#pragma warning restore CA1416
            }
            catch (Exception ex)
            {
                if (StatusText != null)
                    StatusText.Text = $"Chart update error: {ex.Message}";
                Console.WriteLine($"UpdateChart Error: {ex}");
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateAnalytics();
            StatusText.Text = "📊 Analytics refreshed successfully";
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "PNG Image|*.png|PDF Document|*.pdf",
                    DefaultExt = "png",
                    FileName = $"Analytics_Export_{DateTime.Now:yyyyMMdd_HHmmss}"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    if (saveDialog.FileName.EndsWith(".png"))
                    {
                        SensorChart.Plot.SaveFig(saveDialog.FileName);
                    }
                    else if (saveDialog.FileName.EndsWith(".pdf"))
                    {
                        SensorChart.Plot.SaveFig(saveDialog.FileName);
                    }
                    
                    StatusText.Text = $"📤 Analytics exported to {System.IO.Path.GetFileName(saveDialog.FileName)}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export error: {ex.Message}", "Export Error", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            refreshTimer?.Stop();
            sessionTimer?.Stop();
            this.Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                refreshTimer?.Stop();
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