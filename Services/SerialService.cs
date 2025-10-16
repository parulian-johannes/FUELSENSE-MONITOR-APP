using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FuelsenseMonitorApp.Models;

namespace FuelsenseMonitorApp.Services
{
    public class SerialService
    {
        private SerialPort? serialPort;
        private bool isConnected = false;
        private CancellationTokenSource? cancellationTokenSource;

        // CSV index mapping (ditentukan sekali lalu dipakai konsisten)
        private int? csvIdxRPM, csvIdxTorque, csvIdxFuel, csvIdxTemp, csvIdxMaf;

        // Buffer kalibrasi (kumpulkan beberapa baris CSV untuk deteksi mapping)
        private readonly List<double[]> csvCalibBuffer = new();
        private int? csvWidth; // lebar kolom yang konsisten

        // Header-based mapping
        private Dictionary<string, int>? csvHeaderMap;
        private int? csvHeaderWidth;

        // Last seen values (membantu stabilisasi koreksi)
        private double? lastRpm, lastTemp, lastFuel, lastMaf, lastTorque;

        public event EventHandler<SensorData>? DataReceived;
        public event EventHandler<bool>? ConnectionStatusChanged;

        public bool IsConnected => isConnected && serialPort?.IsOpen == true;

        public bool ConnectToCH340(string portName, int baudRate)
        {
            try
            {
                // Disconnect if already connected
                ForceDisconnect();

                Console.WriteLine($"[SERIAL] Attempting to connect to {portName} at {baudRate} baud...");

                serialPort = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One)
                {
                    ReadTimeout = 2000,
                    WriteTimeout = 2000,
                    DtrEnable = true,
                    RtsEnable = true,
                    Handshake = Handshake.None,
                    NewLine = "\n" // pastikan ReadLine berakhir newline
                };

                serialPort.Open();
                serialPort.DiscardInBuffer();
                serialPort.DiscardOutBuffer();

                // reset mapping saat konek baru
                ResetCsvMapping();
                csvCalibBuffer.Clear();
                csvWidth = null;

                if (serialPort.IsOpen)
                {
                    isConnected = true;
                    ConnectionStatusChanged?.Invoke(this, true);

                    // Start reading data in background
                    StartDataReading();

                    Console.WriteLine($"[SERIAL] Successfully connected to {portName} at {baudRate} baud");
                    Console.WriteLine($"[SERIAL] Port settings - DTR: {serialPort.DtrEnable}, RTS: {serialPort.RtsEnable}");
                    Console.WriteLine($"[SERIAL] Port settings - DataBits: {serialPort.DataBits}, Parity: {serialPort.Parity}, StopBits: {serialPort.StopBits}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SERIAL] Connection to {portName} failed: {ex.Message}");
                isConnected = false;
                ConnectionStatusChanged?.Invoke(this, false);
            }

            return false;
        }

        private void StartDataReading()
        {
            cancellationTokenSource = new CancellationTokenSource();

            Task.Run(async () =>
            {
                while (!cancellationTokenSource.Token.IsCancellationRequested && IsConnected)
                {
                    try
                    {
                        if (serialPort?.BytesToRead > 0)
                        {
                            string rawData = serialPort.ReadLine().Trim();

                            if (!string.IsNullOrEmpty(rawData))
                            {
                                // Jika baris adalah header, set mapping dan lanjut tanpa publish data kosong
                                if (IsHeaderLine(rawData))
                                {
                                    if (TrySetHeaderMapping(rawData))
                                        Console.WriteLine($"[CSV HEADER] Mapping set for: {rawData}");
                                    continue;
                                }

                Console.WriteLine($">>> RAW DATA FROM LOGGER: '{rawData}'");
                                Console.WriteLine($">>> DATA LENGTH: {rawData.Length} characters");
                                Console.WriteLine($">>> DATA BYTES: {string.Join(" ", System.Text.Encoding.UTF8.GetBytes(rawData).Select(b => b.ToString("X2")))}");
                                Console.WriteLine($">>> CONTAINS COMMA: {rawData.Contains(",")}");
                                Console.WriteLine($">>> CONTAINS COLON: {rawData.Contains(":")}");

                                // PRE-PROCESS: Cek apakah ada nilai yang terlihat seperti temperature
                                var possibleTemp = ExtractPossibleTemperature(rawData);
                                if (possibleTemp.HasValue)
                                {
                                    Console.WriteLine($">>> POSSIBLE TEMPERATURE DETECTED: {possibleTemp.Value}°C");
                                }

                                var sensorData = ParseSensorData(rawData);
                                
                                // POST-PROCESS: Jika temperature masih 0 dan kita punya possible temp, gunakan itu
                                if (sensorData.Temperature == 0 && possibleTemp.HasValue)
                                {
                                    sensorData.Temperature = possibleTemp.Value;
                                    Console.WriteLine($">>> TEMPERATURE ASSIGNED FROM PRE-PROCESS: {sensorData.Temperature}°C");
                                }
                                DataReceived?.Invoke(this, sensorData);
                            }
                        }

                        await Task.Delay(100, cancellationTokenSource.Token);
                    }
                    catch (TimeoutException)
                    {
                        // Normal timeout, continue reading
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error reading data: {ex.Message}");
                        break;
                    }
                }
            }, cancellationTokenSource.Token);
        }

