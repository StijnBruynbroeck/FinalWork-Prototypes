using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class HelpPanel : MonoBehaviour
{
    [SerializeField] private Canvas targetCanvas;

    private GameObject panelRoot;
    private GameObject miniHint;
    private bool isVisible = true;

    void Start()
    {
        FindCanvas();
        if (targetCanvas == null) return;
        BuildPanel();
    }

    void FindCanvas()
    {
        GameObject statusPanel = GameObject.Find("StatusPanel");
        if (statusPanel != null)
        {
            targetCanvas = statusPanel.GetComponentInParent<Canvas>();
            if (targetCanvas != null) return;
        }

        Canvas[] all = FindObjectsOfType<Canvas>();
        foreach (var c in all)
        {
            if (c.renderMode == RenderMode.ScreenSpaceOverlay && c.isActiveAndEnabled)
            {
                targetCanvas = c;
                return;
            }
        }

        Debug.LogError("[HelpPanel] Geen Canvas gevonden!");
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame)
            Toggle();
    }

    void BuildPanel()
    {
        Color darkBg = new Color(0.02f, 0.07f, 0.03f, 0.92f);
        Color green = new Color(0, 1f, 0.2549f);
        Color dimGreen = new Color(0.04f, 1f, 0f, 0.6f);

        string content =
            "<color=#00FF41>COMMANDS</color>     [<color=#00FF41>H</color>]\n\n" +
            "<color=#00FF41>MORPH</color>\n" +
            "<color=#FFFFFFCC>  \"turn into chair\"\n" +
            "  \"turn into table\"</color>\n\n" +
            "<color=#00FF41>UNMORPH</color>\n" +
            "<color=#FFFFFFCC>  \"unmorph\"</color>\n\n" +
            "<color=#00FF41>DOOR CODE</color>\n" +
            "<color=#FFFFFFCC>  \"one two three ...\"</color>\n\n" +
            "<color=#00FF41>TERMINAL</color>\n" +
            "<color=#FFFFFFCC>  zeg grid-woorden\n" +
            "  (ACCESS, OVERRIDE, ...)</color>\n\n" +
            "<color=#00FF41>PUZZLE</color>\n" +
            "<color=#FFFFFFCC>  volg scherm-instructies</color>\n\n" +
            "<size=14><color=#00FF4166>[ H ] hide/show</color></size>";

        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/JetBrainsMono-Regular SDF");
        if (font == null)
        {
            TMP_Text existing = FindObjectOfType<TMP_Text>();
            if (existing != null) font = existing.font;
        }
        Debug.Log($"[HelpPanel] font={(font != null ? font.name : "NULL")}");

        panelRoot = new GameObject("HelpPanel");
        panelRoot.transform.SetParent(targetCanvas.transform, false);
        RectTransform rootRt = panelRoot.AddComponent<RectTransform>();
        rootRt.anchorMin = new Vector2(1f, 0.5f);
        rootRt.anchorMax = new Vector2(1f, 0.5f);
        rootRt.pivot = new Vector2(1f, 0.5f);
        rootRt.anchoredPosition = new Vector2(-20f, 60f);
        rootRt.sizeDelta = new Vector2(300f, 380f);
        Image borderImg = panelRoot.AddComponent<Image>();
        borderImg.color = green;

        GameObject innerBg = new GameObject("InnerBg");
        innerBg.transform.SetParent(panelRoot.transform, false);
        RectTransform innerRt = innerBg.AddComponent<RectTransform>();
        innerRt.anchorMin = Vector2.zero;
        innerRt.anchorMax = Vector2.one;
        innerRt.offsetMin = new Vector2(2f, 2f);
        innerRt.offsetMax = new Vector2(-2f, -2f);
        Image innerImg = innerBg.AddComponent<Image>();
        innerImg.color = darkBg;

        if (font != null)
        {
            GameObject textGo = new GameObject("HelpText");
            textGo.transform.SetParent(innerBg.transform, false);
            RectTransform textRt = textGo.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(8f, 6f);
            textRt.offsetMax = new Vector2(-8f, -6f);

            TextMeshProUGUI helpText = textGo.AddComponent<TextMeshProUGUI>();
            helpText.text = content;
            helpText.font = font;
            helpText.fontSize = 15;
            helpText.color = Color.white;
            helpText.alignment = TextAlignmentOptions.TopLeft;
            helpText.raycastTarget = false;
        }

        miniHint = new GameObject("HelpMiniHint");
        miniHint.transform.SetParent(targetCanvas.transform, false);
        RectTransform hintRt = miniHint.AddComponent<RectTransform>();
        hintRt.anchorMin = new Vector2(1f, 1f);
        hintRt.anchorMax = new Vector2(1f, 1f);
        hintRt.pivot = new Vector2(1f, 1f);
        hintRt.anchoredPosition = new Vector2(-10f, -10f);
        hintRt.sizeDelta = new Vector2(110f, 36f);

        if (font != null)
        {
            TextMeshProUGUI hintText = miniHint.AddComponent<TextMeshProUGUI>();
            hintText.text = "[ <color=#00FF41>H</color> ] Help";
            hintText.font = font;
            hintText.fontSize = 18;
            hintText.color = dimGreen;
            hintText.alignment = TextAlignmentOptions.Right;
            hintText.raycastTarget = false;
        }

        miniHint.SetActive(false);
    }

    public void Toggle()
    {
        isVisible = !isVisible;
        if (panelRoot != null) panelRoot.SetActive(isVisible);
        if (miniHint != null) miniHint.SetActive(!isVisible);
    }
}
