using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using KModkit;
using UnityEngine;

using Math = UnityEngine.Mathf;
using Random = UnityEngine.Random;

public class scr_colorCode : MonoBehaviour
{
    public KMAudio BombAudio;
    public KMBombModule BombModule;
    public KMBombInfo BombInfo;
    public KMSelectable[] ModuleButtons;
    public KMSelectable ModuleSelect, SubmitButton;
    public GameObject[] ModuleLights;
    public TextMesh[] ColorblindTexts;
    public KMColorblindMode ColorblindMode;
    public Material LedOff;
    public Material LedOn;

    readonly Color[] buttonColors = { new Color32(255, 58, 58, 255), new Color32(0, 226, 26, 255), new Color32(0, 136, 255, 255), Color.gray };
    readonly int[] nowColors = new int[10];
    readonly int[] prevColors = new int[10];
    readonly int[] solColors = new int[10];
    readonly string[] colorNames = { "Red", "Green", "Blue" };

    string serialNum = "";
    int initRules;
    delegate bool checkRules(int x);
    checkRules[] ruleList;
    bool solvedFirstStage;
    bool colorBlindActive;
    bool moduleSolved;

    static int moduleIdCounter = 1;
    int moduleId;

    void Start()
    {
        moduleId = moduleIdCounter++;
        setColorblind(ColorblindMode.ColorblindModeActive);

        for (var i = 0; i < 10; i++)
            ModuleButtons[i].OnInteract += OnButtonPress(i);

        SubmitButton.OnInteract += delegate () { OnSubmitPress(); return false; };

        var lightScalar = transform.lossyScale.x;
        for (var i = 0; i < ModuleLights.Length; i++)
            ModuleLights[i].transform.GetChild(0).GetComponent<Light>().range *= lightScalar;

        serialNum = BombInfo.GetSerialNumber();
        initRules = int.Parse(serialNum.Last().ToString());
        Debug.LogFormat(@"[Ten-Button Color Code #{0}] Stage 1 starting rule number: {1}", moduleId, initRules);
        ShuffleColors(false);
        ruleList = new checkRules[4] {
            x => solColors[x] == solColors[x + 1],
            x => solColors[x] == solColors[x + 5],
            x => solColors[x] == solColors[x + 1] && solColors[x + 1] == solColors[x + 2],
            x => solColors[x] == solColors[x + 1] && solColors[x + 1] == solColors[x + 5] && solColors[x + 5] == solColors[x + 6]        };

        SetLights(0);
        SetRules();
    }

    private void setColorblind(bool active)
    {
        colorBlindActive = active;
        for (var i = 0; i < ColorblindTexts.Length; i++)
            ColorblindTexts[i].gameObject.SetActive(active);
    }

    private void SetLights(int num)
    {
        for (var i = 0; i < ModuleLights.Length; i++)
        {
            ModuleLights[i].transform.GetChild(0).gameObject.SetActive(i < num);
            ModuleLights[i].GetComponent<Renderer>().sharedMaterial = i < num ? LedOn : LedOff;
        }
    }

    void ShuffleColors(bool returnPrev)
    {
        for (var i = 0; i < 10; i++)
        {
            if (!returnPrev)
                solColors[i] = prevColors[i] = nowColors[i] = Random.Range(0, 3);
            else
                nowColors[i] = prevColors[i];

            SetButtonColor(i, nowColors[i]);
        }

        if (!returnPrev)
            Debug.LogFormat(@"[Ten-Button Color Code #{0}] Stage {1} initial colors: {2}", moduleId, solvedFirstStage ? "2" : "1", string.Join("", nowColors.Select(x => colorNames[x].Substring(0, 1)).ToArray()));
    }

    private void SetButtonColor(int btn, int color)
    {
        ModuleButtons[btn].GetComponent<Renderer>().material.color = buttonColors[color];
        ColorblindTexts[btn].text = color == 3 ? "" : "RGB".Substring(color, 1);
    }

    void SetRules()
    {
        for (var i = 0; i < 11; i++)
        {
            if (!solvedFirstStage)
                GetRules((initRules + i) % 10);
            else
                GetRules((initRules + 10 - i) % 10);
        }
    }

