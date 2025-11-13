using System;

namespace EngineMonitoring.Models
{
    public class SensorData
    {
        public DateTime Time { get; set; }
        // Use NaN as a sentinel for "not provided" for doubles, and -1 for RPM
        public double Torque { get; set; } = double.NaN;      // Nm
        public double Fuel { get; set; } = double.NaN;        // gram
        public int RPM { get; set; } = -1;                    // rpm (integer type); -1 means unknown
        public double Temperature { get; set; } = double.NaN; // °C
        public double MAF { get; set; } = double.NaN;         // m/s (sensor value unit)

        public SensorData()
        {
            Time = DateTime.Now;
        }

        public SensorData(double torque, double fuel, int rpm, double temperature, double maf)
        {
            Time = DateTime.Now;
            Torque = torque;
            Fuel = fuel;
            RPM = rpm;
            Temperature = temperature;
            MAF = maf;
        }
    }
}
