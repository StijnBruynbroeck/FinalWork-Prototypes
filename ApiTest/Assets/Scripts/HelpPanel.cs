using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class HelpPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Canvas targetCanvas;

    private GameObject panelRoot;
    private GameObject miniHint;
    private bool isVisible = true;

    void Start()
    {
        if (targetCanvas == null)
            targetCanvas = FindObjectOfType<Canvas>();

        if (targetCanvas == null) return;

        BuildPanel();
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

        string content =
            "<b><color=#00FF41>COMMANDS</color></b>     [<color=#00FF41>H</color>] ⊖\n\n" +
            "<b>MORPH</b>\n" +
            "  \"turn into chair\"\n" +
            "  \"turn into table\"\n\n" +
            "<b>UNMORPH</b>\n" +
            "  \"unmorph\"\n\n" +
            "<b>DOOR CODE</b>\n" +
            "  \"one two three ...\"\n\n" +
            "<b>TERMINAL</b>\n" +
            "  zeg grid-woorden\n" +
            "  (ACCESS, OVERRIDE, ...)\n\n" +
            "<b>PUZZLE</b>\n" +
            "  volg scherm-instructies\n\n" +
            "<size=16>[ <color=#00FF41>H</color> ] hide/show</size>";

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

        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        TextMeshProUGUI helpText = innerBg.AddComponent<TextMeshProUGUI>();
        helpText.text = content;
        if (font != null) helpText.font = font;
        helpText.fontSize = 15;
        helpText.color = Color.white;
        helpText.alignment = TextAlignmentOptions.TopLeft;
        helpText.raycastTarget = false;
        RectTransform textRt = helpText.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(8f, 6f);
        textRt.offsetMax = new Vector2(-8f, -6f);

        miniHint = new GameObject("HelpMiniHint");
        miniHint.transform.SetParent(targetCanvas.transform, false);
        RectTransform hintRt = miniHint.AddComponent<RectTransform>();
        hintRt.anchorMin = new Vector2(1f, 1f);
        hintRt.anchorMax = new Vector2(1f, 1f);
        hintRt.pivot = new Vector2(1f, 1f);
        hintRt.anchoredPosition = new Vector2(-10f, -10f);
        hintRt.sizeDelta = new Vector2(80f, 30f);

        TextMeshProUGUI hintText = miniHint.AddComponent<TextMeshProUGUI>();
        hintText.text = "[ <color=#00FF41>H</color> ] Help";
        if (font != null) hintText.font = font;
        hintText.fontSize = 15;
        hintText.color = new Color(0.04f, 1f, 0f, 0.6f);
        hintText.alignment = TextAlignmentOptions.Right;
        hintText.raycastTarget = false;

        miniHint.SetActive(false);
    }

    public void Toggle()
    {
        isVisible = !isVisible;
        if (panelRoot != null) panelRoot.SetActive(isVisible);
        if (miniHint != null) miniHint.SetActive(!isVisible);
    }
}
