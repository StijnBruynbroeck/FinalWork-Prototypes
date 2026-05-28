using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using System;

public class EnemyAI : MonoBehaviour
{
    public enum EnemyState { Roam, Chase, Investigate }
    
    [Header("State Settings")]
    [SerializeField] private float chaseRange = 30f;
    [SerializeField] private float roamRadius = 20f;
    [SerializeField] [Range(0f, 1f)] private float playerBiasChance = 0.6f;
    
    [Header("Audio Detection")]
    [SerializeField] private float hearingRange = 15f;
    [SerializeField] private float investigateWaitTime = 3f;
    
    [Header("Suspicion Settings")]
    [SerializeField] private float minSuspicion = 5f;
    
    [Header("Suspicion Thresholds")]
    [SerializeField] private float stopThreshold = 30f;
    [SerializeField] private float alertThreshold = 60f;
    [SerializeField] private float detectionThreshold = 100f;
    
    [Header("Suspicion Reaction")]
    [SerializeField] private float suspectSpeedFactor = 0.5f;
    
    [Header("References")]
    [SerializeField] private EnemyVision vision;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Text detectionText;
    [SerializeField] private Renderer enemyRenderer;
    
    [Header("Chase")]
    [SerializeField] private float chaseLostTimeout = 5f;

    [Header("Morph Inspection")]
    [SerializeField] private float inspectDuration = 2f;
    [SerializeField] private float inspectApproachDistance = 4f;
    [SerializeField] private float postInspectCooldown = 4f;
    [SerializeField] private float fleeDistance = 15f;

    [Header("Zone Awareness")]
    [SerializeField] [Range(0f, 10f)] private float outOfZoneSuspicionBonus = 3f;

    [Header("Color Feedback")]
    [SerializeField] private Color safeColor = Color.green;
    [SerializeField] private Color cautionColor = Color.yellow;
    [SerializeField] private Color alertColor = Color.red;
    
    public event Action OnDetected;
    public event Action OnAlert;
    
    public float CurrentSuspicion => vision != null ? vision.CurrentSuspicion : 0f;
    public float DetectionThreshold => detectionThreshold;
    
    private EnemyState currentState = EnemyState.Roam;
    private bool detected = false;
    private float baseSpeed = 3.5f;
    private Vector3 roamDestination;
    private bool hasRoamDestination = false;
    private Vector3 lastDestination;
    private int consecutiveSkips = 0;
    private float roamWaitTime = 0f;
    private Vector3 investigationPoint;
    private float investigateTimer = 0f;
    private float inspectTimer = 0f;
    private float postInspectTimer = 0f;
    private float chaseLostTimer = 0f;
    private Vector3 morphInspectStartPos;
    private bool inspectArrived = false;
    private bool hasInspectedThisMorph = false;
    private Animator animator;
    private ObjectSpawner playerSpawner;
    private ZoneDetectionManager zoneDetection;
    
    void Start()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();
        
        if (agent != null)
            baseSpeed = agent.speed;
        
        agent.speed = GetBaseSpeed();
        agent.stoppingDistance = 1f;

        animator = GetComponent<Animator>();
        
        // Subscribe to audio events
        AudioEventSystem.OnSoundEmitted += HandleAudioEvent;
        
        // Find ObjectSpawner in the scene (not a parent of the player)
        playerSpawner = FindObjectOfType<ObjectSpawner>();
        if (playerSpawner == null)
            Debug.LogWarning("[EnemyAI] Geen ObjectSpawner gevonden in scene");

        zoneDetection = FindObjectOfType<ZoneDetectionManager>();
        if (zoneDetection == null)
            Debug.LogWarning("[EnemyAI] Geen ZoneDetectionManager gevonden in scene");
        
