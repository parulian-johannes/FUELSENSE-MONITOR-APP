using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Microsoft.Win32;
using FuelsenseMonitorApp.Models;
using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;

namespace FuelsenseMonitorApp.Services
{
    public class ExcelService
    {
        public bool ExportToExcel(IEnumerable<SensorData> data)
        {
            try
            {
                // Set EPPlus license context
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel Files (*.xlsx)|*.xlsx",
                    DefaultExt = "xlsx",
                    FileName = $"FuelsenseData_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    using var package = new ExcelPackage();
                    
                    // Create worksheets
                    CreateDataSheet(package, data);
                    CreateSummarySheet(package, data);
                    CreateChartsSheet(package, data);
                    
                    // Save the file
                    var fileInfo = new FileInfo(saveFileDialog.FileName);
                    package.SaveAs(fileInfo);
                    
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Export error: {ex.Message}");
                return false;
            }
        }

        private void CreateDataSheet(ExcelPackage package, IEnumerable<SensorData> data)
        {
            var worksheet = package.Workbook.Worksheets.Add("Sensor Data");
            var dataList = data.ToList();

            // Create headers with Indonesian terms
            worksheet.Cells[1, 1].Value = "Timestamp";
            worksheet.Cells[1, 2].Value = "Torsi (Nm)";        // Changed from "Torque"
            worksheet.Cells[1, 3].Value = "BBM (gram)";        // Changed from "Fuel"
            worksheet.Cells[1, 4].Value = "RPM";
            worksheet.Cells[1, 5].Value = "Temperature (°C)";
            worksheet.Cells[1, 6].Value = "MAF (g/s)";

            // Make headers bold
            using (var range = worksheet.Cells[1, 1, 1, 6])
            {
                range.Style.Font.Bold = true;
            }

            // Add data
            for (int i = 0; i < dataList.Count; i++)
            {
                var row = i + 2;
                worksheet.Cells[row, 1].Value = dataList[i].Time.ToString("yyyy-MM-dd HH:mm:ss");
                worksheet.Cells[row, 2].Value = dataList[i].Torque;
                worksheet.Cells[row, 3].Value = dataList[i].Fuel;
                worksheet.Cells[row, 4].Value = dataList[i].RPM;
                worksheet.Cells[row, 5].Value = dataList[i].Temperature;
                worksheet.Cells[row, 6].Value = dataList[i].MAF;
            }

            // Auto-fit columns
            worksheet.Cells.AutoFitColumns();
        }

        private void CreateSummarySheet(ExcelPackage package, IEnumerable<SensorData> data)
        {
            var worksheet = package.Workbook.Worksheets.Add("Summary");
            var dataList = data.ToList();

            if (!dataList.Any()) return;

            // Title
            worksheet.Cells[1, 1].Value = "FUELSENSE MONITOR - DATA SUMMARY";
            worksheet.Cells[1, 1].Style.Font.Size = 16;
            worksheet.Cells[1, 1].Style.Font.Bold = true;

            // Export info
            worksheet.Cells[3, 1].Value = "Export Date:";
            worksheet.Cells[3, 2].Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            worksheet.Cells[4, 1].Value = "Total Records:";
            worksheet.Cells[4, 2].Value = dataList.Count;
            worksheet.Cells[5, 1].Value = "Duration:";
            worksheet.Cells[5, 2].Value = $"{(dataList.Last().Time - dataList.First().Time).TotalMinutes:F1} minutes";

            // Statistics table
            worksheet.Cells[7, 1].Value = "Parameter";
            worksheet.Cells[7, 2].Value = "Current";
            worksheet.Cells[7, 3].Value = "Average";
            worksheet.Cells[7, 4].Value = "Minimum";
            worksheet.Cells[7, 5].Value = "Maximum";
            worksheet.Cells[7, 6].Value = "Unit";

            // Make statistics header bold
            using (var range = worksheet.Cells[7, 1, 7, 6])
            {
                range.Style.Font.Bold = true;
            }

            // Add statistics data with Indonesian terms
            var stats = new[]
            {
                new { Name = "Torsi", Current = dataList.Last().Torque, Avg = dataList.Average(d => d.Torque), Min = dataList.Min(d => d.Torque), Max = dataList.Max(d => d.Torque), Unit = "Nm" },     // Changed from "Torque"
                new { Name = "BBM", Current = dataList.Last().Fuel, Avg = dataList.Average(d => d.Fuel), Min = dataList.Min(d => d.Fuel), Max = dataList.Max(d => d.Fuel), Unit = "gram" },              // Changed from "Fuel"
                new { Name = "RPM", Current = (double)dataList.Last().RPM, Avg = dataList.Average(d => (double)d.RPM), Min = (double)dataList.Min(d => d.RPM), Max = (double)dataList.Max(d => d.RPM), Unit = "rpm" },
                new { Name = "Temperature", Current = dataList.Last().Temperature, Avg = dataList.Average(d => d.Temperature), Min = dataList.Min(d => d.Temperature), Max = dataList.Max(d => d.Temperature), Unit = "°C" },
                new { Name = "MAF", Current = dataList.Last().MAF, Avg = dataList.Average(d => d.MAF), Min = dataList.Min(d => d.MAF), Max = dataList.Max(d => d.MAF), Unit = "g/s" }
            };

            for (int i = 0; i < stats.Length; i++)
            {
                var row = i + 8;
                worksheet.Cells[row, 1].Value = stats[i].Name;
                worksheet.Cells[row, 2].Value = stats[i].Current;
                worksheet.Cells[row, 3].Value = stats[i].Avg;
                worksheet.Cells[row, 4].Value = stats[i].Min;
                worksheet.Cells[row, 5].Value = stats[i].Max;
                worksheet.Cells[row, 6].Value = stats[i].Unit;

                // Format numbers
                worksheet.Cells[row, 2, row, 5].Style.Numberformat.Format = "0.00";
                worksheet.Cells[row, 1].Style.Font.Bold = true;
            }

            // Auto-fit columns
            worksheet.Cells.AutoFitColumns();
        }

