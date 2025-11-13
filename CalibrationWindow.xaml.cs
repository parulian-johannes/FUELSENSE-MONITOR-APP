using System;
using System.Windows;
using System.Collections.Generic;
using System.Globalization;
using EngineMonitoring.Services;
using EngineMonitoring.Models;

namespace EngineMonitoring
{
    public partial class CalibrationWindow : Window
    {
        private readonly SerialService serialService;
        private double latestRawTorque = 0.0;
        private double zeroOffset = 0.0;
        // Field pengambilan rata-rata (averaging untuk pengambilan sampel)
        private bool capturingZero = false;
        private bool capturingLoad = false;
        private int sampleCount = 10;
        private List<double> captureBuffer = new();

        public CalibrationWindow(SerialService service)
        {
            InitializeComponent();
            serialService = service ?? throw new ArgumentNullException(nameof(service));

            // inisialisasi UI dari store
            // tampilkan panjang lengan dalam meter di UI (fixed value)
            ArmLengthTextBox.Text = "0.305";

            // Berlangganan pembaruan data langsung jika tersedia
            try
            {
                serialService.DataReceived += SerialService_DataReceived;
            }
            catch { }

            // Pasang handler perubahan input agar panel Informasi ter-update berdasarkan dua input (berat & panjang lengan)
            try
            {
                CalibrationWeightTextBox.TextChanged += (s, e) => UpdateInfoPanelFromInputs();
                ArmLengthTextBox.TextChanged += (s, e) => UpdateInfoPanelFromInputs();
                // Checkbox untuk menandai apakah input dari logger adalah raw (perlu dikalibrasi) atau sudah dalam Nm
                try
                {
                    TorqueInputIsRawCheckBox.IsChecked = SettingsStore.TorqueInputIsRaw;
                    TorqueInputIsRawCheckBox.Checked += (s, e) => { SettingsStore.TorqueInputIsRaw = true; SettingsStore.SaveToFile(); };
                    TorqueInputIsRawCheckBox.Unchecked += (s, e) => { SettingsStore.TorqueInputIsRaw = false; SettingsStore.SaveToFile(); };
                }
                catch { }
            }
            catch { }

            // inisialisasi panel informasi dari nilai input dan nilai runtime
            UpdateInfoPanelFromInputs();
        }

        private void UpdateInfoPanelFromInputs()
        {
            // Baca berat dan panjang lengan (meter) dari UI dan perbarui field pada panel informasi.
            // Metode ini aman dipanggil dari thread UI.
            double weightKg = 0.0;
            double armM = 0.0;
            bool hasWeight = double.TryParse(CalibrationWeightTextBox.Text, out weightKg) && weightKg > 0;
            bool hasArm = double.TryParse(ArmLengthTextBox.Text, out armM) && armM > 0;

            // Tampilkan berat
            InfoWeightText.Text = hasWeight ? $"{weightKg:F2} kg" : "-";

            // Torsi yang diharapkan berdasarkan input
            if (hasWeight && hasArm)
            {
                const double g = 9.80665;
                double expectedTorque = weightKg * g * armM;
                InfoTorqueText.Text = $"{expectedTorque:F3} Nm";

                // Persentase terhadap kapasitas sensor
                double capacity = SettingsStore.TorqueSensorCapacity;
                if (capacity > 0)
                {
                    double percent = (expectedTorque / capacity) * 100.0;
                    InfoPercentText.Text = $"{percent:F1}%";
                }
                else
                {
                    InfoPercentText.Text = "-";
                }
            }
            else
            {
                InfoTorqueText.Text = "-";
                InfoPercentText.Text = "-";
            }

            // Pembacaan mentah (terbaru) — tampilkan jika tersedia
            InfoRawText.Text = latestRawTorque != 0.0 ? $"{latestRawTorque:F3}" : "-";
        }

        private double GetRawTorqueValue(Models.SensorData? data)
        {
            if (data == null) return 0.0;
            // prioritaskan field Torque jika tersedia, namun beberapa perangkat mengirim loadcell pada field Fuel atau MAF
            if (data.Torque != 0) return data.Torque;
            if (data.Fuel != 0) return data.Fuel;
            if (data.MAF != 0) return data.MAF;
            // fallback ke temperature atau rpm (kemungkinan kecil) untuk menghindari nilai nol
            if (data.Temperature != 0) return data.Temperature;
            if (data.RPM != 0) return data.RPM;
            return 0.0;
        }