        SetState(EnemyState.Roam);
        GetNewRoamDestination();
    }
    
    void OnDestroy()
    {
        AudioEventSystem.OnSoundEmitted -= HandleAudioEvent;
    }
    
    private void HandleAudioEvent(Vector3 soundPosition, float intensity)
    {
        float distance = Vector3.Distance(transform.position, soundPosition);
        float effectiveRange = hearingRange * intensity;
        
        if (distance <= effectiveRange && currentState != EnemyState.Chase)
        {
            investigationPoint = soundPosition;
            SetState(EnemyState.Investigate);
            Debug.Log($"🔊 Vijand hoort geluid op {soundPosition} (afstand: {distance})");
        }
    }
    
    void Update()
    {
        if (vision == null || agent == null) return;
        
        float currentSuspicion = vision.CurrentSuspicion;
        bool canSeePlayer = vision.IsPlayerInSight;
        
        UpdateColor(currentSuspicion);
        
        if (vision.CurrentSuspicion >= detectionThreshold && !detected)
        {
            detected = true;
            OnDetected?.Invoke();
            if (detectionText != null)
                detectionText.text = "GEDETECTEERD!";
            OnAlert?.Invoke();
            GameOverManager manager = FindObjectOfType<GameOverManager>();
            if (manager != null)
            {
                manager.TriggerGameOver();
            }

            return;
        }
        
        bool playerInRange = vision.Player != null && Vector3.Distance(transform.position, vision.Player.position) <= chaseRange;
        bool playerMorphed = playerSpawner != null && playerSpawner.IsMorphed;
        bool playerWellDisguised = vision.IsPlayerWellDisguised && playerMorphed;

        // Zone compatibility — re-evaluated every frame while player is visible and morphed
        if (canSeePlayer && playerMorphed)
        {
            float zoneFit = EvaluateMorphZoneFit();
            vision.SetZoneFitFactor(zoneFit);
        }
        else
        {
            vision.SetZoneFitFactor(1f);
        }

        // Reset inspect-once flag when player unmorphs (next morph can be inspected)
        if (!playerMorphed)
            hasInspectedThisMorph = false;

        // State selection: well-disguised players never trigger Chase
        if (canSeePlayer && playerMorphed && playerWellDisguised && !hasInspectedThisMorph)
        {
            if (postInspectTimer <= 0f && currentState != EnemyState.Investigate)
            {
                // Go inspect the object
                SetState(EnemyState.Investigate);
                morphInspectStartPos = vision.Player.position;
                inspectArrived = false;
                investigateTimer = 0f;
                inspectTimer = 0f;
            }
            else if (currentState == EnemyState.Chase)
            {
                // Well-disguised but stuck in Chase — reset to Roam
                SetState(EnemyState.Roam);
            }
        }
        else if ((canSeePlayer || playerInRange) && (!playerMorphed || !playerWellDisguised))
        {
            if (currentState != EnemyState.Chase)
                SetState(EnemyState.Chase);
        }
        else if (currentState == EnemyState.Investigate)
        {
            // Blijf in investigate state, wordt later afgehandeld
        }
        else if (currentSuspicion <= 0.01f && currentState == EnemyState.Roam)
        {
            // Already roaming, nothing to do
        }
        
        // Chase-lost timer: give up after losing sight for chaseLostTimeout
        if (currentState == EnemyState.Chase)
        {
            if (!canSeePlayer)
            {
                chaseLostTimer += Time.deltaTime;
                if (chaseLostTimer >= chaseLostTimeout)
                {
                    chaseLostTimer = 0f;
                    SetState(EnemyState.Roam);
                }
            }
            else
            {
                chaseLostTimer = 0f;
            }
        }
        else
        {
            chaseLostTimer = 0f;
        }
        
        // Decrease post-inspect cooldown in any state (not just Roam)
        if (postInspectTimer > 0f)
        {
            postInspectTimer -= Time.deltaTime;
        }
        
        if (currentState == EnemyState.Chase && currentSuspicion > minSuspicion)
        {
            HandleSuspectState(currentSuspicion);
        }
        else if (currentState == EnemyState.Chase && currentSuspicion <= minSuspicion)
        {
            agent.speed = GetBaseSpeed() * 1.2f;
        }

        // Unmorphed player in sight: pump suspicion fast
        if (canSeePlayer && playerSpawner != null && !playerSpawner.IsMorphed && currentState == EnemyState.Chase)
        {
            vision.AddSuspicion(20f * Time.deltaTime);
        }
        
        // Push animation speed parameter
        if (animator != null)
            animator.SetFloat("Speed", agent.velocity.magnitude);
        
        ExecuteState();
    }
    
    private void HandleSuspectState(float suspValue)
    {
        agent.speed = GetBaseSpeed() * suspectSpeedFactor;
        
        if (vision.LastKnownPlayerPosition.HasValue)
        {
            Vector3 targetPos = vision.LastKnownPlayerPosition.Value;
            Vector3 direction = (targetPos - transform.position).normalized;
            Quaternion lookRot = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * 2f);
        }
    }
    
    private void ExecuteState()
    {
        switch (currentState)
        {
            case EnemyState.Roam:
                HandleRoam();
                break;
            case EnemyState.Chase:
                HandleChase();
                break;
            case EnemyState.Investigate:
                HandleInvestigate();
                break;
        }
    }
    
    private void HandleRoam()
    {
        agent.speed = GetBaseSpeed();

        if (roamWaitTime > 0f)
        {
            roamWaitTime -= Time.deltaTime;
            return;
        }
        
        if (!hasRoamDestination)
        {
            GetNewRoamDestination();
        }
        else
        {
            float distToDest = Vector3.Distance(transform.position, roamDestination);
            if (distToDest < 1f)
            {
                hasRoamDestination = false;
                roamWaitTime = 2f;
            }
        }
    }
    
    private void GetNewRoamDestination()
    {
        Vector3 currentPos = transform.position;
        Vector3 playerPos = vision.Player != null ? vision.Player.position : currentPos;
        
        bool biasTowardPlayer = UnityEngine.Random.value < playerBiasChance;
        
        Vector3 targetBase = currentPos;
        
        if (biasTowardPlayer && vision.Player != null)
        {
            Vector3 directionToPlayer = (playerPos - currentPos).normalized;
            float distanceToPlayer = Vector3.Distance(currentPos, playerPos);
            
            float preferredDistance = Mathf.Min(distanceToPlayer * 0.5f, 15f);
            targetBase = playerPos - directionToPlayer * preferredDistance;
            targetBase += new Vector3(UnityEngine.Random.Range(-5f, 5f), 0, UnityEngine.Random.Range(-5f, 5f));
        }
        
        for (int i = 0; i < 5; i++)
        {
            Vector2 randomOffset = UnityEngine.Random.insideUnitCircle.normalized * roamRadius;
            Vector3 newPos = targetBase + new Vector3(randomOffset.x, 0, randomOffset.y);
            
            NavMeshHit hit;
            if (NavMesh.SamplePosition(newPos, out hit, roamRadius, NavMesh.AllAreas))
            {
                if (consecutiveSkips > 0 && Vector3.Distance(hit.position, lastDestination) < 2f)
                {
                    consecutiveSkips++;
                    continue;
                }
                
                roamDestination = hit.position;
                agent.SetDestination(roamDestination);
                hasRoamDestination = true;
                lastDestination = hit.position;
                consecutiveSkips = 0;
                return;
            }
        }
        
        consecutiveSkips++;
        if (consecutiveSkips >= 3)
        {
            consecutiveSkips = 0;
            currentPos = transform.position + UnityEngine.Random.insideUnitSphere * 5f;
            currentPos.y = transform.position.y;
            agent.SetDestination(currentPos);
            hasRoamDestination = true;
            roamDestination = currentPos;
        }
    }
    
    private void HandleChase()
    {
        if (vision.Player != null)
        {
            agent.speed = GetBaseSpeed() * 1.2f;
            agent.SetDestination(vision.Player.position);
        }
    }
    
    private void HandleInvestigate()
    {
        agent.speed = GetBaseSpeed() * 0.8f;
        
        // Check of we een gemorphte speler aan het inspecteren zijn
        bool inspectingMorphedPlayer = playerSpawner != null && playerSpawner.IsMorphed && vision.IsPlayerWellDisguised;

        // Already inspected this morph — treat as normal sound investigation
        if (inspectingMorphedPlayer && hasInspectedThisMorph)
            inspectingMorphedPlayer = false;
        
        if (inspectingMorphedPlayer)
        {
            if (!inspectArrived)
            {
                // Always set destination to player position
                if (vision.Player != null)
                    morphInspectStartPos = vision.Player.position;

                agent.SetDestination(morphInspectStartPos);

                // Simple straight-line distance check: stop when close enough
                if (Vector3.Distance(transform.position, morphInspectStartPos) < inspectApproachDistance)
                {
                    agent.isStopped = true;
                    inspectArrived = true;
                    Debug.Log("🪑 Inspect arrived, stopping");
                }
            }
            else
            {
                // Aangekomen — inspecteer het object
                inspectTimer += Time.deltaTime;

                // Kijk rustig naar de gemorphte speler
                Vector3 lookTarget = vision.Player != null ? vision.Player.position : morphInspectStartPos;
                Vector3 direction = (lookTarget - transform.position).normalized;
                direction.y = 0;
                if (direction != Vector3.zero)
                {
                    Quaternion lookRot = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * 1.5f);
                }

                if (inspectTimer >= inspectDuration)
                {
                    inspectTimer = 0f;
                    inspectArrived = false;
                    hasInspectedThisMorph = true;

                    // Walk away from the player
                    SetState(EnemyState.Roam);
                    Vector3 awayDir = vision.Player != null
                        ? (transform.position - vision.Player.position).normalized
                        : Vector3.forward;
                    awayDir.y = 0;
                    if (awayDir == Vector3.zero) awayDir = Vector3.forward;
                    Vector3 fleePos = transform.position + awayDir * fleeDistance;

                    // Reset agent path fully to break out of stopped state
                    agent.isStopped = false;
                    agent.ResetPath();
                    agent.SetDestination(fleePos);

                    hasRoamDestination = true;
                    roamDestination = fleePos;
                    postInspectTimer = postInspectCooldown;
                    Debug.Log($"🪑 Inspectie voorbij. Vijand loopt weg naar {fleePos}");
                }
            }
        }
        else
        {
            // Normaal onderzoek naar geluid
            if (!agent.hasPath || agent.remainingDistance < 0.5f)
            {
                if (Vector3.Distance(transform.position, investigationPoint) > 1f)
                {
                    agent.SetDestination(investigationPoint);
                }
                else
                {
                    // Aangekomen bij geluidsbron, kijk even rond
                    investigateTimer += Time.deltaTime;
                    
                    // Draai langzaam rond om te zoeken
                    transform.Rotate(0, 60f * Time.deltaTime, 0);
                    
                    if (investigateTimer >= investigateWaitTime)
                    {
                        investigateTimer = 0f;
                        SetState(EnemyState.Roam);
                        Debug.Log("🔍 Onderzoek voltooid, terug naar patrouilleren");
                    }
                }
            }
        }
        
        // Als we onderweg de speler zien (niet gemorpht), switch naar Chase
        if (!inspectingMorphedPlayer && vision.IsPlayerInSight && (playerSpawner == null || !playerSpawner.IsMorphed))
        {
            SetState(EnemyState.Chase);
        }
    }
    
    private void SetRoamAwayFromPlayer()
    {
        Vector3 awayFromPlayer = transform.position;
        if (vision.Player != null)
        {
            Vector3 dirAway = (transform.position - vision.Player.position).normalized;
            if (dirAway == Vector3.zero) dirAway = UnityEngine.Random.onUnitSphere;
            dirAway.y = 0;
            awayFromPlayer = transform.position + dirAway * fleeDistance;
        }

        // MUST call SetState FIRST — it resets hasRoamDestination=false;
        // we'll set it back to true below with the actual flee destination.
        SetState(EnemyState.Roam);

        for (int i = 0; i < 5; i++)
        {
            Vector2 offset = UnityEngine.Random.insideUnitCircle * fleeDistance * 0.5f;
            Vector3 candidate = awayFromPlayer + new Vector3(offset.x, 0, offset.y);
            NavMeshHit hit;
            if (NavMesh.SamplePosition(candidate, out hit, fleeDistance, NavMesh.AllAreas))
            {
                roamDestination = hit.position;
                agent.SetDestination(roamDestination);
                hasRoamDestination = true;
                lastDestination = hit.position;
                consecutiveSkips = 0;
                return;
            }
        }

        // Fallback: just keep moving in the away direction
        agent.SetDestination(awayFromPlayer);
        hasRoamDestination = true;
        roamDestination = awayFromPlayer;
    }

    /// <summary>
    /// Checks whether the player's current morph object fits in the current zone
    /// using zoneType categories (matching what the LLM would judge).
    /// </summary>
    private float EvaluateMorphZoneFit()
    {
        if (playerSpawner == null || !playerSpawner.IsMorphed || string.IsNullOrEmpty(playerSpawner.CurrentPrefabName))
            return 1f;

        if (zoneDetection == null || zoneDetection.CurrentZone == null)
            return 0.5f;

        string morphName = playerSpawner.CurrentPrefabName.ToLower().Replace("_", "").Replace("01", "");
        string zoneType = zoneDetection.CurrentZone.zoneType.ToLower();

        bool fitsInKantoor = morphName.Contains("chair") || morphName.Contains("table") || morphName.Contains("desk")
                           || morphName.Contains("sofa") || morphName.Contains("couch") || morphName.Contains("cushion")
                           || morphName.Contains("bench") || morphName.Contains("drawer") || morphName.Contains("chest")
                           || morphName.Contains("closet") || morphName.Contains("bed");

        bool fitsInServerroom = morphName.Contains("closet") || morphName.Contains("cabinet") || morphName.Contains("locker")
                              || morphName.Contains("drawer") || morphName.Contains("chest") || morphName.Contains("bench");

        if (zoneType.Contains("kantoor") || zoneType.Contains("office"))
            return fitsInKantoor ? 1f : 0.25f;

        if (zoneType.Contains("server"))
            return fitsInServerroom ? 1f : 0.25f;

        return 0.5f;
    }

    private void SetState(EnemyState newState)
    {
        if (currentState != newState)
        {
            currentState = newState;
            
            if (newState == EnemyState.Roam)
            {
                hasRoamDestination = false;
                roamWaitTime = 0f;
            }
        }
    }
    
    private float GetBaseSpeed()
    {
        return baseSpeed > 0 ? baseSpeed : 3.5f;
    }
    
    private void UpdateColor(float suspValue)
    {
        if (enemyRenderer == null) return;
        
        float normalizedSuspicion = Mathf.Clamp01(suspValue / detectionThreshold);
        
        Color targetColor;
        if (suspValue < stopThreshold)
            targetColor = Color.Lerp(safeColor, cautionColor, normalizedSuspicion * 3f);
        else if (suspValue < alertThreshold)
            targetColor = Color.Lerp(cautionColor, alertColor, (suspValue - stopThreshold) / (alertThreshold - stopThreshold));
        else
            targetColor = alertColor;
        
        enemyRenderer.material.color = targetColor;
    }
}