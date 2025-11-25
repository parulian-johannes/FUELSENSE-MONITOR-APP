# API Integration Guide - FuelSense Monitor App

## 📋 Overview

Aplikasi desktop sekarang sudah terintegrasi dengan API web dashboard untuk mengirim data sensor secara real-time.

---

## 🌐 Endpoint API

**Production URL:**
```
https://capstone-website-snowy.vercel.app/api/sensor-data
```

**Method:** `HTTP POST`

**Content-Type:** `application/json`

---

## 📦 Format Data yang Dikirim

```json
{
  "rpm": 3500,
  "torque": 125.5,
  "maf": 45.2,
  "temperature": 85.3,
  "fuelConsumption": 8.5,
  "customSensor": null,
  "alertStatus": null,
  "timestamp": "2024-11-26T10:30:45.123Z"
}
```

### Field Descriptions:
- **rpm** (Number, Required): Engine RPM
- **torque** (Number, Required): Engine torque in Nm (sudah dikalibrasi)
- **maf** (Number, Required): Mass Air Flow in m/s
- **temperature** (Number, Required): Engine temperature in °C
- **fuelConsumption** (Number, Required): Fuel consumption in liters
- **customSensor** (Number, Optional): Custom sensor value
- **alertStatus** (Boolean, Optional): Alert status
- **timestamp** (String, Auto): ISO 8601 timestamp (UTC)

---

## ⚙️ Konfigurasi di Aplikasi

### 1. Buka Settings Window
Klik tombol **"⚙️ SETTINGS"** di aplikasi utama.

### 2. Scroll ke Section "API INTEGRATION"
Anda akan melihat:
- **Server URL**: URL endpoint API (default: production URL)
- **Send Interval**: Interval pengiriman data (2-10 detik)
- **Enable API Data Sending**: Checkbox untuk mengaktifkan/menonaktifkan API

### 2. Test Connection
1. Pastikan URL sudah benar: `https://capstone-website-snowy.vercel.app`
2. Klik tombol **"🔍 Test Connection"**
3. Tunggu hasil test:
   - ✅ **Connected** (hijau): API siap menerima data
   - ❌ **Failed** (merah): Tidak bisa terhubung ke API

### 3. Enable API Sending
1. Centang checkbox **"Send sensor data to web dashboard"**
2. Klik **"💾 SAVE SETTINGS"**
3. Status indicator akan berubah menjadi hijau

---

## 🔄 Cara Kerja Integration

### Automatic Data Sending
- Setiap kali sensor mengirim data baru, aplikasi **otomatis** mengirim ke API
- Pengiriman dilakukan **asynchronous** (non-blocking)
- Tidak akan menyebabkan aplikasi hang jika API lambat/down

### Error Handling
- Jika API gagal, error akan di-log ke Console (tidak crash aplikasi)
- Data tetap disimpan lokal di aplikasi
- User bisa tetap monitoring meskipun API down

### Retry Mechanism
- Class `FuelSenseApiClient` mendukung retry dengan exponential backoff
- Method: `SendWithRetryAsync()` (optional, tidak digunakan secara default)
- Retry interval: 1s, 2s, 3s

---

## 📝 Implementation Details

### Files Modified:
1. **Services/FuelSenseApiClient.cs** - HTTP client untuk API communication
2. **MainWindow.xaml.cs** - Integration di data collection loop
3. **SettingsWindow.xaml** - UI untuk API configuration
4. **SettingsWindow.xaml.cs** - Logic untuk test connection & save settings

### Key Code Snippet (MainWindow.xaml.cs):
```csharp
// Send data to API asynchronously (non-blocking)
_ = Task.Run(async () =>
{
    try
    {
        await apiClient.SendSensorDataAsync(
            rpm: data.RPM,
            torque: calibratedTorque,
            maf: data.MAF,
            temperature: data.Temperature,
            fuelConsumption: data.Fuel
        );
    }
    catch (Exception apiEx)
    {
        Console.WriteLine($"[API] Background send error: {apiEx.Message}");
    }
});
```

