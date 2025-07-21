using BetterSleep.Components;
using BetterSleep.Utilities;
using TLDLoader;
using UnityEngine;

namespace BetterSleep
{
	public class BetterSleep : Mod
	{
		// Mod meta stuff.
		public override string ID => "M_BetterSleep";
		public override string Name => "BetterSleep";
		public override string Author => "M-";
		public override string Version => "1.0.0";

		internal static BetterSleep Mod;

		internal static bool debug = false;
		internal static bool twelveHourFormat = false;

		private SleepManager _sleepManager;

		internal static int screenWidth;
		internal static int screenHeight;

		public BetterSleep()
		{
			Mod = this;

			Utilities.Logger.Init();
		}

		public override void Config()
		{
			SettingAPI setting = new SettingAPI(this);

			// Debug stuff.
			debug = setting.GUICheckbox(debug, "Debug mode", 10, 10);
		}

		public override void OnLoad()
		{
			GameObject controller = new GameObject("BetterSleep");
			_sleepManager = controller.AddComponent<SleepManager>();
		}

		public override void Update()
		{
			if (mainscript.M.sleeping && !_sleepManager.IsSleeping())
			{
				mainscript.M.sleeping = false;
				mainscript.M.sleepEndTime = Time.time + 5f;
				_sleepManager.StartSleep(5f);
			}

			Save.ExecuteQueue();
		}

		public override void OnGUI()
		{
			// Find screen resolution.
			screenWidth = Screen.width;
			screenHeight = Screen.height;
			int resX = settingsscript.s.S.IResolutionX;
			int resY = settingsscript.s.S.IResolutionY;
			if (resX != screenWidth || resY != screenHeight)
			{
				screenWidth = resX;
				screenHeight = resY;
			}
		}
	}
}