        private void SerialService_DataReceived(object? sender, Models.SensorData data)
        {
            // perbarui pembacaan mentah torsi terbaru (menggunakan pemilihan yang robust)
            latestRawTorque = GetRawTorqueValue(data);
            Dispatcher.Invoke(() => {
                LatestRawText.Text = $"{latestRawTorque:F3}";
                double preview;
                if (!SettingsStore.TorqueInputIsRaw)
                {
                    // jika input sudah dalam Nm dari logger, gunakan langsung nilai tersebut sebagai preview
                    preview = latestRawTorque * (SettingsStore.TorqueInvert ? -1.0 : 1.0);
                }
                else
                {
                    preview = (latestRawTorque - SettingsStore.TorqueOffset) * SettingsStore.TorqueScaleFactor * (SettingsStore.TorqueInvert ? -1.0 : 1.0);
                }
                CalibratedPreviewText.Text = $"{preview:F3}";

                // logika pengambilan sampel untuk averaging
                if (capturingZero || capturingLoad)
                {
                    captureBuffer.Add(latestRawTorque);
                    StatusText.Text = $"Mengambil sampel: {captureBuffer.Count}/{sampleCount}";

                    if (captureBuffer.Count >= sampleCount)
                    {
                        double avg = 0.0;
                        foreach (var v in captureBuffer) avg += v;
                        avg /= captureBuffer.Count;

                        if (capturingZero)
                        {
                            SettingsStore.TorqueOffset = avg;
                            StatusText.Text = $"Kalibrasi nol tersimpan (rata-rata) = {avg:F3} (unit mentah)";

                            // Sinkronkan ke device: jalankan ZERO di Arduino (akan sampling+beep)
                            try { serialService?.SendCommand("TCAL ZERO"); } catch { }
                        }
                        else if (capturingLoad)
                        {
                            // hitung readingDelta menggunakan offset yang tersimpan
                            double readingDelta = avg - SettingsStore.TorqueOffset;
                            if (readingDelta <= 0)
                            {
                                StatusText.Text = "Delta rata-rata tidak positif. Periksa kalibrasi nol dan beban.";
                            }
                            else
                            {
                                if (!double.TryParse(CalibrationWeightTextBox.Text, out double weightKg) || weightKg <= 0)
                                {
                                    StatusText.Text = "Berat kalibrasi tidak valid. Masukkan angka positif (kg).";
                                }
                                else if (!double.TryParse(ArmLengthTextBox.Text, out double armM) || armM <= 0)
                                {
                                    StatusText.Text = "Panjang lengan tidak valid. Masukkan angka positif (m).";
                                }
                                else
                                {
                                    const double g = 9.80665;
                                    // armM sudah dalam meter, langsung pakai
                                    double expectedTorque = weightKg * g * armM;
                                    // gunakan kapasitas dari SettingsStore (harus diset di pengaturan jika belum)
                                    double capacity = SettingsStore.TorqueSensorCapacity;
                                    if (capacity <= 0)
                                    {
                                        StatusText.Text = "Kapasitas sensor belum diset. Silakan atur nilai kapasitas sensor di Settings aplikasi sebelum melanjutkan.";
                                        // hentikan pengambilan dan bersihkan buffer
                                        capturingLoad = false;
                                        capturingZero = false;
                                        captureBuffer.Clear();
                                        return;
                                    }
                                    if (expectedTorque < 0.2 * capacity)
                                    {
                                        StatusText.Text = $"Berat kalibrasi terlalu kecil: torsi yang diharapkan {expectedTorque:F2} Nm kurang dari 20% kapasitas sensor ({0.2 * capacity:F2} Nm).";
                                    }
                                    else
                                    {
                                        double scale = expectedTorque / readingDelta;
                                        SettingsStore.TorqueScaleFactor = scale;
                                        SettingsStore.TorqueArmLength = armM; // simpan dalam meter di store
                                        SettingsStore.TorqueSensorCapacity = capacity;
                                        StatusText.Text = $"Kalibrasi beban selesai (rata-rata). Skala={scale:F6}. Torsi yang diharapkan={expectedTorque:F3} Nm, delta pembacaan={readingDelta:F3}.";
                                        CalibratedPreviewText.Text = $"{(avg - SettingsStore.TorqueOffset) * scale:F3}";

                                        // Update informasi yang ditampilkan
                                        InfoWeightText.Text = $"{weightKg:F2} kg";
                                        InfoTorqueText.Text = $"{expectedTorque:F3} Nm";
                                        InfoRawText.Text = $"{avg:F3}";
                                        double percentOfCapacity = (expectedTorque / capacity) * 100.0;
                                        InfoPercentText.Text = $"{percentOfCapacity:F1}%";

                                        // Sinkronkan ke device: jalankan LOAD di Arduino dengan berat (kg) → device akan sampling+beep
                                        try
                                        {
                                            serialService?.SendCommand($"TCAL LOAD {weightKg.ToString(CultureInfo.InvariantCulture)}");
                                        }
                                        catch { }

                                        // Karena device kini mengirim Nm (pasca kalibrasi), nonaktifkan scaling di app agar tidak double
                                        SettingsStore.TorqueInputIsRaw = false;
                                        try
                                        {
                                            SettingsStore.SaveToFile();
                                            TorqueInputIsRawCheckBox.IsChecked = false;
                                        }
                                        catch { }
                                    }
                                }
                            }
                        }

                        // hentikan proses pengambilan sampel
                        capturingZero = false;
                        capturingLoad = false;
                        captureBuffer.Clear();
                    }
                    // Perbarui panel informasi (input berat/panjang lengan + pembacaan mentah terbaru) setelah pemrosesan
                    UpdateInfoPanelFromInputs();
                }
            });
        }