---

## 🔧 Troubleshooting

### ❌ Connection Test Failed
**Kemungkinan penyebab:**
1. Tidak ada koneksi internet
2. URL API salah
3. Server API sedang down
4. Firewall/antivirus memblokir koneksi

**Solusi:**
- Cek koneksi internet
- Pastikan URL benar (https://stingray-app-2envv.ondigitalocean.app)
- Cek Console output untuk detail error
- Disable firewall sementara untuk test

### ⚠️ Data Tidak Muncul di Web Dashboard
**Kemungkinan penyebab:**
1. API integration belum diaktifkan
2. Settings belum disimpan
3. Format data tidak sesuai

**Solusi:**
1. Buka Settings → API Integration
2. Pastikan checkbox "Send sensor data to web dashboard" tercentang
3. Klik "SAVE SETTINGS"
4. Cek Console output untuk log pengiriman:
   - `✅ [API] Data sent successfully` - Berhasil
   - `❌ [API] Failed` - Gagal

### 🐌 Aplikasi Terasa Lambat
**Penyebab:**
- Send interval terlalu cepat

**Solusi:**
- Buka Settings → API Integration
- Ubah "Send Interval" ke 5 atau 10 detik
- Klik "SAVE SETTINGS"

---

## 📊 Monitoring API Activity

### Console Logs
Aplikasi akan menampilkan log di Console:

```
✅ [API] Data sent successfully - Status: 200
✅ [API] Connection successful
❌ [API] Network Error: No such host is known
❌ [API] Timeout: The operation has timed out
```

### Status Indicator
Di Settings Window, perhatikan:
- **🟢 Connected**: API aktif dan terkoneksi
- **🔴 Failed/Disconnected**: API tidak bisa diakses
- **🟡 Testing...**: Sedang test connection
- **⚪ Disabled**: API integration dimatikan

---

## 🚀 Best Practices

### Recommended Settings:
- **Send Interval**: 3-5 seconds
  - 2s: Terlalu cepat, bisa overload server
  - 10s: Terlalu lambat, data tidak real-time
  - **3s**: RECOMMENDED - Balance antara real-time & performance

### During Testing:
- Enable API hanya saat sudah siap monitoring
- Test connection dulu sebelum enable
- Monitor Console untuk error logs

### Production Use:
- Pastikan internet connection stabil
- Gunakan interval 3-5 detik
- Backup data dengan Excel export secara berkala

---

## 🔐 Security Notes

- API menggunakan HTTPS (encrypted)
- Tidak ada authentication token (public endpoint)
- Data sensor tidak mengandung informasi sensitif
- Timestamp menggunakan UTC untuk konsistensi timezone

---

## 📞 Support

Jika ada masalah dengan API integration:
1. Cek Console output untuk detail error
2. Test connection di Settings
3. Pastikan URL API benar
4. Cek koneksi internet

**API Server Status:**
```
Health Check: https://capstone-website-snowy.vercel.app/api/health
```

---

## 📅 Version History

**v2.0 - API Integration (November 26, 2024)**
- ✅ Implemented FuelSenseApiClient
- ✅ Added Settings UI for API configuration
- ✅ Automatic data sending on sensor read
- ✅ Connection status indicator
- ✅ Error handling & logging
- ✅ Non-blocking async send
- ✅ Test connection feature

---

## 🎯 Future Enhancements

Potensi fitur tambahan (belum diimplementasi):
- [ ] Offline data buffering (queue data when offline)
- [ ] Batch sending untuk efisiensi
- [ ] API authentication dengan token
- [ ] Statistics dashboard (success rate, latency, etc.)
- [ ] Manual retry button
- [ ] Export log ke file

---

**Last Updated:** November 26, 2024  
**Status:** ✅ PRODUCTION READY
