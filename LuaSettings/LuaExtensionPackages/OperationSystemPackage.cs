using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using LuaSettings.Externals;
using Neo.IronLua;

namespace LuaSettings.LuaExtensionPackages
{
    public static class OperationSystemPackage
    {
		#region Public methods Custom

        public static string getcwd()
        {
            return Directory.GetCurrentDirectory();
        }

        public static string realpath(string path)
        {
            if (String.IsNullOrEmpty(path))
            {
                return String.Empty;
            }

            var pathToTest = path;

            if (!IsFullPath(pathToTest))
            {
                var currentDir = Directory.GetCurrentDirectory();
                pathToTest = PathCombine(currentDir, path);
            }

            return System.IO.Path.GetFullPath(pathToTest);
        }
		#endregion
		
		// Due to the fact that native OS calls are added as a static class package in the NeoLua, we can't easily extend it
		// Therefore it was necessary to duplicate the implementation here
		// All credit for methods below to the NeoLua library
		// https://github.com/neolithos/neolua/blob/70e796f1e59dab01df02207e37d2b210a430ff97/NeoLua/LuaLibraries.cs
		#region Public methods replicating 'native Lua' - from LuaLibraryOS - NeoLua
		

        private static readonly DateTime unixStartTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Local);

        public static LuaResult clock()
	        => new LuaResult(Process.GetCurrentProcess().TotalProcessorTime.TotalSeconds);

        /// <summary>Converts a number representing the date and time back to some higher-level representation.</summary>
        /// <param name="format">Format string. Same format as the C <see href="http://www.cplusplus.com/reference/ctime/strftime/">strftime()</see> function.</param>
        /// <param name="time">Numeric date-time. It defaults to the current date and time.</param>
        /// <returns>Formatted date string, or table of time information.</returns>
        /// <remarks>by PapyRef</remarks>
        public static object date(string format, object time)
        {
	        // Unix timestamp is seconds past epoch. Epoch date for time_t is 00:00:00 UTC, January 1, 1970.
	        DateTime dt;

	        var toUtc = format != null && format.Length > 0 && format[0] == '!';

	        if (time == null)
		        dt = toUtc ? DateTime.UtcNow : DateTime.Now;
	        else if (time is DateTime dt2)
	        {
		        dt = dt2;
		        switch (dt.Kind)
		        {
			        case DateTimeKind.Utc:
				        if (!toUtc)
					        dt = dt.ToLocalTime();
				        break;
			        case DateTimeKind.Unspecified:
			        case DateTimeKind.Local:
			        default:
				        if (toUtc)
					        dt = dt.ToUniversalTime();
				        break;
		        }
	        }
	        else
	        {
		        dt = unixStartTime.AddSeconds((long)Lua.RtConvertValue(time, typeof(long)));
		        if (toUtc)
			        dt = dt.ToUniversalTime();
	        }

	        // Date and time expressed as coordinated universal time (UTC).
	        if (toUtc)
		        format = format.Substring(1);

	        if (String.Compare(format, "*t", false) == 0)
	        {
		        var lt = new LuaTable
		        {
			        ["year"] = dt.Year,
			        ["month"] = dt.Month,
			        ["day"] = dt.Day,
			        ["hour"] = dt.Hour,
			        ["min"] = dt.Minute,
			        ["sec"] = dt.Second,
			        ["wday"] = (int)dt.DayOfWeek,
			        ["yday"] = dt.DayOfYear,
			        ["isdst"] = (dt.Kind == DateTimeKind.Local ? true : false)
		        };
		        return lt;
	        }
	        else
		        return Tools.ToStrFTime(dt, format);
        }
        // func date

        /// <summary>Calculate the current date and time, coded as a number. That number is the number of seconds since 
		/// Epoch date, that is 00:00:00 UTC, January 1, 1970. When called with a table, it returns the number representing 
		/// the date and time described by the table.</summary>
		/// <param name="table">Table representing the date and time</param>
		/// <returns>The time in system seconds. </returns>
		/// <remarks>by PapyRef</remarks>
		public static LuaResult time(LuaTable table)
		{
			TimeSpan ts;

			if (table == null)
			{
				// Returns the current time when called without arguments
				ts = DateTime.Now.Subtract(unixStartTime);
			}
			else
			{
				try
				{
					ts = datetime(table).Subtract(unixStartTime);
				}
				catch (Exception e)
				{
					return new LuaResult(null, e.Message);
				}
			}

			return new LuaResult(Convert.ToInt64(ts.TotalSeconds));
		}
		// func time

		/// <summary>Converts a time to a .net DateTime</summary>
		/// <param name="time"></param>
		/// <returns></returns>
		public static DateTime datetime(object time)
		{
			switch (time)
			{
				case LuaTable table:
					return new DateTime(
						table.ContainsKey("year") ? (int)table["year"] < 1970 ? 1970 : (int)table["year"] : 1970,
						table.ContainsKey("month") ? (int)table["month"] : 1,
						table.ContainsKey("day") ? (int)table["day"] : 1,
						table.ContainsKey("hour") ? (int)table["hour"] : 0,
						table.ContainsKey("min") ? (int)table["min"] : 0,
						table.ContainsKey("sec") ? (int)table["sec"] : 0,
						table.ContainsKey("isdst") ? (table.ContainsKey("isdst") == true) ? DateTimeKind.Local : DateTimeKind.Utc : DateTimeKind.Local
					);
				case int i32:
					return unixStartTime.AddSeconds(i32);
				case long i64:
					return unixStartTime.AddSeconds(i64);
				case double d:
					return unixStartTime.AddSeconds(d);
				default:
					throw new ArgumentException();
			}
		}
		// func datetime

