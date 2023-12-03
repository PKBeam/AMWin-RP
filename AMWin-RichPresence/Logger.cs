using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace AMWin_RichPresence {
    internal class Logger {

        string logFile;

        public Logger() {
            var date = DateTime.Now.ToString("yyyy-MM-dd");

            // delete old logs
            var logFiles = Directory.GetFiles(Constants.AppDataFolder).Where(f => f.EndsWith(".log"));
            if (logFiles.Count() > Constants.MaxLogFiles) {
                File.Delete(logFiles.Order().First());
            }

            // open new log file
            logFile = Path.Combine(Constants.AppDataFolder, $"{date}.log");
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
