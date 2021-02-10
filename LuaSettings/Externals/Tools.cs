using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuaSettings.Externals
{
	internal static class Tools
	{
		public delegate string DateTimeDelegate(DateTime dateTime);
		private static readonly Dictionary<string, DateTimeDelegate> Formats = new Dictionary<string, DateTimeDelegate>
		{
			// abbreviated weekday name (e.g., Wed)
			{ "a", (dateTime) => dateTime.ToString("ddd", CultureInfo.CurrentCulture) },
			// full weekday name (e.g., Wednesday)
			{ "A", (dateTime) => dateTime.ToString("dddd", CultureInfo.CurrentCulture) },
			// abbreviated month name (e.g., Sep)
			{ "b", (dateTime) => dateTime.ToString("MMM", CultureInfo.CurrentCulture) },
			// full month name (e.g., September)
			{ "B", (dateTime) => dateTime.ToString("MMMM", CultureInfo.CurrentCulture) },
			// date and time (e.g., 09/16/98 23:48:10)
			{ "c", (dateTime) => dateTime.ToString("ddd MMM dd HH:mm:ss yyyy", CultureInfo.CurrentCulture) },
			// day of the month (16) (01-31)
			{ "d", (dateTime) => dateTime.ToString("dd", CultureInfo.CurrentCulture) },
			// day of the month, space-padded ( 1-31)
			{ "e", (dateTime) => dateTime.ToString("%d", CultureInfo.CurrentCulture).PadLeft(2, ' ') },
			// hour, using a 24-hour clock (00-23)
			{ "H", (dateTime) => dateTime.ToString("HH", CultureInfo.CurrentCulture) },
			// hour, using a 12-hour clock (01-12)
			{ "I", (dateTime) => dateTime.ToString("hh", CultureInfo.CurrentCulture) },
			// day of the year (001-366)
			{ "j", (dateTime) => dateTime.DayOfYear.ToString().PadLeft(3, '0') },
			//month (01-12)
			{ "m", (dateTime) => dateTime.ToString("MM", CultureInfo.CurrentCulture) },
			// minute (00-59)
			{ "M", (dateTime) => dateTime.Minute.ToString().PadLeft(2, '0') },
			// either "AM" or "PM"
			{ "p", (dateTime) => dateTime.ToString("tt",new CultureInfo("en-US")) },
			// second (00-59)
			{ "S", (dateTime) => dateTime.ToString("ss", CultureInfo.CurrentCulture) },
			// week number with the first Sunday as the first day of week one (00-53)   
			{ "U", (dateTime) => CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(dateTime, CultureInfo.CurrentCulture.DateTimeFormat.CalendarWeekRule, DayOfWeek.Sunday).ToString().PadLeft(2, '0') },
			// week number with the first Monday as the first day of week one (00-53)
			{ "W", (dateTime) => CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(dateTime, CultureInfo.CurrentCulture.DateTimeFormat.CalendarWeekRule, DayOfWeek.Monday).ToString().PadLeft(2, '0') },
			// weekday as a decimal number with Sunday as 0 (0-6)
			{ "w", (dateTime) => ((int) dateTime.DayOfWeek).ToString() },
			// date (e.g., 09/16/98)
			{ "x", (dateTime) => dateTime.ToString("d", CultureInfo.CurrentCulture) },
			// time (e.g., 23:48:10)
			{ "X", (dateTime) => dateTime.ToString("T", CultureInfo.CurrentCulture) },
			// two-digit year [00-99]
			{ "y", (dateTime) => dateTime.ToString("yy", CultureInfo.CurrentCulture) },
			// full year (e.g., 2014)
			{ "Y", (dateTime) => dateTime.ToString("yyyy", CultureInfo.CurrentCulture) },
			// Timezone name or abbreviation, If timezone cannot be termined, no characters
			{ "Z", (dateTime) => dateTime.ToString("zzz", CultureInfo.CurrentCulture) },
			// the character `%´
			{ "%", (dateTime) => "%" }
		};
		// http://www.cplusplus.com/reference/ctime/strftime/

		/// <summary>
		/// Format time as string
		/// </summary>
		/// <param name="dateTime">Instant in time, typically expressed as a date and time of day.</param>
		/// <param name="pattern">String containing any combination of regular characters and special format specifiers.They all begin with a percentage (%).</param>
		/// <returns>String with expanding its format specifiers into the corresponding values that represent the time described in dateTime</returns>
		public static string ToStrFTime(this DateTime dateTime, string pattern)
		{
			string output = "";
			int n = 0;

			if (string.IsNullOrEmpty(pattern)) { return dateTime.ToString(); }

			while (n < pattern.Length)
			{
				string s = pattern.Substring(n, 1);

				if (n + 1 >= pattern.Length)
					output += s;
				else
					output += s == "%"
						? Formats.ContainsKey(pattern.Substring(++n, 1)) ? Formats[pattern.Substring(n, 1)].Invoke(dateTime) : "%" + pattern.Substring(n, 1)
						: s;
				n++;
			}

			return output;
		}
	}
}
