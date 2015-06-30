using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System;

namespace Neptunium.Logging
{
    public static class LogManager
    {
        private static StorageFile logFile = null;
        private const string logFormat = "{3} | [{0}]: {1} - {2}";

        public static bool IsInitialized { get; private set; }
        public static async Task InitializeAsync()
        {
            // if (IsInitialized) return;

            try
            {
#if DEBUG
                logFile = await DownloadsFolder.CreateFileAsync("NeptuniumLog.log", CreationCollisionOption.GenerateUniqueName);
#endif

                IsInitialized = true;
            }
            catch (Exception)
            {

            }
        }


        public static async Task<string> ReadLogAsync()
        {
            return await FileIO.ReadTextAsync(logFile);
        }

        public static async Task InfoAsync(Type callingType, string message)
        {
            await WriteLineAsync(string.Format(logFormat, callingType.Name, "INFO", message, DateTime.Now));
        }

        public static async Task WarningAsync(Type callingType, string message)
        {
            await WriteLineAsync(string.Format(logFormat, callingType.Name, "WARN", message, DateTime.Now));
        }
        public static async Task ErrorAsync(Type callingType, string message)
        {
            await WriteLineAsync(string.Format(logFormat, callingType.Name, "ERROR", message, DateTime.Now));
        }
        public static async Task LogAsync(Type callingType, string message)
        {
            await WriteLineAsync(string.Format(logFormat, callingType.Name, "LOG", message, DateTime.Now));
        }

        private static async Task WriteLineAsync(string line)
        {
#if DEBUG
            try
            {
                await FileIO.AppendTextAsync(logFile, line + Environment.NewLine);
            }
            catch (UnauthorizedAccessException)
            {
                
            }
#endif
        }

    }
}
