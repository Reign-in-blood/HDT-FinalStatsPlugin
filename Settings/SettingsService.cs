using System;
using System.Diagnostics;
using System.IO;

namespace FinalStatsPlugin.Settings
{
    internal static class SettingsService
    {
        private const string FinalScreenshotOnlyOnKey =
            "FinalScreenshotOnlyOn";

        private static string SettingsPath =>
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData
                ),
                "HearthstoneDeckTracker",
                "HDT-FinalStatsPlugin",
                "settings.ini"
            );

        public static FinalStatsSettings Load()
        {
            FinalStatsSettings settings =
                new FinalStatsSettings();

            try
            {
                if (!File.Exists(SettingsPath))
                    return settings;

                foreach (string rawLine in File.ReadAllLines(SettingsPath))
                {
                    string line = rawLine?.Trim();
                    if (
                        string.IsNullOrWhiteSpace(line)
                        || line.StartsWith("#")
                    )
                    {
                        continue;
                    }

                    int separator = line.IndexOf('=');
                    if (separator <= 0)
                        continue;

                    string key = line
                        .Substring(0, separator)
                        .Trim();
                    string value = line
                        .Substring(separator + 1)
                        .Trim();

                    if (
                        string.Equals(
                            key,
                            FinalScreenshotOnlyOnKey,
                            StringComparison.OrdinalIgnoreCase
                        )
                        && Enum.TryParse(
                            value,
                            true,
                            out FinalScreenshotPlacementFilter filter
                        )
                        && Enum.IsDefined(
                            typeof(FinalScreenshotPlacementFilter),
                            filter
                        )
                    )
                    {
                        settings.FinalScreenshotOnlyOn = filter;
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(
                    "HDT-FinalStatsPlugin settings load failed: "
                    + ex
                );
            }

            return settings;
        }

        public static void Save(FinalStatsSettings settings)
        {
            if (settings == null)
                return;

            try
            {
                string directory =
                    Path.GetDirectoryName(SettingsPath);

                if (!string.IsNullOrWhiteSpace(directory))
                    Directory.CreateDirectory(directory);

                File.WriteAllText(
                    SettingsPath,
                    FinalScreenshotOnlyOnKey
                    + "="
                    + settings.FinalScreenshotOnlyOn
                    + Environment.NewLine
                );
            }
            catch (Exception ex)
            {
                Trace.WriteLine(
                    "HDT-FinalStatsPlugin settings save failed: "
                    + ex
                );
            }
        }
    }
}
