using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class FurnitureCounter : MonoBehaviour
{
    [System.Serializable]
    public class FurnitureTarget
    {
        public string displayName = "COUCH";
        public string searchTag = "couch";
        public int expectedCount = 3;
        public string hint = "Count the couches in the showroom";
    }

    [Header("Meubel Targets")]
    public FurnitureTarget[] furnitureTargets = new FurnitureTarget[] {
        new FurnitureTarget { displayName = "COUCH", searchTag = "couch", expectedCount = 3, hint = "Count the couches in the showroom" },
        new FurnitureTarget { displayName = "LAMP", searchTag = "lamp", expectedCount = 2, hint = "Count the lamps in the showroom" }
    };

    [Header("Deur")]
    public DoorController verbondenDeur;

    private bool isVoltooid = false;
    private int huidigeFurnitureIndex = 0;
    private string wachtwoordCode = "";

    public bool IsVoltooid() => isVoltooid;
    public int HuidigeIndex() => huidigeFurnitureIndex;
    public int TotaalFurniture() => furnitureTargets.Length;

    void Start()
    {
        if (furnitureTargets.Length > 0)
        {
            string code = "";
            foreach (var ft in furnitureTargets)
                code += ft.expectedCount.ToString();
            wachtwoordCode = code;
        }
    }

    public string GetClueText()
    {
        string result = ">_ DECRYPTING FLOOR PLAN...\n";
        result += ">_ SCANNING INVENTORY...\n";
        result += ">_ TARGETS IDENTIFIED:\n\n";
        foreach (var ft in furnitureTargets)
        {
            result += $"  [{ft.displayName}] > {ft.hint}\n";
        }
        result += $"\n>_ SPEAK CODE: {GenereerCodePrompt()}";
        return result;
    }

    private string GenereerCodePrompt()
    {
        string prompt = "Say: ";
        for (int i = 0; i < furnitureTargets.Length; i++)
        {
            if (i > 0) prompt += " en ";
            prompt += $"\"[{furnitureTargets[i].displayName}] = [...number]\"";
        }
        return prompt;
    }

    public int VerwerkInput(string gesprokenTekst)
    {
        if (isVoltooid) return 100;
        string text = gesprokenTekst.ToLower().Trim();

        int[] gevonden = new int[furnitureTargets.Length];

        for (int i = 0; i < furnitureTargets.Length; i++)
        {
            var ft = furnitureTargets[i];
            string tag = ft.searchTag.ToLower();
            string display = ft.displayName.ToLower();

            bool heeftTag = text.Contains(tag) || text.Contains(display);

            int nummer = ExtractNumber(text);

            if (heeftTag && nummer >= 0)
            {
                gevonden[i] = nummer;
            }
            else if (nummer >= 0 && furnitureTargets.Length == 1)
            {
                gevonden[i] = nummer;
            }
            else if (text == wachtwoordCode)
            {
                for (int j = 0; j < furnitureTargets.Length; j++)
                    gevonden[j] = furnitureTargets[j].expectedCount;
                break;
            }
        }

        bool alleCorrect = true;
        for (int i = 0; i < furnitureTargets.Length; i++)
        {
            if (gevonden[i] == 0)
            {
                string numStr = ExtractNumberSingle(text);
                if (numStr != null)
                {
                    if (furnitureTargets.Length == 1)
                        gevonden[i] = int.Parse(numStr);
                }
            }

            if (gevonden[i] != furnitureTargets[i].expectedCount)
            {
                alleCorrect = false;
            }
        }

        if (alleCorrect)
        {
            isVoltooid = true;
            if (verbondenDeur != null)
            {
                verbondenDeur.UnlockDoor();
                verbondenDeur.OpenDoor();
            }
            Debug.Log("[FurnitureCounter] ALLE AANTALLEN CORRECT! Deur geopend!");
            return 100;
        }

        for (int i = 0; i < furnitureTargets.Length; i++)
        {
            if (gevonden[i] > 0 && gevonden[i] != furnitureTargets[i].expectedCount)
            {
                Debug.Log($"[FurnitureCounter] {furnitureTargets[i].displayName}: verwacht {furnitureTargets[i].expectedCount}, kreeg {gevonden[i]}");
            }
        }

        return 0;
    }

    private int ExtractNumber(string text)
    {
        string numStr = ExtractNumberSingle(text);
        if (numStr != null) return int.Parse(numStr);

        if (text.Contains("een") || text.Contains("één")) return 1;
        if (text.Contains("twee")) return 2;
        if (text.Contains("drie")) return 3;
        if (text.Contains("vier")) return 4;
        if (text.Contains("vijf")) return 5;
        if (text.Contains("zes")) return 6;
        if (text.Contains("zeven")) return 7;
        if (text.Contains("acht")) return 8;
        if (text.Contains("negen")) return 9;
        if (text.Contains("nul")) return 0;

        return -1;
    }

    private string ExtractNumberSingle(string text)
    {
        Match match = Regex.Match(text, @"\d+");
        if (match.Success) return match.Value;
        return null;
    }

    public string GetFeedback(int result)
    {
        if (result == 100) return ">_ ACCESS GRANTED // DECRYPTION SUCCESSFUL\n>_ DOOR UNLOCKED";

        string feedback = ">_ ACCESS DENIED // INVALID COUNT\n";
        for (int i = 0; i < furnitureTargets.Length; i++)
        {
            var ft = furnitureTargets[i];
            feedback += $">_ {ft.displayName}: verwacht {ft.expectedCount}\n";
        }
        feedback += "\n>_ PROBEER OPNIEUW";
        return feedback;
    }

    public void ResetPuzzle()
    {
        isVoltooid = false;
        huidigeFurnitureIndex = 0;
    }
}
