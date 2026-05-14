using UnityEngine;
using UnityEngine.UI;

public class SuspicionUI : MonoBehaviour
{
    [SerializeField] private Image suspicionBar;
    [SerializeField] private Text suspicionText;
    
    private EnemyAI enemyAI;
    
    void Start()
    {
        enemyAI = FindObjectOfType<EnemyAI>();
        if (enemyAI == null)
            Debug.LogError("No EnemyAI found in scene!");
    }
    
    void Update()
    {
        if (enemyAI != null)
        {
            float suspicion = enemyAI.CurrentSuspicion;
            float maxSuspicion = enemyAI.DetectionThreshold;
            
            if (suspicionBar != null)
                suspicionBar.fillAmount = maxSuspicion > 0 ? suspicion / maxSuspicion : 0f;
            
            if (suspicionText != null)
                suspicionText.text = "Suspicion: " + Mathf.RoundToInt(suspicion) + "%";
        }
    }
}
