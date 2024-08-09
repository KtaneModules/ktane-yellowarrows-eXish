using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using System;
using System.Text.RegularExpressions;
using Rnd = UnityEngine.Random;
using UnityEngine.Timeline;
using System.Linq.Expressions;
using UnityEditor.Callbacks;

public class YellowArrowsScript : MonoBehaviour
{

    public KMAudio Audio;
    public KMBombInfo Bomb;
    public KMBombModule Module;
    public KMColorblindMode colorblindMode;
    public KMRuleSeedable RuleSeedable;

    public KMSelectable[] ButtonSels;
    public TextMesh DisplayText;
    public GameObject ColorblindText;

    private bool activated;
    private bool cbEnabled;

    private int moduleId;
    private static int moduleIdCounter = 1;
    private bool moduleSolved;

    private int _displayedLetterIx;
    private int _offset;
    private List<ArrowDirection> _solutionDirs;
    private int _inputIx;
    private bool _canInteract;

    public enum ArrowDirection
    {
        Up = 0,
        Right = 1,
        Down = 2,
        Left = 3,
        Any = 4
    };

    private static readonly Func<List<ArrowDirection>, YellowArrowsScript, ArrowDirection>[] _seed1rules = NewArray<Func<List<ArrowDirection>, YellowArrowsScript, ArrowDirection>>(
        (dirs, module) => ArrowDirection.Up,
        (dirs, module) => dirs.Count > 0 && dirs.Last() == ArrowDirection.Left ? ArrowDirection.Down : ArrowDirection.Right,
        (dirs, module) => module.Bomb.GetSerialNumber()[5] == '3' ? ArrowDirection.Left : ArrowDirection.Up,
        (dirs, module) => dirs.Count == 0 ? ArrowDirection.Up : ArrowDirection.Down,
        (dirs, module) => module.Bomb.GetOnIndicators().Contains("SIG") ? ArrowDirection.Right : ArrowDirection.Left,
        (dirs, module) => !module.Bomb.IsPortPresent("PS2") ? ArrowDirection.Down : ArrowDirection.Any,
        (dirs, module) => !dirs.Contains(ArrowDirection.Down) ? ArrowDirection.Up : ArrowDirection.Down,
        (dirs, module) => module.Bomb.IsPortPresent("Serial") ? ArrowDirection.Any : ArrowDirection.Right,
        (dirs, module) => module.Bomb.GetModuleNames().Count == module.Bomb.GetSolvableModuleNames().Count ? ArrowDirection.Down : ArrowDirection.Any,
        (dirs, module) => dirs.Count > 0 && dirs.Last() == ArrowDirection.Down ? ArrowDirection.Left : ArrowDirection.Up,
        (dirs, module) => ArrowDirection.Down,
        (dirs, module) => module.Bomb.GetBatteryCount() == 0 ? ArrowDirection.Up : ArrowDirection.Down,
        (dirs, module) => module.Bomb.GetBatteryHolderCount() < 3 ? ArrowDirection.Right : ArrowDirection.Left,
        (dirs, module) => dirs.Count == 0 ? ArrowDirection.Any : ArrowDirection.Right,
        (dirs, module) => module.Bomb.GetSerialNumber().Any(i => i == 'O') ? ArrowDirection.Left : ArrowDirection.Down,
        (dirs, module) => module.Bomb.GetSerialNumberLetters().Count() == 4 ? ArrowDirection.Down : ArrowDirection.Up,
        (dirs, module) => dirs.Count > 0 && dirs.Last() == ArrowDirection.Right ? ArrowDirection.Down : ArrowDirection.Left,
        (dirs, module) => module.Bomb.GetOffIndicators().Contains("CLR") ? ArrowDirection.Up : ArrowDirection.Down,
        (dirs, module) => ArrowDirection.Left,
        (dirs, module) => module.Bomb.GetBatteryCount() % 2 == 0 ? ArrowDirection.Left : ArrowDirection.Down,
        (dirs, module) => ArrowDirection.Any,
        (dirs, module) => dirs.Count > 0 && dirs.Last() == ArrowDirection.Up ? ArrowDirection.Up : ArrowDirection.Down,
        (dirs, module) => module.Bomb.GetPortPlateCount() == 0 ? ArrowDirection.Right : ArrowDirection.Any,
        (dirs, module) => dirs.Count == 0 ? ArrowDirection.Up : ArrowDirection.Left,
        (dirs, module) => !dirs.Contains(ArrowDirection.Up) ? ArrowDirection.Any : ArrowDirection.Up,
        (dirs, module) => ArrowDirection.Right
    );

