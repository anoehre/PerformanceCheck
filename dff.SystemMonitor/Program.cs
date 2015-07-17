using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Timers;

namespace dff.SystemMonitor
{
    class Program
    {
        static void Main(string[] args)
        {
            var shortTimer = new Timer();
            shortTimer.Elapsed += OnTimedEvent;
            shortTimer.Interval = 2000;
            shortTimer.Enabled = true;

            var dic= new Dictionary<string, string>();

            AddCpuAndRam(dic);
            AddDriveInfos(dic);

            foreach (var item in dic)
            {
                Console.WriteLine(item.Key + ": " + item.Value);
            }

            Console.WriteLine("Systemstart: " + StartTime);
            Console.WriteLine("Last Shutdown: " + GetLastShutdownDate());
            Console.WriteLine("CPU: " + ProcessorInfo());
            Console.WriteLine("Name: " + GetMachineName());
            Console.ReadLine();
        }

        public static DateTime StartTime
        {
            get
            {
                using (var uptime = new PerformanceCounter("System", "System Up Time"))
                {
                    uptime.NextValue();       //Call this an extra time before reading its value
                    return DateTime.Now.Subtract(TimeSpan.FromSeconds(uptime.NextValue()));
                }
            }
        }

        private static void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
            Console.WriteLine("CpuUsage: "+CpuUsage.ToString());
        }

        public static DateTime GetLastShutdownDate()
        {
            const string rKey = @"System\CurrentControlSet\Control\Windows";
            Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(rKey);

            const string rValueName = "ShutdownTime";
            var val = (byte[])key.GetValue(rValueName);
            long asLongValue = BitConverter.ToInt64(val, 0);
            return DateTime.FromFileTime(asLongValue);
        }

        public static string ProcessorInfo()
        {
            var report = string.Empty;
            var searcher = new ManagementObjectSearcher("select * from " + "Win32_Processor");
            foreach (ManagementObject moProcessor in searcher.Get())
            {

                if (moProcessor["maxclockspeed"] != null)
                    report += moProcessor["maxclockspeed"]+" / ";
                if (moProcessor["datawidth"] != null)
                    report += moProcessor["datawidth"] + " / ";
                if (moProcessor["name"] != null)
                    report += moProcessor["name"] + " / ";
                if (moProcessor["manufacturer"] != null)
                    report += moProcessor["manufacturer"] + " / ";


            }
            return report;
        }

        public static string GetMachineName()
        {
            return Environment.MachineName;
        }

        private static void AddDriveInfos(IDictionary<string, string> dictionary)
        {
            var allDrives = DriveInfo.GetDrives();
            foreach (var d in allDrives.Where(d => d.IsReady))
            {
                dictionary.Add("GB frei auf " + d.Name, ConvertBytesToGigabytes(d.TotalFreeSpace).ToString(
                    CultureInfo.GetCultureInfo("en-US").NumberFormat));
            }
        }

        private static void AddCpuAndRam(IDictionary<string, string> dic)
        {
            dic.Add("CPU", CpuUsage.ToString(CultureInfo.GetCultureInfo("en-US").NumberFormat));
            dic.Add("RAM Frei MB", MemoryUsage.ToString(CultureInfo.GetCultureInfo("en-US").NumberFormat));
        }

        private static double ConvertBytesToGigabytes(long bytes)
        {
            return Math.Round(((bytes / 1024f) / 1024f) / 1000, 2);
        }

        private static PerformanceCounter _cpuCounter;
        private static PerformanceCounter _ramCounter;
        private static float CpuUsage
        {
            get
            {
                if (_cpuCounter == null)
                {
                    _cpuCounter = new PerformanceCounter(
                        "Processor", "% Processor Time", "_Total", true);
                }
                return _cpuCounter.NextValue();
            }
        }

        private static float MemoryUsage
        {
            get
            {
                if (_ramCounter == null)
                    _ramCounter = new PerformanceCounter("Memory", "Available MBytes", true);
                return _ramCounter.NextValue();
            }
        }
    }
}
