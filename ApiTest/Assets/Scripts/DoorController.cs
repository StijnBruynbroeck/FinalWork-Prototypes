using UnityEngine;
using System.Collections;

public class DoorController : MonoBehaviour
{
    public string animStateName = "DoorAnimation";
    public float animDuration = 1.667f;
    public bool startLocked = false;

    private Animator animator;
    private Animation legacyAnim;
    private bool isOpen = false;
    private bool isLocked;
    private Coroutine animRoutine;

    void Start()
    {
        isLocked = startLocked;
        if (animator == null)
            animator = GetComponentInParent<Animator>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        legacyAnim = GetComponent<Animation>();
        if (legacyAnim == null)
            legacyAnim = GetComponentInParent<Animation>();
        if (legacyAnim == null)
            legacyAnim = GetComponentInChildren<Animation>();

        if (animator != null)
        {
            Debug.Log($"DoorController: Animator gevonden op '{animator.name}', controller = {animator.runtimeAnimatorController?.name}");
            animator.enabled = false;
        }
        if (legacyAnim != null)
        {
            Debug.Log($"DoorController: Legacy Animation gevonden op '{legacyAnim.gameObject.name}'");
            legacyAnim.playAutomatically = false;
            legacyAnim.Stop();
        }

        if (animator == null && legacyAnim == null)
            Debug.LogWarning("DoorController: GEEN Animator of Animation component gevonden op " + gameObject.name);
    }

    public void OpenDoor()
    {
        if (isOpen) return;
        if (isLocked) { Debug.Log("Deur is vergrendeld!"); return; }
        isOpen = true;

        bool afgespeeld = false;

        if (legacyAnim != null && legacyAnim[animStateName] != null)
        {
            legacyAnim[animStateName].wrapMode = WrapMode.Once;
            legacyAnim[animStateName].speed = 1f;
            legacyAnim[animStateName].time = 0f;
            legacyAnim.Play(animStateName);
            afgespeeld = true;
            Debug.Log("DoorController: Legacy animation afgespeeld");
        }
        else if (animator != null)
        {
            animator.enabled = true;
            animator.speed = 1f;
            animator.Play(animStateName, 0, 0f);
            if (animRoutine != null) StopCoroutine(animRoutine);
            animRoutine = StartCoroutine(StopAnimator());
            afgespeeld = true;
            Debug.Log("DoorController: Animator animation afgespeeld");
        }

        if (!afgespeeld)
        {
            Debug.LogWarning("DoorController: Geen animatie gevonden, draai deur via Transform");
            StartCoroutine(DraaiDeur(Vector3.zero, new Vector3(0, 90, 0), 0.5f));
        }

        Debug.Log("Deur geopend!");
    }

    public void CloseDoor()
    {
        if (!isOpen) return;
        isOpen = false;

        if (legacyAnim != null && legacyAnim[animStateName] != null)
        {
            legacyAnim[animStateName].wrapMode = WrapMode.Once;
            legacyAnim[animStateName].speed = -1f;
            legacyAnim[animStateName].time = legacyAnim[animStateName].length;
            legacyAnim.Play(animStateName);
            Debug.Log("DoorController: Legacy close animation afgespeeld");
        }
        else if (animator != null)
        {
            animator.enabled = true;
            animator.speed = 1f;
            animator.Play(animStateName, 0, 1f);
        }
        else
        {
            StartCoroutine(DraaiDeur(new Vector3(0, 90, 0), Vector3.zero, 0.5f));
        }

        Debug.Log("Deur gesloten!");
    }

    IEnumerator DraaiDeur(Vector3 van, Vector3 naar, float duur)
    {
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / duur;
            transform.localEulerAngles = Vector3.Lerp(van, naar, Mathf.SmoothStep(0, 1, t));
            yield return null;
        }
        transform.localEulerAngles = naar;
    }

    IEnumerator StopAnimator()
    {
        yield return new WaitForSeconds(animDuration);
        if (animator != null)
        {
            animator.speed = 1f;
            animator.enabled = false;
        }
    }

    public void UnlockDoor()
    {
        isLocked = false;
        Debug.Log("Deur ontgrendeld!");
    }

    public void LockDoor()
    {
        isLocked = true;
        Debug.Log("Deur vergrendeld!");
    }

    public bool IsOpen() => isOpen;
    public bool IsLocked() => isLocked;
}
