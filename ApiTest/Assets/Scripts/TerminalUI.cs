using UnityEngine;
using TMPro;
using System.Collections;

[RequireComponent(typeof(TextMeshProUGUI))]
public class TerminalUI : MonoBehaviour
{
    private TextMeshProUGUI tekstVeld;

    [Header("Typewriter Effect")]
    public float charsPerSecond = 20f;

    [Header("Kleuren")]
    public Color normaalKleur = new Color(0, 1, 0);
    public Color succesKleur = Color.green;
    public Color foutKleur = Color.red;
    public Color toegangKleur = new Color(0, 1f, 0.5f);

    [Header("Pulsatie")]
    public float pulsatieSnelheid = 2f;
    public float pulsatieIntensiteit = 0.3f;

    private Coroutine actieveTypewriter;
    private Coroutine actievePulsatie;
    private bool isToegangVerleend = false;

    void Awake()
    {
        tekstVeld = GetComponent<TextMeshProUGUI>();
    }

    public void TypeText(string text, System.Action onComplete = null)
    {
        if (actieveTypewriter != null) StopCoroutine(actieveTypewriter);
        if (actievePulsatie != null) StopCoroutine(actievePulsatie);

        isToegangVerleend = false;
        actieveTypewriter = StartCoroutine(TypewriterRoutine(text, onComplete));
    }

    private IEnumerator TypewriterRoutine(string text, System.Action onComplete)
    {
        tekstVeld.text = "";
        float delay = 1f / charsPerSecond;

        foreach (char c in text)
        {
            tekstVeld.text += c;
            yield return new WaitForSeconds(delay);
        }

        onComplete?.Invoke();
    }

    public void ToonSucces(string text)
    {
        if (actieveTypewriter != null) StopCoroutine(actieveTypewriter);

        tekstVeld.color = succesKleur;
        tekstVeld.text = text;
    }

    public void ToonFout(string text)
    {
        if (actieveTypewriter != null) StopCoroutine(actieveTypewriter);
        if (actievePulsatie != null) StopCoroutine(actievePulsatie);

        tekstVeld.color = foutKleur;
        tekstVeld.text = text;

        StartCoroutine(TrilEffect());
    }

    public void ToonToegangVerleend(string text)
    {
        if (actieveTypewriter != null) StopCoroutine(actieveTypewriter);
        if (actievePulsatie != null) StopCoroutine(actievePulsatie);

        isToegangVerleend = true;
        tekstVeld.color = toegangKleur;
        tekstVeld.text = text;

        actievePulsatie = StartCoroutine(PulsatieRoutine());
    }

    private IEnumerator PulsatieRoutine()
    {
        Color basis = toegangKleur;

        while (isToegangVerleend)
        {
            float t = Mathf.Sin(Time.time * pulsatieSnelheid) * pulsatieIntensiteit + 1f;
            tekstVeld.color = new Color(
                Mathf.Clamp01(basis.r * t),
                Mathf.Clamp01(basis.g * t),
                Mathf.Clamp01(basis.b * t),
                1f
            );
            yield return null;
        }
    }

    private IEnumerator TrilEffect()
    {
        Vector3 origineel = tekstVeld.transform.localPosition;
        float duur = 0.4f;
        float timer = 0f;

        while (timer < duur)
        {
            float x = Random.Range(-5f, 5f);
            float y = Random.Range(-2f, 2f);
            tekstVeld.transform.localPosition = origineel + new Vector3(x, y, 0);
            timer += Time.deltaTime;
            yield return null;
        }

        tekstVeld.transform.localPosition = origineel;
    }

    public void StopPulsatie()
    {
        isToegangVerleend = false;
        if (actievePulsatie != null)
        {
            StopCoroutine(actievePulsatie);
            actievePulsatie = null;
        }
        tekstVeld.color = normaalKleur;
    }

    void OnDestroy()
    {
        StopAllCoroutines();
    }
}
