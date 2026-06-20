using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class RobotIntroController : MonoBehaviour
{
    [Header("Robot")]
    [SerializeField] private Transform robot;

    [Header("Player")]
    [SerializeField] private Transform player;

    [Header("References")]
    [SerializeField] private SpeechBubble speechBubble;
    [SerializeField] private GazeInteractionPrompt gazePrompt;

    [Header("Station")]
    [SerializeField] private Transform stationTarget;
    [SerializeField] private Vector3 robotStationOffset = new Vector3(1.5f, 0f, 0f);

    [Header("Portal")]
    [SerializeField] private Transform portalTarget;
    [SerializeField] private Vector3 robotPortalOffset = new Vector3(1.5f, 0f, 0f);
    [SerializeField] private PortalDoor portalDoor;

    [Header("Input")]
    [SerializeField] private InputActionReference interactAction;

    [Header("Agent Settings")]
    [SerializeField] private float robotSpeed = 10f;
    [SerializeField] private float robotAcceleration = 8f;
    [SerializeField] private float robotAngularSpeed = 120f;

    [Header("Movement Detection")]
    [SerializeField] private float moveDistanceRequired = 1f;

    private RobotAI robotAI;
    private RobotVisual robotVisual;
    private NavMeshAgent robotAgent;
    private PiperSpeakerComponent piperTTS;

    private enum IntroPhase
    {
        WaitingForFirstInteract,
        Greeting,
        TeachingMovement,
        WalkingToStation,
        WaitingForStationEnter,
        StationExplanation,
        WaitingForStationExit,
        WalkingToPortal,
        WaitingAtPortal,
        Complete
    }

    private IntroPhase currentPhase = IntroPhase.WaitingForFirstInteract;
    private bool interactPressed = false;

    private void Awake()
    {
        robotAI = robot.GetComponentInChildren<RobotAI>();
        robotVisual = robot.GetComponentInChildren<RobotVisual>();
        robotAgent = robot.GetComponentInChildren<NavMeshAgent>();
        piperTTS = robot.GetComponentInChildren<PiperSpeakerComponent>();
    }

    private void Start()
    {
        robotAI.enabled = false;

        robotAgent.speed = robotSpeed;
        robotAgent.acceleration = robotAcceleration;
        robotAgent.angularSpeed = robotAngularSpeed;
        if (robotAgent != null && robotAgent.isOnNavMesh)
        {
            robotAgent.isStopped = true;
        }

        if (gazePrompt != null)
        {
            gazePrompt.ShowAction("Interact");
        }
    }

    private void OnEnable()
    {
        if (interactAction != null && interactAction.action != null)
        {
            interactAction.action.Enable();
            interactAction.action.performed += OnInteract;
        }
    }

    private void OnDisable()
    {
        if (interactAction != null && interactAction.action != null)
        {
            interactAction.action.performed -= OnInteract;
        }
    }

    private void OnInteract(InputAction.CallbackContext ctx)
    {
        interactPressed = true;
    }

    private void Update()
    {
        if (!interactPressed) return;
        interactPressed = false;

        switch (currentPhase)
        {
            case IntroPhase.WaitingForFirstInteract:
                gazePrompt.Hide();
                StartCoroutine(GreetingSequence());
                break;
        }
    }

    // ─────────────────────────────────
    // SYNCHRONIZED TTS + BUBBLE
    // ─────────────────────────────────
    private IEnumerator SayWithVoice(string[] lines, System.Action onDone)
    {
        foreach (string line in lines)
        {
            // 1. Explicitly trigger speech
            if (piperTTS != null)
            {
                piperTTS.Speak(line);
            }

            // 2. Display the text in the bubble
            bool lineDone = false;
            speechBubble.Say(new string[] { line }, () => lineDone = true);

            // Wait for reading time (auto-flow)
            float duration = 1.5f + (line.Split(' ').Length * 0.4f);
            yield return new WaitForSeconds(duration);
        }

        onDone?.Invoke();
    }

    // ─────────────────────────────────
    // PHASE 1: Greeting
    // ─────────────────────────────────
    private IEnumerator GreetingSequence()
    {
        currentPhase = IntroPhase.Greeting;

        bool done = false;
        StartCoroutine(SayWithVoice(new string[]
        {
            "Hey there! I'm Granite!",
            "Welcome to this immersive VR learning experience.",
            "Four quizzes are prepared to challenge your knowledge.",
        }, () => done = true));

        yield return new WaitUntil(() => done);

        StartCoroutine(TeachMovement());
    }

    // ─────────────────────────────────
    // PHASE 2: Teach Movement
    // ─────────────────────────────────
    private IEnumerator TeachMovement()
    {
        currentPhase = IntroPhase.TeachingMovement;

        bool done = false;
        StartCoroutine(SayWithVoice(new string[]
        {
            "Let's start with the basics. Try moving around!",
        }, () => done = true));

        yield return new WaitUntil(() => done);

        gazePrompt.ShowAction("Move");

        Vector3 startPos = player.position;
        yield return new WaitUntil(() =>
            Vector3.Distance(player.position, startPos) > moveDistanceRequired
        );

        yield return new WaitForSeconds(1f);

        gazePrompt.Hide();

        done = false;
        StartCoroutine(SayWithVoice(new string[]
        {
            "Good. Now follow me to the back..."
        }, () => done = true));

        yield return new WaitUntil(() => done);

        StartCoroutine(WalkToStation());
    }

    // ─────────────────────────────────
    // PHASE 3: Walk to Station
    // ─────────────────────────────────
    private IEnumerator WalkToStation()
    {
        currentPhase = IntroPhase.WalkingToStation;

        yield return new WaitForSeconds(0.5f);

        Debug.Log($"Agent on NavMesh: {robotAgent.isOnNavMesh}");
        Debug.Log($"Agent position: {robotAgent.transform.position}");
        Debug.Log($"Station target: {stationTarget.position}");
        Debug.Log($"Agent enabled: {robotAgent.enabled}");
        Debug.Log($"Agent isStopped: {robotAgent.isStopped}");

        robotAgent.isStopped = false;
        robotAgent.SetDestination(stationTarget.position);

        yield return null; // Wait one frame for path to calculate

        Debug.Log($"Path pending: {robotAgent.pathPending}");
        Debug.Log($"Has path: {robotAgent.hasPath}");
        Debug.Log($"Path status: {robotAgent.pathStatus}");

        yield return new WaitUntil(() =>
            !robotAgent.pathPending &&
            robotAgent.remainingDistance <= robotAgent.stoppingDistance + 0.5f
        );

        robotAgent.isStopped = true;

        yield return new WaitForSeconds(0.5f);

        currentPhase = IntroPhase.WaitingForStationEnter;

        bool done = false;
        StartCoroutine(SayWithVoice(new string[]
        {
            "Here is a dedicated quiz area!",
            "This is how you will interact with quiz material.",
            "Step inside and give it a try!"
        }, () => done = true));

        yield return new WaitUntil(() => done);

        gazePrompt.ShowAction("Interact");

        yield return new WaitUntil(() => GameState.currentState == GameState.PlayerState.Station);

        gazePrompt.Hide();
        StartCoroutine(StationExplanation());
    }

    // ─────────────────────────────────
    // PHASE 4: Station Explanation
    // ─────────────────────────────────
    private IEnumerator StationExplanation()
    {
        currentPhase = IntroPhase.StationExplanation;

        robotAgent.Warp(stationTarget.position + robotStationOffset);

        yield return new WaitForSeconds(1.5f);

        bool done = false;
        StartCoroutine(SayWithVoice(new string[]
        {
            "Not much else to know...",
            "In the real quizzes, use your intuition to interact with the objects.",
            "Grab, throw, click...",
            "If you are unsure, you can check the user manual. Now let's get moving.",
        }, () => done = true));

        yield return new WaitUntil(() => done);

        // Show hold to exit prompt
        gazePrompt.ShowAction("ExitStation");

        currentPhase = IntroPhase.WaitingForStationExit;

        yield return new WaitUntil(() => GameState.currentState == GameState.PlayerState.Hoverboard);

        gazePrompt.Hide();

        yield return new WaitForSeconds(1f);

        StartCoroutine(WalkToPortal());
    }

    // ─────────────────────────────────
    // PHASE 5: Walk to Portal
    // ─────────────────────────────────
    private IEnumerator WalkToPortal()
    {
        currentPhase = IntroPhase.WalkingToPortal;

        bool done = false;
        StartCoroutine(SayWithVoice(new string[]
        {
        "Now follow me! A wizard needs our help..."
        }, () => done = true));

        yield return new WaitUntil(() => done);

        // Enable the door
        if (portalDoor != null)
        {
            portalDoor.EnableDoor();
        }

        yield return new WaitForSeconds(0.5f);

        // Walk to the offset position, not the portal itself
        Vector3 destination = portalTarget.position + robotPortalOffset;
        robotAgent.isStopped = false;
        robotAgent.SetDestination(destination);

        yield return new WaitUntil(() =>
            !robotAgent.pathPending &&
            robotAgent.remainingDistance <= robotAgent.stoppingDistance + 0.5f
        );

        robotAgent.isStopped = true;

        // No warp needed — robot is already at the offset

        yield return new WaitForSeconds(0.5f);

        currentPhase = IntroPhase.WaitingAtPortal;

        done = false;
        StartCoroutine(SayWithVoice(new string[]
        {
        "Here's the portal!",
        "Walk through when you're ready."
        }, () => done = true));

        yield return new WaitUntil(() => done);

        FinishIntro();
    }

    // ─────────────────────────────────
    // COMPLETE
    // ─────────────────────────────────
    private void FinishIntro()
    {
        currentPhase = IntroPhase.Complete;
        gazePrompt.Hide();
        IntroManager.CompleteIntro();
    }
}