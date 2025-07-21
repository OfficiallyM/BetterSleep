using BetterSleep.Core;
using BetterSleep.Extensions;
using BetterSleep.Utilities;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BetterSleep.Components
{
	internal class SleepManager : MonoBehaviour
	{
		// 5 minutes as fraction of a day.
		private const float FiveMinutes = 5f / 60f / 24f;
		// 2 days.
		private const float BlackoutThreshold = 48f;
		// 3 days.
		private const float HallucinationThreshold = 72f;
		// 5 days.
		private const float MaxAwake = 120f;

		public static SleepManager I;
		private Canvas _canvas;
		private Image _fadeImage;

		private GameObject _clock;
		private ClockController _clockController;
		private bool _isSelectingTime = false;
		private float _holdTimer = 0f;
		private float _repeatDelay = 0.5f;
		private float _repeatRate = 0.05f;
		private float _lastInputTime = 0f;
		private int _scrollDirection = 0;

		private Coroutine _fadeCoroutine;
		private bool _isSleeping = false;
		private float _blackoutDuration = 5f;
		private float _lastSleepQuality = 1f;
		private string _status = string.Empty;

		private float _tiredness = 0f;
		private float _lastSleepTime = 0f;
		private float _lastTirednessUpdate = 0f;
		private bool _isHavingBlackouts = false;
		private bool _isHavingHallucinations = false;

		private GUIStyle _statusStyle = new GUIStyle()
		{
			fontSize = 25,
			alignment = TextAnchor.MiddleCenter,
			normal = new GUIStyleState()
			{
				textColor = Color.white,
			}
		};

		private void Awake()
		{
			I = this;
			CreateFadeCanvas();
			CreateClock();
			TirednessData tirednessData = Save.GetTirednessData();
			if (tirednessData != null)
			{
				_tiredness = tirednessData.Tiredness;
				_lastSleepTime = tirednessData.LastSleepTime;
				_lastTirednessUpdate = tirednessData.LastTirednessUpdate;
				_lastSleepQuality = tirednessData.LastSleepQuality;
			}
			else
			{
				// No save data, use defaults.
				_lastSleepTime = _lastTirednessUpdate = mainscript.M.napszak.t;
			}
		}

		private void OnGUI()
		{
			if (_status != string.Empty)
				GUIExtensions.DrawOutline(new Rect((BetterSleep.screenWidth / 2) - 200f, BetterSleep.screenHeight * 0.1f, 400, 30), _status, _statusStyle, Color.black);

			if (_isSelectingTime)
			{
				GUIExtensions.DrawOutline(new Rect((BetterSleep.screenWidth / 2) - 200f, BetterSleep.screenHeight * 0.2f, 400, 30), $"Select wake up time\nCurrent time: {_clockController.GetTimeString(BetterSleep.twelveHourFormat, mainscript.M.napszak.currentTime)}\n\n{_clockController.GetTimeString(BetterSleep.twelveHourFormat)}", _statusStyle, Color.black);
				GUIExtensions.DrawOutline(new Rect((BetterSleep.screenWidth / 2) - 200f, BetterSleep.screenHeight * 0.8f, 400, 30), "Scroll wheel or up and down arrow keys to change time\nEnter to select", _statusStyle, Color.black);
			}
			else if (BetterSleep.debug)
			{
				GUIExtensions.DrawOutline(new Rect(5, 20, 400, 30), _clockController.GetTimeString(BetterSleep.twelveHourFormat, mainscript.M.napszak.currentTime), _statusStyle, Color.black);
				GUIExtensions.DrawOutline(new Rect(5, 50, 400, 30), $"Tiredness: {_tiredness * 100f:F3}%", _statusStyle, Color.black);
				float timeAwake = mainscript.M.napszak.t - _lastSleepTime;
				if (timeAwake < 0f)
					timeAwake += mainscript.M.napszak.dt + mainscript.M.napszak.nt;
				float timeAwakeHours = timeAwake / 60f;
				GUIExtensions.DrawOutline(new Rect(5, 80, 400, 30), $"Awake for: {timeAwakeHours:F2} hours", _statusStyle, Color.black);
			}
		}

		private void Update()
		{
			_clock.SetActive(_isSelectingTime);
            if (_isSelectingTime)
			{
				Time.timeScale = 1f;
				if (Input.GetButtonDown("Cancel"))
				{
					_isSelectingTime = false;
					SetGamePaused(false);
					mainscript.M.showcrosshair2 = true;
				}

				if (Input.mouseScrollDelta.y > 0f)
				{
					_clockController.IncrementOffset(FiveMinutes);
				}
				else if (Input.mouseScrollDelta.y < 0f)
				{
					_clockController.DecrementOffset(FiveMinutes);
				}

				bool upHeld = Input.GetKey(KeyCode.UpArrow);
				bool downHeld = Input.GetKey(KeyCode.DownArrow);
				bool keyDown = Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow);

				if (upHeld || downHeld)
				{
					int direction = upHeld ? 1 : -1;

					// Reset timer if key just pressed or direction changed
					if (keyDown || direction != _scrollDirection)
					{
						_holdTimer = 0f;
						_lastInputTime = Time.time;
						_scrollDirection = direction;

						AdjustTime(direction);
					}
					else
					{
						_holdTimer += Time.deltaTime;
						if (_holdTimer >= _repeatDelay)
						{
							if (Time.time - _lastInputTime >= _repeatRate)
							{
								AdjustTime(direction);
								_lastInputTime = Time.time;
							}
						}
					}
				}
				else
				{
					_scrollDirection = 0;
				}

				if (Input.GetKeyDown(KeyCode.Return))
				{
					_isSelectingTime = false;
					_isSleeping = true;
					SetGamePaused(false);
					StartCoroutine(Sleep(_clockController.GetTime()));
				}
			}

			// Control tiredness state.
			if (!_isSleeping)
			{
				float currentGameTime = mainscript.M.napszak.t;
				float deltaGameTime = currentGameTime - _lastTirednessUpdate;

				if (deltaGameTime < 0f)
					deltaGameTime += mainscript.M.napszak.dt + mainscript.M.napszak.nt;

				float deltaGameHours = deltaGameTime / 60f;

				float tirednessGain = deltaGameHours / MaxAwake;
				_tiredness = Mathf.Clamp01(_tiredness + tirednessGain);

				_lastTirednessUpdate = currentGameTime;

				float timeAwake = currentGameTime - _lastSleepTime;
				if (timeAwake < 0f)
					timeAwake += mainscript.M.napszak.dt + mainscript.M.napszak.nt;

				float timeAwakeHours = timeAwake / 60f;
				_isHavingBlackouts = timeAwakeHours >= BlackoutThreshold;
				_isHavingHallucinations = timeAwakeHours >= HallucinationThreshold;

				TirednessData tirednessData = new TirednessData(_tiredness, _lastSleepTime, _lastTirednessUpdate, _lastSleepQuality);
				Save.Upsert(tirednessData);
			}
		}

		public void StartSleep(float blackoutDuration)
		{
			if (_isSleeping) return;
			_blackoutDuration = blackoutDuration;
			_isSelectingTime = true;
			_clockController.SetStartTime(mainscript.M.napszak.currentTime);
			mainscript.M.showcrosshair2 = false;
			SetGamePaused(true);
		}

		public bool IsSleeping() => _isSleeping;
		public bool IsHavingBlackouts() => _isHavingBlackouts;
		public bool IsHavingHallucinations() => _isHavingHallucinations;

		private void CreateFadeCanvas()
		{
			GameObject canvasGO = new GameObject("SleepFadeCanvas");
			_canvas = canvasGO.AddComponent<Canvas>();
			_canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvasGO.AddComponent<CanvasScaler>();

			DontDestroyOnLoad(canvasGO);

			GameObject imageGO = new GameObject("FadeImage");
			imageGO.transform.SetParent(canvasGO.transform, false);

			_fadeImage = imageGO.AddComponent<Image>();
			_fadeImage.color = new Color(0, 0, 0, 0);
			_fadeImage.raycastTarget = false;

			RectTransform rt = _fadeImage.rectTransform;
			rt.anchorMin = Vector2.zero;
			rt.anchorMax = Vector2.one;
			rt.offsetMin = Vector2.zero;
			rt.offsetMax = Vector2.zero;
		}

		private void CreateClock()
		{
			_clock = GameObject.Instantiate(itemdatabase.d.gclock01);
			foreach (Renderer renderer in _clock.GetComponentsInChildren<MeshRenderer>().Where(m => m.name.ToLower().StartsWith("cock")))
				renderer.enabled = false;
			foreach (Collider componentsInChild in _clock.GetComponentsInChildren<Collider>())
				componentsInChild.enabled = false;
			Destroy(_clock.GetComponent<Rigidbody>());
			_clock.transform.SetParent(mainscript.M.player.Cam.transform);
			_clock.transform.localPosition = new Vector3(0, 0, 0.1f);
			_clock.transform.localRotation = Quaternion.identity;
			_clockController = _clock.AddComponent<ClockController>();
			_clockController.meter = _clock.GetComponent<meterscript>();
			Destroy(_clock.GetComponent<clockscript>());
			_clock.SetActive(false);
		}

		private void StartFadeToBlack(float duration)
		{
			if (_fadeCoroutine != null)
				StopCoroutine(_fadeCoroutine);
			_fadeCoroutine = StartCoroutine(FadeToBlack(duration));
		}

		private void StartFadeFromBlack(float duration)
		{
			if (_fadeCoroutine != null)
				StopCoroutine(_fadeCoroutine);
			_fadeCoroutine = StartCoroutine(FadeFromBlack(duration));
		}

		private IEnumerator FadeToBlack(float duration)
		{
			float timer = 0f;
			while (timer < duration)
			{
				float t = timer / duration;
				_fadeImage.color = new Color(0, 0, 0, t);
				timer += Time.deltaTime;
				yield return null;
			}
			_fadeImage.color = new Color(0, 0, 0, 1);
		}

		private IEnumerator FadeFromBlack(float duration)
		{
			float timer = 0f;
			while (timer < duration)
			{
				float t = timer / duration;
				_fadeImage.color = new Color(0, 0, 0, 1 - t);
				timer += Time.deltaTime;
				yield return null;
			}
			_fadeImage.color = new Color(0, 0, 0, 0);
		}

		private IEnumerator Sleep(float time)
		{
			_lastSleepQuality = CalculateSleepQuality();
			I.StartFadeToBlack(1.5f);
			yield return new WaitForSeconds(1.5f);
			yield return new WaitForSeconds(_blackoutDuration * _lastSleepQuality);

			napszakvaltakozas napszak = mainscript.M.napszak;
			float preSleepTime = napszak.t;
			float totalDayTime = napszak.dt + napszak.nt;
			float wakeTime = time * totalDayTime;

			// Bias toward waking up too early with poor sleep quality.
			bool sleepFully = UnityEngine.Random.value <= _lastSleepQuality;

			if (!sleepFully)
			{
				float maxEarlyWake = totalDayTime * 0.4f * (1f - _lastSleepQuality);
				// Always wake earlier, never later.
				float offset = UnityEngine.Random.Range(0f, maxEarlyWake);
				wakeTime -= offset;

				// Wrap around if before 0.
				if (wakeTime < 0f)
					wakeTime += totalDayTime;
			}
			napszak.tekeres = wakeTime - napszak.time + napszak.startTime;

			// Wait until the new time has applied.
			yield return new WaitForFixedUpdate();

			// Reduce tiredness.
			float sleepDuration = napszak.t - preSleepTime;
			if (sleepDuration < 0f)
				sleepDuration += totalDayTime;
			float sleepDurationHours = sleepDuration / 60f;
			// Allow even poor quality sleep to have at least some effect.
			float qualityFactor = Mathf.Lerp(0.15f, 1f, _lastSleepQuality);
			float effectiveRestHours = sleepDurationHours * qualityFactor;
			float tirednessReduction = 1f - Mathf.Exp(-effectiveRestHours / 5f);
			_tiredness = Mathf.Clamp01(_tiredness - tirednessReduction);
			_lastSleepTime = _lastTirednessUpdate = mainscript.M.napszak.t;

			yield return null;

			I.StartFadeFromBlack(1.5f);
			_isSleeping = false;
			mainscript.M.showcrosshair2 = true;

			// Show quality status.
			_status = GetSleepQualityFeedback(_lastSleepQuality);
			if (!sleepFully)
				_status += "\n" + GetEarlyWakeMessage();
			yield return new WaitForSeconds(4f);
			_status = string.Empty;
		}

		private float CalculateSleepQuality() => Mathf.Clamp01(GetSleepLocationQuality() * GetSleepEnvironmentQuality());

		private float GetSleepLocationQuality()
		{
			fpscontroller player = mainscript.M.player;

			// Standing, default to 0.1.
			float quality = 0.1f;

			seatscript seat = player.seat;
			if (seat != null)
			{
				// Sleeping on something environmental.
				if (seat.transform.root.name == "G_RoadBuildingsParent")
					quality = 0.25f;
				else if (seat.Car != null)
				{
					if (seat.driverSeat0 || seat.driverSeat1)
						// Sleeping in front of vehicle.
						quality = 0.55f;
					else
						// Sleeping in rear of vehicle.
						quality = 0.7f;
				}
				else if (seat.transform.parent != null && seat.transform.parent.name.ToLower().Contains("bed"))
				{
					// Sleeping in bed.
					quality = 1f;
				}
				else
					// Sleeping on any other seat.
					quality = 0.4f;
			}
			return quality;
		}

		private float GetSleepEnvironmentQuality()
		{
			// TODO: Expand with envrionmental factors:
			// Temp, inside, outside, current time, etc.
			return 1;
		}

		private string GetSleepQualityFeedback(float sleepQuality)
		{
			string[] messages;

			if (sleepQuality <= 0.3f)
			{
				messages = new string[]
				{
					"You awake feeling exhausted",
					"You awake feeling barely rested",
					"You awake feeling drained",
					"You awake with a headache and a strange sense of deja vu",
					"You awake feeling no better than before"
				};
			}
			else if (sleepQuality <= 0.5f)
			{
				messages = new string[]
				{
					"You awake feeling a bit groggy",
					"You awake feeling tired and sluggish",
					"You awake feeling somewhat unrested",
					"You awake with a strange ache in your neck, likely from sleeping poorly",
					"You awake in a haze, wondering if you even slept"
				};
			}
			else if (sleepQuality <= 0.7f)
			{
				messages = new string[]
				{
					"You awake feeling mildly rested",
					"You awake feeling a bit better, but still tired",
					"You awake feeling somewhat refreshed, but not fully",
					"You awake with a slight headache, but it’s manageable",
					"You awake in the middle of a bizarre dream, feeling a little confused"
				};
			}
			else if (sleepQuality <= 0.9f)
			{
				messages = new string[]
				{
					"You awake feeling well rested",
					"You awake feeling refreshed and ready to go",
					"You awake feeling pretty good",
					"You awake feeling optimistic, like today’s going to be a good day",
					"You awake feeling ready to take on the world"
				};
			}
			else
			{
				messages = new string[]
				{
					"You awake feeling fully rested and energetic",
					"You awake feeling rejuvenated and ready for the day",
					"You awake feeling excellent",
					"You awake feeling like you could take on the world",
					"You awake feeling amazing, like a new person!"
				};
			}

			return messages[UnityEngine.Random.Range(0, messages.Length)];
		}

		private string GetEarlyWakeMessage()
		{
			string[] messages = new[]
			{
				"You woke up earlier than you meant to",
				"You stir from sleep sooner than expected",
				"Something woke you up before you were ready",
				"Your eyes open, but it doesn’t feel like enough",
				"You didn't quite sleep as long as you planned"
			};
			return messages[UnityEngine.Random.Range(0, messages.Length)];
		}

		private void AdjustTime(int direction)
		{
			if (direction > 0)
				_clockController.IncrementOffset(FiveMinutes);
			else
				_clockController.DecrementOffset(FiveMinutes);
		}

		private void SetGamePaused(bool isPaused)
		{
			mainscript.M.crsrLocked = !isPaused;
			mainscript.M.menu.gameObject.SetActive(!isPaused);
		}
	}
}
