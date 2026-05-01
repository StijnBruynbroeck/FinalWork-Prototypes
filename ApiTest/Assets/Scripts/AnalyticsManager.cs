using UnityEngine;
using System.IO;
using System;

public class AnalyticsManager : MonoBehaviour
{
    private string filePath;

    void Start()
    {
        // Maakt een bestand genaamd "LLM_Performance_Log.csv" in je project map
        filePath = Application.dataPath + "/LLM_Performance_Log.csv";

        // Als het bestand nog niet bestaat, maken we de headers (kolommen) aan
        if (!File.Exists(filePath))
        {
            string header = "DatumTijd,GevraagdObject,AI_Selectie,Score,TotaleTijd_ms\n";
            File.WriteAllText(filePath, header);
        }
    }

    // Deze functie roepen we straks aan als de hele AI-loop klaar is
    public void LogData(string spelerInput, string aiPrefab, int score, long totaleTijdMs)
    {
        string tijdstip = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        
        // Zorg dat we geen komma's in de tekst hebben die de CSV breken
        spelerInput = spelerInput.Replace(",", " "); 

        // Bouw de regel op: Datum, Input, Output, Score, Tijd
        string newRow = $"{tijdstip},{spelerInput},{aiPrefab},{score},{totaleTijdMs}\n";

        // Schrijf het onderaan het bestand erbij
        File.AppendAllText(filePath, newRow);
        
        Debug.Log($"<color=green>Data succesvol opgeslagen in CSV! ({totaleTijdMs} ms)</color>");
    }
}