        private void CreateChartsSheet(ExcelPackage package, IEnumerable<SensorData> data)
        {
            var worksheet = package.Workbook.Worksheets.Add("Charts & Visualization");
            var dataList = data.Take(50).ToList(); // Limit to last 50 points for better chart readability

            if (!dataList.Any()) return;

            try
            {
                // Title
                worksheet.Cells[1, 1].Value = "SENSOR DATA VISUALIZATION";
                worksheet.Cells[1, 1].Style.Font.Size = 18;
                worksheet.Cells[1, 1].Style.Font.Bold = true;

                // Create data table for charts (starting from row 3)
                worksheet.Cells[3, 1].Value = "Time";
                worksheet.Cells[3, 2].Value = "Torsi";             // Changed from "Torque"
                worksheet.Cells[3, 3].Value = "BBM";               // Changed from "Fuel"
                worksheet.Cells[3, 4].Value = "RPM";
                worksheet.Cells[3, 5].Value = "Temperature";
                worksheet.Cells[3, 6].Value = "MAF";

                // Make headers bold
                using (var range = worksheet.Cells[3, 1, 3, 6])
                {
                    range.Style.Font.Bold = true;
                }

                // Add chart data
                for (int i = 0; i < dataList.Count; i++)
                {
                    var row = i + 4;
                    worksheet.Cells[row, 1].Value = i + 1; // Use sequence number for X-axis
                    worksheet.Cells[row, 2].Value = dataList[i].Torque;
                    worksheet.Cells[row, 3].Value = dataList[i].Fuel;
                    worksheet.Cells[row, 4].Value = dataList[i].RPM / 100.0; // Scale RPM for better visualization
                    worksheet.Cells[row, 5].Value = dataList[i].Temperature;
                    worksheet.Cells[row, 6].Value = dataList[i].MAF;
                }

                var dataEndRow = 3 + dataList.Count;

                // Create Line Chart for All Sensors
                var lineChart = worksheet.Drawings.AddLineChart("AllSensorsChart", eLineChartType.Line);
                lineChart.Title.Text = "Real-time Sensor Data Trends";
                lineChart.SetPosition(dataEndRow + 2, 0, 1, 0);
                lineChart.SetSize(800, 400);

                // Add series for each sensor with Indonesian labels
                var torqueSeries = lineChart.Series.Add(worksheet.Cells[4, 2, dataEndRow, 2], worksheet.Cells[4, 1, dataEndRow, 1]);
                torqueSeries.Header = "Torsi (Nm)";           // Changed from "Torque"

                var fuelSeries = lineChart.Series.Add(worksheet.Cells[4, 3, dataEndRow, 3], worksheet.Cells[4, 1, dataEndRow, 1]);
                fuelSeries.Header = "BBM (gram)";            // Changed from "Fuel"

                var rpmSeries = lineChart.Series.Add(worksheet.Cells[4, 4, dataEndRow, 4], worksheet.Cells[4, 1, dataEndRow, 1]);
                rpmSeries.Header = "RPM (/100)";

                var tempSeries = lineChart.Series.Add(worksheet.Cells[4, 5, dataEndRow, 5], worksheet.Cells[4, 1, dataEndRow, 1]);
                tempSeries.Header = "Temperature (°C)";

                var mafSeries = lineChart.Series.Add(worksheet.Cells[4, 6, dataEndRow, 6], worksheet.Cells[4, 1, dataEndRow, 1]);
                mafSeries.Header = "MAF (g/s)";

                // Style the line chart
                lineChart.Legend.Position = eLegendPosition.Bottom;
                lineChart.XAxis.Title.Text = "Data Points";
                lineChart.YAxis.Title.Text = "Sensor Values";

                // Create separate charts for better visualization
                var chartStartRow = dataEndRow + 25; // Position below the first chart

                // Torque Chart with Indonesian title
                var torqueChart = worksheet.Drawings.AddLineChart("TorqueChart", eLineChartType.LineMarkers);
                torqueChart.Title.Text = "Analisis Torsi";    // Changed from "Torque Analysis"
                torqueChart.SetPosition(chartStartRow, 0, 1, 0);
                torqueChart.SetSize(380, 250);
                var torqueChartSeries = torqueChart.Series.Add(worksheet.Cells[4, 2, dataEndRow, 2], worksheet.Cells[4, 1, dataEndRow, 1]);
                torqueChartSeries.Header = "Torsi (Nm)";
                torqueChart.XAxis.Title.Text = "Data Points";
                torqueChart.YAxis.Title.Text = "Torsi (Nm)";

                // Fuel Chart with Indonesian title
                var fuelChart = worksheet.Drawings.AddLineChart("FuelChart", eLineChartType.LineMarkers);
                fuelChart.Title.Text = "Konsumsi BBM";        // Changed from "Fuel Consumption"
                fuelChart.SetPosition(chartStartRow, 0, 6, 0);
                fuelChart.SetSize(380, 250);
                var fuelChartSeries = fuelChart.Series.Add(worksheet.Cells[4, 3, dataEndRow, 3], worksheet.Cells[4, 1, dataEndRow, 1]);
                fuelChartSeries.Header = "BBM (gram)";
                fuelChart.XAxis.Title.Text = "Data Points";
                fuelChart.YAxis.Title.Text = "BBM (gram)";

                // RPM Chart
                var rpmChart = worksheet.Drawings.AddLineChart("RPMChart", eLineChartType.LineMarkers);
                rpmChart.Title.Text = "RPM Performance";
                rpmChart.SetPosition(chartStartRow + 15, 0, 1, 0);
                rpmChart.SetSize(380, 250);
                var rpmChartSeries = rpmChart.Series.Add(worksheet.Cells[4, 4, dataEndRow, 4], worksheet.Cells[4, 1, dataEndRow, 1]);
                rpmChartSeries.Header = "RPM (/100)";
                rpmChart.XAxis.Title.Text = "Data Points";
                rpmChart.YAxis.Title.Text = "RPM (/100)";

                // Temperature Chart
                var tempChart = worksheet.Drawings.AddLineChart("TemperatureChart", eLineChartType.LineMarkers);
                tempChart.Title.Text = "Temperature Monitoring";
                tempChart.SetPosition(chartStartRow + 15, 0, 6, 0);
                tempChart.SetSize(380, 250);
                var tempChartSeries = tempChart.Series.Add(worksheet.Cells[4, 5, dataEndRow, 5], worksheet.Cells[4, 1, dataEndRow, 1]);
                tempChartSeries.Header = "Temperature (°C)";
                tempChart.XAxis.Title.Text = "Data Points";
                tempChart.YAxis.Title.Text = "Temperature (°C)";

                // MAF Chart
                var mafChart = worksheet.Drawings.AddLineChart("MAFChart", eLineChartType.LineMarkers);
                mafChart.Title.Text = "MAF Analysis";
                mafChart.SetPosition(chartStartRow + 30, 0, 1, 0);
                mafChart.SetSize(380, 250);
                var mafChartSeries = mafChart.Series.Add(worksheet.Cells[4, 6, dataEndRow, 6], worksheet.Cells[4, 1, dataEndRow, 1]);
                mafChartSeries.Header = "MAF (g/s)";
                mafChart.XAxis.Title.Text = "Data Points";
                mafChart.YAxis.Title.Text = "MAF (g/s)";

                // Add analysis notes with Indonesian terms
                worksheet.Cells[chartStartRow + 30, 6].Value = "CATATAN ANALISIS:";
                worksheet.Cells[chartStartRow + 30, 6].Style.Font.Bold = true;
                worksheet.Cells[chartStartRow + 31, 6].Value = $"• Torsi Puncak: {dataList.Max(d => d.Torque):F2} Nm";     // Changed from "Peak Torque"
                worksheet.Cells[chartStartRow + 32, 6].Value = $"• RPM Puncak: {dataList.Max(d => d.RPM)} rpm";
                worksheet.Cells[chartStartRow + 33, 6].Value = $"• Temperatur Maks: {dataList.Max(d => d.Temperature):F1} °C";
                worksheet.Cells[chartStartRow + 34, 6].Value = $"• Rata-rata BBM: {dataList.Average(d => d.Fuel):F2} gram";  // Changed from "Avg Fuel Rate"
                worksheet.Cells[chartStartRow + 35, 6].Value = $"• Jumlah Data: {dataList.Count}";

                Console.WriteLine("Charts created successfully in Excel file");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chart creation error: {ex.Message}");
                // Add error message to worksheet if chart creation fails
                worksheet.Cells[5, 1].Value = $"Chart creation error: {ex.Message}";
                worksheet.Cells[6, 1].Value = "Data table is still available below:";
            }
        }
    }
}