		/// <summary>Calculate the number of seconds between time t1 to time t2.</summary>
		/// <param name="t2">Higher bound of the time interval whose length is calculated.</param>
		/// <param name="t1">Lower bound of the time interval whose length is calculated. If this describes a time point later than end, the result is negative.</param>
		/// <returns>The number of seconds from time t1 to time t2. In other words, the result is t2 - t1.</returns>
		/// <remarks>by PapyRef</remarks>
		public static long difftime(object t2, object t1)
		{
			var time2 = Convert.ToInt64(t2 is LuaTable ? time((LuaTable)t2)[0] : t2);
			var time1 = Convert.ToInt64(t1 is LuaTable ? time((LuaTable)t1)[0] : t1);

			return time2 - time1;
		}
		// func difftime

		internal static void SplitCommand(string command, out string fileName, out string arguments)
		{
			// check the parameter
			if (command == null)
				throw new ArgumentNullException(nameof(command));
			command = command.Trim();
			if (command.Length == 0)
				throw new ArgumentNullException(nameof(command));

			// split the command
			if (command[0] == '"')
			{
				var pos = command.IndexOf('"', 1);
				if (pos == -1)
				{
					fileName = command;
					arguments = null;
				}
				else
				{
					fileName = command.Substring(1, pos - 1).Trim();
					arguments = command.Substring(pos + 1).Trim();
				}
			}
			else
			{
				fileName = System.IO.Path.Combine(Environment.SystemDirectory, "cmd.exe");
				arguments = "/c " + command;
			}
		}
		// proc SplitCommand

		public static LuaResult execute(string command, Func<string, LuaResult> output, Func<string, LuaResult> error)
		{
			if (command == null)
				return new LuaResult(true);
			try
			{
				SplitCommand(command, out var fileName, out var arguments);
				var psi = new ProcessStartInfo(fileName, arguments)
				{
					RedirectStandardOutput = output != null,
					RedirectStandardError = error != null,
				};

				psi.UseShellExecute = !psi.RedirectStandardOutput && !psi.RedirectStandardError;
				psi.CreateNoWindow = !psi.UseShellExecute;

				using (var p = Process.Start(psi))
				{
					p.OutputDataReceived += (sender, e) => output.Invoke(e.Data);
					p.ErrorDataReceived += (sender, e) => error.Invoke(e.Data);
					p.EnableRaisingEvents = true;

					p.WaitForExit();
					return new LuaResult(true, "exit", p.ExitCode);
				}
			}
			catch (Exception e)
			{
				return new LuaResult(null, e.Message);
			}
		}
		// func execute

		public static void exit(int code = 0, bool close = true)
			=> Environment.Exit(code);

		public static string getenv(string varname)
			=> Environment.GetEnvironmentVariable(varname);

		public static LuaResult remove(string filename)
		{
			try
			{
				File.Delete(filename);
				return new LuaResult(true);
			}
			catch (Exception e)
			{
				return new LuaResult(null, e.Message);
			}
		}
		// func remove

		public static LuaResult rename(string oldname, string newname)
		{
			try
			{
				File.Move(oldname, newname);
				return new LuaResult(true);
			}
			catch (Exception e)
			{
				return new LuaResult(null, e.Message);
			}
		}
		// func rename

		public static void setlocale()
			=> throw new NotImplementedException();

		public static string tmpname()
			=> System.IO.Path.GetTempFileName();

		#endregion

		#region Private utils
		private static bool IsFullPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || path.IndexOfAny(System.IO.Path.GetInvalidPathChars()) != -1 || !System.IO.Path.IsPathRooted(path))
                return false;

            string pathRoot = System.IO.Path.GetPathRoot(path);
            if (pathRoot.Length <= 2 && pathRoot != "/") // Accepts X:\ and \\UNC\PATH, rejects empty string, \ and X:, but accepts / to support Linux
                return false;

            if (pathRoot[0] != '\\' || pathRoot[1] != '\\')
                return true; // Rooted and not a UNC path

            return pathRoot.Trim('\\').IndexOf('\\') != -1; // A UNC server name without a share name (e.g "\\NAME" or "\\NAME\") is invalid
        }

        private static string PathCombine(string path1, string path2)
        {
            if (System.IO.Path.IsPathRooted(path2))
            {
                path2 = path2.TrimStart(System.IO.Path.DirectorySeparatorChar);
                path2 = path2.TrimStart(System.IO.Path.AltDirectorySeparatorChar);
            }

            return System.IO.Path.Combine(path1, path2);
        }
        #endregion
    }
}
