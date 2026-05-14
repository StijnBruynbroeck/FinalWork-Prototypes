using UnityEngine;

public class EnemyVision : MonoBehaviour
{
    [Header("Vision Settings")]
    [SerializeField] private float viewAngle = 45f;
    [SerializeField] private float viewDistance = 10f;
    [SerializeField] private bool showDebug = true;
    
    [Header("Suspicion Settings")]
    [SerializeField] private float suspicionRate = 10f;
    [SerializeField] private float suspicionDecay = 15f;
    [SerializeField] private float baseSuspicion = 0f;
    
    [Header("References")]
    [SerializeField] private LayerMask obstacleMask = -1; // Everything
    
    private Transform player;
 
    private float currentSuspicionMultiplier = 1f;
    private float currentSuspicion = 0f;
    private int lastLLMScore = 0;
    
    public float CurrentSuspicion => currentSuspicion;
    public bool IsPlayerInSight { get; private set; }
    public Vector3? LastKnownPlayerPosition { get; private set; }
    public Transform Player => player;
    public int LastLLMScore => lastLLMScore;
    public bool IsPlayerWellDisguised => lastLLMScore >= 80;
    
    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            Debug.Log("[EnemyVision] Player found: " + player.name);
        }
        else
        {
            Debug.LogWarning("[EnemyVision] No player found with tag 'Player'!");
        }
        
        currentSuspicion = baseSuspicion;
    }
    
    void Update()
    {
        if (player == null) return;
        
        CheckVision();
        UpdateSuspicion();
    }
    
    private void CheckVision()
    {
        Vector3 direction = player.position - transform.position;
        float distance = direction.magnitude;

        if (distance < 2.0f)
        {
            IsPlayerInSight = true;
            LastKnownPlayerPosition = player.position;
            return;
        }

        float angle = Vector3.Angle(transform.forward, direction);
        bool inCone = angle <= viewAngle * 0.5f && distance <= viewDistance;
        
        if (inCone)
        {
            
            RaycastHit hit;
            
            if (Physics.Linecast(transform.position, player.position, out hit, obstacleMask))
            {
                
                if (hit.transform == player || hit.transform.CompareTag("Player"))
                {
                    IsPlayerInSight = true;
                    LastKnownPlayerPosition = player.position;
                }
                else
                {
                    IsPlayerInSight = false; 
                }
            }
            else
            {
                
                IsPlayerInSight = true; 
                LastKnownPlayerPosition = player.position;
            }
        }
        else
        {
            IsPlayerInSight = false;
        }
        
        if (showDebug)
        {
            Color coneColor = IsPlayerInSight ? Color.red : Color.yellow;
            Vector3 rayDir = direction.normalized;
            Debug.DrawRay(transform.position, rayDir * viewDistance, coneColor);
        }
    }
    
    private void UpdateSuspicion()
    {
        if (IsPlayerInSight)
        {
            currentSuspicion += (suspicionRate * currentSuspicionMultiplier) * Time.deltaTime;
        }
        else
        {
            currentSuspicion -= suspicionDecay * Time.deltaTime;
        }
        
        currentSuspicion = Mathf.Clamp(currentSuspicion, 0f, 100f);
    }
 public void SetLLMScoreMultiplier(int llmScore)
    {
        lastLLMScore = llmScore;
        
        if (llmScore >= 80)
        {
            currentSuspicionMultiplier = 0f;
            Debug.Log($"🤖 Perfecte vermomming (Score {llmScore}). Suspicion balk is BEVROREN (0x).");
        }
        else if (llmScore >= 40)
        {
            currentSuspicionMultiplier = 0.5f;
            Debug.Log($"🤖 Twijfelachtig object (Score {llmScore}). Suspicion stijgt traag (0.5x).");
        }
        else
        {
            currentSuspicionMultiplier = 5f;
            Debug.Log($"🤖 ONLOGISCH! (Score {llmScore}). Suspicion schiet gigantisch snel omhoog (5x)!");
        }
    }
}