# 🚗⚡ ENGINE MONITORING APP



**Modern Real-Time Engine Monitoring System with Advanced RPM Alert System**

## Cara Menjalankan Aplikasi



A sophisticated WPF/.NET 6 application for real-time monitoring of engine parameters with professional alert notifications and comprehensive analytics.### Metode 1: Menggunakan dotnet run (RECOMMENDED)

```bash

## 🌟 Featurescd "d:\FUELSENSE MONITOR APP"

dotnet run --project EngineMonitoring.csproj

### 📊 Real-Time Monitoring```

- **6 Speedometer Gauges**: RPM, Torque, MAF, Temperature, Fuel Consumption, and custom sensors

- **600px Modern Speedometers** with animated needles and professional styling### Metode 2: Build dan jalankan executable

- **Auto-Detecting Serial Connection** with COM port scanning (9600-115200 baud)```bash

- **Live Data Visualization** with real-time updatescd "d:\FUELSENSE MONITOR APP"

dotnet build EngineMonitoring.csproj

### 🚨 Advanced Alert System.\bin\Debug\net6.0-windows\win-x64\FuelsenseMonitorApp.exe

- **RPM Warning Threshold**: Customizable alert at 5000+ RPM```

- **Professional Audio Alerts**: Multi-layered sound system with Windows notifications

- **Smart Alert Patterns**: Sophisticated frequency progressions (750-1350Hz)### Metode 3: Menggunakan batch file

- **Visual Pulsing Animations** with smooth easing transitions```bash

- **Continuous Monitoring** with intelligent timing intervalscd "d:\FUELSENSE MONITOR APP"

.\run.bat

### 📈 Analytics & Reporting```

- **Separate Analytics Window** with ScottPlot integration

- **Real-Time Charts**: Line graphs for all sensor parameters## Troubleshooting

- **Session Statistics**: Min/Max/Average calculations

- **Data Export**: Excel export functionality with EPPlus### Jika dotnet run tidak bekerja:

- **Historical Data**: Complete session data collection- Pastikan menggunakan `--project EngineMonitoring.csproj`

- Masalah terjadi karena ada multiple project files di folder

### 🎨 Modern UI/UX

- **Borderless Window Design** with custom title bar controls### Jika build gagal:

- **Professional Vector Icons**: Minimize/close buttons with hover effects- Jalankan `dotnet restore` terlebih dahulu

- **Gradient Themes**: Modern dark theme with blue accents- Pastikan .NET 6 SDK terinstall

- **Responsive Layout**: Adaptive grid system with proper spacing

- **Smooth Animations**: QuadraticEase transitions throughout### System Requirements:

- .NET 6.0 Windows Runtime

## 🔧 Technical Specifications- Windows 7 atau lebih baru

- Visual Studio 2022 atau .NET 6 SDK

### System Requirements

- **OS**: Windows 10/11## Fitur Aplikasi:

- **Framework**: .NET 6.0 Windows- Real-time sensor monitoring (RPM, Torsi, MAF, Temperature, Fuel)

- **Dependencies**: - Data visualization dengan charts

  - ScottPlot.WPF (5.0.36)- Export data ke Excel

  - EPPlus (7.2.2)- Serial communication untuk data acquisition

  - System.IO.Ports (8.0.0)- Modern monochrome UI theme

### Architecture
- **MVVM Pattern** with proper separation of concerns
- **Async/Await** for non-blocking operations
- **Timer-based Updates** with configurable intervals
- **Thread-safe Operations** for serial communication
- **Memory Efficient** with proper resource disposal

## 🚀 Quick Start

### Installation
1. Clone the repository:
   ```bash
   git clone https://github.com/parulian-johannes/FUELSENSE-MONITOR-APP.git
   ```

2. Navigate to project directory:
   ```bash
   cd FUELSENSE-MONITOR-APP
   ```

3. Build the project:
   ```bash
   dotnet build EngineMonitoring.csproj
   ```

4. Run the application:
   ```bash
   dotnet run --project EngineMonitoring.csproj
   ```

### Alternative Running Methods

#### Method 1: Using dotnet run (RECOMMENDED)
```bash
cd "FUELSENSE-MONITOR-APP"
dotnet run --project EngineMonitoring.csproj
```

