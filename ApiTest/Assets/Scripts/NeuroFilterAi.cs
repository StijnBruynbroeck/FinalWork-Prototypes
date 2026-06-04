using UnityEngine;
using UnityEngine.AI;

public class NeuroFilterAI : MonoBehaviour
{
    private NavMeshAgent agent;
    private bool isInvestigating = false;

    [Header("Patrol Settings")]
    public Transform[] patrolPoints; 
    private int currentPointIndex = 0;
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (patrolPoints.Length > 0)
        {
            agent.SetDestination(patrolPoints[0].position);
        }
    }

    void Update()
    {
        if (!isInvestigating && !agent.pathPending && agent.remainingDistance < 0.5f)
        {
            GoToNextPoint();
        }
    }

    void GoToNextPoint()
    {
        if (patrolPoints.Length == 0) return;
        
        currentPointIndex = (currentPointIndex + 1) % patrolPoints.Length;
        agent.SetDestination(patrolPoints[currentPointIndex].position);
    }

    public void InvestigateAnomaly(Vector3 targetLocation, int plausibilityScore)
    {
        if (plausibilityScore < 50) 
        {
            isInvestigating = true;
            agent.SetDestination(targetLocation);
            Debug.LogWarning("⚠️ NEURO-FILTER: Corrupte data gedetecteerd! Onderzoek gestart.");
        }
        else
        {
            Debug.Log("Neuro-Filter: Data is logisch. Patrouille wordt hervat.");
        }
    }
}