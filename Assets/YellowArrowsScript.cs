using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using System;

public class YellowArrowsScript : MonoBehaviour {

    public KMAudio audio;
    public KMBombInfo bomb;

    public KMSelectable[] buttons;
    public GameObject numDisplay;

    private string[] moves = new string[5];
    private string[] letters = { "A","B","C","D","E","F","G","H","I","J","K","L","M","N","O","P","Q","R","S","T","U","V","W","X","Y","Z" };
    private int current;
    private int letindex;

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
        if(moduleSolved != true)
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
                current++;
                if(current == 5)
                {
                    StartCoroutine(victory());
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
        GetComponent<KMBombModule>().HandlePass();
        moduleSolved = true;
    }

    private IEnumerator getMoves()
    {
        yield return null;
        yield return new WaitForSeconds(0.5f);
        string letter = letters[letindex];
        int offset;
        string num = ""+bomb.GetSerialNumber().ElementAt(5);
        int.TryParse(num, out offset);
        offset += 1;
        for(int i = 0; i < 5; i++)
        {
            if (letter.Equals("A"))
            {
                moves[i] = "UP";
            }else if (letter.Equals("B"))
            {
                if(i != 0)
                {
                    if (moves[i - 1].Equals("LEFT"))
                    {
                        moves[i] = "DOWN";
                    }
                    else
                    {
                        moves[i] = "RIGHT";
                    }
                }
                else
                {
                    moves[i] = "RIGHT";
                }
            }
            else if (letter.Equals("C"))
            {
                if((offset-1) == 3){
                    moves[i] = "LEFT";
                }
                else
                {
                    moves[i] = "UP";
                }
            }
            else if (letter.Equals("D"))
            {
                if(letters[letindex].Equals("D"))
                {
                    moves[i] = "UP";
                }
                else
                {
                    moves[i] = "DOWN";
                }
            }
            else if (letter.Equals("E"))
            {
                if (bomb.IsIndicatorPresent("SIG") && bomb.IsIndicatorOn("SIG"))
                {
                    moves[i] = "RIGHT";
                }
                else
                {
                    moves[i] = "LEFT";
                }
            }
            else if (letter.Equals("F"))
            {
                if (!bomb.IsPortPresent("PS2"))
                {
                    moves[i] = "DOWN";
                }
                else
                {
                    moves[i] = "ANY";
                }
            }
            else if (letter.Equals("G"))
            {
                if (!moves.Contains("DOWN"))
                {
                    moves[i] = "UP";
                }
                else
                {
                    moves[i] = "DOWN";
                }
            }
            else if (letter.Equals("H"))
            {
                if (bomb.IsPortPresent("Serial"))
                {
                    moves[i] = "ANY";
                }
                else
                {
                    moves[i] = "RIGHT";
                }
            }
            else if (letter.Equals("I"))
            {
                if (bomb.GetModuleNames().Count == bomb.GetSolvableModuleNames().Count)
                {
                    moves[i] = "DOWN";
                }
                else
                {
                    moves[i] = "ANY";
                }
            }
            else if (letter.Equals("J"))
            {
                if (i != 0)
                {
                    if (moves[i - 1].Equals("DOWN"))
                    {
                        moves[i] = "LEFT";
                    }
                    else
                    {
                        moves[i] = "UP";
                    }
                }
                else
                {
                    moves[i] = "UP";
                }
            }
            else if (letter.Equals("K"))
            {
                moves[i] = "DOWN";
            }
            else if (letter.Equals("L"))
            {
                if (bomb.GetBatteryCount() == 0)
                {
                    moves[i] = "UP";
                }
                else
                {
                    moves[i] = "DOWN";
                }
            }
            else if (letter.Equals("M"))
            {
                if (bomb.GetBatteryHolderCount() < 3)
                {
                    moves[i] = "RIGHT";
                }
                else
                {
                    moves[i] = "LEFT";
                }
            }
            else if (letter.Equals("N"))
            {
                if (letters[letindex].Equals("N"))
                {
                    moves[i] = "ANY";
                }
                else
                {
                    moves[i] = "RIGHT";
                }
            }
            else if (letter.Equals("O"))
            {
                if (bomb.GetSerialNumber().Contains("O"))
                {
                    moves[i] = "LEFT";
                }
                else
                {
                    moves[i] = "DOWN";
                }
            }
            else if (letter.Equals("P"))
            {
                if (bomb.GetSerialNumberLetters().Count() == 4)
                {
                    moves[i] = "DOWN";
                }
                else
                {
                    moves[i] = "UP";
                }
            }
            else if (letter.Equals("Q"))
            {
                if (i != 0)
                {
                    if (moves[i - 1].Equals("RIGHT"))
                    {
                        moves[i] = "DOWN";
                    }
                    else
                    {
                        moves[i] = "LEFT";
                    }
                }
                else
                {
                    moves[i] = "LEFT";
                }
            }
            else if (letter.Equals("R"))
            {
                if (bomb.IsIndicatorPresent("CLR") && bomb.IsIndicatorOff("CLR"))
                {
                    moves[i] = "UP";
                }
                else
                {
                    moves[i] = "DOWN";
                }
            }
            else if (letter.Equals("S"))
            {
                moves[i] = "LEFT";
            }
            else if (letter.Equals("T"))
            {
                if (bomb.GetBatteryCount() % 2 == 0)
                {
                    moves[i] = "LEFT";
                }
                else
                {
                    moves[i] = "DOWN";
                }
            }
            else if (letter.Equals("U"))
            {
                moves[i] = "ANY";
            }
            else if (letter.Equals("V"))
            {
                if (i != 0)
                {
                    if (moves[i - 1].Equals("UP"))
                    {
                        moves[i] = "UP";
                    }
                    else
                    {
                        moves[i] = "DOWN";
                    }
                }
                else
                {
                    moves[i] = "DOWN";
                }
            }
            else if (letter.Equals("W"))
            {
                if (bomb.GetPortPlateCount() == 0)
                {
                    moves[i] = "RIGHT";
                }
                else
                {
                    moves[i] = "ANY";
                }
            }
            else if (letter.Equals("X"))
            {
                if (letters[letindex].Equals("X"))
                {
                    moves[i] = "UP";
                }
                else
                {
                    moves[i] = "LEFT";
                }
            }
            else if (letter.Equals("Y"))
            {
                if (!moves.Contains("UP"))
                {
                    moves[i] = "ANY";
                }
                else
                {
                    moves[i] = "UP";
                }
            }
            else if (letter.Equals("Z"))
            {
                moves[i] = "RIGHT";
            }
            int next = Array.IndexOf(letters, letter) + offset;
            if (next > 25)
            {
                next %= 26;
            }
            Debug.LogFormat("[Yellow Arrows #{0}] Press #{1} should be '{2}' according to row '{3}'!", moduleId, i+1, moves[i], letter);
            letter = letters[next];
        }
        StopCoroutine("getMoves");
    }

