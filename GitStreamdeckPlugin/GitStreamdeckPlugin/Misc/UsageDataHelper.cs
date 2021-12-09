using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Avalonia;

namespace Plugin.Misc
{
    public static class UsageDataHelper<T>
    {
        public const string USAGE_DATA_FILE_NAME = "GitPluginUsageData.json";

        public static string APPLICATION_DIR = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        public static string APPLICATION_NAME = Application.Current.Name ?? "StreamdeckGitPlugin";

        private static string UsageDataLocation { get; } = Path.Combine(APPLICATION_DIR, APPLICATION_NAME, USAGE_DATA_FILE_NAME);

        public static IList<T> Load()
        {
            IList<T> usageData;

            if (File.Exists(UsageDataLocation))
            {
                usageData = JsonSerializer.Deserialize<IEnumerable<T>>(File.ReadAllText(UsageDataLocation)).ToList();
            }
            else
            {
                usageData = new List<T>();
                Save(usageData);
            }

            return usageData;
        }

        public static void Save(IEnumerable<T> usageData)
        {
            DirectoryInfo dir = new DirectoryInfo(Path.Combine(APPLICATION_DIR, APPLICATION_NAME));

            if(!dir.Exists)
                dir.Create();

            File.WriteAllText(UsageDataLocation, JsonSerializer.Serialize(usageData));
        }
    }
}
