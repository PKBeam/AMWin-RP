using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;

namespace AMWin_RichPresence {
    internal class Logger {

        string? logFile;
        string? lastMsg;
        int msgRepeatCount = 0;

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

        ~Logger() {
            FlushLog();
        }

        public void Log(string msg) {
            if (msg == lastMsg) {
                msgRepeatCount++;
            } else {
                FlushLog();
                lastMsg = msg;
                WriteLog(msg);
            }
        }

        private void WriteLog(string msg) {
            if (logFile == null) {
                return;
            }

            var newMsg = $"[{DateTime.Now.ToString("HH:mm:ss")}] {msg}";
#if DEBUG
            Trace.WriteLine(newMsg);
#endif
            File.AppendAllText(logFile, $"{newMsg}\n");
        }

        private void FlushLog() {
            if (msgRepeatCount > 0) {
                WriteLog($"Previous message repeated {msgRepeatCount} times");
                msgRepeatCount = 0;
            }
        }
    }
}
