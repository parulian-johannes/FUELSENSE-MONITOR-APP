using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using FuelsenseMonitorApp.Models;

namespace FuelsenseMonitorApp
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
            sensorDataCollection = data;
            UpdateChart();
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
                var data = sensorDataCollection.TakeLast(100).ToList();
                if (data.Count < 2) return;

#pragma warning disable CA1416
                MainChart.Plot.Clear();
                
                var timeData = Enumerable.Range(0, data.Count).Select(i => (double)i).ToArray();
                var torqueData = data.Select(d => d.Torque).ToArray();
                var fuelData = data.Select(d => d.Fuel).ToArray();
                var rpmData = data.Select(d => d.RPM / 100.0).ToArray();
                var tempData = data.Select(d => d.Temperature).ToArray();
                var mafData = data.Select(d => d.MAF).ToArray();

                MainChart.Plot.AddScatter(timeData, torqueData, System.Drawing.Color.FromArgb(0, 217, 255), label: "Torsi (Nm)");
                MainChart.Plot.AddScatter(timeData, fuelData, System.Drawing.Color.FromArgb(218, 54, 51), label: "BBM (gram)");
                MainChart.Plot.AddScatter(timeData, rpmData, System.Drawing.Color.FromArgb(253, 126, 20), label: "RPM (/100)");
                MainChart.Plot.AddScatter(timeData, tempData, System.Drawing.Color.FromArgb(31, 111, 235), label: "Temperature (°C)");
                MainChart.Plot.AddScatter(timeData, mafData, System.Drawing.Color.FromArgb(124, 58, 237), label: "MAF (g/s)");
                
                MainChart.Plot.Legend();
                MainChart.Refresh();
#pragma warning restore CA1416

                DataPointsText.Text = $"Data Points: {data.Count}";
            }
            catch (Exception ex)
            {
                ChartStatusText.Text = $"Chart update error: {ex.Message}";
            }
        }

        private void PauseChartButton_Click(object sender, RoutedEventArgs e)
        {
            isChartPaused = !isChartPaused;
            PauseChartButton.Content = isChartPaused ? "▶️ RESUME" : "⏸️ PAUSE";
            ChartStatusText.Text = isChartPaused ? "Chart paused" : "Real-time monitoring active";
        }

        private void ResetZoomButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
#pragma warning disable CA1416
                MainChart.Plot.AxisAuto();
                MainChart.Refresh();
#pragma warning restore CA1416
                ChartStatusText.Text = "Zoom reset";
            }
            catch (Exception ex)
            {
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
