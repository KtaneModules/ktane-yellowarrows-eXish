using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using System;
using System.Text.RegularExpressions;

public class YellowArrowsScript : MonoBehaviour {

    public KMAudio audio;
    public KMBombInfo bomb;

    public KMSelectable[] buttons;
    public GameObject numDisplay;

    private string[] moves = new string[5];
    private string[] movesperf = new string[5];
    private string[] letters = { "A","B","C","D","E","F","G","H","I","J","K","L","M","N","O","P","Q","R","S","T","U","V","W","X","Y","Z" };
    private string letter;
    private int offset;
    private int current;
    private int letindex;

    private bool resetting = false;

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    void Awake()
    {
        current = 0;
        moduleId = moduleIdCounter++;
        moduleSolved = false;
        foreach(KMSelectable obj in buttons){
            KMSelectable pressed = obj;
            pressed.OnInteract += delegate () { PressButton(pressed); return false; };
        }
    }

    void Start () {
        current = 0;
        numDisplay.GetComponent<TextMesh>().text = " ";
        StartCoroutine(generateNewLet());
        StartCoroutine(getMoves());
    }

    void PressButton(KMSelectable pressed)
    {
        if(moduleSolved != true && resetting != true)
        {
            pressed.AddInteractionPunch(0.25f);
            audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
            if(pressed == buttons[0] && !moves[current].Equals("UP") && !moves[current].Equals("ANY"))
            {
                GetComponent<KMBombModule>().HandleStrike();
                Debug.LogFormat("[Yellow Arrows #{0}] The button 'UP' was incorrect, expected '{1}'! Resetting module...", moduleId, moves[current]);
                Start();
            }
            else if (pressed == buttons[1] && !moves[current].Equals("DOWN") && !moves[current].Equals("ANY"))
            {
                GetComponent<KMBombModule>().HandleStrike();
                Debug.LogFormat("[Yellow Arrows #{0}] The button 'DOWN' was incorrect, expected '{1}'! Resetting module...", moduleId, moves[current]);
                Start();
            }
            else if (pressed == buttons[2] && !moves[current].Equals("LEFT") && !moves[current].Equals("ANY"))
            {
                GetComponent<KMBombModule>().HandleStrike();
                Debug.LogFormat("[Yellow Arrows #{0}] The button 'LEFT' was incorrect, expected '{1}'! Resetting module...", moduleId, moves[current]);
                Start();
            }
            else if (pressed == buttons[3] && !moves[current].Equals("RIGHT") && !moves[current].Equals("ANY"))
            {
                GetComponent<KMBombModule>().HandleStrike();
                Debug.LogFormat("[Yellow Arrows #{0}] The button 'RIGHT' was incorrect, expected '{1}'! Resetting module...", moduleId, moves[current]);
                Start();
            }
            else
            {
                if(pressed == buttons[0])
                {
                    movesperf[current] = "UP";
                }else if (pressed == buttons[1])
                {
                    movesperf[current] = "DOWN";
                }else if (pressed == buttons[2])
                {
                    movesperf[current] = "LEFT";
                }else if (pressed == buttons[3])
                {
                    movesperf[current] = "RIGHT";
                }
                current++;
                if (current == 5)
                {
                    StartCoroutine(victory());
                }
                else
                {
                    StartCoroutine(getMoves());
                }
            }
        }
    }

    private IEnumerator generateNewLet()
    {
        yield return null;
        int rando = UnityEngine.Random.RandomRange(0, 26);
        yield return new WaitForSeconds(0.5f);
        numDisplay.GetComponent<TextMesh>().text = "" + letters[rando];
        StopCoroutine("generateNewLet");
        Debug.LogFormat("[Yellow Arrows #{0}] The Starting row is '{1}'!", moduleId, letters[rando]);
        letindex = rando;
        letter = letters[letindex];
    }

