﻿using System;
using System.Collections.Generic;
using System.Threading;

namespace BrightnessControl.Driver
{
    class Brightness : IDisposable
    {
        public struct MonitorInfo
        {
            public short Minimum;
            public short Current;
            public short Maximum;            
        }

        public event Action<int> BrightnessChanged;


        private readonly WMI wmiDriver = new WMI();
        private readonly MonitorInfo capabilities;

        /// <summary>
        /// Returns the current brightness value
        /// </summary>
        public int CurrentValue { get { return this.getCurrentValue(); } }


        /// <summary>
        /// Returns the monitor capabilities
        /// </summary>
        public MonitorInfo Capabilities { get { return this.capabilities; } }



        public Brightness()
        {
            this.wmiDriver.BrightnessChanged += (level) => this.BrightnessChanged?.Invoke(level);
            this.capabilities = this.getCapabilities();
        }


        /// <summary>
        /// Returns the initial value
        /// </summary>
        private int getCurrentValue()
        {
            var value = (int)this.getCapabilities().Current;
            if (value <= 0)
                value = this.wmiDriver.GetBrightness();

            return Math.Min(100, Math.Max(value, 0));
        }


        /// <summary>
        /// Returns the capabilities of the monitor
        /// </summary>
        private MonitorInfo getCapabilities()
        {
            var info = new MonitorInfo();
            try
            {
                using (var monitors = new Monitors())
                    NativeCalls.GetMonitorBrightness(monitors.Primary.hPhysicalMonitor, ref info.Minimum, ref info.Current, ref info.Maximum);

                info.Minimum = 0;
                info.Maximum = 100;
            }
            catch { }
            return info;
        }


        /// <summary>
        /// Sets the brightness level
        /// </summary>
        public bool SetBrightness(int brightness)
        {
            try
            {
                using (var monitors = new Monitors())
                    foreach (NativeStructures.PHYSICAL_MONITOR m in monitors)
                        NativeCalls.SetMonitorBrightness(m.hPhysicalMonitor, (short)brightness);

                this.wmiDriver.SetBrightness(brightness);
                return true;
            }
            catch { }
            return false;
        }

        /// <summary>
        /// Returns the brightness levels
        /// </summary>
        public int[] GetDefaultLevels()
        {
            var levels = new List<int>();
            foreach (var l in new int[] { 0, 10, 30, 60, 100 })
                if (l >= this.capabilities.Minimum && l <= this.capabilities.Maximum)
                    levels.Add(l);

            return levels.ToArray();
        }


        /// <summary>
        /// Returns the brightness levels
        /// </summary>
        public int[] GetLevels(int noOfLevels = 6)
        {
            var levels = new int[noOfLevels];
            var step = (this.capabilities.Maximum - this.capabilities.Minimum) / (noOfLevels - 1);

            for (int i = 0; i < noOfLevels; i++)
                levels[i] = this.capabilities.Minimum + i * step;

            return levels;
        }


        /// <summary>
        /// Turns the screen off
        /// </summary>
        public void TurnOff()
        {
            try
            {
                var t = new Thread(() =>
                {
                    try
                    {
                        NativeCalls.SendMessage(0xFFFF, 0x112, 0xF170, 2);
                    }
                    catch { }
                });
                t.Start();
                Thread.Sleep(500);
                t.Abort();
            }
            catch { }
        }


        public void Dispose()
        {
            this.wmiDriver.Dispose();
        }
    }
}
