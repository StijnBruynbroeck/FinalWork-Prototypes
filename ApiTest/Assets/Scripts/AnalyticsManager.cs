using UnityEngine;
using System.IO;
using System;

public class AnalyticsManager : MonoBehaviour
{
    private string filePath;

    void Start()
    {
        filePath = Application.dataPath + "/LLM_Performance_Log.csv";

        if (!File.Exists(filePath))
        {
            string header = "DatumTijd,GevraagdObject,AI_Selectie,Score,TotaleTijd_ms\n";
            File.WriteAllText(filePath, header);
        }
    }

    public void LogData(string spelerInput, string aiPrefab, int score, long totaleTijdMs)
    {
        string tijdstip = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        
        spelerInput = spelerInput.Replace(",", " ");

        string newRow = $"{tijdstip},{spelerInput},{aiPrefab},{score},{totaleTijdMs}\n";

        File.AppendAllText(filePath, newRow);
        
        Debug.Log($"<color=green>Data succesvol opgeslagen in CSV! ({totaleTijdMs} ms)</color>");
    }
}