using BetterSleep.Utilities;
using UnityEngine;

namespace BetterSleep.Components
{
	internal class ClockController : MonoBehaviour
	{
		private float _time;
		private float _timeOffset;
		private float _displayTime;
		private const float FiveMinutes = 5f / 60f / 24f;
		public meterscript meter;

		public void SetStartTime(float t)
		{
			// Snap start time to the nearest 5 minute increment.
			_time = Mathf.Round(t / FiveMinutes) * FiveMinutes;
			_timeOffset = 0;
		}

		public void IncrementOffset(float amount)
		{
			_timeOffset += amount;
		}

		public void DecrementOffset(float amount) 
		{
			_timeOffset -= amount;
		}

		public void Update()
		{
			foreach (meterscript.szamlap szamlap in meter.szamlapok)
			{
				if (szamlap.tipus == "time")
				{
					float displayTime = _time + _timeOffset;
					// Clamp time between 0 and 1 with wrapping.
					displayTime %= 1f;
					if (displayTime < 0f) displayTime += 1f;
					displayTime = Mathf.Round(displayTime / FiveMinutes) * FiveMinutes;
					_displayTime = displayTime;
					displayTime *= 360f;
					szamlap.forgo.szamok[0].alakiertek = displayTime * 24f;
					szamlap.forgo.szamok[1].alakiertek = displayTime * 2f;
				}
			}
		}

		public float GetTime() => _displayTime;

		public string GetTimeString(float? time = null)
		{
			return TimeUtilities.GetTimeString(_displayTime);
		}
	}
}