    void GetRules(int rule)
    {
        var topRow = solColors.Take(5).ToArray();
        var bottomRow = solColors.Skip(5).ToArray();

        switch (rule)
        {
            case 0:
                var getInit = initRules;
                getInit = (getInit == 0) ? 9 : getInit - 1;

                for (var i = 0; i < 10; i++)
                {
                    if (i != getInit)
                    {
                        SetSolColor(i, 1);
                    }
                }
                break;

            case 1:
                for (var i = 0; i < 8; i++)
                {
                    var nowCheck = i + ((i > 3) ? 1 : 0);

                    if (ruleList[0](nowCheck))
                    {
                        solColors[nowCheck + 1] = (solColors[nowCheck + 1] == 0) ? 1 : 0;
                        i = (i <= 3) ? 3 : 7;
                    }
                }
                break;

            case 2:
                var pressed = 1;

                for (var i = 0; i < 3; i++)
                {
                    if (topRow.Count(x => x == i) >= 3)
                    {
                        for (var j = 0; j < 5; j++)
                        {
                            if (solColors[j] == i)
                                SetSolColor(j, pressed++);

                            if (pressed > 2) break;
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

                for (var i = 0; i < 10; i++)
                    solColors[switchCols[0, i]] = (switchCols[1, i] == -1) ? tempCols[tempCount++] : solColors[switchCols[1, i]];
                break;

            case 4:
                for (var i = 0; i < 3; i++)
                {
                    if (topRow.All(x => x == i))
                    {
                        for (var j = 0; j < 3; j++)
                        {
                            SetSolColor(j + j, 1);
                        }
                    }

                    if (bottomRow.All(x => x == i))
                    {
                        for (var j = 0; j < 3; j++)
                        {
                            SetSolColor(5 + (j + j), 1);
                        }
                    }
                }
                break;

            case 5:
                for (var i = 0; i < 5; i++)
                {
                    if (ruleList[1](i))
                    {
                        solColors[i] = 2;

                        if (int.Parse(serialNum.Last().ToString()) % 2 == 0)
                        {
                            solColors[i + 5] = 1;
                        }
                        else
                        {
                            solColors[i + 5] = 0;
                        }
                    }
                }
                break;

            case 6:
                if (!solColors.Contains(0))
                {
                    var setColors = new[] { 1, 5, 8 };

                    for (var i = 0; i < 3; i++)
                        solColors[setColors[i]] = 0;
                }
                break;

            case 7:
                var greenCount = 0;

                if (solColors.Count(x => x == 1) > 5)
                {
                    for (var i = 0; i < 10; i++)
                    {
                        if (solColors[i] == 1)
                        {
                            greenCount++;

                            if (greenCount == 8)
                            {
                                solColors[i] = 2;
                            }
                            else if (greenCount == 1 || greenCount == 3 || greenCount == 4)
                            {
                                SetSolColor(i, new[] { 2, 0, 1, 2 }[greenCount - 1]);
                            }
                        }
                    }
                }
                break;

            case 8:
                for (var i = 0; i < 6; i++)
                {
                    var nowCheck = i + ((i > 2) ? 2 : 0);

                    if (ruleList[2](nowCheck))
                    {
                        SetSolColor(nowCheck + 1, 1);
                        i = (i <= 2) ? 2 : 5;
                    }
                }
                break;

            case 9:
                for (var i = 0; i < 4; i++)
                {
                    if (ruleList[3](i))
                    {
                        SetSolColor(i, 2);
                        SetSolColor(i + 6, 2);

                        break;
                    }
                }
                break;
        }
        Debug.LogFormat(@"[Ten-Button Color Code #{0}] After rule {1}: {2}", moduleId, rule, string.Join("", solColors.Select(x => colorNames[x].Substring(0, 1)).ToArray()));
    }

    KMSelectable.OnInteractHandler OnButtonPress(int buttonPressed)
    {
        return delegate
        {
            BombAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
            ModuleSelect.AddInteractionPunch();

            if (moduleSolved)
                return false;

            nowColors[buttonPressed]++;
            nowColors[buttonPressed] %= 3;
            SetButtonColor(buttonPressed, nowColors[buttonPressed]);
            return false;
        };
    }

    void SetSolColor(int nowSol, int quanSol)
    {
        solColors[nowSol] += quanSol;
        solColors[nowSol] %= 3;
    }

    void OnSubmitPress()
    {
        BombAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        ModuleSelect.AddInteractionPunch();

        if (moduleSolved) return;

        if (nowColors.SequenceEqual(solColors))
        {
            if (!solvedFirstStage)
            {
                SetLights(1);
                Debug.LogFormat(@"[Ten-Button Color Code #{0}] You passed Stage 1!", moduleId);
                solvedFirstStage = true;
                initRules = BombInfo.GetSerialNumberNumbers().Sum() % 10;
                Debug.LogFormat(@"[Ten-Button Color Code #{0}] Stage 2 starting rule number: {1}", moduleId, initRules);
                ShuffleColors(false);
                SetRules();
            }
            else
            {
                SetLights(2);
                BombModule.HandlePass();

                for (var i = 0; i < 10; i++)
                    SetButtonColor(i, 3);

                moduleSolved = true;
                Debug.LogFormat(@"[Ten-Button Color Code #{0}] Module solved!", moduleId);
            }
        }
        else
        {
            Debug.LogFormat(@"[Ten-Button Color Code #{0}] Strike! You submitted: {1}", moduleId, string.Join("", nowColors.Select(x => colorNames[x].Substring(0, 1)).ToArray()));
            BombModule.HandleStrike();
            ShuffleColors(true);
        }
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} press 1 2 3 [buttons to press, from 1 to 10] | !{0} submit | !{0} colorblind";
#pragma warning restore 414

    KMSelectable[] ProcessTwitchCommand(string command)
    {
        if (Regex.IsMatch(command, @"^\s*(colorblind|cb)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            setColorblind(!colorBlindActive);
            return new KMSelectable[0];
        }

        Match m;
        if ((m = Regex.Match(command, @"^\s*(?:press +)?([0-9^, |&]+)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).Success)
        {
            var presses = m.Groups[1].Value.Split(new[] { ',', ' ', '|', '&' }, StringSplitOptions.RemoveEmptyEntries);
            var pressList = new List<KMSelectable>();

            int btn;
            for (var i = 0; i < presses.Length; i++)
                if (int.TryParse(presses[i], out btn) && btn >= 1 && btn <= ModuleButtons.Length)
                    pressList.Add(ModuleButtons[btn - 1]);
                else
                    return null;
            return pressList.ToArray();
        }

        if (Regex.IsMatch(command, @"^\s*(submit|sub|s)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            return new[] { SubmitButton };

        return null;
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        while (!moduleSolved)
        {
            for (int j = 0; j < 10; j++)
            {
                while (nowColors[j] != solColors[j])
                {
                    ModuleButtons[j].OnInteract();
                    yield return new WaitForSeconds(0.1f);
                }
            }
            SubmitButton.OnInteract();
            yield return new WaitForSeconds(0.25f);
        }
    }
}