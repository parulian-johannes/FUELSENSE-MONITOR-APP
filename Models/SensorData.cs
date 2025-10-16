using System;

namespace FuelsenseMonitorApp.Models
{
    public class SensorData
    {
        public DateTime Time { get; set; }
        public double Torque { get; set; }      // Nm
        public double Fuel { get; set; }        // gram
        public int RPM { get; set; }            // rpm (integer type)
        public double Temperature { get; set; }  // °C
        public double MAF { get; set; }         // g/s

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