    //twitch plays
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} up [Presses the up arrow button] | !{0} right [Presses the right arrow button] | !{0} down [Presses the down arrow button once] | !{0} left [Presses the left arrow button once] | !{0} left right down up [Chain button presses] | !{0} reset [Resets the module back to the start] | Direction words can be substituted as one letter (Ex. right as r)";
    #pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string command)
    {
        string[] parameters = command.Split(' ');
        foreach (string param in parameters)
        {
            yield return null;
            if (param.EqualsIgnoreCase("up") || param.EqualsIgnoreCase("u"))
            {
                buttons[0].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
            else if (param.EqualsIgnoreCase("down") || param.EqualsIgnoreCase("d"))
            {
                buttons[1].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
            else if (param.EqualsIgnoreCase("left") || param.EqualsIgnoreCase("l"))
            {
                buttons[2].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
            else if (param.EqualsIgnoreCase("right") || param.EqualsIgnoreCase("r"))
            {
                buttons[3].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
            else if (param.EqualsIgnoreCase("reset"))
            {
                numDisplay.GetComponent<TextMesh>().text = " ";
                yield return new WaitForSeconds(0.5f);
                current = 0;
                numDisplay.GetComponent<TextMesh>().text = "" + letters[letindex];
            }
            else
            {
                break;
            }
        }
    }
}
