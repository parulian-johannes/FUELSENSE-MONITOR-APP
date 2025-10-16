using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using FuelsenseMonitorApp.Models;
using ScottPlot;

namespace FuelsenseMonitorApp
{
    public partial class AnalyticsWindow : Window
    {
        private readonly ObservableCollection<SensorData> sensorDataCollection;
        private DispatcherTimer refreshTimer;
        private DispatcherTimer sessionTimer;
        private DateTime sessionStartTime;

        public AnalyticsWindow(ObservableCollection<SensorData> data)
        {
            InitializeComponent();
            sensorDataCollection = data;
            sessionStartTime = DateTime.Now;
            
            InitializeChart();
            InitializeTimers();
            UpdateAnalytics();
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
                if (sensorDataCollection.Count > 0)
                {
                    // Update analytics cards
                    TotalRecordsText.Text = sensorDataCollection.Count.ToString();
                    AvgTorqueText.Text = $"{sensorDataCollection.Average(d => d.Torque):F1} Nm";
                    MaxRpmText.Text = sensorDataCollection.Max(d => d.RPM).ToString();
                    MaxMafText.Text = $"{sensorDataCollection.Max(d => d.MAF):F1} g/s";
                    AvgFuelText.Text = $"{sensorDataCollection.Average(d => d.Fuel):F1} g";
                    MaxTempText.Text = $"{sensorDataCollection.Max(d => d.Temperature):F1}°C";
                    
                    // Update data points count
                    DataPointsCount.Text = sensorDataCollection.Count.ToString();
                    
                    UpdateChart();
                }
                else
                {
                    // Reset values when no data
                    TotalRecordsText.Text = "0";
                    AvgTorqueText.Text = "0.0 Nm";
                    MaxRpmText.Text = "0";
                    MaxMafText.Text = "0.0 g/s";
                    AvgFuelText.Text = "0.0 g";
                    MaxTempText.Text = "0°C";
                    DataPointsCount.Text = "0";
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Analytics update error: {ex.Message}";
            }
        }

        private void UpdateChart()
        {
            try
            {
                SensorChart.Plot.Clear();

                if (sensorDataCollection.Count > 0)
                {
                    var dataPoints = sensorDataCollection.ToArray();
                    var timePoints = dataPoints.Select((d, i) => (double)i).ToArray();
                    
                    // Plot different sensor values
                    var torqueValues = dataPoints.Select(d => d.Torque).ToArray();
                    var rpmValues = dataPoints.Select(d => (double)d.RPM / 10.0).ToArray(); // Scale down RPM
                    var tempValues = dataPoints.Select(d => d.Temperature).ToArray();
                    var mafValues = dataPoints.Select(d => d.MAF * 10.0).ToArray(); // Scale up MAF
                    var fuelValues = dataPoints.Select(d => d.Fuel / 10.0).ToArray(); // Scale down Fuel

                    SensorChart.Plot.AddScatter(timePoints, torqueValues, label: "Torque (Nm)", lineWidth: 2);
                    SensorChart.Plot.AddScatter(timePoints, rpmValues, label: "RPM (/10)", lineWidth: 2);
                    SensorChart.Plot.AddScatter(timePoints, tempValues, label: "Temperature (°C)", lineWidth: 2);
                    SensorChart.Plot.AddScatter(timePoints, mafValues, label: "MAF (×10 g/s)", lineWidth: 2);
                    SensorChart.Plot.AddScatter(timePoints, fuelValues, label: "Fuel (/10 g)", lineWidth: 2);

                    SensorChart.Plot.Legend();
                }

                SensorChart.Refresh();
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Chart update error: {ex.Message}";
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