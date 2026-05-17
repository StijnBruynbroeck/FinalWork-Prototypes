using UnityEngine;
using System.Collections.Generic;
using System;
using System.Text.RegularExpressions;

public class HackMinigame : MonoBehaviour
{
    [Header("Woordenlijst Instellingen")]
    public string[] woordLijst = {
        "ACCESS", "OVERRIDE", "SECURE", "LOCKED", "GUARD",
        "BREACH", "TARGET", "ENSIGN", "BYPASS", "ROBOT",
        "THREAD", "PATHOS", "SECTOR", "ASSIGN", "LEGACY"
    };
    public int aantalWoorden = 12;
    public int maxPogingen = 4;

    [Header("Dud Removers")]
    public string dudChars = "[](){}<>";

    [Header("Events")]
    public System.Action OnHackSuccess;
    public System.Action OnHackFailed;
    public System.Action<int, int> OnAttemptUsed;

    private string correctWoord;
    private List<string> actieveWoorden = new List<string>();
    private int pogingenOver;
    private bool isVoltooid = false;
    private bool isGestart = false;
    private string laatstGeselecteerd = "";

    public bool IsVoltooid() => isVoltooid;
    public bool IsGestart() => isGestart;
    public int PogingenOver() => pogingenOver;
    public int MaxPogingen() => maxPogingen;
    public string CorrectWoord() => correctWoord;
    public List<string> ActieveWoorden() => actieveWoorden;

    public void StartGame()
    {
        if (woordLijst.Length < 2)
        {
            Debug.LogError("HackMinigame: woordLijst moet minimaal 2 woorden bevatten!");
            return;
        }

        isGestart = true;
        isVoltooid = false;
        pogingenOver = maxPogingen;
        actieveWoorden.Clear();
        laatstGeselecteerd = "";

        List<string> beschikbaar = new List<string>(woordLijst);
        System.Random rng = new System.Random();

        correctWoord = beschikbaar[rng.Next(beschikbaar.Count)];
        beschikbaar.Remove(correctWoord);

        actieveWoorden.Add(correctWoord);

        int nodig = Mathf.Min(aantalWoorden - 1, beschikbaar.Count);
        for (int i = 0; i < nodig; i++)
        {
            int idx = rng.Next(beschikbaar.Count);
            actieveWoorden.Add(beschikbaar[idx]);
            beschikbaar.RemoveAt(idx);
        }

        for (int i = actieveWoorden.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            string temp = actieveWoorden[i];
            actieveWoorden[i] = actieveWoorden[j];
            actieveWoorden[j] = temp;
        }

        Debug.Log($"[HackMinigame] Nieuw spel gestart! Correct: {correctWoord} | Woorden: {actieveWoorden.Count} | Pogingen: {pogingenOver}");
    }

    public int SelecteerWoord(string gesprokenWoord)
    {
        if (!isGestart || isVoltooid || pogingenOver <= 0)
            return -1;

        string genormaliseerd = NormaliseerWoord(gesprokenWoord);
        string match = VindBesteMatch(genormaliseerd, actieveWoorden);

        if (match == null)
        {
            Debug.Log($"[HackMinigame] '{gesprokenWoord}' komt niet overeen met een woord in de lijst.");
            return -2;
        }

        laatstGeselecteerd = match;

        if (match == correctWoord)
        {
            isVoltooid = true;
            Debug.Log("[HackMinigame] CORRECT! Hack geslaagd!");
            OnHackSuccess?.Invoke();
            return 100;
        }

        pogingenOver--;
        int likeness = BerekenLikeness(match, correctWoord);
        Debug.Log($"[HackMinigame] FOUT! '{match}' = {likeness}/{correctWoord.Length} correct. Pogingen over: {pogingenOver}");
        OnAttemptUsed?.Invoke(pogingenOver, maxPogingen);

        if (pogingenOver <= 0)
        {
            isVoltooid = true;
            Debug.Log("[HackMinigame] FAILED! Geen pogingen meer!");
            OnHackFailed?.Invoke();
            return -3;
        }

        return likeness;
    }

    public string CheckDudRemover(string text)
    {
        string lower = text.ToLower().Trim();

        // Voice: zoek naar brackets in text (Whisper zet ze soms als "open bracket", "close bracket")
        for (int i = 0; i < dudChars.Length; i += 2)
        {
            char open = dudChars[i];
            char close = dudChars[i + 1];

            int startIdx = lower.IndexOf(open);
            int endIdx = lower.IndexOf(close);

            if (startIdx >= 0 && endIdx > startIdx)
            {
                string inner = lower.Substring(startIdx + 1, endIdx - startIdx - 1);
                if (inner.Contains("dud") || inner.Contains("remove") || inner.Contains("fake"))
                {
                    RemoveDuds();
                    return "DUD_REMOVED";
                }
                if (inner.Contains("reset") || inner.Contains("retry") || inner.Contains("again"))
                {
                    pogingenOver = Mathf.Min(pogingenOver + 1, maxPogingen);
                    return "ATTEMPT_RESTORED";
                }
            }
        }

        // Voice fallback: speler zegt gewoon "dud", "remove dud", "reset attempt" etc.
        if (lower.Contains("dud") || lower.Contains("remove fake"))
        {
            RemoveDuds();
            return "DUD_REMOVED";
        }
        if (lower.Contains("reset attempt") || lower.Contains("retry") || lower.Contains("extra poging"))
        {
            pogingenOver = Mathf.Min(pogingenOver + 1, maxPogingen);
            return "ATTEMPT_RESTORED";
        }

        return null;
    }

