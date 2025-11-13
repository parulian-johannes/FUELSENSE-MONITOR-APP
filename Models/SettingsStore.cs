using System;
using System.IO;
using System.Text.Json;
using EngineMonitoring.Services;

namespace EngineMonitoring.Models
{
    // In-memory store for calibration and other runtime settings.
    public static class SettingsStore
    {
    private static readonly string AppDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EngineMonitoring");
        private static readonly string SettingsFile = Path.Combine(AppDataFolder, "calibration.json");

        // Torque calibration
        // Scale factor: multiply (raw - offset) * Scale => torque in Nm
        public static double TorqueScaleFactor { get; set; } = 1.0;
        public static double TorqueOffset { get; set; } = 0.0;
        public static double TorqueArmLength { get; set; } = 0.0; // meters
        public static bool TorqueInvert { get; set; } = false;
    public static double TorqueSensorCapacity { get; set; } = 1000.0; // Nm default
    // Jika true, nilai Torque yang datang dari logger adalah nilai mentah (raw) yang perlu dikalibrasi
    // Jika false, nilai yang datang diasumsikan sudah dalam Nm dan tidak akan dikenai skala/offset
    public static bool TorqueInputIsRaw { get; set; } = true;
    // Scale (timbangan) calibration
    // raw readings from ADC/loadcell: weight = (raw - offset) / calibrationFactor
    public static double ScaleOffset { get; set; } = 0.0; // raw offset (no-load)
    public static double ScaleFactor { get; set; } = 1.0; // raw units per kg

        static SettingsStore()
        {
            // Try load persisted settings on startup
            try
            {
                LoadFromFile();
            }
            catch
            {
                // ignore load errors; use defaults
            }
        }

        // Helper to reset
        public static void ResetTorqueCalibration()
        {
            TorqueScaleFactor = 1.0;
            TorqueOffset = 0.0;
            TorqueArmLength = 0.0;
            TorqueInvert = false;
            TorqueSensorCapacity = 1000.0;
            TorqueInputIsRaw = true;
        }

        public static void ResetScaleCalibration()
        {
            ScaleOffset = 0.0;
            ScaleFactor = 1.0;
        }

        public static void SaveToFile()
        {
            try
            {
                if (!Directory.Exists(AppDataFolder)) Directory.CreateDirectory(AppDataFolder);

                var dto = new
                {
                    TorqueScaleFactor,
                    TorqueOffset,
                    TorqueArmLength,
                    TorqueInvert,
                    TorqueSensorCapacity,
                    TorqueInputIsRaw,
                    ScaleOffset,
                    ScaleFactor
                };

                var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFile, json);
                
                // Simpan juga ke Excel (tambahkan sheet Kalibrasi)
                try
                {
                    Services.ExcelService.AddCalibrationSheet();
                }
                catch (Exception exExcel)
                {
                    Console.WriteLine($"Note: Calibration saved to JSON, but Excel update failed: {exExcel.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving settings: {ex.Message}");
            }
        }

        public static void LoadFromFile()
        {
            try
            {
                if (!File.Exists(SettingsFile)) return;

                var json = File.ReadAllText(SettingsFile);
                var doc = JsonSerializer.Deserialize<JsonElement>(json);
                if (doc.TryGetProperty("TorqueScaleFactor", out var p)) TorqueScaleFactor = p.GetDouble();
                if (doc.TryGetProperty("TorqueOffset", out var p2)) TorqueOffset = p2.GetDouble();
                if (doc.TryGetProperty("TorqueArmLength", out var p3)) TorqueArmLength = p3.GetDouble();
                if (doc.TryGetProperty("TorqueInvert", out var p4)) TorqueInvert = p4.GetBoolean();
                if (doc.TryGetProperty("TorqueSensorCapacity", out var p5)) TorqueSensorCapacity = p5.GetDouble();
                if (doc.TryGetProperty("TorqueInputIsRaw", out var p6)) TorqueInputIsRaw = p6.GetBoolean();
                if (doc.TryGetProperty("ScaleOffset", out var p7)) ScaleOffset = p7.GetDouble();
                if (doc.TryGetProperty("ScaleFactor", out var p8)) ScaleFactor = p8.GetDouble();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading settings: {ex.Message}");
            }
        }
    }
}
