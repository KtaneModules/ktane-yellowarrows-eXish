using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using System;
using System.Text.RegularExpressions;
using Rnd = UnityEngine.Random;

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
        (dirs, module) => ArrowDirection.Down,
        (dirs, module) => module.Bomb.GetOnIndicators().Contains("SIG") ? ArrowDirection.Right : ArrowDirection.Left,
        (dirs, module) => !module.Bomb.IsPortPresent("PS2") ? ArrowDirection.Down : ArrowDirection.Any,
        (dirs, module) => !dirs.Contains(ArrowDirection.Down) ? ArrowDirection.Up : ArrowDirection.Down,
        (dirs, module) => module.Bomb.IsPortPresent("Serial") ? ArrowDirection.Any : ArrowDirection.Right,
        (dirs, module) => module.Bomb.GetModuleNames().Count == module.Bomb.GetSolvableModuleNames().Count ? ArrowDirection.Down : ArrowDirection.Any,
        (dirs, module) => dirs.Count > 0 && dirs.Last() == ArrowDirection.Down ? ArrowDirection.Left : ArrowDirection.Up,
        (dirs, module) => ArrowDirection.Down,
        (dirs, module) => module.Bomb.GetBatteryCount() == 0 ? ArrowDirection.Up : ArrowDirection.Down,
        (dirs, module) => module.Bomb.GetBatteryHolderCount() < 3 ? ArrowDirection.Right : ArrowDirection.Left,
        (dirs, module) => ArrowDirection.Right,
        (dirs, module) => module.Bomb.GetSerialNumber().Any(i => i == 'O') ? ArrowDirection.Left : ArrowDirection.Down,
        (dirs, module) => module.Bomb.GetSerialNumberLetters().Count() == 4 ? ArrowDirection.Down : ArrowDirection.Up,
        (dirs, module) => dirs.Count > 0 && dirs.Last() == ArrowDirection.Right ? ArrowDirection.Down : ArrowDirection.Left,
        (dirs, module) => module.Bomb.GetOffIndicators().Contains("CLR") ? ArrowDirection.Up : ArrowDirection.Down,
        (dirs, module) => ArrowDirection.Left,
        (dirs, module) => module.Bomb.GetBatteryCount() % 2 == 0 ? ArrowDirection.Left : ArrowDirection.Down,
        (dirs, module) => ArrowDirection.Any,
        (dirs, module) => dirs.Count > 0 && dirs.Last() == ArrowDirection.Up ? ArrowDirection.Up : ArrowDirection.Down,
        (dirs, module) => module.Bomb.GetPortPlateCount() == 0 ? ArrowDirection.Right : ArrowDirection.Any,
        (dirs, module) => ArrowDirection.Left,
        (dirs, module) => !dirs.Contains(ArrowDirection.Up) ? ArrowDirection.Any : ArrowDirection.Up,
        (dirs, module) => ArrowDirection.Right
    );

    private struct RuleInfo
    {
        public string Name;
        public Func<List<ArrowDirection>, ArrowDirection> Rule;

        public RuleInfo(string name, Func<List<ArrowDirection>, ArrowDirection> rule)
        {
            Name = name;
            Rule = rule;
        }
    }

    private static readonly Func<YellowArrowsScript, MonoRandom, RuleInfo>[] _ruleSeededRules = NewArray<Func<YellowArrowsScript, MonoRandom, RuleInfo>>(
        (module, rnd) => new RuleInfo("Up", dirs => ArrowDirection.Up),
        (module, rnd) => new RuleInfo("Right", dirs => ArrowDirection.Right),
        (module, rnd) => new RuleInfo("Down", dirs => ArrowDirection.Down),
        (module, rnd) => new RuleInfo("Left", dirs => ArrowDirection.Left),
        (module, rnd) => new RuleInfo("Any", dirs => ArrowDirection.Any),
        ParametrizedRule("Up hasn’t been pressed yet", (dirs, module) => !dirs.Contains(ArrowDirection.Up)),
        ParametrizedRule("Right hasn’t been pressed yet", (dirs, module) => !dirs.Contains(ArrowDirection.Right)),
        ParametrizedRule("Down hasn’t been pressed yet", (dirs, module) => !dirs.Contains(ArrowDirection.Down)),
        ParametrizedRule("Left hasn’t been pressed yet", (dirs, module) => !dirs.Contains(ArrowDirection.Left)),
        ParametrizedRule(rnd => (ArrowDirection) rnd.Next(0, 4), prev => $"Previous input was {prev}", (dirs, module, dir) => dirs.Count > 0 && dirs.Last() == dir),
        ParametrizedRule(rnd => (ArrowDirection) rnd.Next(0, 4), prev => $"Previous input was not {prev}", (dirs, module, dir) => dirs.Count > 0 && dirs.Last() != dir),
        ParametrizedRule("This is the Starting Row", (dirs, module) => false),
        ParametrizedRule("Number of batteries is even", (dirs, module) => module.Bomb.GetBatteryCount() % 2 == 0),
        ParametrizedRule("No batteries on bomb", (dirs, module) => module.Bomb.GetBatteryCount() == 0),
        ParametrizedRule("Exactly 1 battery on bomb", (dirs, module) => module.Bomb.GetBatteryCount() == 1),
        ParametrizedRule("Exactly 2 batteries on bomb", (dirs, module) => module.Bomb.GetBatteryCount() == 2),
        ParametrizedRule("Exactly 3 batteries on bomb", (dirs, module) => module.Bomb.GetBatteryCount() == 3),
        ParametrizedRule("At most 3 batteries on bomb", (dirs, module) => module.Bomb.GetBatteryCount() <= 3),
        ParametrizedRule("At least 4 batteries on bomb", (dirs, module) => module.Bomb.GetBatteryCount() >= 4),
        ParametrizedRule("Number of batteries is even", (dirs, module) => module.Bomb.GetBatteryHolderCount() % 2 == 0),
        ParametrizedRule("No battery holders on bomb", (dirs, module) => module.Bomb.GetBatteryHolderCount() == 0),
        ParametrizedRule("Exactly 1 battery holder on bomb", (dirs, module) => module.Bomb.GetBatteryHolderCount() == 1),
        ParametrizedRule("Exactly 2 battery holders on bomb", (dirs, module) => module.Bomb.GetBatteryHolderCount() == 2),
        ParametrizedRule("Exactly 3 battery holders on bomb", (dirs, module) => module.Bomb.GetBatteryHolderCount() == 3),
        ParametrizedRule("At most 3 battery holders on bomb", (dirs, module) => module.Bomb.GetBatteryHolderCount() <= 3),
        ParametrizedRule("At least 4 battery holders on bomb", (dirs, module) => module.Bomb.GetBatteryHolderCount() >= 4),
        ParametrizedRule("Number of indicators is even", (dirs, module) => module.Bomb.GetIndicators().Count() % 2 == 0),
        ParametrizedRule("SND indicator present", (dirs, module) => module.Bomb.GetIndicators().Contains("SND")),
        ParametrizedRule("CLR indicator present", (dirs, module) => module.Bomb.GetIndicators().Contains("CLR")),
        ParametrizedRule("CAR indicator present", (dirs, module) => module.Bomb.GetIndicators().Contains("CAR")),
        ParametrizedRule("IND indicator present", (dirs, module) => module.Bomb.GetIndicators().Contains("IND")),
        ParametrizedRule("FRQ indicator present", (dirs, module) => module.Bomb.GetIndicators().Contains("FRQ")),
        ParametrizedRule("SIG indicator present", (dirs, module) => module.Bomb.GetIndicators().Contains("SIG")),
        ParametrizedRule("NSA indicator present", (dirs, module) => module.Bomb.GetIndicators().Contains("NSA")),
        ParametrizedRule("MSA indicator present", (dirs, module) => module.Bomb.GetIndicators().Contains("MSA")),
        ParametrizedRule("TRN indicator present", (dirs, module) => module.Bomb.GetIndicators().Contains("TRN")),
        ParametrizedRule("BOB indicator present", (dirs, module) => module.Bomb.GetIndicators().Contains("BOB")),
        ParametrizedRule("FRK indicator present", (dirs, module) => module.Bomb.GetIndicators().Contains("FRK")),
        ParametrizedRule("Number of lit indicators is even", (dirs, module) => module.Bomb.GetOnIndicators().Count() % 2 == 0),
        ParametrizedRule("Lit SND indicator present", (dirs, module) => module.Bomb.GetOnIndicators().Contains("SND")),
        ParametrizedRule("Lit CLR indicator present", (dirs, module) => module.Bomb.GetOnIndicators().Contains("CLR")),
        ParametrizedRule("Lit CAR indicator present", (dirs, module) => module.Bomb.GetOnIndicators().Contains("CAR")),
        ParametrizedRule("Lit IND indicator present", (dirs, module) => module.Bomb.GetOnIndicators().Contains("IND")),
        ParametrizedRule("Lit FRQ indicator present", (dirs, module) => module.Bomb.GetOnIndicators().Contains("FRQ")),
        ParametrizedRule("Lit SIG indicator present", (dirs, module) => module.Bomb.GetOnIndicators().Contains("SIG")),
        ParametrizedRule("Lit NSA indicator present", (dirs, module) => module.Bomb.GetOnIndicators().Contains("NSA")),
        ParametrizedRule("Lit MSA indicator present", (dirs, module) => module.Bomb.GetOnIndicators().Contains("MSA")),
        ParametrizedRule("Lit TRN indicator present", (dirs, module) => module.Bomb.GetOnIndicators().Contains("TRN")),
        ParametrizedRule("Lit BOB indicator present", (dirs, module) => module.Bomb.GetOnIndicators().Contains("BOB")),
        ParametrizedRule("Lit FRK indicator present", (dirs, module) => module.Bomb.GetOnIndicators().Contains("FRK")),
        ParametrizedRule("Number of unlit indicators is even", (dirs, module) => module.Bomb.GetOffIndicators().Count() % 2 == 0),
        ParametrizedRule("Unlit SND indicator present", (dirs, module) => module.Bomb.GetOffIndicators().Contains("SND")),
        ParametrizedRule("Unlit CLR indicator present", (dirs, module) => module.Bomb.GetOffIndicators().Contains("CLR")),
        ParametrizedRule("Unlit CAR indicator present", (dirs, module) => module.Bomb.GetOffIndicators().Contains("CAR")),
        ParametrizedRule("Unlit IND indicator present", (dirs, module) => module.Bomb.GetOffIndicators().Contains("IND")),
        ParametrizedRule("Unlit FRQ indicator present", (dirs, module) => module.Bomb.GetOffIndicators().Contains("FRQ")),
        ParametrizedRule("Unlit SIG indicator present", (dirs, module) => module.Bomb.GetOffIndicators().Contains("SIG")),
        ParametrizedRule("Unlit NSA indicator present", (dirs, module) => module.Bomb.GetOffIndicators().Contains("NSA")),
        ParametrizedRule("Unlit MSA indicator present", (dirs, module) => module.Bomb.GetOffIndicators().Contains("MSA")),
        ParametrizedRule("Unlit TRN indicator present", (dirs, module) => module.Bomb.GetOffIndicators().Contains("TRN")),
        ParametrizedRule("Unlit BOB indicator present", (dirs, module) => module.Bomb.GetOffIndicators().Contains("BOB")),
        ParametrizedRule("Unlit FRK indicator present", (dirs, module) => module.Bomb.GetOffIndicators().Contains("FRK")),
        ParametrizedRule("Number of ports is even", (dirs, module) => module.Bomb.GetPortCount() % 2 == 0),
        ParametrizedRule("Parallel port on bomb", (dirs, module) => module.Bomb.GetPorts().Contains("Parallel")),
        ParametrizedRule("Serial port on bomb", (dirs, module) => module.Bomb.GetPorts().Contains("Serial")),
        ParametrizedRule("PS/2 port on bomb", (dirs, module) => module.Bomb.GetPorts().Contains("PS2")),
        ParametrizedRule("DVI-D port on bomb", (dirs, module) => module.Bomb.GetPorts().Contains("DVI")),
        ParametrizedRule("Stereo RCA port on bomb", (dirs, module) => module.Bomb.GetPorts().Contains("StereoRCA")),
        ParametrizedRule("RJ-45 port on bomb", (dirs, module) => module.Bomb.GetPorts().Contains("RJ45")),
        ParametrizedRule("Duplicate port on bomb", (dirs, module) => module.Bomb.IsDuplicatePortPresent()),
        ParametrizedRule("No ports on bomb", (dirs, module) => module.Bomb.GetPortCount() == 0),
        ParametrizedRule("Exactly 1 port on bomb", (dirs, module) => module.Bomb.GetPortCount() == 1),
        ParametrizedRule("Exactly 2 ports on bomb", (dirs, module) => module.Bomb.GetPortCount() == 2),
        ParametrizedRule("Exactly 3 ports on bomb", (dirs, module) => module.Bomb.GetPortCount() == 3),
        ParametrizedRule("At most 3 ports on bomb", (dirs, module) => module.Bomb.GetPortCount() <= 3),
        ParametrizedRule("At least 4 ports on bomb", (dirs, module) => module.Bomb.GetPortCount() >= 4),
        ParametrizedRule("Number of port plates is even", (dirs, module) => module.Bomb.GetPortPlateCount() % 2 == 0),
        ParametrizedRule("No port plates on bomb", (dirs, module) => module.Bomb.GetPortPlateCount() == 0),
        ParametrizedRule("Exactly 1 port plate on bomb", (dirs, module) => module.Bomb.GetPortPlateCount() == 1),
        ParametrizedRule("Exactly 2 port plates on bomb", (dirs, module) => module.Bomb.GetPortPlateCount() == 2),
        ParametrizedRule("Exactly 3 port plates on bomb", (dirs, module) => module.Bomb.GetPortPlateCount() == 3),
        ParametrizedRule("At most 3 port plates on bomb", (dirs, module) => module.Bomb.GetPortPlateCount() <= 3),
        ParametrizedRule("At least 4 port plates on bomb", (dirs, module) => module.Bomb.GetPortPlateCount() >= 4),
        ParametrizedRule("Serial number has 2 letters", (dirs, module) => module.Bomb.GetSerialNumberLetters().Count() == 2),
        ParametrizedRule("Serial number has 3 letters", (dirs, module) => module.Bomb.GetSerialNumberLetters().Count() == 3),
        ParametrizedRule("Serial number has 4 letters", (dirs, module) => module.Bomb.GetSerialNumberLetters().Count() == 4),
        ParametrizedRule(rnd => "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"[rnd.Next(0, 36)], prev => $"First serial number character is {prev}", (dirs, module, ch) => module.Bomb.GetSerialNumber()[0] == ch),
        ParametrizedRule(rnd => "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"[rnd.Next(0, 36)], prev => $"Second serial number character is {prev}", (dirs, module, ch) => module.Bomb.GetSerialNumber()[1] == ch),
        ParametrizedRule(rnd => "0123456789"[rnd.Next(0, 10)], prev => $"Third serial number character is {prev}", (dirs, module, ch) => module.Bomb.GetSerialNumber()[2] == ch),
        ParametrizedRule(rnd => "ABCDEFGHIJKLMNOPQRSTUVWXYZ"[rnd.Next(0, 26)], prev => $"Fourth serial number character is {prev}", (dirs, module, ch) => module.Bomb.GetSerialNumber()[3] == ch),
        ParametrizedRule(rnd => "ABCDEFGHIJKLMNOPQRSTUVWXYZ"[rnd.Next(0, 26)], prev => $"Fifth serial number character is {prev}", (dirs, module, ch) => module.Bomb.GetSerialNumber()[4] == ch),
        ParametrizedRule(rnd => "0123456789"[rnd.Next(0, 10)], prev => $"The serial number ends with a {prev}", (dirs, module, ch) => module.Bomb.GetSerialNumber()[5] == ch),
        ParametrizedRule("No Needy present", (dirs, module) => module.Bomb.GetModuleNames().Count == module.Bomb.GetSolvableModuleNames().Count)
    );

    private static Func<YellowArrowsScript, MonoRandom, RuleInfo> ParametrizedRule(
        string ruleName, Func<List<ArrowDirection>, YellowArrowsScript, bool> rule)
    {
        return (module, rnd) =>
        {
            var rands = GenerateDirections(rnd);
            return new RuleInfo($"{ruleName} ? {rands[0]} : {rands[1]}", dirs => rule(dirs, module) ? rands[0] : rands[1]);
        };
    }

    private static Func<YellowArrowsScript, MonoRandom, RuleInfo> ParametrizedRule<T>(
        Func<MonoRandom, T> getRnd, Func<T, string> ruleName, Func<List<ArrowDirection>, YellowArrowsScript, T, bool> rule)
    {
        return (module, rnd) =>
        {
            var rands = GenerateDirections(rnd);
            var randPrev = getRnd(rnd);
            return new RuleInfo($"{ruleName(randPrev)} ? {rands[0]} : {rands[1]}", dirs => rule(dirs, module, randPrev) ? rands[0] : rands[1]);
        };
    }

    private static ArrowDirection[] GenerateDirections(MonoRandom rnd)
    {
        return rnd.ShuffleFisherYates((ArrowDirection[]) Enum.GetValues(typeof(ArrowDirection)));
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
        _rules = rnd.Seed == 1 ? _seed1rules.Select(rule => new Func<List<ArrowDirection>, ArrowDirection>(n => rule(n, this))).ToArray() : GenerateRuleSeed(rnd);
        Generate();
    }

    private Func<List<ArrowDirection>, ArrowDirection>[] GenerateRuleSeed(MonoRandom rnd)
    {
        var rulesRaw = rnd.ShuffleFisherYates(_ruleSeededRules).Select(r => r(this, rnd)).ToArray();
        var rulesStrings = rulesRaw.Take(26).Select((r, ix) => $"<Yellow Arrows #{moduleId}> {(char) ('A' + ix)} = {r.Name}").ToArray();
        Debug.Log($"<Yellow Arrows #{moduleId}> Rules:\n{rulesStrings.Join("\n")}");
        return rulesRaw.Select(r => r.Rule).ToArray();
    }

    private void Generate()
    {
        _inputIx = 0;
        _displayedLetterIx = Rnd.Range(0, 26);
        Debug.LogFormat("[Yellow Arrows #{0}] The starting row is {1}.", moduleId, "ABCDEFGHIJKLMNOPQRSTUVWXYZ"[_displayedLetterIx]);
        _offset = Bomb.GetSerialNumber()[5] - '0' + 1;
        int ix = _displayedLetterIx;
        _solutionDirs = new List<ArrowDirection>();
        for (int i = 0; i < 5; i++)
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
            if (_solutionDirs[_inputIx] == ArrowDirection.Any || _solutionDirs[_inputIx] == (ArrowDirection) btn)
            {
                Debug.LogFormat("[Yellow Arrows #{0}] {1} was correctly pressed.", moduleId, (ArrowDirection) btn);
                _inputIx++;
                if (_inputIx == 5)
                {
                    _canInteract = false;
                    StartCoroutine(Victory());
                }
            }
            else
            {
                Debug.LogFormat("[Yellow Arrows #{0}] {1} was pressed, when {2} was expected. Strike.", moduleId, _solutionDirs[_inputIx], (ArrowDirection) btn);
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
        while (_inputIx != 5)
        {
            ButtonSels[(int) _solutionDirs[_inputIx] % 4].OnInteract(); // Modulo 4, otherwise "Any" causes an IndexOutOfRange exception.
            yield return new WaitForSeconds(0.1f);
        }
        while (!moduleSolved)
            yield return true;
    }
}
