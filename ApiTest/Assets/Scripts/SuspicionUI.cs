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

        SetupBorder();
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

    private void SetupBorder()
    {
        Transform bg = transform.parent.Find("background");
        if (bg == null) return;
        if (transform.parent.Find("SusBorder") != null) return;

        RectTransform bgRt = bg.GetComponent<RectTransform>();
        Image bgImg = bg.GetComponent<Image>();
        if (bgRt == null || bgImg == null) return;

        Sprite origSprite = bgImg.sprite;
        Color origColor = bgImg.color;

        GameObject border = new GameObject("SusBorder");
        border.layer = 5;
        border.transform.SetParent(transform.parent, false);
        RectTransform borderRt = border.AddComponent<RectTransform>();
        borderRt.anchorMin = bgRt.anchorMin;
        borderRt.anchorMax = bgRt.anchorMax;
        borderRt.pivot = bgRt.pivot;
        borderRt.anchoredPosition = bgRt.anchoredPosition;
        borderRt.sizeDelta = bgRt.sizeDelta + new Vector2(4, 4);
        Image borderImg = border.AddComponent<Image>();
        borderImg.sprite = null;
        borderImg.type = Image.Type.Simple;
        borderImg.color = new Color(0, 1f, 0.2549f);

        GameObject fill = new GameObject("SusFill");
        fill.layer = 5;
        fill.transform.SetParent(border.transform, false);
        RectTransform fillRt = fill.AddComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = new Vector2(2f, 2f);
        fillRt.offsetMax = new Vector2(-2f, -2f);
        Image fillImg = fill.AddComponent<Image>();
        fillImg.sprite = origSprite;
        fillImg.type = Image.Type.Simple;
        fillImg.color = origColor;

        border.transform.SetSiblingIndex(bg.GetSiblingIndex());
        Object.Destroy(bg.gameObject);
    }
}