    private IEnumerator victory()
    {
        yield return null;
        for(int i = 0; i < 100; i++)
        {
            int rand1 = UnityEngine.Random.RandomRange(0, 10);
            if (i < 50)
            {
                numDisplay.GetComponent<TextMesh>().text = rand1 + "";
            }
            else
            {
                numDisplay.GetComponent<TextMesh>().text = "G" + rand1;
            }
            yield return new WaitForSeconds(0.025f);
        }
        numDisplay.GetComponent<TextMesh>().text = "GG";
        StopCoroutine("victory");
        Debug.LogFormat("[Yellow Arrows #{0}] All Presses were correct! Module Disarmed!", moduleId);
        GetComponent<KMBombModule>().HandlePass();
        moduleSolved = true;
    }

    private IEnumerator getMoves()
    {
        resetting = true;
        yield return null;
        yield return new WaitForSeconds(0.5f);
        string num = "" + bomb.GetSerialNumber().ElementAt(5);
        int.TryParse(num, out offset);
        offset += 1;
        int next = Array.IndexOf(letters, letter) + offset;
        if (next > 25)
        {
            next %= 26;
        }
        letter = letters[next];
        if (letter.Equals("A"))
        {
            moves[current] = "UP";
        }
        else if (letter.Equals("B"))
        {
            if (current != 0)
            {
                if (movesperf[current - 1].Equals("LEFT"))
                {
                    moves[current] = "DOWN";
                }
                else
                {
                    moves[current] = "RIGHT";
                }
            }
            else
            {
                moves[current] = "RIGHT";
            }
        }
        else if (letter.Equals("C"))
        {
            if ((offset - 1) == 3)
            {
                moves[current] = "LEFT";
            }
            else
            {
                moves[current] = "UP";
            }
        }
        else if (letter.Equals("D"))
        {
            if (letters[letindex].Equals("D"))
            {
                moves[current] = "UP";
            }
            else
            {
                moves[current] = "DOWN";
            }
        }
        else if (letter.Equals("E"))
        {
            if (bomb.IsIndicatorPresent("SIG") && bomb.IsIndicatorOn("SIG"))
            {
                moves[current] = "RIGHT";
            }
            else
            {
                moves[current] = "LEFT";
            }
        }
        else if (letter.Equals("F"))
        {
            if (!bomb.IsPortPresent("PS2"))
            {
                moves[current] = "DOWN";
            }
            else
            {
                moves[current] = "ANY";
            }
        }
        else if (letter.Equals("G"))
        {
            if (!movesperf.Contains("DOWN"))
            {
                moves[current] = "UP";
            }
            else
            {
                moves[current] = "DOWN";
            }
        }
        else if (letter.Equals("H"))
        {
            if (bomb.IsPortPresent("Serial"))
            {
                moves[current] = "ANY";
            }
            else
            {
                moves[current] = "RIGHT";
            }
        }
        else if (letter.Equals("I"))
        {
            if (bomb.GetModuleNames().Count == bomb.GetSolvableModuleNames().Count)
            {
                moves[current] = "DOWN";
            }
            else
            {
                moves[current] = "ANY";
            }
        }
        else if (letter.Equals("J"))
        {
            if (current != 0)
            {
                if (movesperf[current - 1].Equals("DOWN"))
                {
                    moves[current] = "LEFT";
                }
                else
                {
                    moves[current] = "UP";
                }
            }
            else
            {
                moves[current] = "UP";
            }
        }
        else if (letter.Equals("K"))
        {
            moves[current] = "DOWN";
        }
        else if (letter.Equals("L"))
        {
            if (bomb.GetBatteryCount() == 0)
            {
                moves[current] = "UP";
            }
            else
            {
                moves[current] = "DOWN";
            }
        }
        else if (letter.Equals("M"))
        {
            if (bomb.GetBatteryHolderCount() < 3)
            {
                moves[current] = "RIGHT";
            }
            else
            {
                moves[current] = "LEFT";
            }
        }
        else if (letter.Equals("N"))
        {
            if (letters[letindex].Equals("N"))
            {
                moves[current] = "ANY";
            }
            else
            {
                moves[current] = "RIGHT";
            }
        }
        else if (letter.Equals("O"))
        {
            if (bomb.GetSerialNumber().Contains("O"))
            {
                moves[current] = "LEFT";
            }
            else
            {
                moves[current] = "DOWN";
            }
        }
        else if (letter.Equals("P"))
        {
            if (bomb.GetSerialNumberLetters().Count() == 4)
            {
                moves[current] = "DOWN";
            }
            else
            {
                moves[current] = "UP";
            }
        }
        else if (letter.Equals("Q"))
        {
            if (current != 0)
            {
                if (movesperf[current - 1].Equals("RIGHT"))
                {
                    moves[current] = "DOWN";
                }
                else
                {
                    moves[current] = "LEFT";
                }
            }
            else
            {
                moves[current] = "LEFT";
            }
        }
        else if (letter.Equals("R"))
        {
            if (bomb.IsIndicatorPresent("CLR") && bomb.IsIndicatorOff("CLR"))
            {
                moves[current] = "UP";
            }
            else
            {
                moves[current] = "DOWN";
            }
        }
        else if (letter.Equals("S"))
        {
            moves[current] = "LEFT";
        }
        else if (letter.Equals("T"))
        {
            if (bomb.GetBatteryCount() % 2 == 0)
            {
                moves[current] = "LEFT";
            }
            else
            {
                moves[current] = "DOWN";
            }
        }
        else if (letter.Equals("U"))
        {
            moves[current] = "ANY";
        }
        else if (letter.Equals("V"))
        {
            if (current != 0)
            {
                if (movesperf[current - 1].Equals("UP"))
                {
                    moves[current] = "UP";
                }
                else
                {
                    moves[current] = "DOWN";
                }
            }
            else
            {
                moves[current] = "DOWN";
            }
        }
        else if (letter.Equals("W"))
        {
            if (bomb.GetPortPlateCount() == 0)
            {
                moves[current] = "RIGHT";
            }
            else
            {
                moves[current] = "ANY";
            }
        }
        else if (letter.Equals("X"))
        {
            if (letters[letindex].Equals("X"))
            {
                moves[current] = "UP";
            }
            else
            {
                moves[current] = "LEFT";
            }
        }
        else if (letter.Equals("Y"))
        {
            if (!movesperf.Contains("UP"))
            {
                moves[current] = "ANY";
            }
            else
            {
                moves[current] = "UP";
            }
        }
        else if (letter.Equals("Z"))
        {
            moves[current] = "RIGHT";
        }
        Debug.LogFormat("[Yellow Arrows #{0}] Press #{1} should be '{2}' according to row '{3}'!", moduleId, current+1, moves[current], letter);
        StopCoroutine("getMoves");
        resetting = false;
    }

