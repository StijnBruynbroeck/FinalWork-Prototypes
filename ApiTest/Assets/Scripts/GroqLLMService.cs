using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using System;

public class GroqLLMService : MonoBehaviour
{
    [Header("Groq Settings")]
    public string apiKey = "JOUW_GROQ_API_KEY"; // Vul hier je key weer in!
    private string groqEndpoint = "https://api.groq.com/openai/v1/chat/completions";

    public void EvaluatePlausibility(string roomContext, string playerInput, Action<int, string> onComplete)
    {
        StartCoroutine(SendToLLM(roomContext, playerInput, onComplete));
    }

    private IEnumerator SendToLLM(string roomContext, string playerInput, Action<int, string> onComplete)
    {
        // 1. De Prompts
        string systemPrompt = "Je bent een strenge beveiligings-AI (Neuro-Filter) in een sci-fi serverruimte. " +
                              "De speler probeert te transformeren in een object. " +
                              "Geef een plausibility score van 0 (extreem onlogisch/verdacht) tot 100 (perfect logisch/onzichtbaar). " +
                              "Antwoord UITSLUITEND met een JSON object met twee velden: 'score' (int) en 'reason' (string korte uitleg).";
        
        string userPrompt = $"Huidige locatie: {roomContext}. De speler wil transformeren in: {playerInput}";

        // 2. We bouwen het request nu op de veilige C# manier (geen handmatige string-knutsels meer)
        GroqChatRequest chatRequest = new GroqChatRequest
        {
            model = "llama-3.1-8b-instant",
            response_format = new ResponseFormat { type = "json_object" },
            messages = new RequestMessage[]
            {
                new RequestMessage { role = "system", content = systemPrompt },
                new RequestMessage { role = "user", content = userPrompt }
            }
        };

        // Unity regelt nu volautomatisch alle correcte leestekens, enters en escapes!
        string jsonPayload = JsonUtility.ToJson(chatRequest);

        // 3. Versturen
        using (UnityWebRequest request = new UnityWebRequest(groqEndpoint, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + apiKey.Trim());

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                // We pellen de JSON af om bij de score te komen
                GroqChatResponse response = JsonUtility.FromJson<GroqChatResponse>(request.downloadHandler.text);
                string content = response.choices[0].message.content;
                
                // Unity leest de score en de reden uit de output van de AI
                LLMResult result = JsonUtility.FromJson<LLMResult>(content);
                
                Debug.Log($"🤖 LLM Oordeel: Score {result.score} - Reden: {result.reason}");
                onComplete?.Invoke(result.score, result.reason);
            }
            else
            {
                // Mocht hij nog een keer falen, dan print hij nu ook exact WAT Groq te klagen heeft in de console!
                Debug.LogError("LLM Fout: " + request.error + " | Server detail: " + request.downloadHandler.text);
                onComplete?.Invoke(50, "Error connecting to AI.");
            }
        }
    }
}

// --- REQUEST DATA STRUCTUREN (Om veilig naar Groq te sturen) ---
[Serializable] public class GroqChatRequest { public string model; public ResponseFormat response_format; public RequestMessage[] messages; }
[Serializable] public class ResponseFormat { public string type; }
[Serializable] public class RequestMessage { public string role; public string content; }

// --- RESPONSE DATA STRUCTUREN (Om Groq's antwoord te lezen) ---
[Serializable] public class GroqChatResponse { public Choice[] choices; }
[Serializable] public class Choice { public Message message; }
[Serializable] public class Message { public string content; }
[Serializable] public class LLMResult { public int score; public string reason; }