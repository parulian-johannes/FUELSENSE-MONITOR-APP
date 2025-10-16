using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using FuelsenseMonitorApp.Models;
using FuelsenseMonitorApp.Services;

namespace FuelsenseMonitorApp
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
            sensorDataCollection = data;
            MainDataGrid.ItemsSource = sensorDataCollection;
            UpdateStats();
        }

        private void UpdateStats()
        {
            if (sensorDataCollection?.Any() == true)
            {
                TotalRecordsCount.Text = sensorDataCollection.Count.ToString();
                AvgTorqueValue.Text = $"{sensorDataCollection.Average(d => d.Torque):F2} Nm";
                AvgFuelValue.Text = $"{sensorDataCollection.Average(d => d.Fuel):F2} g";
                MaxRpmValue.Text = sensorDataCollection.Max(d => d.RPM).ToString();
                MaxTempValue.Text = $"{sensorDataCollection.Max(d => d.Temperature):F1}°C";
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
