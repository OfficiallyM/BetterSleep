using UnityEngine;

namespace BetterSleep.Utilities
{
	internal static class TimeUtilities
	{
		public static string GetTimeString(float time)
		{
			int totalMinutes = Mathf.FloorToInt(time * 24f * 60f);
			int hours = totalMinutes / 60;
			int minutes = totalMinutes % 60;

			if (BetterSleep.twelveHourFormat)
			{
				string period = hours >= 12 ? "PM" : "AM";
				int displayHour = hours % 12;
				if (displayHour == 0) displayHour = 12;
				return $"{displayHour:D2}:{minutes:D2} {period}";
			}
			return $"{hours:D2}:{minutes:D2}";
		}
	}
}