        private void ZeroCalibrateButton_Click(object sender, RoutedEventArgs e)
        {
            // Jika input logger bukan raw, tidak bisa melakukan kalibrasi nol pada nilai mentah
            if (!SettingsStore.TorqueInputIsRaw)
            {
                StatusText.Text = "Input logger diasumsikan sudah dalam Nm. Nonaktifkan opsi 'Input mentah' untuk melakukan kalibrasi pada nilai mentah.";
                return;
            }
            // gunakan sampleCount default (averaging noise) karena UI hanya menampilkan 2 input
            if (sampleCount <= 0) sampleCount = 10;
            captureBuffer.Clear();
            capturingZero = true;
            capturingLoad = false;
            StatusText.Text = $"Memulai pengambilan nol (rata-rata {sampleCount} sampel)...";
        }

        private void LoadCalibrateButton_Click(object sender, RoutedEventArgs e)
        {
            // Jika input logger bukan raw, kalibrasi beban tidak berlaku
            if (!SettingsStore.TorqueInputIsRaw)
            {
                StatusText.Text = "Input logger diasumsikan sudah dalam Nm. Nonaktifkan opsi 'Input mentah' untuk melakukan kalibrasi pada nilai mentah.";
                return;
            }
            // Mulai pengambilan rata-rata untuk beban - pengguna harus memasang berat kalibrasi sekarang
            if (sampleCount <= 0) sampleCount = 10;
            captureBuffer.Clear();
            capturingLoad = true;
            capturingZero = false;
            StatusText.Text = $"Memulai pengambilan beban (rata-rata {sampleCount} sampel). Pasang berat kalibrasi sekarang...";
        }

        private void SaveCalibrationButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SettingsStore.SaveToFile();
                StatusText.Text = "Kalibrasi tersimpan di pengaturan runtime dan telah disimpan ke disk.";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Tersimpan di runtime tetapi gagal menyimpan ke disk: {ex.Message}";
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                if (serialService != null)
                    serialService.DataReceived -= SerialService_DataReceived;
            }
            catch { }
            base.OnClosed(e);
        }

        private void OpenScaleCalibrationButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var w = new ScaleCalibrationWindow(serialService);
                w.Owner = this;
                w.Show();
            }
            catch (Exception ex)
            {
                StatusText.Text = "Gagal membuka Kalibrasi Timbangan: " + ex.Message;
            }
        }

        private void ResetCalibrationButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show(
                    "Reset kalibrasi torsi ke nilai default? Ini akan menghapus offset dan faktor skala saat ini.",
                    "Konfirmasi Reset",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    SettingsStore.ResetTorqueCalibration();
                    SettingsStore.SaveToFile();

                    // Update UI to reflect defaults
                    ArmLengthTextBox.Text = "0.305";
                    CalibratedPreviewText.Text = "--";
                    InfoWeightText.Text = "-";
                    InfoTorqueText.Text = "-";
                    InfoRawText.Text = "-";
                    InfoPercentText.Text = "-";
                    TorqueInputIsRawCheckBox.IsChecked = SettingsStore.TorqueInputIsRaw;

                    StatusText.Text = "Kalibrasi torsi telah di-reset ke default dan tersimpan.";
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = "Gagal mereset kalibrasi: " + ex.Message;
            }
        }
    }
}