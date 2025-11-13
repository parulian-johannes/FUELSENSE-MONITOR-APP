using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Globalization;
using Microsoft.Win32;
using EngineMonitoring.Models;
using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;

namespace EngineMonitoring.Services
{
    public class ExcelService
    {
        // Path to last exported Excel file (untuk append sheet kalibrasi)
        private static string LastExportedFile = string.Empty;

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
                    FileName = $"EngineMonitoringData_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    using var package = new ExcelPackage();
                    
                    // Set workbook properties untuk format Indonesia (koma desimal)
                    package.Workbook.Properties.Company = "Engine Monitoring";
                    
                    // Create worksheets
                    CreateDataSheet(package, data);
                    CreateSummarySheet(package, data);
                    CreateChartsSheet(package, data);
                    
                    // Save the file
                    var fileInfo = new FileInfo(saveFileDialog.FileName);
                    package.SaveAs(fileInfo);
                    
                    // Simpan path untuk nanti bisa ditambah sheet kalibrasi
                    LastExportedFile = saveFileDialog.FileName;
                    
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
            var dataList = data.OrderBy(d => d.Time).ToList();

            // Create headers dengan kolom Time di awal (sama seperti Charts & Visualization)
            worksheet.Cells[1, 1].Value = "Time";
            worksheet.Cells[1, 2].Value = "Torsi";
            worksheet.Cells[1, 3].Value = "BBM";
            worksheet.Cells[1, 4].Value = "RPM";
            worksheet.Cells[1, 5].Value = "Temperature";
            worksheet.Cells[1, 6].Value = "MAF";

            // Style headers - Professional dengan warna dan border (SAMA PERSIS seperti Charts sheet)
            using (var range = worksheet.Cells[1, 1, 1, 6])
            {
                range.Style.Font.Bold = true;
                range.Style.Font.Size = 11;
                range.Style.Font.Color.SetColor(System.Drawing.Color.White);
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(52, 73, 94)); // Dark Blue-Gray
                range.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                range.Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                range.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                range.Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                range.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                worksheet.Row(1).Height = 22;
            }

            // Add data - Format dengan KOMA desimal (WRITE AS TEXT dengan format Indonesia)
            var indonesiaCulture = new CultureInfo("id-ID"); // Indonesia culture: koma desimal, titik ribuan
            
            for (int i = 0; i < dataList.Count; i++)
            {
                var row = i + 2; // Data mulai dari row 2
                
                // Time - Format as DateTime
                worksheet.Cells[row, 1].Value = dataList[i].Time;
                worksheet.Cells[row, 1].Style.Numberformat.Format = "dd/mm/yyyy hh:mm:ss";
                
                // Torsi - WRITE AS TEXT dengan format koma
                if (!double.IsNaN(dataList[i].Torque))
                {
                    worksheet.Cells[row, 2].Value = dataList[i].Torque.ToString("N2", indonesiaCulture);
                }
                
                // BBM - WRITE AS TEXT dengan format koma
                if (!double.IsNaN(dataList[i].Fuel))
                {
                    worksheet.Cells[row, 3].Value = dataList[i].Fuel.ToString("N2", indonesiaCulture);
                }
                
                // RPM - integer
                if (dataList[i].RPM >= 0)
                {
                    worksheet.Cells[row, 4].Value = dataList[i].RPM;
                }
                
                // Temperature - WRITE AS TEXT dengan format koma
                if (!double.IsNaN(dataList[i].Temperature))
                {
                    worksheet.Cells[row, 5].Value = dataList[i].Temperature.ToString("N2", indonesiaCulture);
                }
                
                // MAF - WRITE AS TEXT dengan format koma
                if (!double.IsNaN(dataList[i].MAF))
                {
                    worksheet.Cells[row, 6].Value = dataList[i].MAF.ToString("N2", indonesiaCulture);
                }

                // Alternating row colors untuk readability
                if (i % 2 == 0)
                {
                    worksheet.Cells[row, 1, row, 6].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    worksheet.Cells[row, 1, row, 6].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(250, 250, 250));
                }