    private static readonly Func<YellowArrowsScript, MonoRandom, Func<List<ArrowDirection>, ArrowDirection>>[] _ruleSeededRules = NewArray<Func<YellowArrowsScript, MonoRandom, Func<List<ArrowDirection>, ArrowDirection>>>(
        (module, rnd) =>
        {
            return dirs => (ArrowDirection)rnd.Next(0, 5);
        },
        (module, rnd) =>
        {
            var rands = GenerateDirections(rnd);
            var randPrev = (ArrowDirection)rnd.Next(0, 4);
            return dirs => dirs.Count > 0 && dirs.Last() == randPrev ? rands[0] : rands[1];
        },
        (module, rnd) =>
        {
            var rands = GenerateDirections(rnd);
            return dirs => dirs.Count == 0 ? rands[0] : rands[1];
        },
        (module, rnd) =>
        {
            return dirs => ArrowDirection.Up;
        }
    );

    private static ArrowDirection[] GenerateDirections(MonoRandom rnd)
    {
        return rnd.ShuffleFisherYates((ArrowDirection[])Enum.GetValues(typeof(ArrowDirection)));
    }

    private static T[] NewArray<T>(params T[] array) { return array; }

    private Func<List<ArrowDirection>, ArrowDirection>[] _rules;

    private void Start()
    {
        moduleId = moduleIdCounter++;
        cbEnabled = colorblindMode.ColorblindModeActive;
        for (int i = 0; i < ButtonSels.Length; i++)
            ButtonSels[i].OnInteract += ButtonPress(i);
        GetComponent<KMBombModule>().OnActivate += OnActivate;
        DisplayText.text = "";

        var rnd = RuleSeedable.GetRNG();
        if (rnd.Seed != 1)
            Debug.LogFormat("[Yellow Arrows #{0}] Using rule seed {1}.", moduleId, rnd.Seed);
        _rules = rnd.Seed == 1 ?
            _seed1rules.Select(rule => new Func<List<ArrowDirection>, ArrowDirection>(n => rule(n, this))).ToArray() :
            _seed1rules.Select(rule => new Func<List<ArrowDirection>, ArrowDirection>(n => rule(n, this))).ToArray();
        Generate();
    }

    private void Generate()
    {
        _inputIx = 0;
        _displayedLetterIx = Rnd.Range(0, 26);
        Debug.LogFormat("[Yellow Arrows #{0}] The starting row is {1}.", moduleId, "ABCDEFGHIJKLMNOPQRSTUVWXYZ"[_displayedLetterIx]);
        _offset = Bomb.GetSerialNumber()[5] - '0' + 1;
        int ix = _displayedLetterIx;
        _solutionDirs = new List<ArrowDirection>();
        for (int i = 0; i < 7; i++)
        {
            ix = (ix + _offset) % 26;
            _solutionDirs.Add(_rules[ix](_solutionDirs));
            Debug.LogFormat("[Yellow Arrows #{0}] Press #{1} should be {2} according to row {3}.", moduleId, i + 1, _solutionDirs[i].ToString().ToUpperInvariant(), "ABCDEFGHIJKLMNOPQRSTUVWXYZ"[ix]);
        }
    }

    private IEnumerator Delay()
    {
        DisplayText.text = "";
        yield return new WaitForSeconds(0.5f);
        DisplayText.text = "ABCDEFGHIJKLMNOPQRSTUVWXYZ"[_displayedLetterIx].ToString();
        _canInteract = true;
    }

