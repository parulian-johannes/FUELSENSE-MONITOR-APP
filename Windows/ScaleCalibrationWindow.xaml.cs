using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using EngineMonitoring.Services;
using EngineMonitoring.Models;

namespace EngineMonitoring
{
    public partial class ScaleCalibrationWindow : Window
    {
        private readonly SerialService serialService;

        public ScaleCalibrationWindow(SerialService service)
        {
            InitializeComponent();
            serialService = service ?? throw new ArgumentNullException(nameof(service));

            // initialize display from store dengan koma desimal
            var ci = System.Globalization.CultureInfo.InvariantCulture;
            OffsetText.Text = SettingsStore.ScaleOffset.ToString("F3", ci).Replace(".", ",");
            CalFactorText.Text = SettingsStore.ScaleFactor.ToString("F6", ci).Replace(".", ",");

            try
            {
                serialService.DataReceived += SerialService_DataReceived;
            }
            catch { }
        }

        private void SerialService_DataReceived(object? sender, Models.SensorData data)
        {
            // Determine raw load reading from SensorData. Prefer Fuel, then Torque, then MAF.
            double raw = GetRawLoadValue(data);
            var ci = System.Globalization.CultureInfo.InvariantCulture;
            Dispatcher.Invoke(() => {
                LatestRawText.Text = raw.ToString("F3", ci).Replace(".", ",");

                // Real-time calculation using stored calibration
                if (SettingsStore.ScaleFactor > 0)
                {
                    double weight = (raw - SettingsStore.ScaleOffset) / SettingsStore.ScaleFactor;
                    CalibratedWeightText.Text = weight.ToString("F2", ci).Replace(".", ",");
                }
                else
                {
                    CalibratedWeightText.Text = "-";
                }
            });
        }

        private double GetRawLoadValue(Models.SensorData data)
        {
            if (data == null) return 0.0;
            // prefer raw numeric fields likely to hold loadcell ADC: Fuel, Torque, MAF
            if (data.Fuel != 0) return data.Fuel;
            if (data.Torque != 0) return data.Torque;
            if (data.MAF != 0) return data.MAF;
            // fallback: try temperature or RPM as last resort (unlikely)
            if (data.Temperature != 0) return data.Temperature;
            if (data.RPM != 0) return data.RPM;
            return 0.0;
        }

        private void ZeroButton_Click(object sender, RoutedEventArgs e)
        {
            if (!serialService.IsConnected)
            {
                StatusText.Text = "Perhatian: serial tidak tersambung. Data mungkin tidak masuk.";
                return;
            }

            // Langsung capture nilai saat ini sebagai offset
            double currentRaw = 0;
            var ci = System.Globalization.CultureInfo.InvariantCulture;
            if (double.TryParse(LatestRawText.Text.Replace(",", "."), System.Globalization.NumberStyles.Float, ci, out currentRaw))
            {
                SettingsStore.ScaleOffset = currentRaw;
                OffsetText.Text = SettingsStore.ScaleOffset.ToString("F3", ci).Replace(".", ",");
                StatusText.Text = $"Zero tersimpan: {currentRaw.ToString("F3", ci).Replace(".", ",")}";
            }
            else
            {
                StatusText.Text = "Belum ada data raw. Tunggu data masuk.";
            }
        }

        private void CaptureButton_Click(object sender, RoutedEventArgs e)
        {
            if (!serialService.IsConnected)
            {
                StatusText.Text = "Perhatian: serial tidak tersambung. Pastikan perangkat terhubung untuk capture.";
                return;
            }

            // Langsung capture dan hitung kalibrasi tanpa input berat pembanding
            var ci = System.Globalization.CultureInfo.InvariantCulture;
            if (!double.TryParse(LatestRawText.Text.Replace(",", "."), System.Globalization.NumberStyles.Float, ci, out double currentRaw))
            {
                StatusText.Text = "Belum ada data raw. Tunggu data masuk.";
                return;
            }

            double rawDelta = currentRaw - SettingsStore.ScaleOffset;
            if (rawDelta <= 0)
            {
                StatusText.Text = "Delta raw tidak positif. Periksa zero dan ulangi.";
            }
            else
            {
                // Simpan raw delta sebagai faktor (untuk konversi nanti jika diperlukan)
                // Atau bisa langsung set factor = 1 jika ingin langsung pakai nilai raw
                double factor = rawDelta / 1000.0; // Default factor untuk gram
                SettingsStore.ScaleFactor = factor;
                CalFactorText.Text = factor.ToString("F6", ci).Replace(".", ",");
                StatusText.Text = $"Kalibrasi beban selesai. Raw delta={rawDelta.ToString("F3", ci).Replace(".", ",")}. Faktor={factor.ToString("F6", ci).Replace(".", ",")} raw/gram.";
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SettingsStore.SaveToFile();
                StatusText.Text = "Kalibrasi timbangan disimpan ke disk.";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Gagal menyimpan: {ex.Message}";
            }
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsStore.ResetScaleCalibration();
            var ci = System.Globalization.CultureInfo.InvariantCulture;
            OffsetText.Text = SettingsStore.ScaleOffset.ToString("F3", ci).Replace(".", ",");
            CalFactorText.Text = SettingsStore.ScaleFactor.ToString("F6", ci).Replace(".", ",");
            StatusText.Text = "Kalibrasi timbangan di-reset ke default.";
            SettingsStore.SaveToFile();
        }

        protected override void OnClosed(EventArgs e)
        {
            try { serialService.DataReceived -= SerialService_DataReceived; } catch { }
            base.OnClosed(e);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }
    }
}
