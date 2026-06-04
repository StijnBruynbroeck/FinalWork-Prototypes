using UnityEngine;
using System.Collections;

public class DoorController : MonoBehaviour
{
    public float openAngle = 90f;
    public float openDuration = 0.5f;
    public bool startLocked = false;
    public Vector3 hingeOffset = Vector3.zero;

    private bool isOpen = false;
    private bool isLocked;
    private Coroutine animRoutine;

    void Start()
    {
        isLocked = startLocked;
    }

    public void OpenDoor()
    {
        if (isOpen) return;
        if (isLocked) { Debug.Log("Deur is vergrendeld!"); return; }
        isOpen = true;
        if (animRoutine != null) StopCoroutine(animRoutine);
        animRoutine = StartCoroutine(Swing(openAngle));
    }

    public void CloseDoor()
    {
        if (!isOpen) return;
        isOpen = false;
        if (animRoutine != null) StopCoroutine(animRoutine);
        animRoutine = StartCoroutine(Swing(-openAngle));
    }

    IEnumerator Swing(float targetAngle)
    {
        float t = 0;
        float duration = openDuration;
        Vector3 hingePoint = transform.TransformPoint(hingeOffset);
        float rotatedSoFar = 0;

        while (t < 1)
        {
            t += Time.deltaTime / duration;
            float smooth = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t));
            float target = targetAngle * smooth;
            float delta = target - rotatedSoFar;
            rotatedSoFar = target;
            transform.RotateAround(hingePoint, Vector3.up, delta);
            yield return null;
        }

        float finalDelta = targetAngle - rotatedSoFar;
        if (Mathf.Abs(finalDelta) > 0.001f)
            transform.RotateAround(hingePoint, Vector3.up, finalDelta);
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
