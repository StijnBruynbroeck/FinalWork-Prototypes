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
public void EvaluatePlausibility(string roomContext, string playerInput, Action<LLMResult> onComplete)
    {
        StartCoroutine(SendToLLM(roomContext, playerInput, onComplete));
    }

    private IEnumerator SendToLLM(string roomContext, string playerInput, Action<LLMResult> onComplete)
    {
        // 1. De verbeterde Prompt
        // Geef hier de namen op van de objecten die je in je ObjectSpawner lijst hebt zitten!
        string availablePrefabs = "air_hockey_001,bathroom_item_001,bed_001,box_001,camera_001,closet_001,coffee_machine_001,coffee_table_001,door_001,dresser_001,plant_001,office_table_001,couch_001,fridge_001,lamp_001,lounge_chair_001,washing_machine_001"; 
string systemPrompt = "Je bent de Neuro-Filter AI in een sci-fi stealth videogame. " +
                      "De speler spreekt in het Nederlands of Engels en wil transformeren in een object. " +
                      "Jouw taak is dubbel: " +
                      "1. Beoordeel of dit object logisch in een retro-serverruimte past (score 0-100). " +
                      $"2. Vertaal het gekozen object naar de dichtstbijzijnde Engelse prefab uit deze exacte lijst: [{availablePrefabs}]. " +
                      "Als de speler iets roept dat totaal niet in de lijst past, vul in: 'none'. " +
                      "Antwoord UITSLUITEND met een JSON object met drie velden: 'score' (int), 'reason' (string), en 'prefab_name' (string, EXACT overgenomen uit de lijst inclusief hoofdletters).";
        string userPrompt = $"Huidige locatie: {roomContext}. De speler zegt: '{playerInput}'";

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
                onComplete?.Invoke(result);
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
[Serializable] public class LLMResult { public int score; 
    public string reason; 
    public string prefab_name;
    public string color_hex; }