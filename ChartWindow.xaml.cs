using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using EngineMonitoring.Models;

namespace EngineMonitoring
{
    public partial class ChartWindow : Window
    {
        private ObservableCollection<SensorData> sensorDataCollection;
        private bool isChartPaused = false;

        public ChartWindow()
        {
            InitializeComponent();
            InitializeChart();
        }

        public ChartWindow(ObservableCollection<SensorData> data) : this()
        {
            try
            {
                if (data == null)
                {
                    MessageBox.Show("No data provided to Chart Window", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                    return;
                }
                
                sensorDataCollection = data;
                UpdateChart();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing Chart Window: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}", 
                               "Chart Window Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Console.WriteLine($"ChartWindow Constructor Error: {ex}");
            }
        }

        private void InitializeChart()
        {
            try
            {
#pragma warning disable CA1416
                MainChart.Plot.Title("Live Sensor Analytics - Full View");
                MainChart.Plot.XLabel("Data Points");
                MainChart.Plot.YLabel("Sensor Values");
                MainChart.Plot.Style(ScottPlot.Style.Black);
                MainChart.Refresh();
#pragma warning restore CA1416
            }
            catch (Exception ex)
            {
                ChartStatusText.Text = $"Chart init error: {ex.Message}";
            }
        }

        private void UpdateChart()
        {
            if (isChartPaused || sensorDataCollection == null) return;

            try
            {
                // Make sure we're on UI thread
                if (!Dispatcher.CheckAccess())
                {
                    Dispatcher.Invoke(() => UpdateChart());
                    return;
                }

                var data = sensorDataCollection.TakeLast(100).ToList();
                if (data.Count < 2) return;

                // Filter out invalid data (NaN, Infinity)
                var validData = data
                    .Where(d => !double.IsNaN(d.Torque) && !double.IsInfinity(d.Torque) &&
                               !double.IsNaN(d.Temperature) && !double.IsInfinity(d.Temperature) &&
                               !double.IsNaN(d.MAF) && !double.IsInfinity(d.MAF) &&
                               !double.IsNaN(d.Fuel) && !double.IsInfinity(d.Fuel))
                    .ToList();

                if (validData.Count < 2) return;

#pragma warning disable CA1416
                if (MainChart != null)
                {
                    MainChart.Plot.Clear();
                    
                    var timeData = Enumerable.Range(0, validData.Count).Select(i => (double)i).ToArray();
                    var torqueData = validData.Select(d => d.Torque).ToArray();
                    var fuelData = validData.Select(d => d.Fuel).ToArray();
                    var rpmData = validData.Select(d => d.RPM / 100.0).ToArray();
                    var tempData = validData.Select(d => d.Temperature).ToArray();
                    var mafData = validData.Select(d => d.MAF).ToArray();

                    MainChart.Plot.AddScatter(timeData, torqueData, System.Drawing.Color.FromArgb(0, 217, 255), label: "Torsi (Nm)");
                    MainChart.Plot.AddScatter(timeData, fuelData, System.Drawing.Color.FromArgb(218, 54, 51), label: "BBM (gram)");
                    MainChart.Plot.AddScatter(timeData, rpmData, System.Drawing.Color.FromArgb(255, 193, 7), label: "RPM (/100)");
                    MainChart.Plot.AddScatter(timeData, tempData, System.Drawing.Color.FromArgb(76, 175, 80), label: "Suhu (°C)");
                    MainChart.Plot.AddScatter(timeData, mafData, System.Drawing.Color.FromArgb(156, 39, 176), label: "MAF (m/s)");

                    MainChart.Plot.Legend(location: ScottPlot.Alignment.UpperRight);
                    MainChart.Refresh();
                }
#pragma warning restore CA1416
            }
            catch (Exception ex)
            {
                if (ChartStatusText != null)
                    ChartStatusText.Text = $"Chart update error: {ex.Message}";
                Console.WriteLine($"UpdateChart Error: {ex}");
            }
        }

        private void PauseChartButton_Click(object sender, RoutedEventArgs e)
        {
            isChartPaused = !isChartPaused;
            if (PauseChartButton != null)
                PauseChartButton.Content = isChartPaused ? "▶️ RESUME" : "⏸️ PAUSE";
            if (ChartStatusText != null)
                ChartStatusText.Text = isChartPaused ? "Chart paused" : "Real-time monitoring active";
        }

        private void ResetZoomButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
#pragma warning disable CA1416
                if (MainChart != null)
                {
                    MainChart.Plot.AxisAuto();
                    MainChart.Refresh();
                }
#pragma warning restore CA1416
                if (ChartStatusText != null)
                    ChartStatusText.Text = "Zoom reset";
            }
            catch (Exception ex)
            {
                if (ChartStatusText != null)
                    ChartStatusText.Text = $"Reset error: {ex.Message}";
            }
        }

        private void CloseWindow_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        public void RefreshChart()
        {
            UpdateChart();
        }
    }
}
