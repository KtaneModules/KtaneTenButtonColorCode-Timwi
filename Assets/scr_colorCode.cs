using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using KMBombInfoHelper;
using UnityEngine;

using Random = UnityEngine.Random;
using Math = UnityEngine.Mathf;

public class scr_colorCode : MonoBehaviour {
	public KMAudio BombAudio;
	public KMBombModule BombModule;
	public KMBombInfo BombInfo;
	public KMSelectable[] ModuleButtons;
	public KMSelectable ModuleSelect, SubmitButton;
	public GameObject[] ModuleLights;

	readonly Color[] buttonColors = { new Color32(255, 58, 58, 255), new Color32(0, 226, 26, 255), new Color32(0, 136, 255, 255) };
	int[] nowColors = new int[10];
	readonly int[] prevColors = new int[10];
	int[] solColors = new int[10];

	string serialNum = "";
	int initRules;
	delegate bool checkRules(int x);
	checkRules[] ruleList;
	bool nextStage;
	bool solvedFirst;

	bool moduleSolved;

	static int moduleIdCounter = 1;
	int moduleId;

	void Start() {
		moduleId = moduleIdCounter++;
		ShuffleColors(false);

		for (int i = 0; i < 10; i++) {
			int j = i;

			ModuleButtons[i].OnInteract += delegate() {
				OnButtonPress(j);

				return false;
			};
		}

		SubmitButton.GetComponent<Renderer>().material.color = new Color32(239, 228, 176, 255);
		SubmitButton.OnInteract += delegate() {
			OnSubmitPress();

			return false;
		};

		var lightScalar = transform.lossyScale.x;

		for (int i = 0; i < ModuleLights.Length; i++) {
			ModuleLights[i].transform.GetChild(0).GetComponent<Light>().range *= lightScalar;
		}

		serialNum = BombInfo.GetSerialNumber();
		initRules = int.Parse(serialNum[5].ToString());
		Debug.LogFormat(@"[Ten-Button Color Code #{0}] Starting rule number in stage 1 is: {1}", moduleId, initRules);
		ruleList = new checkRules [4] {
			((x) => solColors[x] == solColors[x + 1]),
			((x) => solColors[x] == solColors[x + 5]),
			((x) => solColors[x] == solColors[x + 1] && solColors[x + 1] == solColors[x + 2]),
			((x) => solColors[x] == solColors[x + 1] && solColors[x + 1] == solColors[x + 5] && solColors[x + 5] == solColors[x + 6])
		};

		SetRules();
	}

	void Update() {
		if (nextStage) {
			ShuffleColors(false);
			initRules = 0;

			for (int i = 0; i < 6; i++) {
				initRules += (char.IsDigit(serialNum[i])) ? int.Parse(serialNum[i].ToString()) : 0;
			}
				
			initRules %= 10;
			Debug.LogFormat(@"[Ten-Button Color Code #{0}] Starting rule number in stage 2 is: {1}", moduleId, initRules);
			SetRules();
			nextStage = false;
		}
	}

	void ShuffleColors(bool returnPrev) {
		for (int i = 0; i < 10; i++) {
			if (!returnPrev) {
				nowColors[i] = Random.Range(0, 3);
				prevColors[i] = nowColors[i];
				solColors[i] = nowColors[i];
			} else {
				nowColors[i] = prevColors[i];
			}

			ModuleButtons[i].GetComponent<Renderer>().material.color = buttonColors[nowColors[i]];
		}

		if (!returnPrev) {
			var logColors = "";

			foreach (int colors in nowColors) {
				logColors += new[] { "Red ", "Green ", "Blue " }[colors];
			}

			Debug.LogFormat(@"[Ten-Button Color Code #{0}] The initial colors for stage {1} are: {2}", moduleId, (solvedFirst) ? "2" : "1", logColors);
		}
	}

	void SetRules() {
		for (int i = 0; i < 11; i++) {
			if (!solvedFirst) {
				GetRules((initRules + i) % 10);
			} else {
				GetRules((initRules + ((10 - i) % 10)) % 10);
			}
		}

		var logSol = "";

		foreach (int solution in solColors) {
			logSol += new[] { "Red ", "Green ", "Blue " }[solution];
		}

		Debug.LogFormat(@"[Ten-Button Color Code #{0}] The solution for stage {1} is: {2}", moduleId, (solvedFirst) ? 2 : 1, logSol);
	}

