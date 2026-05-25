using UnityEngine;
using TMPro;
using System.Collections;
using System;

public class VoiceRiddlePuzzle : MonoBehaviour
{
    [Header("Puzzle Settings")]
    public string puzzleName = "SECURITY BYPASS";
    public float interactionDistance = 3f;
    
    [Header("Stages")]
    public string[] stageDescriptions = {
        "SCAN DE KAMER - Beschrijf wat je ziet",
        "IDENTIFICEER - Welk object heeft een blauw lichtje?",
        "IMITATIE - Maak het geluid van een server",
        "CODE - Geef de autorisatiecode"
    };
    
    [Header("Expected Answers (hints for LLM)")]
    public string[] expectedConcepts = {
        "serverruimte, computers, kasten",
        "serverkast, blauw led, indicator",
        "zoemen, brommen, electronisch",
        "4821, autorisatie, code"
    };
    
    [Header("UI")]
    public TextMeshProUGUI puzzleDisplay;
    public TextMeshProUGUI feedbackDisplay;
    public GameObject puzzleCanvas;
    
    [Header("Rewards")]
    public DoorController connectedDoor;
    public GameObject rewardObject;
    
    private int currentStage = 0;
    private bool isActive = false;
    private bool isCompleted = false;
    private GroqLLMService llmService;
    private Transform player;
    
    void Start()
    {
        llmService = FindObjectOfType<GroqLLMService>();
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
        
        if (puzzleCanvas != null)
            puzzleCanvas.SetActive(false);
        
        ResetPuzzle();
    }
    
    void Update()
    {
        if (player == null) return;
        
        float distance = Vector3.Distance(transform.position, player.position);
        
        if (distance <= interactionDistance && !isCompleted)
        {
            if (!isActive && puzzleCanvas != null)
            {
                puzzleCanvas.SetActive(true);
                isActive = true;
                UpdateDisplay();
            }
        }
        else
        {
            if (isActive && puzzleCanvas != null)
            {
                puzzleCanvas.SetActive(false);
                isActive = false;
            }
        }
    }
    
    public void ProcessVoiceInput(string input)
    {
        if (!isActive || isCompleted || llmService == null) return;
        
        string context = $"Security Bypass Puzzel: {puzzleName}. " +
                       $"Je bent in een serverruimte en moet het beveiligingssysteem omzeilen. " +
                       $"Je bent nu bij hoofdstuk {currentStage + 1} van {stageDescriptions.Length}.";
        
        llmService.EvaluatePuzzleResponse(context, stageDescriptions[currentStage], input, (result) =>
        {
            ShowFeedback(result.feedback);
            
            if (result.correct)
            {
                StageCompleted();
            }
        });
    }
    
    private void StageCompleted()
    {
        currentStage++;
        
        if (currentStage >= stageDescriptions.Length)
        {
            PuzzleCompleted();
        }
        else
        {
            UpdateDisplay();
            if (feedbackDisplay != null)
            {
                feedbackDisplay.text = "✓ Stap " + currentStage + " voltooid!";
                StartCoroutine(ClearFeedback());
            }
        }
    }
    
    private void PuzzleCompleted()
    {
        isCompleted = true;
        
        if (puzzleDisplay != null)
        {
            puzzleDisplay.color = Color.green;
            puzzleDisplay.text = $">_ {puzzleName}\n>_ PUZZEL VOLTOOID!\n>_ BEVEILIGING OVERRIDDEN\n>_ TOEGANG VERLEEND";
        }
        
        if (connectedDoor != null)
        {
            connectedDoor.UnlockDoor();
            connectedDoor.OpenDoor();
        }
        
        if (rewardObject != null)
            rewardObject.SetActive(true);
        
        Debug.Log("🎉 Nieuwe puzzel voltooid!");
    }
    
    private void UpdateDisplay()
    {
        if (puzzleDisplay != null && currentStage < stageDescriptions.Length)
        {
            puzzleDisplay.text = $">_ {puzzleName}\n" +
                                $">_ STAP {currentStage + 1}/{stageDescriptions.Length}\n" +
                                $">_ {stageDescriptions[currentStage]}\n\n" +
                                $">_ Verwacht: {expectedConcepts[currentStage]}";
        }
    }
    
    private void ShowFeedback(string feedback)
    {
        if (feedbackDisplay != null)
        {
            feedbackDisplay.text = ">> " + feedback;
        }
    }
    
    private IEnumerator ClearFeedback()
    {
        yield return new WaitForSeconds(2f);
        if (feedbackDisplay != null)
            feedbackDisplay.text = "";
    }
    
    private void ResetPuzzle()
    {
        currentStage = 0;
        isCompleted = false;
        isActive = false;
        
        if (puzzleDisplay != null)
        {
            puzzleDisplay.color = new Color(0, 1, 0);
            UpdateDisplay();
        }
    }
    
    public bool IsInRange => isActive;
    public bool IsCompleted => isCompleted;
    public int CurrentStage => currentStage;
    public int TotalStages => stageDescriptions.Length;
}
