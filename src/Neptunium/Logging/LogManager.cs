using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
        private static ManualResetEvent logFileMutex = null;

        public static bool IsInitialized { get; private set; }
        public static async Task InitializeAsync()
        {
            if (IsInitialized) return;

            try
            {
#if DEBUG
                logFileMutex = new ManualResetEvent(true);

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

        public static void Info(Type callingType, string message)
        {
            WriteLine(string.Format(logFormat, callingType.Name, "INFO", message, DateTime.Now));
        }

        public static void Warning(Type callingType, string message)
        {
            WriteLine(string.Format(logFormat, callingType.Name, "WARN", message, DateTime.Now));
        }
        public static void Error(Type callingType, string message)
        {
            WriteLine(string.Format(logFormat, callingType.Name, "ERROR", message, DateTime.Now));
        }
        public static void Log(Type callingType, string message)
        {
            WriteLine(string.Format(logFormat, callingType.Name, "LOG", message, DateTime.Now));
        }

        private static void WriteLine(string line)
        {
#if DEBUG
            try
            {
                logFileMutex.WaitOne();

                FileIO.AppendTextAsync(logFile, line + Environment.NewLine).AsTask().Wait();

                logFileMutex.Set();
            }
            catch (Exception)
            {
                
            }
#endif
        }

    }
}
