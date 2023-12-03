using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace AMWin_RichPresence {
    internal class Logger {

        string logFile;

        public Logger() {
            var date = DateTime.Now.ToString("yyyy-MM-dd");
            var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Constants.AppDataFolderName);

            // delete old logs
            var logFiles = Directory.GetFiles(appDataPath).Where(f => f.EndsWith(".log"));
            if (logFiles.Count() > Constants.MaxLogFiles) {
                File.Delete(logFiles.Order().First());
            }

            // open new log file
            logFile = Path.Combine(appDataPath, $"{date}.log");
        }

        public void Log(string s) {
            var newMsg = $"[{DateTime.Now.ToString("HH:mm:ss")}] {s}";
#if DEBUG
            Trace.WriteLine(newMsg);
#endif
            File.AppendAllText(logFile, newMsg);
        }
    }
}