        private SensorData ParseSensorData(string rawData)
        {
            // Parser yang robust untuk RPM, mendukung:
            // - Key-value (R:2500, RPM:2500)
            // - CSV (berbagai urutan)
            // - Nilai terdeteksi berdasarkan rentang
            var sensorData = new SensorData { Time = DateTime.Now };
            var ci = CultureInfo.InvariantCulture;

            // Initialize temperature with a default value to ensure it's never NaN or uninitialized
            Console.WriteLine($"[INIT] SensorData initialized - Temperature: {sensorData.Temperature}");

            try
            {
                if (string.IsNullOrWhiteSpace(rawData))
                    return sensorData;

                string CleanNumeric(string s)
                {
                    // ambil angka, minus, dan titik
                    var chars = s.Trim();
                    var buf = new System.Text.StringBuilder(chars.Length);
                    foreach (var ch in chars)
                        if ((ch >= '0' && ch <= '9') || ch == '.' || ch == '-' ) buf.Append(ch);
                    return buf.ToString();
                }

                bool TryParseDouble(string s, out double value)
                {
                    return double.TryParse(s, NumberStyles.Any, ci, out value);
                }

                // 1) Key-value (R:, RPM:, dll)
                if (rawData.Contains(":"))
                {
                    var parts = rawData.Split(',');
                    foreach (var part in parts)
                    {
                        var kv = part.Split(':');
                        if (kv.Length != 2) continue;

                        var key = kv[0].Trim().ToUpperInvariant();
                        var valStr = CleanNumeric(kv[1]);

                        if (!TryParseDouble(valStr, out double val)) continue;

                        switch (key)
                        {
                            case "R":
                            case "RPM":
                            case "PUTARAN":
                                sensorData.RPM = (int)Math.Round(val);
                                break;
                            case "T":
                            case "TORQUE":
                            case "TORSI":
                                sensorData.Torque = val;
                                break;
                            case "F":
                            case "FUEL":
                            case "BBM":
                                sensorData.Fuel = val;
                                break;
                            case "TEMP":
                            case "TEMPERATURE":
                            case "TMP":
                            case "SUHU":
                                sensorData.Temperature = val;
                                Console.WriteLine($"[KEY-VALUE] Temperature set to: {val}°C from key: {key}");
                                break;
                            case "MAF":
                            case "AIRFLOW":
                                sensorData.MAF = val;
                                break;
                        }
                    }

                    // fallback cari angka yang tampak seperti RPM jika belum ada
                    if (sensorData.RPM == 0)
                    {
                        foreach (var part in parts)
                        {
                            var v = CleanNumeric(part);
                            if (TryParseDouble(v, out double num) && num >= 500 && num <= 10000)
                            {
                                sensorData.RPM = (int)Math.Round(num);
                                break;
                            }
                        }
                    }

                    // EnforceAndFixPerRow(ref sensorData); // HAPUS BARIS INI - menyebabkan temperature tidak masuk
                    return sensorData;
                }

                // 2) CSV (angka dipisah koma) - MAPPING LANGSUNG BERDASARKAN POSISI
                if (rawData.Contains(","))
                {
                    var vals = rawData.Split(',');
                    Console.WriteLine($"[CSV] Raw CSV parts: [{string.Join(" | ", vals)}]");
                    
                    double[] nums = new double[vals.Length];
                    for (int i = 0; i < vals.Length; i++)
                    {
                        var v = CleanNumeric(vals[i]);
                        Console.WriteLine($"[CSV] Part {i}: '{vals[i]}' -> cleaned: '{v}'");
                        nums[i] = TryParseDouble(v, out double d) ? d : double.NaN;
                        Console.WriteLine($"[CSV] Part {i}: parsed value = {nums[i]}");
                    }

                    // PRIORITASKAN TEMPERATURE - PASTIKAN SELALU DIPROSES
                    if (nums.Length >= 1 && !double.IsNaN(nums[0]))
                    {
                        sensorData.Temperature = nums[0];
                        Console.WriteLine($"[TEMPERATURE] Set from position 0: {sensorData.Temperature}°C");
                    }
                    
                    // MAPPING UNTUK DATA LAINNYA
                    if (nums.Length >= 2 && !double.IsNaN(nums[1])) 
                    {
                        sensorData.Torque = nums[1];
                        Console.WriteLine($"[TORQUE] Set from position 1: {sensorData.Torque}");
                    }
                    if (nums.Length >= 3 && !double.IsNaN(nums[2])) 
                    {
                        sensorData.Fuel = nums[2];
                        Console.WriteLine($"[FUEL] Set from position 2: {sensorData.Fuel}");
                    }
                    if (nums.Length >= 4 && !double.IsNaN(nums[3])) 
                    {
                        sensorData.MAF = nums[3];
                        Console.WriteLine($"[MAF] Set from position 3: {sensorData.MAF}");
                    }
                    if (nums.Length >= 5 && !double.IsNaN(nums[4])) 
                    {
                        sensorData.RPM = (int)Math.Round(nums[4]);
                        Console.WriteLine($"[RPM] Set from position 4: {sensorData.RPM}");
                    }

                    Console.WriteLine($"[CSV RESULT] Temp:{sensorData.Temperature} Torque:{sensorData.Torque} Fuel:{sensorData.Fuel} MAF:{sensorData.MAF} RPM:{sensorData.RPM}");
                    return sensorData;
                }                // 3) Single value
                {
                    var v = CleanNumeric(rawData);
                    Console.WriteLine($"[SINGLE] Cleaned value: '{v}'");
                    if (TryParseDouble(v, out double single))
                    {
                        Console.WriteLine($"[SINGLE] Parsed value: {single}");
                        if (single >= 500 && single <= 10000) 
                        {
                            sensorData.RPM = (int)Math.Round(single);
                            Console.WriteLine($"[SINGLE] RPM set to: {sensorData.RPM}");
                        }
                        else if (single >= 0 && single <= 300) 
                        {
                            sensorData.Temperature = single;
                            Console.WriteLine($"[SINGLE] Temperature set to: {sensorData.Temperature}°C");
                        }
                        else 
                        {
                            sensorData.Fuel = single;
                            Console.WriteLine($"[SINGLE] Fuel set to: {sensorData.Fuel}");
                        }
                    }
                }

                // Safeguard: Ensure temperature values are reasonable and preserved
                if (sensorData.Temperature < 0 || sensorData.Temperature > 500)
                {
                    Console.WriteLine($"[TEMP WARNING] Temperature value {sensorData.Temperature} seems unreasonable, keeping as-is");
                }

                // ADDITIONAL TEMPERATURE DETECTION: Try to find temperature value if it's still 0
                if (sensorData.Temperature == 0 && !string.IsNullOrEmpty(rawData))
                {
                    var tempValue = TryExtractTemperature(rawData);
                    if (tempValue.HasValue)
                    {
                        sensorData.Temperature = tempValue.Value;
                        Console.WriteLine($"[TEMP RECOVERY] Found temperature value: {sensorData.Temperature}°C");
                    }
                }

                Console.WriteLine($"[PARSE RESULT] Final SensorData - Temp:{sensorData.Temperature} Torque:{sensorData.Torque} Fuel:{sensorData.Fuel} MAF:{sensorData.MAF} RPM:{sensorData.RPM}");
                return sensorData; // LANGSUNG RETURN TANPA KOREKSI
            }
            catch
            {
                return sensorData;
            }
        }