	void GetRules(int rule) {
		var topRow = solColors.Take(5).ToArray();
		var bottomRow = solColors.Skip(5).ToArray();

		switch (rule) {
			case 0:
				var getInit = initRules;
				getInit = (getInit == 0) ? 9 : getInit - 1;

				for (int i = 0; i < 10; i++) {
					if (i != getInit) {
						SetSolColor(i, 1);
					}
				}
				break;

			case 1:
				for (int i = 0; i < 8; i++) {
					var nowCheck = i + ((i > 3) ? 1 : 0);

					if (ruleList[0](nowCheck)) {
						solColors[nowCheck + 1] = (solColors[nowCheck + 1] == 0) ? 1 : 0;

						if (i <= 3) {
							i = 3;
						} else {
							i = 7;
						}
					}
				}
				break;

			case 2:
				var pressed = 1;

				for (int i = 0; i < 3; i++) {
					if (topRow.Count(x => x == i) >= 3) {
						for (int j = 0; j < 5; j++) {
							if (solColors[j] == i) {
								SetSolColor(j, pressed++);
							}

							if (pressed > 2) {
								break;
							}
						}

						break;
					}
				}
				break;

			case 3:
				var tempCols = new[] { solColors[3], solColors[8], solColors[4], solColors[9], solColors[6] };
				var switchCols = new[,] {
					{ 3, 8, 0, 5, 4, 9, 2, 7, 6, 1 },
					{ 0, 5, -1, -1, 2, 7, -1, -1, 1, -1 }
				};

				var tempCount = 0;

				for (int i = 0; i < 10; i++) {
					if (switchCols[1, i] == -1) {
						solColors[switchCols[0, i]] = tempCols[tempCount++];
					} else {
						solColors[switchCols[0, i]] = solColors[switchCols[1, i]];
					}
				}
				break;

			case 4:
				for (int i = 0; i < 3; i++) {
					if (topRow.All(x => x == i)) {
						for (int j = 0; j < 3; j++) {
							SetSolColor(j + j, 1);
						}
					}

					if (bottomRow.All(x => x == i)) {
						for (int j = 0; j < 3; j++) {
							SetSolColor(5 + (j + j), 1);
						}
					}
				}
				break;

			case 5:
				for (int i = 0; i < 5; i++) {
					if (ruleList[1](i)) {
						solColors[i] = 2;

						if (int.Parse(serialNum[5].ToString()) % 2 == 0) {
							solColors[i + 5] = 1;
						} else {
							solColors[i + 5] = 0;
						}
					}
				}
				break;

			case 6:
				if (!solColors.Contains(0)) {
					var setColors = new[] { 1, 5, 8 };

					for (int i = 0; i < 3; i++) {
						solColors[setColors[i]] = 0;
					}
				}
				break;

			case 7:
				var greenCount = 0;

				if (solColors.Count(x => x == 1) > 5) {
					for (int i = 0; i < 10; i++) {
						if (solColors[i] == 1) {
							greenCount++;

							if (greenCount == 8) {
								solColors[i] = 2;
							} else if (greenCount == 1 || greenCount == 3 || greenCount == 4) {
								SetSolColor(i, new[] { 2, 0, 1, 2 }[greenCount - 1]);
							}
						}
					}
				}
				break;

			case 8:
				for (int i = 0; i < 6; i++) {
					var nowCheck = i + ((i > 2) ? 2 : 0);

					if (ruleList[2](nowCheck)) {
						SetSolColor(nowCheck + 1, 1);

						if (i <= 2) {
							i = 2;
						} else {
							i = 5;
						}
					}
				}
				break;

			case 9:
				for (int i = 0; i < 4; i++) {
					if (ruleList[3](i)) {
						SetSolColor(i, 2);
						SetSolColor(i + 6, 2);
						break;
					}
				}
				break;
		}
	}

	void OnButtonPress(int buttonPressed) {
		BombAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
		ModuleSelect.AddInteractionPunch();

		if (moduleSolved) {
			return;
		}

		nowColors[buttonPressed]++;
		nowColors[buttonPressed] %= 3;
		ModuleButtons[buttonPressed].GetComponent<Renderer>().material.color = buttonColors[nowColors[buttonPressed]];
	}

	void SetSolColor(int nowSol, int quanSol) {
		solColors[nowSol] += quanSol;
		solColors[nowSol] %= 3;
	}

	void OnSubmitPress() {
		BombAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
		ModuleSelect.AddInteractionPunch();

		if (moduleSolved) {
			return;
		}
			
		if (nowColors.SequenceEqual(solColors)) {
			if (!solvedFirst) {
				ModuleLights[0].GetComponent<Renderer>().material.color = new Color32(239, 228, 176, 255);
				ModuleLights[0].transform.GetChild(0).GetComponent<Light>().intensity = 40.0f;
				nextStage = true;
				solvedFirst = true;
				Debug.LogFormat(@"[Ten-Button Color Code #{0}] You passed first stage!", moduleId);
			} else {
				ModuleLights[1].GetComponent<Renderer>().material.color = new Color32(239, 228, 176, 255);
				ModuleLights[1].transform.GetChild(0).GetComponent<Light>().intensity = 40.0f;
				BombModule.HandlePass();

				for (int i = 0; i < 10; i++) {
					ModuleButtons[i].GetComponent<Renderer>().material.color = Color.gray;
				}

				moduleSolved = true;
				Debug.LogFormat(@"[Ten-Button Color Code #{0}] Module solved!", moduleId); 
			}
		} else {
			BombModule.HandleStrike();
			ShuffleColors(true);
			Debug.LogFormat(@"[Ten-Button Color Code #{0}] The combination you submitted was wrong, restarting stage.", moduleId);
		}
	}

	#pragma warning disable 414
	private readonly string TwitchHelpMessage = @"!{0} press 1 2 3... (buttons to press [from 1 to 10]) | !{0} submit/sub/s (submits current combination)]";
	#pragma warning restore 414

	KMSelectable[] ProcessTwitchCommand(string command) {
		command = command.ToLowerInvariant().Trim();

		if (Regex.IsMatch(command, @"^press +[0-9^, |&]+$")) {
			command = command.Substring(6).Trim();

			var presses = command.Split(new[] { ',', ' ', '|', '&' }, System.StringSplitOptions.RemoveEmptyEntries);
			var pressList = new List<KMSelectable>();

			for (int i = 0; i < presses.Length; i++) {
				if (Regex.IsMatch(presses[i], @"^[0-9]{1,2}$")) {
					pressList.Add(ModuleButtons[Math.Clamp(Math.Max(1, int.Parse(presses[i].ToString())) - 1, 0, ModuleButtons.Length - 1)]);
				}
			}

			return pressList.ToArray();
		}

		if (Regex.IsMatch(command, @"^(submit|sub|s)$")) {
			return new[] { SubmitButton };
		}

		return null;
	}
}