using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using CpuThreadingTest.ConsoleApp.Detectors;

namespace CpuThreadingTest.ConsoleApp
{
    internal sealed class RunConfiguration
    {
        private readonly IReadOnlyList<Type> _detectorsTypes;
        private const double DefaultSecondsPerMeasurement = 3;

        private RunConfiguration(IReadOnlyList<Type> detectorsTypes)
        {
            if (detectorsTypes == null) throw new ArgumentNullException(nameof(detectorsTypes));

            _detectorsTypes = detectorsTypes;

            WarmingSeconds = 5;
            double warmingSeconds;

            var secondsStr = ConfigurationManager.AppSettings["WarmingSeconds"];
            if (double.TryParse(secondsStr, NumberStyles.Float, CultureInfo.InvariantCulture, out warmingSeconds))
            {
                WarmingSeconds = warmingSeconds;
            }
        }

        public static RunConfiguration ParseFromCommandLine(string[] args)
        {
            var defaults = new List<Type>()
            {
                typeof(CpuInfoDetector),
                typeof(HyperthreadingDetector),
                typeof(NumaNodeDetector)
            };

            if (args == null || args.Length == 0)
            {
                return new RunConfiguration(defaults)
                {
                    DisplayUsage = true
                };
            }

            var exit = false;
            double secondsPerMeasure = DefaultSecondsPerMeasurement;
            var secondsPerMeasureInitialized = false;
            var detectors = new List<Type>();

            foreach (var arg in args)
            {
                double doubleValue;
                if (double.TryParse(arg, NumberStyles.Float, CultureInfo.InvariantCulture, out doubleValue))
                {
                    if (secondsPerMeasureInitialized)
                    {
                        exit = true;
                    }

                    secondsPerMeasure = doubleValue;
                    secondsPerMeasureInitialized = true;
                    continue;
                }

                var argument = arg.TrimStart('-', '/', '\\');

                if (argument.Is("?", "help", "h"))
                {
                    exit = true;
                    continue;
                }

                if (argument.Is("cpu", "cpuinfo"))
                {
                    detectors.Add(typeof(CpuInfoDetector));
                    continue;
                }

                if (argument.Is("hyper", "hyperthreading", "cores"))
                {
                    detectors.Add(typeof(HyperthreadingDetector));
                    continue;
                }

                if (argument.Is("numa", "socket", "sockets", "numaNode", "numaNodes"))
                {
                    detectors.Add(typeof(NumaNodeDetector));
                    continue;
                }

                exit = true;
                break;
            }

            if (!detectors.Any())
            {
                detectors.AddRange(defaults);
            }

            return new RunConfiguration(detectors)
            {
                SecondsPerMeasure = secondsPerMeasureInitialized ? secondsPerMeasure : DefaultSecondsPerMeasurement,
                Exit = exit,
                DisplayUsage = exit
            };
        }

        public bool Exit { get; private set; }
        public bool DisplayUsage { get; private set; }
        public double SecondsPerMeasure { get; private set; } = DefaultSecondsPerMeasurement;
        public double WarmingSeconds { get; private set; }

        public IReadOnlyList<IDetector> InstantiateDetectors()
        {
            return _detectorsTypes
                .Select(Activator.CreateInstance)
                .Cast<IDetector>()
                .ToList();
        }
    }

    internal static class Extensions
    {
        public static bool Is(this string value, params string[] comparands)
        {
            foreach (var comparand in comparands)
            {
                if (string.Equals(value, comparand, StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}