        // Additional method to extract temperature from raw data using various strategies
        private double? TryExtractTemperature(string rawData)
        {
            if (string.IsNullOrEmpty(rawData)) return null;

            try
            {
                var ci = CultureInfo.InvariantCulture;
                
                // Strategy 1: Look for temperature keywords
                var tempKeywords = new[] { "TEMP:", "TEMPERATURE:", "TMP:", "SUHU:", "T:" };
                foreach (var keyword in tempKeywords)
                {
                    var index = rawData.ToUpperInvariant().IndexOf(keyword);
                    if (index >= 0)
                    {
                        var afterKeyword = rawData.Substring(index + keyword.Length);
                        var match = System.Text.RegularExpressions.Regex.Match(afterKeyword, @"(\d+\.?\d*)");
                        if (match.Success && double.TryParse(match.Value, NumberStyles.Any, ci, out double val))
                        {
                            if (val >= 0 && val <= 300) // reasonable temperature range
                            {
                                Console.WriteLine($"[TEMP EXTRACT] Found temp via keyword '{keyword}': {val}");
                                return val;
                            }
                        }
                    }
                }

                // Strategy 2: Look for values in typical temperature range in CSV
                if (rawData.Contains(","))
                {
                    var parts = rawData.Split(',');
                    foreach (var part in parts)
                    {
                        var cleanValue = part.Trim();
                        if (double.TryParse(cleanValue, NumberStyles.Any, ci, out double val))
                        {
                            if (val >= 20 && val <= 150) // typical engine temperature range
                            {
                                Console.WriteLine($"[TEMP EXTRACT] Found temp via range check: {val}");
                                return val;
                            }
                        }
                    }
                }

                // Strategy 3: If single value and in temperature range
                if (!rawData.Contains(",") && !rawData.Contains(":"))
                {
                    if (double.TryParse(rawData.Trim(), NumberStyles.Any, ci, out double val))
                    {
                        if (val >= 20 && val <= 150)
                        {
                            Console.WriteLine($"[TEMP EXTRACT] Found temp as single value: {val}");
                            return val;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TEMP EXTRACT] Error: {ex.Message}");
            }

            return null;
        }

        // Method untuk quickly detect temperature dari raw data
        private double? ExtractPossibleTemperature(string rawData)
        {
            if (string.IsNullOrEmpty(rawData)) return null;

            try
            {
                var ci = CultureInfo.InvariantCulture;
                
                // Cek semua angka dalam string
                var matches = System.Text.RegularExpressions.Regex.Matches(rawData, @"\d+\.?\d*");
                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    if (double.TryParse(match.Value, NumberStyles.Any, ci, out double val))
                    {
                        // Range temperature yang masuk akal untuk engine
                        if (val >= 20 && val <= 200)
                        {
                            return val;
                        }
                    }
                }
            }
            catch
            {
                // Ignore errors
            }
            
            return null;
        }

        // Tentukan mapping indeks CSV dari satu baris data (heuristik cepat: dipertahankan untuk fallback)
        private void DetermineCsvMapping(double[] nums)
        {
            ResetCsvMapping();
            if (nums == null || nums.Length == 0) return;

            // 1) Cari RPM: pilih kandidat 500..10000, ambil nilai terbesar
            var rpmCandidates = new List<(int idx, double val)>();
            for (int i = 0; i < nums.Length; i++)
                if (!double.IsNaN(nums[i]) && nums[i] >= 500 && nums[i] <= 10000)
                    rpmCandidates.Add((i, nums[i]));
            if (rpmCandidates.Count > 0)
                csvIdxRPM = rpmCandidates.OrderByDescending(x => x.val).First().idx;

            // 2) Cari Temperature: 0..300 (prioritas 60..120 jika ada)
            var tempCandidates = new List<(int idx, double val)>();
            for (int i = 0; i < nums.Length; i++)
            {
                if (i == csvIdxRPM) continue;
                if (!double.IsNaN(nums[i]) && nums[i] >= 0 && nums[i] <= 300)
                    tempCandidates.Add((i, nums[i]));
            }
            if (tempCandidates.Count > 0)
            {
                var prio = tempCandidates.Where(t => t.val >= 60 && t.val <= 120).ToList();
                csvIdxTemp = (prio.Count > 0 ? prio : tempCandidates).First().idx;
            }

            // 3) Sisa: Torque, Fuel, MAF
            var remaining = new List<(int idx, double val)>();
            for (int i = 0; i < nums.Length; i++)
            {
                if (i == csvIdxRPM || i == csvIdxTemp) continue;
                if (!double.IsNaN(nums[i]))
                    remaining.Add((i, nums[i]));
            }

            // Heuristik:
            // - Torque: terbesar di antara sisa (0..1000+)
            // - Fuel: terkecil di antara sisa (biasanya 0..50/100)
            // - MAF: nilai sisa lainnya (umumnya 0..200)
            if (remaining.Count >= 3)
            {
                var ordered = remaining.OrderBy(x => x.val).ToList(); // ascending
                csvIdxFuel = ordered[0].idx;             // paling kecil → Fuel
                csvIdxMaf = ordered[1].idx;              // tengah → MAF
                csvIdxTorque = ordered[^1].idx;          // paling besar → Torque
            }
            else
            {
                // fallback jika kolom kurang dari 5
                if (remaining.Count >= 1) csvIdxTorque ??= remaining.OrderByDescending(x => x.val).First().idx;
                if (remaining.Count >= 2)
                {
                    var rem2 = remaining.Where(x => x.idx != csvIdxTorque).OrderBy(x => x.val).ToList();
                    if (rem2.Count > 0) csvIdxFuel ??= rem2[0].idx;
                    if (rem2.Count > 1) csvIdxMaf ??= rem2[1].idx;
                }
            }
        }

        // VALIDATOR: pastikan mapping CSV yang tersimpan valid untuk array nums saat ini
        private bool HasValidCsvMapping(double[] numsLocal)
        {
            bool idxInRange(int? idx) =>
                idx.HasValue &&
                idx.Value >= 0 &&
                idx.Value < numsLocal.Length &&
                !double.IsNaN(numsLocal[idx.Value]);

            return idxInRange(csvIdxRPM)
                && idxInRange(csvIdxTorque)
                && idxInRange(csvIdxFuel)
                && idxInRange(csvIdxTemp)
                && idxInRange(csvIdxMaf);
        }

        // Heuristik per-baris: tentukan indeks RPM, Temp, Fuel, MAF, Torque dari 1 baris CSV
        private CsvMap ComputeHeuristicMapping(double[] nums)
        {
            var map = new CsvMap(); // defaults to -1
            if (nums == null || nums.Length < 3) return map;

            // RPM: nilai besar (biasanya >500). Pilih nilai terbesar (beri bonus jika >500)
            int rpmIdx = -1;
            double bestRpmScore = double.MinValue;
            for (int i = 0; i < nums.Length; i++)
            {
                double v = nums[i];
                if (double.IsNaN(v)) continue;
                double score = v + (v > 500 ? 2000 : 0);
                if (score > bestRpmScore) { bestRpmScore = score; rpmIdx = i; }
            }
            if (rpmIdx == -1) return map;

            // Temp: 0..300, prioritaskan yg dekat 90C
            int tempIdx = -1;
            double bestTempScore = double.MaxValue;
            for (int i = 0; i < nums.Length; i++)
            {
                if (i == rpmIdx) continue;
                double v = nums[i];
                if (double.IsNaN(v)) continue;
                if (v >= 0 && v <= 300)
                {
                    double score = Math.Abs(v - 90); // makin kecil makin baik
                    if (score < bestTempScore) { bestTempScore = score; tempIdx = i; }
                }
            }
            // fallback: pilih nilai terkecil selain RPM
            if (tempIdx == -1)
            {
                double minVal = double.MaxValue;
                for (int i = 0; i < nums.Length; i++)
                {
                    if (i == rpmIdx) continue;
                    double v = nums[i];
                    if (double.IsNaN(v)) continue;
                    if (v < minVal) { minVal = v; tempIdx = i; }
                }
            }

            // Sisa: pilih Fuel (nilai terkecil), sisanya MAF/Torque
            var remaining = new List<int>();
            for (int i = 0; i < nums.Length; i++)
                if (i != rpmIdx && i != tempIdx && !double.IsNaN(nums[i]))
                    remaining.Add(i);

            int fuelIdx = -1, mafIdx = -1, torqueIdx = -1;
            if (remaining.Count > 0)
            {
                fuelIdx = remaining.OrderBy(idx => nums[idx]).First();
                var rest = remaining.Where(i => i != fuelIdx).ToList();

                if (rest.Count == 1)
                {
                    // hanya satu kolom sisa → anggap torque
                    torqueIdx = rest[0];
                }
                else if (rest.Count >= 2)
                {
                    // pilih MAF sebagai yang lebih dekat ke fuel (lebih menengah), torque sisanya
                    int a = rest[0], b = rest[1];
                    double da = Math.Abs(nums[a] - nums[fuelIdx]);
                    double db = Math.Abs(nums[b] - nums[fuelIdx]);
                    if (da <= db) { mafIdx = a; torqueIdx = b; }
                    else { mafIdx = b; torqueIdx = a; }

                    // koreksi: jika "MAF" jauh lebih besar dari torque → tukar
                    if (mafIdx != -1 && torqueIdx != -1 && nums[mafIdx] > nums[torqueIdx] * 1.5)
                        (mafIdx, torqueIdx) = (torqueIdx, mafIdx);
                }
            }

            map.RPM = rpmIdx;
            map.Temp = tempIdx;
            map.Fuel = fuelIdx;
            map.MAF = mafIdx;
            map.Torque = torqueIdx;
            return map;
        }

        // Assign nilai SensorData dari baris CSV berdasarkan mapping global (locked)
        private void AssignCsvValues(double[] nums, SensorData sensorData)
        {
            try
            {
                if (csvIdxRPM.HasValue && InRange(csvIdxRPM.Value, nums))
                    sensorData.RPM = (int)Math.Round(nums[csvIdxRPM.Value]);
                if (csvIdxTorque.HasValue && InRange(csvIdxTorque.Value, nums))
                    sensorData.Torque = nums[csvIdxTorque.Value];
                if (csvIdxFuel.HasValue && InRange(csvIdxFuel.Value, nums))
                    sensorData.Fuel = nums[csvIdxFuel.Value];
                if (csvIdxTemp.HasValue && InRange(csvIdxTemp.Value, nums))
                    sensorData.Temperature = nums[csvIdxTemp.Value];
                if (csvIdxMaf.HasValue && InRange(csvIdxMaf.Value, nums))
                    sensorData.MAF = nums[csvIdxMaf.Value];

                // Validasi cepat RPM
                if (sensorData.RPM < 0 || sensorData.RPM > 10000)
                {
                    Console.WriteLine("[CSV MAP] RPM out-of-range, recalibrating mapping");
                    DetermineCsvMapping(nums);
                    TryCalibrateCsvMapping(force: true);
                    if (csvIdxRPM.HasValue && InRange(csvIdxRPM.Value, nums))
                        sensorData.RPM = (int)Math.Round(nums[csvIdxRPM.Value]);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AssignCsvValues error: {ex.Message}");
            }

            static bool InRange(int idx, double[] a) => idx >= 0 && idx < a.Length && !double.IsNaN(a[idx]);
        }

        // Assign nilai berdasarkan mapping heuristik temporer (dipakai sebelum mapping global terkunci)
        private void AssignCsvValues(double[] nums, SensorData sensorData, CsvMap map)
        {
            try
            {
                if (map.RPM >= 0 && map.RPM < nums.Length && !double.IsNaN(nums[map.RPM]))
                    sensorData.RPM = (int)Math.Round(nums[map.RPM]);

                if (map.Torque >= 0 && map.Torque < nums.Length && !double.IsNaN(nums[map.Torque]))
                    sensorData.Torque = nums[map.Torque];

                if (map.Fuel >= 0 && map.Fuel < nums.Length && !double.IsNaN(nums[map.Fuel]))
                    sensorData.Fuel = nums[map.Fuel];

                if (map.Temp >= 0 && map.Temp < nums.Length && !double.IsNaN(nums[map.Temp]))
                    sensorData.Temperature = nums[map.Temp];

                if (map.MAF >= 0 && map.MAF < nums.Length && !double.IsNaN(nums[map.MAF]))
                    sensorData.MAF = nums[map.MAF];
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AssignCsvValues(temp) error: {ex.Message}");
            }
        }

        // Struktur mapping heuristik temporer (default -1 agar aman)
        private class CsvMap
        {
            public int RPM { get; set; } = -1;
            public int Torque { get; set; } = -1;
            public int Fuel { get; set; } = -1;
            public int Temp { get; set; } = -1;
            public int MAF { get; set; } = -1;
        }

        // Deteksi baris header: mengandung huruf dan koma, bukan baris numeric murni
        private bool IsHeaderLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return false;
            if (!line.Contains(",")) return false;
            // Header biasanya berisi huruf
            return line.Any(ch => char.IsLetter(ch));
        }

        // Set mapping header dari baris header
        private bool TrySetHeaderMapping(string headerLine)
        {
            try
            {
                var tokens = headerLine.Split(',');
                var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                for (int i = 0; i < tokens.Length; i++)
                {
                    var raw = tokens[i].Trim();
                    var key = NormalizeHeaderToken(raw);
                    if (string.IsNullOrEmpty(key)) continue;
                    // Simpan key langsung dan juga synonyms populer ke index yang sama
                    map[key] = i;

                    void alias(string k)
                    {
                        if (!map.ContainsKey(k)) map[k] = i;
                    }

                    switch (key)
                    {
                        case "RPM":
                        case "R":
                        case "PUTARAN":
                            alias("RPM"); alias("R"); alias("PUTARAN");
                            break;

                        case "TORQUE":
                        case "TORSI":
                        case "T":
                        case "NM":
                            alias("TORQUE"); alias("TORSI"); alias("T"); alias("NM");
                            break;

                        case "FUEL":
                        case "BBM":
                        case "F":
                        case "FUELRATE":
                            alias("FUEL"); alias("BBM"); alias("F"); alias("FUELRATE");
                            break;

                        case "TEMP":
                        case "TEMPERATURE":
                        case "TMP":
                        case "SUHU":
                        case "C":
                            alias("TEMP"); alias("TEMPERATURE"); alias("TMP"); alias("SUHU"); alias("C");
                            break;

                        case "MAF":
                        case "AIRFLOW":
                        case "MASSAIRFLOW":
                            alias("MAF"); alias("AIRFLOW"); alias("MASSAIRFLOW");
                            break;
                    }
                }

                // Harus minimal ada RPM + 2 lainnya agar berguna
                bool ok = map.ContainsKey("RPM") &&
                          (map.ContainsKey("TORQUE") || map.ContainsKey("TORSI") || map.ContainsKey("T")) &&
                          (map.ContainsKey("FUEL") || map.ContainsKey("BBM") || map.ContainsKey("F"));

                if (ok)
                {
                    csvHeaderMap = map;
                    csvHeaderWidth = tokens.Length;
                    Console.WriteLine($"[CSV HEADER] Detected columns: {string.Join(", ", tokens)}");
                }
                return ok;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Header mapping error: {ex.Message}");
                return false;
            }
        }

        private bool HasHeaderMappingFor(int width)
        {
            return csvHeaderMap != null && csvHeaderWidth.HasValue && csvHeaderWidth.Value == width;
        }

        private static string NormalizeHeaderToken(string token)
        {
            // Uppercase
            token = token.Trim().ToUpperInvariant();

            // Hapus apapun di dalam tanda kurung, contoh: "TEMP (C)" → "TEMP "
            int paren = token.IndexOf('(');
            if (paren >= 0) token = token.Substring(0, paren);

            // Hapus karakter non-huruf (spasi, tanda, unit)
            token = new string(token.Where(char.IsLetter).ToArray());

            // Normalisasi umum yang sering muncul
            if (token == "CELCIUS" || token == "CELSIUS") token = "TEMP";
            if (token == "NM") token = "TORQUE";
            if (token == "PUTARAN" || token == "ROTATION") token = "RPM";
            if (token == "SUHU" || token == "TMP") token = "TEMP";
            if (token == "BBM") token = "FUEL";
            if (token == "AIRFLOW" || token == "MASSAIRFLOW") token = "MAF";

            return token;
        }

        private int? GetHeaderIndex(params string[] keys)
        {
            if (csvHeaderMap == null) return null;
            foreach (var k in keys)
            {
                if (csvHeaderMap.TryGetValue(k, out int idx)) return idx;
            }
            return null;
        }

        private void AssignUsingHeader(double[] nums, SensorData sensorData)
        {
            try
            {
                int? idx;

                // RPM
                idx = GetHeaderIndex("RPM", "R", "PUTARAN");
                if (idx.HasValue && InRange(idx.Value, nums))
                    sensorData.RPM = (int)Math.Round(nums[idx.Value]);

                // Torque / Torsi
                idx = GetHeaderIndex("TORQUE", "TORSI", "T", "NM");
                if (idx.HasValue && InRange(idx.Value, nums))
                    sensorData.Torque = nums[idx.Value];

                // Fuel / BBM
                idx = GetHeaderIndex("FUEL", "BBM", "F", "FUELRATE");
                if (idx.HasValue && InRange(idx.Value, nums))
                    sensorData.Fuel = nums[idx.Value];

                // Temperature
                idx = GetHeaderIndex("TEMP", "TEMPERATURE", "TMP", "SUHU", "C");
                if (idx.HasValue && InRange(idx.Value, nums))
                    sensorData.Temperature = nums[idx.Value];

                // MAF
                idx = GetHeaderIndex("MAF", "AIRFLOW", "MASSAIRFLOW");
                if (idx.HasValue && InRange(idx.Value, nums))
                    sensorData.MAF = nums[idx.Value];

                // Jika RPM tidak terisi dari header (nilai hilang), fallback
                if (sensorData.RPM <= 0)
                {
                    int rpmIdx = DetectRpmIndexSingle(nums);
                    if (rpmIdx >= 0) sensorData.RPM = (int)Math.Round(nums[rpmIdx]);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AssignUsingHeader error: {ex.Message}");
            }

            static bool InRange(int idx, double[] a) => idx >= 0 && idx < a.Length && !double.IsNaN(a[idx]);
        }

        // Reset all mapping state and last-known values
        private void ResetCsvMapping()
        {
            csvIdxRPM = csvIdxTorque = csvIdxFuel = csvIdxTemp = csvIdxMaf = null;
            csvHeaderMap = null;
            csvHeaderWidth = null;
            lastRpm = lastTemp = lastFuel = lastMaf = lastTorque = null;
        }

        // Allow external callers (MainWindow) to cleanly disconnect the serial link
        public void ForceDisconnect()
        {
            try
            {
                cancellationTokenSource?.Cancel();

                if (serialPort?.IsOpen == true)
                {
                    serialPort.Close();
                }

                serialPort?.Dispose();
                serialPort = null;
                isConnected = false;

                // Reset all mapping state and buffers
                ResetCsvMapping();
                csvCalibBuffer.Clear();
                csvWidth = null;

                ConnectionStatusChanged?.Invoke(this, false);
                Console.WriteLine("Serial connection closed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error disconnecting: {ex.Message}");
            }
        }

        // Koreksi per-baris: perbaiki swap umum berdasarkan rentang wajar
        // RPM: 400..10000, Temp: 0..300 (umumnya 60..120), Fuel: 0..200, Torque: 0..1000, MAF: 0..500
        private void EnforceAndFixPerRow(ref SensorData d)
        {
            // 1) RPM vs Temperature
            if (d.RPM > 0 && d.Temperature > 0)
            {
                // Jika RPM tampak kecil seperti suhu, dan Temperature tampak besar seperti RPM → tukar
                if (d.RPM <= 300 && d.Temperature >= 400)
                {
                    int tmpRpm = d.RPM;
                    d.RPM = (int)Math.Round(d.Temperature);
                    d.Temperature = tmpRpm;
                }
                // Jika keduanya kecil (<=300), pilih nilai yang lebih cocok untuk suhu
                else if (d.RPM <= 300 && d.Temperature <= 300)
                {
                    bool rpmLooksTemp = d.RPM >= 50 && d.RPM <= 130;
                    bool tempLooksTemp = d.Temperature >= 50 && d.Temperature <= 130;

                    if (rpmLooksTemp && !tempLooksTemp)
                    {
                        d.Temperature = d.RPM;
                        d.RPM = (int)Math.Round(lastRpm.GetValueOrDefault(d.RPM * 3.0));
                    }
                    else if (!rpmLooksTemp && tempLooksTemp)
                    {
                        if (lastRpm.HasValue && lastRpm.Value >= 400) d.RPM = (int)Math.Round(lastRpm.Value);
                    }
                }
            }

            // 2) Temperature vs Fuel (BBM)
            if (d.Temperature > 0 && d.Fuel > 0)
            {
                // Temperature tak wajar >300 tapi Fuel masuk akal → swap
                if (d.Temperature > 300 && d.Fuel <= 200)
                {
                    double tmp = d.Temperature;
                    d.Temperature = d.Fuel;
                    d.Fuel = tmp;
                }
                // Temperature sangat kecil (<=30) dan Fuel >30 → kemungkinan kebalik
                else if (d.Temperature <= 30 && d.Fuel > 30 && d.Fuel <= 200)
                {
                    double tmp = d.Temperature;
                    d.Temperature = d.Fuel;
                    d.Fuel = tmp;
                }
            }

            // 3) RPM final enforcement menggunakan lastRpm jika tersedia
            if ((d.RPM <= 300 || d.RPM > 12000) && lastRpm.HasValue && lastRpm.Value >= 400 && lastRpm.Value <= 10000)
            {
                d.RPM = (int)Math.Round(lastRpm.Value);
            }

            // 4) Clamp ringan untuk menghindari outlier ekstrem
            d.Temperature = Clamp(d.Temperature, 0, 300);
            d.Fuel = Clamp(d.Fuel, 0, 10000);
            d.MAF = Clamp(d.MAF, 0, 2000);
            d.Torque = Clamp(d.Torque, 0, 5000);

            // Update last values
            if (d.RPM >= 400 && d.RPM <= 10000) lastRpm = d.RPM;
            if (d.Temperature > 0 && d.Temperature <= 300) lastTemp = d.Temperature;
            if (d.Fuel >= 0) lastFuel = d.Fuel;
            if (d.MAF >= 0) lastMaf = d.MAF;
            if (d.Torque >= 0) lastTorque = d.Torque;

            static double Clamp(double v, double lo, double hi) => v < lo ? lo : (v > hi ? hi : v);
        }

        // ====== Tambahan: Kalibrasi robust berbasis beberapa sampel ======

        private void AddCsvRowForCalibration(double[] row)
        {
            // Pastikan lebar konsisten; jika berubah, reset buffer
            if (row == null || row.Length == 0) return;

            if (csvWidth == null)
                csvWidth = row.Length;
            else if (csvWidth != row.Length)
            {
                csvCalibBuffer.Clear();
                csvWidth = row.Length;
            }

            // Simpan hanya baris yang lengkap (tanpa NaN semuanya)
            if (row.Any(double.IsNaN)) return;

            csvCalibBuffer.Add(row.ToArray());
            if (csvCalibBuffer.Count > 50) // batasi panjang buffer
                csvCalibBuffer.RemoveAt(0);
        }

        private bool TryCalibrateCsvMapping(bool force = false)
        {
            // Minimal sampel agar stabil
            if (!force && csvCalibBuffer.Count < 8) return false;
            if (csvWidth == null || csvWidth < 5) return false;

            int cols = csvWidth.Value;

            // PRIORITAS: jika persis 5 kolom, coba kalibrasi berbasis permutasi (pemetaan paling akurat)
            if (cols == 5 && TryResolveMappingByPermutation())
                return HasAllIndices();

            // Kumpulkan kolom
            var colsData = new List<double>[cols];
            for (int j = 0; j < cols; j++)
                colsData[j] = new List<double>(csvCalibBuffer.Count);

            foreach (var row in csvCalibBuffer)
                for (int j = 0; j < cols; j++)
                    colsData[j].Add(row[j]);

            // Hitung statistik tiap kolom
            double[] mean = new double[cols];
            double[] min = new double[cols];
            double[] max = new double[cols];
            double[] range = new double[cols];

            for (int j = 0; j < cols; j++)
            {
                var arr = colsData[j];
                min[j] = arr.Min();
                max[j] = arr.Max();
                mean[j] = arr.Average();
                range[j] = max[j] - min[j];
            }

            // Normalisasi pembobotan
            double maxOfRange = Math.Max(1e-9, range.Max());
            double maxOfMax = Math.Max(1e-9, max.Max());

            // 1) Deteksi RPM:
            // Skor = w1*(range norm) + w2*(max norm)
            int rpmIdx = -1;
            double bestRpmScore = double.MinValue;
            for (int j = 0; j < cols; j++)
            {
                double rNorm = range[j] / maxOfRange;
                double mNorm = max[j] / maxOfMax;
                double score = 0.6 * rNorm + 0.4 * mNorm;
                if (score > bestRpmScore) { bestRpmScore = score; rpmIdx = j; }
            }
            if (rpmIdx < 0) return false;

            // 2) Deteksi Temperature:
            // Kandidat dalam 0..300, pilih yang paling stabil dan mean terdekat 90
            int tempIdx = -1;
            double bestTempScore = double.MaxValue;
            for (int j = 0; j < cols; j++)
            {
                if (j == rpmIdx) continue;
                if (mean[j] < 0 || mean[j] > 300) continue; // suhu wajar
                // skor gabungan: kedekatan ke 90C dan kestabilan
                double score = Math.Abs(mean[j] - 90) + (range[j] * 0.2);
                if (score < bestTempScore) { bestTempScore = score; tempIdx = j; }
            }
            if (tempIdx < 0)
            {
                // fallback: pilih kolom paling stabil yang bukan RPM
                tempIdx = Enumerable.Range(0, cols).Where(j => j != rpmIdx).OrderBy(j => range[j]).First();
            }

            // 3) Fuel (BBM): pilih mean terkecil dari sisa (umumnya paling kecil)
            var remaining = Enumerable.Range(0, cols).Where(j => j != rpmIdx && j != tempIdx).ToList();
            int fuelIdx = remaining.OrderBy(j => mean[j]).First();

            // 4) MAF dan Torque dari sisa
            var rest = remaining.Where(j => j != fuelIdx).ToList();
            int mafIdx = -1, torqueIdx = -1;
            if (rest.Count == 1)
            {
                // Sisa satu → anggap torque
                torqueIdx = rest[0];
            }
            else if (rest.Count >= 2)
            {
                // Pilih MAF berdasarkan korelasi tertinggi dengan RPM
                double corrA = Math.Abs(Correlation(colsData[rpmIdx], colsData[rest[0]]));
                double corrB = Math.Abs(Correlation(colsData[rpmIdx], colsData[rest[1]]));
                if (corrA >= corrB) { mafIdx = rest[0]; torqueIdx = rest[1]; }
                else { mafIdx = rest[1]; torqueIdx = rest[0]; }
            }

            // Set mapping
            csvIdxRPM = rpmIdx;
            csvIdxTemp = tempIdx;
            csvIdxFuel = fuelIdx;
            csvIdxMaf = mafIdx >= 0 ? mafIdx : (int?)null;
            csvIdxTorque = torqueIdx >= 0 ? torqueIdx : (int?)null;

            return HasAllIndices();
        }

        // Coba semua permutasi untuk memetakan 5 kolom → [RPM, Torque, Fuel, Temp, MAF]
        // Pilih yang skornya terbaik berdasarkan range, mean, korelasi, dan validasi rentang nilai.
        private bool TryResolveMappingByPermutation()
        {
            try
            {
                if (csvWidth != 5 || csvCalibBuffer.Count < 8) return false;

                int n = csvCalibBuffer.Count;
                int cols = csvWidth!.Value;

                // Kumpulkan data per kolom
                var colsData = new List<double>[cols];
                for (int j = 0; j < cols; j++)
                    colsData[j] = new List<double>(n);

                foreach (var row in csvCalibBuffer)
                    for (int j = 0; j < cols; j++)
                        colsData[j].Add(row[j]);

                // Precompute statistik per kolom
                double[] mean = new double[cols], max = new double[cols], min = new double[cols], range = new double[cols], stepMeanAbs = new double[cols];
                for (int j = 0; j < cols; j++)
                {
                    var arr = colsData[j];
                    min[j] = arr.Min();
                    max[j] = arr.Max();
                    mean[j] = arr.Average();
                    range[j] = max[j] - min[j];
                    double sumStep = 0;
                    for (int i = 1; i < arr.Count; i++) sumStep += Math.Abs(arr[i] - arr[i - 1]);
                    stepMeanAbs[j] = sumStep / Math.Max(1, arr.Count - 1);
                }

                double maxRange = Math.Max(1e-9, range.Max());
                double maxMax = Math.Max(1e-9, max.Max());
                double maxStep = Math.Max(1e-9, stepMeanAbs.Max());

                // Helper score rentang valid
                double FractionInRange(List<double> a, double lo, double hi)
                {
                    int ok = a.Count(v => v >= lo && v <= hi);
                    return (double)ok / Math.Max(1, a.Count);
                }

                // Semua permutasi indeks [0..4]
                var indices = new[] { 0, 1, 2, 3, 4 };
                double bestScore = double.MinValue;
                (int r, int tq, int fu, int tp, int mf) best = (-1, -1, -1, -1, -1);

                foreach (var p in GetPermutations(indices, 5))
                {
                    int r = p[0], tq = p[1], fu = p[2], tp = p[3], mf = p[4];

                    // Hitung skor
                    double score = 0;

                    // RPM: variasi tinggi + puncak tinggi + fraksi di rentang wajar
                    double rpmRangeN = range[r] / maxRange;
                    double rpmMaxN = max[r] / maxMax;
                    double rpmIn = FractionInRange(colsData[r], 400, 10000);
                    score += 0.6 * rpmRangeN + 0.4 * rpmMaxN + 3.0 * rpmIn;

                    // Temp: 0..300, stabil, dekat 90C, perubahan antar-sampel kecil
                    double tempIn = FractionInRange(colsData[tp], 0, 300);
                    double tempStab = 1.0 - (range[tp] / Math.Max(1e-9, maxRange)); // makin kecil range → makin besar nilai
                    double tempNear = 1.0 - Math.Min(1.0, Math.Abs(mean[tp] - 90) / 90.0);
                    double tempStepSmall = 1.0 - (stepMeanAbs[tp] / maxStep);
                    score += 3.0 * tempIn + 1.5 * tempStab + 1.5 * tempNear + 1.0 * tempStepSmall;

                    // Fuel (BBM): biasanya kecil 0..200, mean relatif kecil
                    double fuelIn = FractionInRange(colsData[fu], 0, 200);
                    // prefer mean kecil → 1 - (mean / maxMean)
                    double maxMean = Math.Max(1e-9, mean.Max());
                    double fuelSmall = 1.0 - (mean[fu] / maxMean);
                    score += 2.5 * fuelIn + 1.0 * fuelSmall;

                    // MAF: 0..500, korelasi positif dengan RPM
                    double mafIn = FractionInRange(colsData[mf], 0, 500);
                    double corrMafRpm = Math.Abs(Correlation(colsData[r], colsData[mf]));
                    score += 2.0 * mafIn + 2.0 * corrMafRpm;

                    // Torque: 0..1000
                    double tqIn = FractionInRange(colsData[tq], 0, 1000);
                    score += 1.5 * tqIn;

                    // Penalti: korelasi Temp dengan RPM tinggi (tidak wajar)
                    double corrTempRpm = Math.Abs(Correlation(colsData[r], colsData[tp]));
                    score -= 1.5 * corrTempRpm;

                    // Bonus: RPM berubah lebih cepat dari Temp
                    double rpmStep = stepMeanAbs[r] / maxStep;
                    double tempStep = stepMeanAbs[tp] / maxStep;
                    if (rpmStep > tempStep) score += 0.5;

                    // Cek terbaik
                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = (r, tq, fu, tp, mf);
                    }
                }

                if (best.r >= 0)
                {
                    csvIdxRPM = best.r;
                    csvIdxTorque = best.tq;
                    csvIdxFuel = best.fu;
                    csvIdxTemp = best.tp;
                    csvIdxMaf = best.mf;
                    Console.WriteLine($"[CSV MAP PERM] RPM:{best.r} TORQUE:{best.tq} FUEL:{best.fu} TEMP:{best.tp} MAF:{best.mf} (score={bestScore:F2})");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Permutation mapping error: {ex.Message}");
            }

            return false;
        }

        // Generator permutasi sederhana
        private static IEnumerable<int[]> GetPermutations(int[] source, int length)
        {
            if (length == 1)
            {
                foreach (var t in source)
                    yield return new[] { t };
                yield break;
            }

            foreach (var t in source)
            {
                var remaining = source.Where(x => x != t).ToArray();
                foreach (var p in GetPermutations(remaining, length - 1))
                    yield return (new[] { t }).Concat(p).ToArray();
            }
        }

        private static double Correlation(List<double> a, List<double> b)
        {
            // Pearson correlation
            int n = Math.Min(a.Count, b.Count);
            if (n < 2) return 0;

            double meanA = a.Average();
            double meanB = b.Average();
            double cov = 0, varA = 0, varB = 0;

            for (int i = 0; i < n; i++)
            {
                double da = a[i] - meanA;
                double db = b[i] - meanB;
                cov += da * db;
                varA += da * da;
                varB += db * db;
            }

            if (varA == 0 || varB == 0) return 0;
            return cov / Math.Sqrt(varA * varB);
        }

        private int DetectRpmIndexSingle(double[] nums)
        {
            // Deteksi RPM untuk satu baris (heuristik minimal)
            int idx = -1;
            double bestScore = double.MinValue;

            for (int i = 0; i < nums.Length; i++)
            {
                double v = nums[i];
                if (double.IsNaN(v)) continue;

                // Skor: nilai besar lebih disukai, nilai > 500 sangat disukai
                double score = v;
                if (v > 500) score += 2000;
                if (score > bestScore)
                {
                    bestScore = score;
                    idx = i;
                }
            }
            return idx;
        }

        private bool HasAllIndices()
        {
            return csvIdxRPM.HasValue && csvIdxTorque.HasValue && csvIdxFuel.HasValue && csvIdxTemp.HasValue && csvIdxMaf.HasValue;
        }

        public void SendTestCommand()
        {
            try
            {
                if (IsConnected && serialPort != null)
                {
                    serialPort.WriteLine("TEST");
                    Console.WriteLine("Test command sent to logger");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending test command: {ex.Message}");
            }
        }

        public SensorData GetSimulatedData()
        {
            var random = new Random();

            return new SensorData
            {
                Time = DateTime.Now,
                Torque = 10 + random.NextDouble() * 40,
                Fuel = 5 + random.NextDouble() * 20,
                RPM = (int)(1000 + random.NextDouble() * 3000),
                Temperature = 60 + random.NextDouble() * 80,
                MAF = 8 + random.NextDouble() * 15
            };
        }

        // Method untuk test parsing temperature
        public void TestTemperatureParsing()
        {
            Console.WriteLine("\n=== TESTING TEMPERATURE PARSING ===");
            
            string[] testData = {
                "85.5,25.3,15.2,12.8,2500",  // CSV format with temp as first value
                "TEMP:85.5,TORQUE:25.3,FUEL:15.2,MAF:12.8,RPM:2500", // Key-value format
                "85.5", // Single temperature value
                "85.5,2500", // Two values: temp and RPM
                "85.5,25.3,15.2" // Three values: temp, torque, fuel
            };
            
            foreach (string data in testData)
            {
                Console.WriteLine($"\n--- Testing data: '{data}' ---");
                var result = ParseSensorData(data);
                Console.WriteLine($"Result Temperature: {result.Temperature}°C");
                Console.WriteLine($"Result RPM: {result.RPM}");
                Console.WriteLine($"Result Torque: {result.Torque}");
                Console.WriteLine($"Result Fuel: {result.Fuel}");
                Console.WriteLine($"Result MAF: {result.MAF}");
            }
            
            Console.WriteLine("\n=== END TEMPERATURE PARSING TEST ===\n");
        }

        public static string[] GetAvailablePorts()
        {
            return SerialPort.GetPortNames();
        }
    }
}