using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using System;

public class GroqLLMService : MonoBehaviour
{
    [Header("Ollama Local Settings")]
    public string apiKey = "ollama"; // Geen echte key nodig lokaal
    private string localEndpoint = "http://localhost:11434/v1/chat/completions";

    public void EvaluatePlausibility(string roomContext, string playerInput, Action<LLMResult> onComplete)
    {
        StartCoroutine(SendToLLM(roomContext, playerInput, onComplete));
    }

    public void EvaluatePuzzleResponse(string puzzleContext, string stageDescription, string playerInput, Action<PuzzleResult> onComplete)
    {
        StartCoroutine(SendToLLMForPuzzle(puzzleContext, stageDescription, playerInput, onComplete));
    }

    private IEnumerator SendToLLMForPuzzle(string puzzleContext, string stageDescription, string playerInput, Action<PuzzleResult> onComplete)
    {
        // Volledig Engelse prompt, maar we vragen de AI expliciet om Nederlandse feedback/hints terug te geven
        string systemPrompt = "You are a puzzle master in a sci-fi stealth game. " +
                              "The player must solve a multi-step puzzle using voice commands. " +
                              "Evaluate if the player gives the correct answer for the current step. " +
                              "Respond EXCLUSIVELY in JSON format with: 'correct' (true/false), 'feedback' (string IN DUTCH), 'hint' (string IN DUTCH). " +
                              "Be generous: if the answer seems logical or has the right intent, set correct=true. " +
                              "When in doubt: provide a hint instead of marking it strictly false.";

        string userPrompt = $"Puzzle context: {puzzleContext}\n" +
                            $"Current step: {stageDescription}\n" +
                            $"Player says: '{playerInput}'\n" +
                            $"Is this answer correct for this step?";

        GroqChatRequest chatRequest = new GroqChatRequest
        {
            model = "llama3",
            response_format = new ResponseFormat { type = "json_object" },
            messages = new RequestMessage[]
            {
                new RequestMessage { role = "system", content = systemPrompt },
                new RequestMessage { role = "user", content = userPrompt }
            }
        };

        string jsonPayload = JsonUtility.ToJson(chatRequest);

        using (UnityWebRequest request = new UnityWebRequest(localEndpoint, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + apiKey.Trim());

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                GroqChatResponse response = JsonUtility.FromJson<GroqChatResponse>(request.downloadHandler.text);
                string content = response.choices[0].message.content;

                PuzzleResult result = JsonUtility.FromJson<PuzzleResult>(content);
                Debug.Log($"🧩 Puzzle evaluatie: Correct={result.correct}, Feedback: {result.feedback}");
                onComplete?.Invoke(result);
            }
            else
            {
                Debug.LogError("Error met LLM API voor puzzel: " + request.error);
                onComplete?.Invoke(new PuzzleResult { correct = false, feedback = "API fout, probeer opnieuw", hint = "Controleer je verbinding of lokale server" });
            }
        }
    }

    private IEnumerator SendToLLM(string roomContext, string playerInput, Action<LLMResult> onComplete)
    {
        string availablePrefabs = "air_hockey_001,bathroom_item_001,bed_001,box_001,camera_001,closet_001,closet_002,clothes_001,clothes_002,coffee_machine_001,coffee_table_001,couch_001,door_001,door_frame_001,dresser_001,dish_001,dish_002,drink_001,drink_002,dumbbell_001,dumbbell_002,fridge_001,ketchup_001,kitchen_chair_001,kitchen_sink_001,kitchen_table_001,lamp_001,lamp_002,lounge_chair_001,microwave_oven_001,musical_instrument_001,office_table_001,plant_001,scratching_post_001,training_item_001,training_item_002,toy_001,toy_002,tv_wall_001,washing_machine_001";

        string systemPrompt = "You are a morphing AI in a stealth game. " +
                              "Rules: " +
                              "1) Score how well the object fits in the current location (0-100). Items that belong there=high, out of place=low. " +
                              "Vague/no object: score=0, prefab_name='none'. " +
                              $"2)op Map object to prefab name from list: [{availablePrefabs}]. " +
                              "chair/office chair/seat → lounge_chair_001. server/cabinet → closet_001 or closet_002. box/crate → box_001. " +
                              "3) door_action='none' unless 'open/close the door'. " +
                              "4) unmorph=true if player says 'unmorph/turn me back/revert/change me back/undo'. " +
                              "Output JSON only: {{\"score\":0,\"reason\":\"\",\"prefab_name\":\"\",\"door_action\":\"none\",\"unmorph\":false}}";

        string userPrompt = $"Current location: {roomContext}. The player says: '{playerInput}'";
        Debug.Log($"[LLM] VERSTUURD NAAR OLLAMA: {userPrompt}");

        GroqChatRequest chatRequest = new GroqChatRequest
        {
            model = "llama3",
            response_format = new ResponseFormat { type = "json_object" },
            messages = new RequestMessage[]
            {
                new RequestMessage { role = "system", content = systemPrompt },
                new RequestMessage { role = "user", content = userPrompt }
            }
        };

        string jsonPayload = JsonUtility.ToJson(chatRequest);

        using (UnityWebRequest request = new UnityWebRequest(localEndpoint, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + apiKey.Trim());

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                GroqChatResponse response = JsonUtility.FromJson<GroqChatResponse>(request.downloadHandler.text);
                string content = response.choices[0].message.content;

                LLMResult result = JsonUtility.FromJson<LLMResult>(content);

                Debug.Log($"🤖 LLM Oordeel: Score {result.score} - Reden: {result.reason} - Deur: {result.door_action}");
                onComplete?.Invoke(result);
            }
            else
            {
                Debug.LogError("Error met lokale LLM API: " + request.error);
                LLMResult lokaalResultaat = VoerOfflineFallbackUit(playerInput);
                onComplete?.Invoke(lokaalResultaat); 
            }
        }
    }

    private LLMResult VoerOfflineFallbackUit(string gesprokenTekst)
    {
        Debug.LogWarning("Lokale API offline! Lokale Fail State geactiveerd.");
        
        gesprokenTekst = gesprokenTekst.ToLower();
        LLMResult fallbackResult = new LLMResult(); 
        
        // Nu aangepast voor Engelse spraakherkenning
        if (gesprokenTekst.Contains("unmorph") || gesprokenTekst.Contains("turn me back") || gesprokenTekst.Contains("revert") || gesprokenTekst.Contains("change me back"))
        {
            fallbackResult.prefab_name = "none";
            fallbackResult.score = 0;
            fallbackResult.door_action = "none";
            fallbackResult.unmorph = true;
        }
        else if (gesprokenTekst.Contains("server") || gesprokenTekst.Contains("cabinet"))
        {
            fallbackResult.prefab_name = "closet_001"; 
            fallbackResult.score = 90; 
            fallbackResult.door_action = "none";
        }
        else if (gesprokenTekst.Contains("chair") || gesprokenTekst.Contains("desk"))
        {
            fallbackResult.prefab_name = "lounge_chair_001";
            fallbackResult.score = 80; 
            fallbackResult.door_action = "none";
        }
        else if (gesprokenTekst.Contains("open") && gesprokenTekst.Contains("door"))
        {
            fallbackResult.prefab_name = "none";
            fallbackResult.score = 0; 
            fallbackResult.door_action = "open";
        }
        else
        {
            fallbackResult.prefab_name = "none";
            fallbackResult.score = 0; 
            fallbackResult.door_action = "none";
        }

        fallbackResult.reason = "Offline Fallback Gebruikt";
        return fallbackResult;
    }
}

[Serializable] public class GroqChatRequest { public string model; public ResponseFormat response_format; public RequestMessage[] messages; }
[Serializable] public class ResponseFormat { public string type; }
[Serializable] public class RequestMessage { public string role; public string content; }

[Serializable] public class GroqChatResponse { public Choice[] choices; }
[Serializable] public class Choice { public Message message; }
[Serializable] public class Message { public string content; }
[Serializable] public class LLMResult {
    public int score;
    public string reason;
    public string prefab_name;
    public string door_action;
    public string color_hex;
    public bool unmorph;
}

[Serializable] public class PuzzleResult {
    public bool correct;
    public string feedback;
    public string hint;
}