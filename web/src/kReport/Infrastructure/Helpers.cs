using System;
using System.Text.RegularExpressions;

namespace kReport.Infrastructure
{
	public class Helpers
	{
		public static string Sanitize(string str)
		{
			if (str == null) return str;

			string temp = new Regex("[^a-zA-Z0-9 ]").Replace(str, "");
			if (temp.Length == 0)
			{
				temp = "Some crazy name";
			}

			return temp;
		}

		public static string Sanitize(string str, int maxLength)
		{
			if (str == null) return str;

			string temp = Sanitize(str);
			if (temp.Length > maxLength)
			{
				temp = temp.Substring(0, maxLength - 3) + "...";
			}

			return temp;
		}

		public static bool ValidateKey(string key)
		{
			Startup.RebuildConfiguration();
			string correctKey = Startup.Configuration.GetSection("kreport:key").Value;
			return key == correctKey;
		}
	}

	//Thank you http://stackoverflow.com/a/38064/382456
	public static class DateTimeExtensions
	{
		public static DateTime StartOfWeek(this DateTime dt, DayOfWeek startOfWeek)
		{
			int diff = dt.DayOfWeek - startOfWeek;
			if (diff < 0)
			{
				diff += 7;
			}

			return dt.AddDays(-1 * diff).Date;
		}
	}
}
