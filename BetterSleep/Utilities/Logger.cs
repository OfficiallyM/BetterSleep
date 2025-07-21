using System.Diagnostics;
using System;
using System.IO;
using TLDLoader;

namespace BetterSleep.Utilities
{
	internal static class Logger
	{
		private static string _logFile = "";
		private static bool _initialised = false;
		public enum LogLevel
		{
			Debug,
			Info,
			Warning,
			Error,
			Critical
		}

		public static void Init()
		{
			if (!_initialised)
			{
				// Create logs directory.
				if (Directory.Exists(ModLoader.ModsFolder))
				{
					Directory.CreateDirectory(Path.Combine(ModLoader.ModsFolder, "Logs"));
					_logFile = ModLoader.ModsFolder + $"\\Logs\\{BetterSleep.Mod.ID}.log";
					File.WriteAllText(_logFile, $"{BetterSleep.Mod.Name} v{BetterSleep.Mod.Version} initialised\r\n");
					_initialised = true;
				}
			}
		}

		/// <summary>
		/// Log messages to a file.
		/// </summary>
		/// <param name="msg">The message to log</param>
		public static void Log(string msg, LogLevel logLevel = LogLevel.Info)
		{
			// Don't print debug messages outside of debug mode.
			if (!BetterSleep.debug && logLevel == LogLevel.Debug) return;

			if (_logFile != string.Empty)
				File.AppendAllText(_logFile, $"{DateTime.Now.ToString("s")} [{logLevel}] {msg}\r\n");

			// Show stack trace in debug mode for anything higher than a warning.
			if (BetterSleep.debug && logLevel >= LogLevel.Warning)
			{
				File.AppendAllText(_logFile, "Stack trace:\r\n");
				StackTrace stackTrace = new StackTrace(1);
				foreach (var frame in stackTrace.GetFrames())
					File.AppendAllText(_logFile, $"{frame.GetMethod()}\r\n");
			}
		}
	}
}