    private void RemoveDuds()
    {
        if (actieveWoorden.Count <= 1) return;
        List<string> overgebleven = new List<string>();
        foreach (var w in actieveWoorden)
        {
            if (w == correctWoord || UnityEngine.Random.Range(0, 3) > 0)
                overgebleven.Add(w);
        }
        if (overgebleven.Count < 2)
            overgebleven.Add(correctWoord);
        actieveWoorden = overgebleven;
    }

    private int BerekenLikeness(string gekozen, string correct)
    {
        int count = 0;
        int len = Mathf.Min(gekozen.Length, correct.Length);
        for (int i = 0; i < len; i++)
        {
            if (char.ToUpper(gekozen[i]) == char.ToUpper(correct[i]))
                count++;
        }
        return count;
    }

    private string NormaliseerWoord(string tekst)
    {
        return tekst.Trim().ToUpper();
    }

    private string VindBesteMatch(string input, List<string> woorden)
    {
        string bestMatch = null;
        int bestScore = int.MaxValue;

        foreach (var w in woorden)
        {
            int dist = Levenshtein(input, w);
            if (dist < bestScore)
            {
                bestScore = dist;
                bestMatch = w;
            }
        }

        float maxTolerantie = 3f;
        if (bestScore <= maxTolerantie)
            return bestMatch;

        if (bestMatch != null && input.Length >= 3)
        {
            if (bestMatch.StartsWith(input) || input.StartsWith(bestMatch))
                return bestMatch;
        }

        return null;
    }

    private int Levenshtein(string a, string b)
    {
        a = a.ToUpper();
        b = b.ToUpper();
        int lenA = a.Length;
        int lenB = b.Length;
        int[,] matrix = new int[lenA + 1, lenB + 1];
        for (int i = 0; i <= lenA; i++) matrix[i, 0] = i;
        for (int j = 0; j <= lenB; j++) matrix[0, j] = j;
        for (int i = 1; i <= lenA; i++)
        {
            for (int j = 1; j <= lenB; j++)
            {
                int cost = (a[i - 1] == b[j - 1]) ? 0 : 1;
                matrix[i, j] = Mathf.Min(
                    Mathf.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost
                );
            }
        }
        return matrix[lenA, lenB];
    }

    public string FormatWoordGrid()
    {
        if (actieveWoorden.Count == 0) return "";

        int kolommen = 3;
        int rijen = Mathf.CeilToInt((float)actieveWoorden.Count / kolommen);
        string grid = "";

        grid += "╔══════════════════════════════════╗\n";
        grid += "║        SECURITY OVERRIDE         ║\n";
        grid += "╠══════════════════════════════════╣\n";

        for (int r = 0; r < rijen; r++)
        {
            string row = "║  ";
            for (int c = 0; c < kolommen; c++)
            {
                int idx = r + c * rijen;
                if (idx < actieveWoorden.Count)
                {
                    string w = actieveWoorden[idx].PadRight(8);
                    row += w + "  ";
                }
                else
                {
                    row += "          ";
                }
            }
            grid += row + "║\n";
        }

        string dudLine = GenereerDudLine();
        grid += "╠══════════════════════════════════╣\n";
        grid += dudLine;
        grid += "╚══════════════════════════════════╝\n";

        grid += $"\nPOGINGEN: {pogingenOver}/{maxPogingen}";

        if (!string.IsNullOrEmpty(laatstGeselecteerd) && laatstGeselecteerd != correctWoord)
        {
            int l = BerekenLikeness(laatstGeselecteerd, correctWoord);
            grid += $"  |  LAATSTE: {laatstGeselecteerd} = {l}/{correctWoord.Length} CORRECT";
        }

        return grid;
    }

    private string GenereerDudLine()
    {
        string[] dudOptions = {
            "[DUD REMOVED]", "(RESET TOKEN)", "<R>ETRY>", "{FAKE}",
            "[----]", "(....)", "<....>", "{----}"
        };
        System.Random rng = new System.Random();
        int count = rng.Next(1, 4);
        string line = "║  ";
        for (int i = 0; i < count; i++)
        {
            line += dudOptions[rng.Next(dudOptions.Length)] + "  ";
        }
        return line.PadRight(42) + "║\n";
    }

    public string GetLastResult()
    {
        if (string.IsNullOrEmpty(laatstGeselecteerd)) return "";
        if (laatstGeselecteerd == correctWoord) return ">_ ACCESS GRANTED // OVERRIDE SUCCESSFUL";
        int l = BerekenLikeness(laatstGeselecteerd, correctWoord);
        return $">_ ACCESS DENIED // {laatstGeselecteerd} = {l}/{correctWoord.Length} MATCH";
    }

    public void ResetGame()
    {
        isGestart = false;
        isVoltooid = false;
        pogingenOver = maxPogingen;
        actieveWoorden.Clear();
        laatstGeselecteerd = "";
    }
}
