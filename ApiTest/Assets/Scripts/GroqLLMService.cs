using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using System;

public class GroqLLMService : MonoBehaviour
{
    [Header("Ollama Local Settings")]
    public string apiKey = "ollama"; // No real key needed locally
    private string localEndpoint = "http://localhost:11434/v1/chat/completions";

    private static string cachedPrefabList = null;
    private static bool hasWarmedUp = false;

    void Start()
    {
        if (!hasWarmedUp)
            StartCoroutine(WarmUp());
    }

    private IEnumerator WarmUp()
    {
        hasWarmedUp = true;
        string dummyJson = "{\"model\":\"phi3:mini\",\"messages\":[{\"role\":\"user\",\"content\":\"hello\"}],\"max_tokens\":1}";
        byte[] bodyRaw = Encoding.UTF8.GetBytes(dummyJson);

        using (UnityWebRequest request = new UnityWebRequest(localEndpoint, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + apiKey.Trim());
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
                Debug.Log("Ollama warm-up voltooid (model geladen in geheugen)");
            else
                Debug.Log("Ollama warm-up mislukt (model wordt bij eerste request geladen): " + request.error);
        }
    }

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
            model = "phi3:mini",
            response_format = new ResponseFormat { type = "json_object" },
            temperature = 0.0f,
            max_tokens = 100,
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
                onComplete?.Invoke(new PuzzleResult { correct = false, feedback = "API error, try again", hint = "Check your connection or local server" });
            }
        }
    }

    private IEnumerator SendToLLM(string roomContext, string playerInput, Action<LLMResult> onComplete)
    {
        if (cachedPrefabList == null)
        {
            GameObject[] allPrefabs = Resources.LoadAll<GameObject>("Props");
            cachedPrefabList = "";
            foreach (GameObject prefab in allPrefabs)
                cachedPrefabList += prefab.name + ",";
        }
        string availablePrefabs = cachedPrefabList;

        string systemPrompt = "You are a morphing AI in a stealth game. Follow these rules STRICTLY. " +
                              "1) Score 0-100: how well the object fits the current zone. High=belongs, low=out of place. Score is ONLY about zone fit. " +
                              "2) If input is unclear, garbled, or does NOT clearly request a morph → score=0, prefab_name='none'. " +
                              $"3) Only pick from this list: [{availablePrefabs}]. Use these EXACT mappings (no substitutions): " +
                              "'chair'/'seat'/'stool' → Chair01. 'office chair' → OfficeChair. " +
                              "'couch'/'sofa' → Sofa01. 'bed'/'cot' → Bed01. " +
                              "'table'/'desk' → Table01. " +
                              "'closet'/'locker'/'wardrobe' → Closet01. " +
                              "'cabinet'/'cupboard'/'kitchen cabinet' → KitchenCabinet01. " +
                              "'bath'/'tub'/'bathtub' → BathTub01. " +
                              "'cushion'/'pillow' → Cushion01. " +
                              "'drawer'/'chest' → Drawer01. " +
                              "'bench' → Bench. " +
                              "'toilet'/'wc'/'lavatory' → Toilet01. " +
                              "'sink'/'basin'/'washbasin' → WashBasin01. " +
                              "'shower' → Shower01. " +
                              "'vanity'/'bathroom vanity' → BathroomVanity01. " +
                              "'fridge'/'refrigerator'/'freezer' → Refrigerator01. " +
                              "'oven' → Oven01. " +
                              "'stove'/'cooker' → Stove01. " +
                              "'kitchen sink' → KitchenSink01. " +
                              "'microwave' → Microwave01. " +
                              "CRITICAL: 'chair' → Chair01, NEVER Sofa01. 'sofa' → Sofa01, NEVER Chair01. " +
                              "Return exactly one prefab name from the list. If no furniture → prefab_name='none'. " +
                              "4) door_action='none' unless player says open/close door. " +
                              "5) unmorph=true if player says unmorph/turn back/revert/change back/undo/morph back. When unmorph=true, prefab_name MUST be 'none'. " +
                              "Output JSON ONLY: {{\"score\":0,\"reason\":\"\",\"prefab_name\":\"none\",\"door_action\":\"none\",\"unmorph\":false}}";

        string userPrompt = $"Current location: {roomContext}. The player says: '{playerInput}'";
        Debug.Log($"[LLM] VERSTUURD NAAR OLLAMA: {userPrompt}");

        GroqChatRequest chatRequest = new GroqChatRequest
        {
            model = "phi3:mini",
            response_format = new ResponseFormat { type = "json_object" },
            temperature = 0.0f,
            max_tokens = 100,
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
        
        if (gesprokenTekst.Contains("unmorph") || gesprokenTekst.Contains("turn me back") || gesprokenTekst.Contains("revert") || gesprokenTekst.Contains("change me back"))
        {
            fallbackResult.prefab_name = "none";
            fallbackResult.score = 0;
            fallbackResult.door_action = "none";
            fallbackResult.unmorph = true;
        }
        else if (gesprokenTekst.Contains("toilet") || gesprokenTekst.Contains("wc") || gesprokenTekst.Contains("lavatory"))
        {
            fallbackResult.prefab_name = "Toilet01";
            fallbackResult.score = 75;
            fallbackResult.door_action = "none";
        }
        else if (gesprokenTekst.Contains("sink") || gesprokenTekst.Contains("basin") || gesprokenTekst.Contains("washbasin"))
        {
            fallbackResult.prefab_name = "WashBasin01";
            fallbackResult.score = 75;
            fallbackResult.door_action = "none";
        }
        else if (gesprokenTekst.Contains("shower"))
        {
            fallbackResult.prefab_name = "Shower01";
            fallbackResult.score = 70;
            fallbackResult.door_action = "none";
        }
        else if (gesprokenTekst.Contains("vanity"))
        {
            fallbackResult.prefab_name = "BathroomVanity01";
            fallbackResult.score = 70;
            fallbackResult.door_action = "none";
        }
        else if (gesprokenTekst.Contains("fridge") || gesprokenTekst.Contains("refrigerator") || gesprokenTekst.Contains("freezer"))
        {
            fallbackResult.prefab_name = "Refrigerator01";
            fallbackResult.score = 75;
            fallbackResult.door_action = "none";
        }
        else if (gesprokenTekst.Contains("oven"))
        {
            fallbackResult.prefab_name = "Oven01";
            fallbackResult.score = 70;
            fallbackResult.door_action = "none";
        }
        else if (gesprokenTekst.Contains("stove") || gesprokenTekst.Contains("cooker"))
        {
            fallbackResult.prefab_name = "Stove01";
            fallbackResult.score = 70;
            fallbackResult.door_action = "none";
        }
        else if (gesprokenTekst.Contains("kitchen sink"))
        {
            fallbackResult.prefab_name = "KitchenSink01";
            fallbackResult.score = 70;
            fallbackResult.door_action = "none";
        }
        else if (gesprokenTekst.Contains("microwave"))
        {
            fallbackResult.prefab_name = "Microwave01";
            fallbackResult.score = 70;
            fallbackResult.door_action = "none";
        }
        else if (gesprokenTekst.Contains("cabinet") || gesprokenTekst.Contains("cupboard") || gesprokenTekst.Contains("kitchen cabinet"))
        {
            fallbackResult.prefab_name = "KitchenCabinet01";
            fallbackResult.score = 80;
            fallbackResult.door_action = "none";
        }
        else if (gesprokenTekst.Contains("closet") || gesprokenTekst.Contains("locker") || gesprokenTekst.Contains("wardrobe") || gesprokenTekst.Contains("server"))
        {
            fallbackResult.prefab_name = "Closet01";
            fallbackResult.score = 90;
            fallbackResult.door_action = "none";
        }
        else if (gesprokenTekst.Contains("chair") || gesprokenTekst.Contains("seat") || gesprokenTekst.Contains("stool"))
        {
            fallbackResult.prefab_name = "Chair01";
            fallbackResult.score = 80;
            fallbackResult.door_action = "none";
        }
        else if (gesprokenTekst.Contains("desk") || gesprokenTekst.Contains("table"))
        {
            fallbackResult.prefab_name = "Table01";
            fallbackResult.score = 80;
            fallbackResult.door_action = "none";
        }
        else if (gesprokenTekst.Contains("bed") || gesprokenTekst.Contains("cot"))
        {
            fallbackResult.prefab_name = "Bed01";
            fallbackResult.score = 75;
            fallbackResult.door_action = "none";
        }
        else if (gesprokenTekst.Contains("sofa") || gesprokenTekst.Contains("couch"))
        {
            fallbackResult.prefab_name = "Sofa01";
            fallbackResult.score = 80;
            fallbackResult.door_action = "none";
        }
        else if (gesprokenTekst.Contains("bath") || gesprokenTekst.Contains("tub") || gesprokenTekst.Contains("bathtub"))
        {
            fallbackResult.prefab_name = "BathTub01";
            fallbackResult.score = 75;
            fallbackResult.door_action = "none";
        }
        else if (gesprokenTekst.Contains("cushion") || gesprokenTekst.Contains("pillow"))
        {
            fallbackResult.prefab_name = "Cushion01";
            fallbackResult.score = 70;
            fallbackResult.door_action = "none";
        }
        else if (gesprokenTekst.Contains("drawer") || gesprokenTekst.Contains("chest"))
        {
            fallbackResult.prefab_name = "Drawer01";
            fallbackResult.score = 75;
            fallbackResult.door_action = "none";
        }
        else if (gesprokenTekst.Contains("bench"))
        {
            fallbackResult.prefab_name = "Bench";
            fallbackResult.score = 75;
            fallbackResult.door_action = "none";
        }
        else if (gesprokenTekst.Contains("office chair"))
        {
            fallbackResult.prefab_name = "OfficeChair";
            fallbackResult.score = 85;
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

        fallbackResult.reason = "Offline fallback used";
        return fallbackResult;
    }
}

[Serializable] public class GroqChatRequest { public string model; public ResponseFormat response_format; public float temperature; public int max_tokens; public RequestMessage[] messages; }
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