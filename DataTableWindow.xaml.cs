using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using EngineMonitoring.Models;
using EngineMonitoring.Services;

namespace EngineMonitoring
{
    public partial class DataTableWindow : Window
    {
        private ObservableCollection<SensorData> sensorDataCollection;
        private ExcelService excelService = new();

        public DataTableWindow()
        {
            InitializeComponent();
        }

        public DataTableWindow(ObservableCollection<SensorData> data) : this()
        {
            try
            {
                if (data == null)
                {
                    MessageBox.Show("No data provided to Data Table Window", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                    return;
                }
                
                sensorDataCollection = data;
                MainDataGrid.ItemsSource = sensorDataCollection;
                UpdateStats();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing Data Table Window: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}", 
                               "Data Table Window Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Console.WriteLine($"DataTableWindow Constructor Error: {ex}");
            }
        }

        private void UpdateStats()
        {
            if (sensorDataCollection?.Any() == true)
            {
                TotalRecordsCount.Text = sensorDataCollection.Count.ToString();
                // Compute stats ignoring missing values (NaN or RPM <= 0)
                var torqueVals = sensorDataCollection.Where(d => !double.IsNaN(d.Torque)).Select(d => d.Torque).ToList();
                var fuelVals = sensorDataCollection.Where(d => !double.IsNaN(d.Fuel)).Select(d => d.Fuel).ToList();
                var tempVals = sensorDataCollection.Where(d => !double.IsNaN(d.Temperature)).Select(d => d.Temperature).ToList();
                var rpmVals = sensorDataCollection.Where(d => d.RPM > 0).Select(d => d.RPM).ToList();

                var ci = new System.Globalization.CultureInfo("id-ID");
                ci.NumberFormat.NumberGroupSeparator = ""; // ensure no thousands separator

                AvgTorqueValue.Text = torqueVals.Any() ? $"{torqueVals.Average().ToString("F2", ci)} Nm" : "-";
                AvgFuelValue.Text = fuelVals.Any() ? $"{fuelVals.Average().ToString("F2", ci)} g" : "-";
                MaxRpmValue.Text = rpmVals.Any() ? rpmVals.Max().ToString() : "-";
                MaxTempValue.Text = tempVals.Any() ? $"{tempVals.Max().ToString("F1", ci)}°C" : "-";
                RecordCountText.Text = $"({sensorDataCollection.Count} records)";
            }
        }

        private void RefreshTableButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateStats();
            MainDataGrid.Items.Refresh();
        }

        private void ExportTableButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sensorDataCollection?.Count == 0)
                {
                    MessageBox.Show("No data to export.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (excelService.ExportToExcel(sensorDataCollection))
                {
                    MessageBox.Show("Data exported successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Export failed.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseWindow_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        public void RefreshData()
        {
            UpdateStats();
            MainDataGrid.Items.Refresh();
        }
    }
}
