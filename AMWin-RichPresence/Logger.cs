using DiscordRPC.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;

namespace AMWin_RichPresence {
    internal class Logger : ILogger {

        string? logFile;
        string? lastMsg;
        int msgRepeatCount = 0;

        public Logger() {
            var date = DateTime.Now.ToString("yyyy-MM-dd");

            if (!Directory.Exists(Constants.AppDataFolder)) {
                Directory.CreateDirectory(Constants.AppDataFolder);
            }

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
            System.Diagnostics.Trace.WriteLine(newMsg);
#endif
            File.AppendAllText(logFile, $"{newMsg}\n");
        }

        private void FlushLog() {
            if (msgRepeatCount > 0) {
                WriteLog($"Previous message repeated {msgRepeatCount} times");
                msgRepeatCount = 0;
            }
        }

        public LogLevel Level { get { return LogLevel.Error; } set { } }


        public void Trace(string message, params object[] args) {
            Log($"[RPC-TRACE] {string.Format(message, args)}");
        }

        public void Info(string message, params object[] args) {
            Log($"[RPC-INFO] {string.Format(message, args)}");
        }

        public void Warning(string message, params object[] args) {
            Log($"[RPC-WARN] {string.Format(message, args)}");
        }

        public void Error(string message, params object[] args) {
            Log($"[RPC-ERROR] {string.Format(message, args)}");
        }
    }
}