    //twitch plays
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} up [Presses the up arrow button] | !{0} right [Presses the right arrow button] | !{0} down [Presses the down arrow button once] | !{0} left [Presses the left arrow button once] | !{0} left right down up [Chain button presses] | !{0} reset [Resets the module back to the start] | Direction words can be substituted as one letter (Ex. right as r)";
    #pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string command)
    {
        if (Regex.IsMatch(command, @"^\s*reset\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            numDisplay.GetComponent<TextMesh>().text = " ";
            yield return new WaitForSeconds(0.5f);
            current = 0;
            numDisplay.GetComponent<TextMesh>().text = "" + letters[letindex];
            Debug.LogFormat("[Yellow Arrows #{0}] Module Reset back to initial state (no inputs)!", moduleId);
            yield break;
        }

        string[] parameters = command.Split(' ');
        var buttonsToPress = new List<KMSelectable>();
        foreach (string param in parameters)
        {
            if (param.EqualsIgnoreCase("up") || param.EqualsIgnoreCase("u"))
                buttonsToPress.Add(buttons[0]);
            else if (param.EqualsIgnoreCase("down") || param.EqualsIgnoreCase("d"))
                buttonsToPress.Add(buttons[1]);
            else if (param.EqualsIgnoreCase("left") || param.EqualsIgnoreCase("l"))
                buttonsToPress.Add(buttons[2]);
            else if (param.EqualsIgnoreCase("right") || param.EqualsIgnoreCase("r"))
                buttonsToPress.Add(buttons[3]);
            else
                yield break;
        }

        yield return null;
        foreach(KMSelectable km in buttonsToPress)
        {
            km.OnInteract();
            yield return new WaitForSeconds(0.6f);
        }
    }
}