#### Method 2: Build and run executable
```bash
cd "FUELSENSE-MONITOR-APP"
dotnet build EngineMonitoring.csproj
.\bin\Debug\net6.0-windows\win-x64\EngineMonitoring.exe
```

#### Method 3: Using batch file (if available)
```bash
cd "FUELSENSE-MONITOR-APP"
.\run.bat
```

### Hardware Setup
1. **Connect Engine Control Unit** via USB/Serial (CH340 driver recommended)
2. **Configure COM Port**: Application auto-detects available ports
3. **Start Monitoring**: Click "Start Monitoring" button
4. **View Analytics**: Use "Analytics" button for detailed charts

## 📋 Usage Guide

### Basic Monitoring
1. Launch application
2. Auto-detection will scan for available COM ports
3. Click "Start Monitoring" to begin data collection
4. Monitor real-time speedometer readings
5. RPM alerts activate automatically at 5000+ RPM

### Analytics Features
1. Click "Analytics" button to open analytics window
2. View real-time charts for all parameters
3. Monitor session statistics (duration, min/max values)
4. Export data to Excel for further analysis

### Alert System
- **Automatic Alerts**: Trigger at RPM ≥ 5000
- **Manual Testing**: The in-UI "Test RPM Alert" button has been removed. To test alerts manually, either simulate high-RPM input via your logger/serial interface or set the RPM threshold in Settings and feed test data. The default threshold is 5000 RPM.
- **Audio Control**: Professional multi-tone alert sequences
- **Visual Feedback**: Pulsing red alert panel with status information

## 🛠️ Development

### Project Structure
```
FUELSENSE-MONITOR-APP/
├── MainWindow.xaml(.cs)      # Main monitoring interface
├── AnalyticsWindow.xaml(.cs) # Analytics and charting
├── Controls/
│   └── SpeedometerControl.xaml(.cs) # Custom speedometer widget
├── Models/
│   └── SensorData.cs         # Data model for sensor readings
├── Services/
│   ├── SerialService.cs      # Serial communication handling
│   └── ExcelService.cs       # Excel export functionality
└── Properties/
    └── AssemblyInfo.cs       # Application metadata
```

## 🔊 Alert System Details

### Audio Patterns
- **Initial Alert**: SystemSounds.Hand + SystemSounds.Exclamation
- **Main Sequence**: 9-tone frequency progression (750→1350→750Hz)
- **Attention Pattern**: 5x rapid 1000Hz professional beeps
- **Emphasis Sequence**: 800→1200→1000Hz final tones
- **Continuous**: Multi-pattern recurring alerts every 3 seconds

### Professional Design
- **No Harsh Frequencies**: Stays below 1400Hz threshold
- **Rhythmic Patterns**: Consistent timing for professional feel
- **Gradual Transitions**: Smooth frequency progressions
- **Layered Approach**: Multiple sound types working together

## 🔧 Troubleshooting

### If dotnet run doesn't work:
- Make sure to use `--project EngineMonitoring.csproj`
- Issue occurs due to multiple project files in folder

### If build fails:
- Run `dotnet restore` first
- Ensure .NET 6 SDK is installed

### System Requirements:
- .NET 6.0 Windows Runtime
- Windows 7 or newer
- Visual Studio 2022 or .NET 6 SDK

## 🤝 Contributing

1. Fork the repository
2. Create feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to branch (`git push origin feature/AmazingFeature`)
5. Open Pull Request

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

---

**Built with ❤️ for Professional Engine Monitoring**

## 📦 Share as EXE (no VS Code)

If you want to give this app to non-developers, publish a self-contained, single-file EXE and share the folder:

### Create the EXE (publisher)

On a Windows machine with .NET SDK installed:

```powershell
# From the project root
./publish.ps1 -Clean
```

This generates the portable app here:

- publish/win-x64/FuelsenseMonitorApp.exe

Zip the whole folder publish/win-x64 and send it to your friend.

### Run it (recipient)

1. Extract the zip to a normal folder (e.g., Desktop)
2. Double-click FuelsenseMonitorApp.exe
3. If Windows SmartScreen shows “Windows protected your PC”: click More info > Run anyway

Notes

- No .NET runtime installation is needed (self-contained build)
- If the app needs to access a USB/Serial device, ensure its driver (e.g., CH340/CP210x) is installed
- Keep the EXE inside the extracted folder; do not run from inside the ZIP