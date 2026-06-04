using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

public class DoorAnimationGenerator : EditorWindow
{
    private string savePath = "Assets/Animations/Deur";
    private float openAngle = -90f;
    private float animatieDuur = 1.5f;

    [MenuItem("Tools/Deur Animatie Generator")]
    public static void ToonVenster()
    {
        GetWindow<DoorAnimationGenerator>("Deur Animatie Generator");
    }

    void OnGUI()
    {
        GUILayout.Label("Deur Animatie Generator", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        savePath = EditorGUILayout.TextField("Opslagpad", savePath);
        openAngle = EditorGUILayout.FloatField("Open hoek (graden)", openAngle);
        animatieDuur = EditorGUILayout.FloatField("Animatie duur (sec)", animatieDuur);

        EditorGUILayout.Space();

        if (GUILayout.Button("Genereer Animation Clips", GUILayout.Height(40)))
        {
            GenereerClips();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Genereer + Koppel aan geselecteerde deur"))
        {
            if (Selection.activeGameObject == null)
            {
                EditorUtility.DisplayDialog("Fout", "Selecteer eerst een deur GameObject in de scene.", "OK");
                return;
            }
            GenereerClips();
            KoppelAanGameObject(Selection.activeGameObject);
        }
    }

    void GenereerClips()
    {
        Directory.CreateDirectory(savePath);

        string openClipPath = Path.Combine(savePath, "DeurOpen.anim");
        string closeClipPath = Path.Combine(savePath, "DeurDicht.anim");
        string controllerPath = Path.Combine(savePath, "DeurAnimator.controller");

        // Open clip
        AnimationClip openClip = new AnimationClip();
        openClip.legacy = false;
        openClip.wrapMode = WrapMode.ClampForever;

        AnimationCurve rotCurve = AnimationCurve.EaseInOut(0f, 0f, animatieDuur, openAngle);
        openClip.SetCurve("", typeof(Transform), "localEulerAnglesAngleY", rotCurve);

        AnimationEvent openEvent = new AnimationEvent();
        openEvent.time = animatieDuur;
        openEvent.functionName = "OnDeurGeopend";
        AnimationUtility.SetAnimationEvents(openClip, new AnimationEvent[] { openEvent });

        AssetDatabase.CreateAsset(openClip, openClipPath);

        // Close clip
        AnimationClip closeClip = new AnimationClip();
        closeClip.legacy = false;
        closeClip.wrapMode = WrapMode.ClampForever;

        AnimationCurve closeRotCurve = AnimationCurve.EaseInOut(0f, openAngle, animatieDuur, 0f);
        closeClip.SetCurve("", typeof(Transform), "localEulerAnglesAngleY", closeRotCurve);

        AnimationEvent closeEvent = new AnimationEvent();
        closeEvent.time = animatieDuur;
        closeEvent.functionName = "OnDeurGesloten";
        AnimationUtility.SetAnimationEvents(closeClip, new AnimationEvent[] { closeEvent });

        AssetDatabase.CreateAsset(closeClip, closeClipPath);

        // Animator controller
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

        AnimatorControllerLayer layer = controller.layers[0];
        AnimatorStateMachine stateMachine = layer.stateMachine;

        AnimatorState closedState = stateMachine.AddState("Closed", new Vector3(300, 0, 0));
        closedState.motion = null;
        closedState.writeDefaultValues = false;

        AnimatorState openState = stateMachine.AddState("Open", new Vector3(300, 100, 0));
        openState.motion = openClip;
        openState.writeDefaultValues = false;

        AnimatorState closingState = stateMachine.AddState("Closing", new Vector3(150, 100, 0));
        closingState.motion = closeClip;
        closingState.writeDefaultValues = false;

        // Parameters
        controller.AddParameter("OpenDoor", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("CloseDoor", AnimatorControllerParameterType.Trigger);

        // Transitions
        AnimatorStateTransition closedToOpen = closedState.AddTransition(openState);
        closedToOpen.AddCondition(AnimatorConditionMode.If, 0, "OpenDoor");
        closedToOpen.duration = 0f;
        closedToOpen.hasExitTime = false;

        AnimatorStateTransition openToClosing = openState.AddTransition(closingState);
        openToClosing.AddCondition(AnimatorConditionMode.If, 0, "CloseDoor");
        openToClosing.duration = 0f;
        openToClosing.hasExitTime = false;

        AnimatorStateTransition closingToClosed = closingState.AddTransition(closedState);
        closingToClosed.hasExitTime = true;
        closingToClosed.duration = 0f;
        closingToClosed.exitTime = 1f;

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Animatie gegenereerd!\nOpen: {openClipPath}\nDicht: {closeClipPath}\nController: {controllerPath}");
    }

    void KoppelAanGameObject(GameObject obj)
    {
        string controllerPath = Path.Combine(savePath, "DeurAnimator.controller");
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);

        if (controller == null)
        {
            EditorUtility.DisplayDialog("Fout", "Genereer eerst de animatie!", "OK");
            return;
        }

        Animator animator = obj.GetComponent<Animator>();
        if (animator == null)
            animator = obj.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;

        DoorController door = obj.GetComponent<DoorController>();
        if (door == null)
            door = obj.AddComponent<DoorController>();

        EditorUtility.SetDirty(obj);
        Debug.Log($"Deur animatie gekoppeld aan: {obj.name}");
    }
}
