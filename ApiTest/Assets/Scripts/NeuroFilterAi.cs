using UnityEngine;
using UnityEngine.AI;

public class NeuroFilterAI : MonoBehaviour
{
    private NavMeshAgent agent;
    private bool isInvestigating = false;

    [Header("Patrol Settings")]
    public Transform[] patrolPoints; // Sleep hier lege GameObjects in als 'waypoints'
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
        // Als hij gewoon aan het patrouilleren is en zijn bestemming heeft bereikt
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

    // DEZE FUNCTIE WORDT LATER DOOR JOUW SPRAAK-SCRIPT AANGEROEPEN
    public void InvestigateAnomaly(Vector3 targetLocation, int plausibilityScore)
    {
        // Als de score laag is, is de actie onlogisch! Tijd om aan te vallen.
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