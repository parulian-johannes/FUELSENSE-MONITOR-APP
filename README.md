# 🚗 FuelSense Monitor App v3.0

Modern Desktop Application untuk Engine Monitoring & Fuel Consumption Analysis

## 📋 Overview

FuelSense Monitor App adalah aplikasi desktop berbasis WPF (.NET 6) untuk monitoring real-time sensor engine, analisis konsumsi bahan bakar, dan integrasi otomatis dengan web dashboard.

## ✨ Features

- **Real-time Monitoring**: RPM, Torque, MAF, Temperature, Fuel Consumption
- **Auto Web Integration**: Otomatis kirim data ke cloud database tanpa konfigurasi
- **Connection Indicator**: Status indikator real-time koneksi ke website
- **Data Visualization**: Chart, Analytics, dan Data Table
- **Excel Export**: Export data sensor ke Excel (.xlsx)
- **Alert System**: Notifikasi otomatis dengan audio alarm
- **Serial Communication**: Support COM port untuk hardware sensor
- **Calibration Tools**: Kalibrasi sensor dan scale adjustment

## 🏗️ Project Structure

```
FuelsenseMonitorApp/
├── Windows/              # XAML Window Files
│   ├── MainWindow.*
│   ├── AnalyticsWindow.*
│   ├── ChartWindow.*
│   ├── DataTableWindow.*
│   ├── SettingsWindow.*
│   ├── CalibrationWindow.*
│   └── ScaleCalibrationWindow.*
├── Services/             # Business Logic
│   ├── FuelSenseApiClient.cs
│   ├── SerialService.cs
│   └── ExcelService.cs
├── Models/               # Data Models
│   ├── SensorData.cs
│   ├── SettingsStore.cs
│   └── CalibrationRecord.cs
├── Controls/             # Custom Controls
│   └── SpeedometerControl.*
├── Properties/           # Assembly Info
├── publish/              # Release Builds
│   ├── Release/          # Latest Build
│   └── FuelsenseMonitorApp-v3.0.zip
└── FuelsenseMonitorApp.csproj
```

## 🚀 Quick Start

### Development

```powershell
# Build
dotnet build FuelsenseMonitorApp.csproj -c Release

# Run
dotnet run --project FuelsenseMonitorApp.csproj

# Publish
.\publish.ps1
```

### End User

1. Download `FuelsenseMonitorApp-v3.0.zip` dari folder `publish/`
2. Extract file
3. Run `FuelsenseMonitorApp.exe`
4. Aplikasi otomatis connect ke website!

## 🌐 API Integration

**Endpoint**: `https://capstone-website-snowy.vercel.app/api/sensor-data`

**Auto-enabled**: Data otomatis terkirim tanpa konfigurasi manual  
**Connection Status**: Indikator hijau/merah di footer aplikasi

## 🛠️ Tech Stack

- **.NET 6.0** - Windows Desktop Framework
- **WPF** - Windows Presentation Foundation
- **ScottPlot** - Data Visualization
- **EPPlus** - Excel Export
- **System.IO.Ports** - Serial Communication

## 📦 Dependencies

```xml
<PackageReference Include="System.IO.Ports" Version="7.0.0" />
<PackageReference Include="ScottPlot.WPF" Version="4.1.71" />
<PackageReference Include="System.Drawing.Common" Version="7.0.0" />
<PackageReference Include="EPPlus" Version="7.0.0" />
```

## 🔧 Configuration

Konfigurasi tersimpan otomatis di:
```
%APPDATA%\FuelsenseMonitor\settings.json
```

## 📝 License

Educational Project - Capstone 2025

## 👥 Contributors

Parulian Johannes & Team

---

**Version**: 3.0  
**Last Updated**: November 26, 2025  
**Repository**: https://github.com/parulian-johannes/FUELSENSE-MONITOR-APP
