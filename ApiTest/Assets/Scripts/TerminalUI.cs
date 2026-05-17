using UnityEngine;
using TMPro;
using System.Collections;

public class TerminalUI : MonoBehaviour
{
    private TextMeshProUGUI tekstVeld;

    public void StelTekstVeldIn(TextMeshProUGUI tmp)
    {
        tekstVeld = tmp;
        if (tekstVeld != null)
            tekstVeld.color = wachtKleur;
    }

    [Header("Typewriter Effect")]
    public float charsPerSecond = 25f;

    [Header("Kleuren - Magazine Stijl")]
    public Color normaalKleur = new Color(0.5f, 0.5f, 0.5f);
    public Color systeemKleur = new Color(0, 1, 0.255f);
    public Color foutKleur = new Color(1, 0, 0.255f);
    public Color toegangKleur = new Color(0, 1, 0.255f);
    public Color wachtKleur = new Color(0.4f, 0.4f, 0.4f);

    [Header("Pulsatie")]
    public float pulsatieSnelheid = 1.5f;
    public float pulsatieIntensiteit = 0.2f;

    private Coroutine actieveTypewriter;
    private Coroutine actievePulsatie;
    private bool isToegangVerleend = false;

    public bool IsBezig { get; private set; }

    void Awake()
    {
        ZoekTekstVeld();
    }

    private void ZoekTekstVeld()
    {
        if (tekstVeld != null) return;
        tekstVeld = GetComponent<TextMeshProUGUI>();
        if (tekstVeld != null)
            tekstVeld.color = wachtKleur;
    }

    private bool CheckTekstVeld()
    {
        ZoekTekstVeld();
        return tekstVeld != null;
    }

    public void TypeText(string text, System.Action onComplete = null)
    {
        if (actieveTypewriter != null) StopCoroutine(actieveTypewriter);
        if (actievePulsatie != null) StopCoroutine(actievePulsatie);
        isToegangVerleend = false;
        if (!CheckTekstVeld()) return;
        IsBezig = true;
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
        IsBezig = false;
        onComplete?.Invoke();
    }

    public void ToonTekst(string text, Color? kleur = null)
    {
        if (actieveTypewriter != null) StopCoroutine(actieveTypewriter);
        if (actievePulsatie != null) StopCoroutine(actievePulsatie);
        isToegangVerleend = false;
        IsBezig = false;
        if (!CheckTekstVeld())
        {
            Debug.LogError("[UI] ToonTekst: CheckTekstVeld faalde!");
            return;
        }
        tekstVeld.text = text;
        tekstVeld.color = kleur ?? systeemKleur;
        Debug.Log($"[UI] ToonTekst: text lengte={text.Length}, color={tekstVeld.color}, actief={tekstVeld.gameObject.activeInHierarchy}");
    }

    public void ToonFout(string text)
    {
        if (actieveTypewriter != null) StopCoroutine(actieveTypewriter);
        if (actievePulsatie != null) StopCoroutine(actievePulsatie);
        isToegangVerleend = false;
        IsBezig = false;
        if (!CheckTekstVeld()) return;
        tekstVeld.color = foutKleur;
        tekstVeld.text = text;
        StartCoroutine(TrilEffect());
    }

    public void ToonToegang(string text)
    {
        if (actieveTypewriter != null) StopCoroutine(actieveTypewriter);
        if (actievePulsatie != null) StopCoroutine(actievePulsatie);
        isToegangVerleend = true;
        IsBezig = false;
        if (!CheckTekstVeld()) return;
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
                Mathf.Clamp01(basis.b * t), 1f
            );
            yield return null;
        }
    }

    private IEnumerator TrilEffect()
    {
        if (!CheckTekstVeld()) yield break;
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
        if (CheckTekstVeld())
            tekstVeld.color = normaalKleur;
    }

    void OnDestroy()
    {
        StopAllCoroutines();
    }
}