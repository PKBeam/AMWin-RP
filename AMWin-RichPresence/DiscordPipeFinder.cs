using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace AMWin_RichPresence {

    public enum DiscordClientType {
        Auto = 0,
        Stable = 1,
        PTB = 2,
        Canary = 3
    }

    internal static class DiscordPipeFinder {

        const int PipeConnectTimeoutMs = 100;

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetNamedPipeServerProcessId(SafePipeHandle Pipe, out uint ServerProcessId);

        public static string? ProcessNameFor(DiscordClientType client) {
            return client switch {
                DiscordClientType.Stable => "Discord",
                DiscordClientType.PTB    => "DiscordPTB",
                DiscordClientType.Canary => "DiscordCanary",
                _ => null
            };
        }

        public static int? FindPipeForClient(DiscordClientType client, Logger? logger = null) {
            var wantedProcess = ProcessNameFor(client);
            if (wantedProcess == null) {
                return null;
            }

            string[] pipeNames;
            try {
                pipeNames = Directory.GetFiles(@"\\.\pipe\", "discord-ipc-*")
                    .Select(Path.GetFileName)
                    .Where(name => name != null)
                    .ToArray()!;
            } catch (Exception ex) {
                logger?.Log($"Could not enumerate Discord IPC pipes: {ex.Message}");
                return null;
            }

            foreach (var pipeName in pipeNames) {
                var owner = GetPipeServerProcessName(pipeName, logger);
                if (owner != null && string.Equals(owner, wantedProcess, StringComparison.OrdinalIgnoreCase)) {
                    var pipeNumber = ParsePipeNumber(pipeName);
                    if (pipeNumber != null) {
                        logger?.Log($"Matched Discord client '{client}' ({wantedProcess}) to {pipeName}");
                        return pipeNumber;
                    }
                }
            }

            logger?.Log($"No running '{wantedProcess}' IPC pipe found; falling back to autodetect");
            return null;
        }

        static string? GetPipeServerProcessName(string pipeName, Logger? logger) {
            try {
                using var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.In);
                pipe.Connect(PipeConnectTimeoutMs);

                if (!GetNamedPipeServerProcessId(pipe.SafePipeHandle, out uint serverPid)) {
                    return null;
                }

                using var process = Process.GetProcessById((int)serverPid);
                return process.ProcessName;
            } catch (Exception ex) {
                logger?.Log($"Could not inspect pipe {pipeName}: {ex.Message}");
                return null;
            }
        }

        static int? ParsePipeNumber(string pipeName) {
            var dash = pipeName.LastIndexOf('-');
            if (dash >= 0 && dash < pipeName.Length - 1
                && int.TryParse(pipeName.AsSpan(dash + 1), out var number)) {
                return number;
            }
            return null;
        }
    }
}