    private void OnActivate()
    {
        if (cbEnabled)
            ColorblindText.SetActive(true);
        StartCoroutine(Delay());
    }

    private KMSelectable.OnInteractHandler ButtonPress(int btn)
    {
        return delegate ()
        {
            if (moduleSolved || !_canInteract)
                return false;
            if (_solutionDirs[_inputIx] == ArrowDirection.Any || _solutionDirs[_inputIx] == (ArrowDirection)btn)
            {
                Debug.LogFormat("[Yellow Arrows #{0}] {1} was correctly pressed.", moduleId, (ArrowDirection)btn);
                _inputIx++;
                if (_inputIx == 7)
                {
                    _canInteract = false;
                    StartCoroutine(Victory());
                }
            }
            else
            {
                Debug.LogFormat("[Yellow Arrows #{0}] {1} was pressed, when {2} was expected. Strike.", moduleId, _solutionDirs[_inputIx], (ArrowDirection)btn);
                _canInteract = false;
                Generate();
                StartCoroutine(Delay());
                Module.HandleStrike();
            }
            return false;
        };
    }

    private IEnumerator Victory()
    {
        yield return null;
        for (int i = 0; i < 100; i++)
        {
            int rand1 = Rnd.Range(0, 10);
            if (i < 50)
                DisplayText.GetComponent<TextMesh>().text = rand1 + "";
            else
                DisplayText.GetComponent<TextMesh>().text = "G" + rand1;
            yield return new WaitForSeconds(0.025f);
        }
        DisplayText.text = "GG";
        Debug.LogFormat("[Yellow Arrows #{0}] All Presses were correct! Module Disarmed!", moduleId);
        moduleSolved = true;
        Module.HandlePass();
    }

    //twitch plays
#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} up/down/left/right [Presses the specified arrow button] | !{0} left right down up [Chain button presses] | !{0} reset [Resets the module back to the start] | Direction words can be substituted as one letter (Ex. right as r)";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.Trim().ToLowerInvariant();
        while (!_canInteract)
            yield return "trycancel";
        if (Regex.IsMatch(command, @"^\s*reset\s*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            Debug.LogFormat("[Yellow Arrows #{0}] Twitch Plays received a RESET command. Resetting to intial state with no inputs.", moduleId);
            yield return "sendtochat Module {1} (Yellow Arrows) has been reset to the initial state with no inputs.";
            _canInteract = false;
            DisplayText.text = "";
            _inputIx = 0;
            yield return new WaitForSeconds(0.5f);
            DisplayText.text = "ABCDEFGHIJKLMNOPQRSTUVWXYZ"[_displayedLetterIx].ToString();
            yield break;
        }
        var cmds = command.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        var btns = new List<KMSelectable>();
        for (int i = 0; i < cmds.Length; i++)
        {
            if (cmds[i].EqualsIgnoreCase("up") || cmds[i].EqualsIgnoreCase("u"))
                btns.Add(ButtonSels[0]);
            else if (cmds[i].EqualsIgnoreCase("right") || cmds[i].EqualsIgnoreCase("r"))
                btns.Add(ButtonSels[1]);
            else if (cmds[i].EqualsIgnoreCase("down") || cmds[i].EqualsIgnoreCase("d"))
                btns.Add(ButtonSels[2]);
            else if (cmds[i].EqualsIgnoreCase("left") || cmds[i].EqualsIgnoreCase("l"))
                btns.Add(ButtonSels[3]);
            else
                yield break;
        }
        yield return null;
        yield return btns;
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        while (_inputIx != 7)
        {
            ButtonSels[(int)_solutionDirs[_inputIx] % 4].OnInteract(); // Modulo 4, otherwise "Any" causes an IndexOutOfRange exception.
            yield return new WaitForSeconds(0.1f);
        }
        while (!moduleSolved)
            yield return true;
    }
}