                // Add borders untuk setiap cell
                worksheet.Cells[row, 1, row, 6].Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                worksheet.Cells[row, 1, row, 6].Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                worksheet.Cells[row, 1, row, 6].Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                worksheet.Cells[row, 1, row, 6].Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
            }

            // Center align semua kolom angka (kecuali Time yang tetap center juga)
            if (dataList.Any())
            {
                var lastRow = dataList.Count + 1;
                worksheet.Cells[2, 1, lastRow, 6].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            }

            // Auto-fit columns
            worksheet.Cells.AutoFitColumns();
            
            // Set minimum column widths (dengan Time di kolom pertama)
            worksheet.Column(1).Width = Math.Max(worksheet.Column(1).Width, 18); // Time - lebih lebar
            worksheet.Column(2).Width = Math.Max(worksheet.Column(2).Width, 10); // Torsi
            worksheet.Column(3).Width = Math.Max(worksheet.Column(3).Width, 10); // BBM
            worksheet.Column(4).Width = Math.Max(worksheet.Column(4).Width, 10); // RPM
            worksheet.Column(5).Width = Math.Max(worksheet.Column(5).Width, 13); // Temperature
            worksheet.Column(6).Width = Math.Max(worksheet.Column(6).Width, 10); // MAF
        }

        private void CreateSummarySheet(ExcelPackage package, IEnumerable<SensorData> data)
        {
            var worksheet = package.Workbook.Worksheets.Add("Summary");
            var dataList = data.OrderBy(d => d.Time).ToList();

            if (!dataList.Any()) return;

            // Title dengan styling profesional
            worksheet.Cells[1, 1].Value = "RINGKASAN DATA MONITORING";
            worksheet.Cells[1, 1, 1, 6].Merge = true;
            worksheet.Cells[1, 1].Style.Font.Size = 20;
            worksheet.Cells[1, 1].Style.Font.Bold = true;
            worksheet.Cells[1, 1].Style.Font.Color.SetColor(System.Drawing.Color.White);
            worksheet.Cells[1, 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            worksheet.Cells[1, 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(76, 175, 80)); // Green
            worksheet.Cells[1, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            worksheet.Cells[1, 1].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
            worksheet.Row(1).Height = 35;

            // Export info dengan styling
            worksheet.Cells[3, 1].Value = "Tanggal Export:";
            worksheet.Cells[3, 1].Style.Font.Bold = true;
            worksheet.Cells[3, 2].Value = DateTime.Now;
            worksheet.Cells[3, 2].Style.Numberformat.Format = "dd/mm/yyyy hh:mm:ss";
            
            worksheet.Cells[4, 1].Value = "Total Data:";
            worksheet.Cells[4, 1].Style.Font.Bold = true;
            worksheet.Cells[4, 2].Value = dataList.Count;
            
            worksheet.Cells[5, 1].Value = "Durasi Monitoring:";
            worksheet.Cells[5, 1].Style.Font.Bold = true;
            var duration = dataList.Last().Time - dataList.First().Time;
            worksheet.Cells[5, 2].Value = duration.TotalMinutes >= 60
                ? $"{duration.TotalHours:F2} jam"
                : $"{duration.TotalMinutes:F1} menit";

            // Style info section
            worksheet.Cells[3, 1, 5, 2].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            worksheet.Cells[3, 1, 5, 2].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(240, 248, 255));
            worksheet.Cells[3, 1, 5, 2].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);

            // Statistics table header
            worksheet.Cells[7, 1].Value = "Parameter";
            worksheet.Cells[7, 2].Value = "Nilai Terakhir";
            worksheet.Cells[7, 3].Value = "Rata-Rata";
            worksheet.Cells[7, 4].Value = "Minimum";
            worksheet.Cells[7, 5].Value = "Maximum";
            worksheet.Cells[7, 6].Value = "Satuan";

            // Style statistics header - Professional dengan warna
            using (var range = worksheet.Cells[7, 1, 7, 6])
            {
                range.Style.Font.Bold = true;
                range.Style.Font.Size = 12;
                range.Style.Font.Color.SetColor(System.Drawing.Color.White);
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(52, 73, 94)); // Dark Blue-Gray
                range.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                range.Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Medium;
                range.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Medium;
                range.Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Medium;
                range.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Medium;
                worksheet.Row(7).Height = 25;
            }

            // Add statistics data with Indonesian terms (ignore missing values)
            var torqueVals = dataList.Where(d => !double.IsNaN(d.Torque)).Select(d => d.Torque).ToList();
            var fuelVals = dataList.Where(d => !double.IsNaN(d.Fuel)).Select(d => d.Fuel).ToList();
            var tempVals = dataList.Where(d => !double.IsNaN(d.Temperature)).Select(d => d.Temperature).ToList();
            var mafVals = dataList.Where(d => !double.IsNaN(d.MAF)).Select(d => d.MAF).ToList();
            // RPM >= 0 is valid (0 = engine off, -1 = no data)
            var rpmVals = dataList.Where(d => d.RPM >= 0).Select(d => d.RPM).ToList();

            var stats = new[]
            {
                new { Name = "⚡ Torsi", Current = torqueVals.Any() ? torqueVals.Last() : double.NaN, Avg = torqueVals.Any() ? torqueVals.Average() : double.NaN, Min = torqueVals.Any() ? torqueVals.Min() : double.NaN, Max = torqueVals.Any() ? torqueVals.Max() : double.NaN, Unit = "Nm", Color = System.Drawing.Color.FromArgb(255, 193, 7) },
                new { Name = "⛽ BBM", Current = fuelVals.Any() ? fuelVals.Last() : double.NaN, Avg = fuelVals.Any() ? fuelVals.Average() : double.NaN, Min = fuelVals.Any() ? fuelVals.Min() : double.NaN, Max = fuelVals.Any() ? fuelVals.Max() : double.NaN, Unit = "gram", Color = System.Drawing.Color.FromArgb(255, 152, 0) },
                new { Name = "🔄 RPM", Current = rpmVals.Any() ? (double)rpmVals.Last() : double.NaN, Avg = rpmVals.Any() ? rpmVals.Average() : double.NaN, Min = rpmVals.Any() ? (double)rpmVals.Min() : double.NaN, Max = rpmVals.Any() ? (double)rpmVals.Max() : double.NaN, Unit = "rpm", Color = System.Drawing.Color.FromArgb(33, 150, 243) },
                new { Name = "🌡️ Temperature", Current = tempVals.Any() ? tempVals.Last() : double.NaN, Avg = tempVals.Any() ? tempVals.Average() : double.NaN, Min = tempVals.Any() ? tempVals.Min() : double.NaN, Max = tempVals.Any() ? tempVals.Max() : double.NaN, Unit = "°C", Color = System.Drawing.Color.FromArgb(244, 67, 54) },
                new { Name = "💨 MAF", Current = mafVals.Any() ? mafVals.Last() : double.NaN, Avg = mafVals.Any() ? mafVals.Average() : double.NaN, Min = mafVals.Any() ? mafVals.Min() : double.NaN, Max = mafVals.Any() ? mafVals.Max() : double.NaN, Unit = "m/s", Color = System.Drawing.Color.FromArgb(156, 39, 176) }
            };

            for (int i = 0; i < stats.Length; i++)
            {
                var row = i + 8;
                
                // Parameter name dengan background color
                worksheet.Cells[row, 1].Value = stats[i].Name;
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                worksheet.Cells[row, 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[row, 1].Style.Fill.BackgroundColor.SetColor(stats[i].Color);
                worksheet.Cells[row, 1].Style.Font.Color.SetColor(System.Drawing.Color.White);
                
                // Values - WRITE AS TEXT dengan format Indonesia
                var indonesiaCulture = new CultureInfo("id-ID");
                worksheet.Cells[row, 2].Value = double.IsNaN(stats[i].Current) ? "" : stats[i].Current.ToString("N2", indonesiaCulture);
                worksheet.Cells[row, 3].Value = double.IsNaN(stats[i].Avg) ? "" : stats[i].Avg.ToString("N2", indonesiaCulture);
                worksheet.Cells[row, 4].Value = double.IsNaN(stats[i].Min) ? "" : stats[i].Min.ToString("N2", indonesiaCulture);
                worksheet.Cells[row, 5].Value = double.IsNaN(stats[i].Max) ? "" : stats[i].Max.ToString("N2", indonesiaCulture);
                worksheet.Cells[row, 6].Value = stats[i].Unit;
                
                // Center align numeric values
                worksheet.Cells[row, 2, row, 6].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                
                // Alternating row colors untuk data values
                if (i % 2 == 0)
                {
                    worksheet.Cells[row, 2, row, 6].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    worksheet.Cells[row, 2, row, 6].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(250, 250, 250));
                }

                // Add borders
                worksheet.Cells[row, 1, row, 6].Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                worksheet.Cells[row, 1, row, 6].Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                worksheet.Cells[row, 1, row, 6].Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                worksheet.Cells[row, 1, row, 6].Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
            }

            // Border around entire statistics table
            worksheet.Cells[7, 1, 12, 6].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Medium);

            // Auto-fit columns
            worksheet.Cells.AutoFitColumns();
            
            // Set minimum column widths
            worksheet.Column(1).Width = Math.Max(worksheet.Column(1).Width, 18);
            worksheet.Column(2).Width = Math.Max(worksheet.Column(2).Width, 14);
            worksheet.Column(3).Width = Math.Max(worksheet.Column(3).Width, 14);
            worksheet.Column(4).Width = Math.Max(worksheet.Column(4).Width, 12);
            worksheet.Column(5).Width = Math.Max(worksheet.Column(5).Width, 12);
            worksheet.Column(6).Width = Math.Max(worksheet.Column(6).Width, 10);
        }

        private void CreateChartsSheet(ExcelPackage package, IEnumerable<SensorData> data)
        {
            var worksheet = package.Workbook.Worksheets.Add("Charts & Visualization");
            // Order by time and take the most recent 50 points for charts
            var dataList = data.OrderBy(d => d.Time).ToList();
            if (dataList.Count > 50)
            {
                dataList = dataList.Skip(Math.Max(0, dataList.Count - 50)).ToList();
            }

            if (!dataList.Any()) return;

            try
            {
                // Title dengan styling profesional
                worksheet.Cells[1, 1].Value = "VISUALISASI DATA SENSOR";
                worksheet.Cells[1, 1, 1, 6].Merge = true;
                worksheet.Cells[1, 1].Style.Font.Size = 18;
                worksheet.Cells[1, 1].Style.Font.Bold = true;
                worksheet.Cells[1, 1].Style.Font.Color.SetColor(System.Drawing.Color.White);
                worksheet.Cells[1, 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[1, 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(156, 39, 176)); // Purple
                worksheet.Cells[1, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                worksheet.Cells[1, 1].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                worksheet.Row(1).Height = 30;

                // Create data table for charts (starting from row 3)
                worksheet.Cells[3, 1].Value = "Timestamp";
                worksheet.Cells[3, 2].Value = "Torsi";
                worksheet.Cells[3, 3].Value = "BBM";
                worksheet.Cells[3, 4].Value = "RPM";
                worksheet.Cells[3, 5].Value = "Temperature";
                worksheet.Cells[3, 6].Value = "MAF";

                // Style headers - Professional dengan warna dan border
                using (var range = worksheet.Cells[3, 1, 3, 6])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Font.Size = 11;
                    range.Style.Font.Color.SetColor(System.Drawing.Color.White);
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(52, 73, 94)); // Dark Blue-Gray
                    range.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                    range.Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    worksheet.Row(3).Height = 22;
                }

                // Add chart data (use actual timestamps for X-axis)
                for (int i = 0; i < dataList.Count; i++)
                {
                    var row = i + 4;
                    
                    // Timestamp
                    worksheet.Cells[row, 1].Value = dataList[i].Time;
                    worksheet.Cells[row, 1].Style.Numberformat.Format = "dd/mm/yy hh:mm:ss";
                    
                    // Torsi dengan format koma desimal
                    if (!double.IsNaN(dataList[i].Torque))
                    {
                        worksheet.Cells[row, 2].Value = dataList[i].Torque;
                        worksheet.Cells[row, 2].Style.Numberformat.Format = "#,##0.00";
                    }
                    
                    // BBM dengan format koma desimal
                    if (!double.IsNaN(dataList[i].Fuel))
                    {
                        worksheet.Cells[row, 3].Value = dataList[i].Fuel;
                        worksheet.Cells[row, 3].Style.Numberformat.Format = "#,##0.00";
                    }
                    
                    // RPM (tanpa desimal)
                    if (dataList[i].RPM >= 0)
                    {
                        worksheet.Cells[row, 4].Value = dataList[i].RPM;
                        worksheet.Cells[row, 4].Style.Numberformat.Format = "#,##0";
                    }
                    
                    // Temperature dengan format koma desimal
                    if (!double.IsNaN(dataList[i].Temperature))
                    {
                        worksheet.Cells[row, 5].Value = dataList[i].Temperature;
                        worksheet.Cells[row, 5].Style.Numberformat.Format = "#,##0.00";
                    }
                    
                    // MAF dengan format koma desimal
                    if (!double.IsNaN(dataList[i].MAF))
                    {
                        worksheet.Cells[row, 6].Value = dataList[i].MAF;
                        worksheet.Cells[row, 6].Style.Numberformat.Format = "#,##0.00";
                    }

                    // Alternating row colors untuk readability
                    if (i % 2 == 0)
                    {
                        worksheet.Cells[row, 1, row, 6].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        worksheet.Cells[row, 1, row, 6].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(250, 250, 250));
                    }

                    // Add borders untuk setiap cell
                    worksheet.Cells[row, 1, row, 6].Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    worksheet.Cells[row, 1, row, 6].Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    worksheet.Cells[row, 1, row, 6].Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    worksheet.Cells[row, 1, row, 6].Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                }

                var dataEndRow = 3 + dataList.Count;
                
                // Center align numeric columns
                if (dataList.Any())
                {
                    worksheet.Cells[4, 2, dataEndRow, 6].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                }
                
                // Auto-fit columns to show Time properly (prevent ######)
                worksheet.Column(1).AutoFit();
                worksheet.Column(2).AutoFit();
                worksheet.Column(3).AutoFit();
                worksheet.Column(4).AutoFit();
                worksheet.Column(5).AutoFit();
                worksheet.Column(6).AutoFit();
                
                // Set minimum column widths
                worksheet.Column(1).Width = Math.Max(worksheet.Column(1).Width, 18);
                worksheet.Column(2).Width = Math.Max(worksheet.Column(2).Width, 10);
                worksheet.Column(3).Width = Math.Max(worksheet.Column(3).Width, 10);
                worksheet.Column(4).Width = Math.Max(worksheet.Column(4).Width, 10);
                worksheet.Column(5).Width = Math.Max(worksheet.Column(5).Width, 13);
                worksheet.Column(6).Width = Math.Max(worksheet.Column(6).Width, 10);

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
                rpmSeries.Header = "RPM";

                var tempSeries = lineChart.Series.Add(worksheet.Cells[4, 5, dataEndRow, 5], worksheet.Cells[4, 1, dataEndRow, 1]);
                tempSeries.Header = "Temperature (°C)";

                var mafSeries = lineChart.Series.Add(worksheet.Cells[4, 6, dataEndRow, 6], worksheet.Cells[4, 1, dataEndRow, 1]);
                mafSeries.Header = "MAF (m/s)";

                // Style the line chart
                lineChart.Legend.Position = eLegendPosition.Bottom;
                lineChart.XAxis.Title.Text = "Time";
                // Set X-axis to display dates properly
                lineChart.XAxis.Format = "dd/mm/yy hh:mm";
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
                torqueChart.XAxis.Title.Text = "Time";
                torqueChart.XAxis.Format = "dd/mm/yy hh:mm";
                torqueChart.YAxis.Title.Text = "Torsi (Nm)";

                // Fuel Chart with Indonesian title
                var fuelChart = worksheet.Drawings.AddLineChart("FuelChart", eLineChartType.LineMarkers);
                fuelChart.Title.Text = "Konsumsi BBM";        // Changed from "Fuel Consumption"
                fuelChart.SetPosition(chartStartRow, 0, 6, 0);
                fuelChart.SetSize(380, 250);
                var fuelChartSeries = fuelChart.Series.Add(worksheet.Cells[4, 3, dataEndRow, 3], worksheet.Cells[4, 1, dataEndRow, 1]);
                fuelChartSeries.Header = "BBM (gram)";
                fuelChart.XAxis.Title.Text = "Time";
                fuelChart.XAxis.Format = "dd/mm/yy hh:mm";
                fuelChart.YAxis.Title.Text = "BBM (gram)";

                // RPM Chart
                var rpmChart = worksheet.Drawings.AddLineChart("RPMChart", eLineChartType.LineMarkers);
                rpmChart.Title.Text = "RPM Performance";
                rpmChart.SetPosition(chartStartRow + 15, 0, 1, 0);
                rpmChart.SetSize(380, 250);
                var rpmChartSeries = rpmChart.Series.Add(worksheet.Cells[4, 4, dataEndRow, 4], worksheet.Cells[4, 1, dataEndRow, 1]);
                rpmChartSeries.Header = "RPM";
                rpmChart.XAxis.Title.Text = "Time";
                rpmChart.XAxis.Format = "dd/mm/yy hh:mm";
                rpmChart.YAxis.Title.Text = "RPM";

                // Temperature Chart
                var tempChart = worksheet.Drawings.AddLineChart("TemperatureChart", eLineChartType.LineMarkers);
                tempChart.Title.Text = "Temperature Monitoring";
                tempChart.SetPosition(chartStartRow + 15, 0, 6, 0);
                tempChart.SetSize(380, 250);
                var tempChartSeries = tempChart.Series.Add(worksheet.Cells[4, 5, dataEndRow, 5], worksheet.Cells[4, 1, dataEndRow, 1]);
                tempChartSeries.Header = "Temperature (°C)";
                tempChart.XAxis.Title.Text = "Time";
                tempChart.XAxis.Format = "dd/mm/yy hh:mm";
                tempChart.YAxis.Title.Text = "Temperature (°C)";

                // MAF Chart
                var mafChart = worksheet.Drawings.AddLineChart("MAFChart", eLineChartType.LineMarkers);
                mafChart.Title.Text = "MAF Analysis";
                mafChart.SetPosition(chartStartRow + 30, 0, 1, 0);
                mafChart.SetSize(380, 250);
                var mafChartSeries = mafChart.Series.Add(worksheet.Cells[4, 6, dataEndRow, 6], worksheet.Cells[4, 1, dataEndRow, 1]);
                mafChartSeries.Header = "MAF (m/s)";
                mafChart.XAxis.Title.Text = "Time";
                mafChart.XAxis.Format = "dd/mm/yy hh:mm";
                mafChart.YAxis.Title.Text = "MAF (m/s)";

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

        // Method untuk menambahkan sheet kalibrasi ke file Excel yang sudah ada
        public static bool AddCalibrationSheet(string? excelFilePath = null)
        {
            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                // Jika tidak ada path, gunakan file terakhir yang di-export
                string targetFile = excelFilePath ?? LastExportedFile;

                // Jika masih belum ada file, buat file baru
                if (string.IsNullOrEmpty(targetFile) || !File.Exists(targetFile))
                {
                    var saveFileDialog = new SaveFileDialog
                    {
                        Filter = "Excel Files (*.xlsx)|*.xlsx",
                        DefaultExt = "xlsx",
                        FileName = $"Kalibrasi_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                    };

                    if (saveFileDialog.ShowDialog() == true)
                    {
                        targetFile = saveFileDialog.FileName;
                        
                        // Buat file Excel baru dengan sheet kalibrasi
                        using var newPackage = new ExcelPackage();
                        CreateCalibrationSheet(newPackage);
                        var fileInfo = new FileInfo(targetFile);
                        newPackage.SaveAs(fileInfo);
                        LastExportedFile = targetFile;
                        return true;
                    }
                    return false;
                }

                // Buka file Excel yang ada dan tambahkan sheet kalibrasi
                var existingFile = new FileInfo(targetFile);
                using var package = new ExcelPackage(existingFile);
                
                // Hapus sheet Kalibrasi jika sudah ada (update)
                var existingSheet = package.Workbook.Worksheets["Kalibrasi"];
                if (existingSheet != null)
                {
                    package.Workbook.Worksheets.Delete(existingSheet);
                }

                // Tambahkan sheet kalibrasi baru
                CreateCalibrationSheet(package);
                package.Save();
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding calibration sheet: {ex.Message}");
                return false;
            }
        }

        private static void CreateCalibrationSheet(ExcelPackage package)
        {
            var worksheet = package.Workbook.Worksheets.Add("Kalibrasi");

            // Title
            worksheet.Cells[1, 1].Value = "DATA KALIBRASI FUELSENSE MONITOR";
            worksheet.Cells[1, 1].Style.Font.Size = 16;
            worksheet.Cells[1, 1].Style.Font.Bold = true;
            worksheet.Cells[1, 1, 1, 4].Merge = true;

            // Timestamp
            worksheet.Cells[2, 1].Value = "Disimpan pada:";
            worksheet.Cells[2, 2].Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            worksheet.Cells[2, 2, 2, 4].Merge = true;

            // Section: KALIBRASI TORSI
            worksheet.Cells[4, 1].Value = "KALIBRASI TORSI";
            worksheet.Cells[4, 1].Style.Font.Size = 14;
            worksheet.Cells[4, 1].Style.Font.Bold = true;
            worksheet.Cells[4, 1].Style.Font.Color.SetColor(System.Drawing.Color.Blue);

            worksheet.Cells[5, 1].Value = "Parameter";
            worksheet.Cells[5, 2].Value = "Nilai";
            worksheet.Cells[5, 3].Value = "Satuan";
            worksheet.Cells[5, 4].Value = "Keterangan";
            worksheet.Cells[5, 1, 5, 4].Style.Font.Bold = true;
            worksheet.Cells[5, 1, 5, 4].Style.Fill.SetBackground(System.Drawing.Color.LightBlue);

            worksheet.Cells[6, 1].Value = "Torque Scale Factor";
            worksheet.Cells[6, 2].Value = SettingsStore.TorqueScaleFactor;
            worksheet.Cells[6, 3].Value = "Nm/raw";
            worksheet.Cells[6, 4].Value = "Faktor pengali untuk konversi raw ke Nm";

            worksheet.Cells[7, 1].Value = "Torque Offset";
            worksheet.Cells[7, 2].Value = SettingsStore.TorqueOffset;
            worksheet.Cells[7, 3].Value = "raw";
            worksheet.Cells[7, 4].Value = "Nilai offset nol beban";

            worksheet.Cells[8, 1].Value = "Arm Length";
            worksheet.Cells[8, 2].Value = SettingsStore.TorqueArmLength;
            worksheet.Cells[8, 3].Value = "m";
            worksheet.Cells[8, 4].Value = "Panjang lengan torsi (fixed: 0.305 m)";

            worksheet.Cells[9, 1].Value = "Torque Invert";
            worksheet.Cells[9, 2].Value = SettingsStore.TorqueInvert ? "Ya" : "Tidak";
            worksheet.Cells[9, 3].Value = "-";
            worksheet.Cells[9, 4].Value = "Arah torsi dibalik";

            worksheet.Cells[10, 1].Value = "Sensor Capacity";
            worksheet.Cells[10, 2].Value = SettingsStore.TorqueSensorCapacity;
            worksheet.Cells[10, 3].Value = "Nm";
            worksheet.Cells[10, 4].Value = "Kapasitas maksimal sensor torsi";

            worksheet.Cells[11, 1].Value = "Input Is Raw";
            worksheet.Cells[11, 2].Value = SettingsStore.TorqueInputIsRaw ? "Ya" : "Tidak";
            worksheet.Cells[11, 3].Value = "-";
            worksheet.Cells[11, 4].Value = "Data masukan dalam bentuk raw (perlu kalibrasi)";

            // Section: KALIBRASI TIMBANGAN
            worksheet.Cells[13, 1].Value = "KALIBRASI TIMBANGAN (LOAD CELL)";
            worksheet.Cells[13, 1].Style.Font.Size = 14;
            worksheet.Cells[13, 1].Style.Font.Bold = true;
            worksheet.Cells[13, 1].Style.Font.Color.SetColor(System.Drawing.Color.Green);

            worksheet.Cells[14, 1].Value = "Parameter";
            worksheet.Cells[14, 2].Value = "Nilai";
            worksheet.Cells[14, 3].Value = "Satuan";
            worksheet.Cells[14, 4].Value = "Keterangan";
            worksheet.Cells[14, 1, 14, 4].Style.Font.Bold = true;
            worksheet.Cells[14, 1, 14, 4].Style.Fill.SetBackground(System.Drawing.Color.LightGreen);

            worksheet.Cells[15, 1].Value = "Scale Offset";
            worksheet.Cells[15, 2].Value = SettingsStore.ScaleOffset;
            worksheet.Cells[15, 3].Value = "raw";
            worksheet.Cells[15, 4].Value = "Nilai raw tanpa beban (zero calibration)";

            worksheet.Cells[16, 1].Value = "Scale Factor";
            worksheet.Cells[16, 2].Value = SettingsStore.ScaleFactor;
            worksheet.Cells[16, 3].Value = "raw/gram";
            worksheet.Cells[16, 4].Value = "Faktor kalibrasi untuk konversi raw ke gram";

            // Format angka dengan koma desimal (format Indonesia eksplisit)
            worksheet.Cells[6, 2].Style.Numberformat.Format = "0,000000";
            worksheet.Cells[7, 2].Style.Numberformat.Format = "0,000";
            worksheet.Cells[8, 2].Style.Numberformat.Format = "0,000";
            worksheet.Cells[10, 2].Style.Numberformat.Format = "0,0";
            worksheet.Cells[15, 2].Style.Numberformat.Format = "0,000";
            worksheet.Cells[16, 2].Style.Numberformat.Format = "0,000000";

            // Formula section
            worksheet.Cells[18, 1].Value = "RUMUS PERHITUNGAN";
            worksheet.Cells[18, 1].Style.Font.Size = 14;
            worksheet.Cells[18, 1].Style.Font.Bold = true;
            worksheet.Cells[18, 1].Style.Font.Color.SetColor(System.Drawing.Color.DarkOrange);

            worksheet.Cells[19, 1].Value = "Torsi Terkalibrasi:";
            worksheet.Cells[19, 2].Value = "(raw - offset) × scale_factor";
            worksheet.Cells[19, 2, 19, 4].Merge = true;

            worksheet.Cells[20, 1].Value = "Berat Terkalibrasi:";
            worksheet.Cells[20, 2].Value = "(raw - offset) / scale_factor";
            worksheet.Cells[20, 2, 20, 4].Merge = true;

            // Auto-fit columns
            worksheet.Cells.AutoFitColumns();
            worksheet.Column(4).Width = 45; // Keterangan column lebih lebar
        }
    }
}
