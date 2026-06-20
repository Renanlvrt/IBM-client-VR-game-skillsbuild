using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class FantasyCauldron : MonoBehaviour
{
    [SerializeField] private FantasyStationConfig[] allStations;
    [SerializeField] private IngredientOrbit ingredientOrbit;
    [SerializeField] private Transform cauldronTarget;
    [SerializeField] private WizardInteract wizard;
    [SerializeField] private FantasyRobotBrain robotBrain;

    [Header("Cinematic Success Animation")]
    public CauldronBrewController brewController;
    public CinematicPotion cinematicPotion;

    [Header("Success")]
    [SerializeField] private GameObject amuletPrefab;
    [SerializeField] private Transform amuletSpawnPoint;
    [SerializeField] private Transform robotTransform;

    [Header("Timing")]
    [SerializeField] private float delayBetweenOrbs = 0.5f;
    [SerializeField] private float mixDuration = 3f;
    [SerializeField] private float resultDelay = 1f;
    [SerializeField] private float delayBetweenRejects = 0.3f;

    [Header("Effects")]
    [SerializeField] private CauldronBrewEffect brewEffect;

    private bool evaluated = false;
    private bool brewing = false;
    private int failureCount = 0;

    private void Update()
    {
        // Debug Test: Press 'K' to force the Win Sequence animation!
        if (Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame)
        {
            Debug.Log("<color=yellow>[FantasyCauldron]</color> 'K' pressed - Forcing SuccessSequence for Testing.");
            StartCoroutine(SuccessSequence());
        }
    }

    public void StartBrewing()
    {
        if (evaluated || brewing) return;

        if (allStations.Length == 0)
        {
            Debug.LogWarning("No stations configured on cauldron!");
            return;
        }

        int collected = IngredientTracker.Instance.TotalCollected;
        int required = 0;
        foreach (FantasyStationConfig station in allStations)
        {
            required += station.correctIngredientIDs.Length;
        }

        if (collected < required)
        {
            Debug.Log($"Need {required} ingredients, only have {collected}");
            return;
        }

        brewing = true;
        StartCoroutine(BrewSequence());
    }

    private IEnumerator BrewSequence()
    {
        Debug.Log("[CAULDRON] BrewSequence START");
        if (ingredientOrbit == null) Debug.LogError("[CAULDRON] ingredientOrbit is NULL!");
        if (cauldronTarget == null) Debug.LogError("[CAULDRON] cauldronTarget is NULL!");

        ingredientOrbit.SendAllOrbsTo(cauldronTarget.position, delayBetweenOrbs);

        int totalOrbs = IngredientTracker.Instance.TotalCollected;
        float totalFlyTime = totalOrbs * delayBetweenOrbs + 1f;
        Debug.Log($"[CAULDRON] Waiting {totalFlyTime}s for {totalOrbs} orbs to fly...");
        yield return new WaitForSeconds(totalFlyTime);

        brewEffect?.StartBrewing();

        Debug.Log($"[CAULDRON] Waiting {mixDuration}s for mix animation...");
        yield return new WaitForSeconds(mixDuration);

        brewEffect?.StopBrewing();

        Debug.Log("[CAULDRON] Evaluating results...");
        if (IngredientTracker.Instance == null) Debug.LogError("[CAULDRON] IngredientTracker.Instance is NULL!");
        CauldronResult result = IngredientTracker.Instance.EvaluateResults(allStations);
        Debug.Log($"[CAULDRON] Result: {result.correct}/{result.total} correct");

        yield return new WaitForSeconds(resultDelay);

        if (result.correct == result.total)
        {
            Debug.Log("[CAULDRON] SUCCESS - starting SuccessSequence");
            StartCoroutine(SuccessSequence());
        }
        else
        {
            Debug.Log($"[CAULDRON] FAIL - {result.total - result.correct} wrong ingredients - starting FailSequence");
            StartCoroutine(FailSequence(result));
        }
    }

    private IEnumerator SuccessSequence()
    {
        evaluated = true;
        brewing = false;

        // --- NEW TRAILER ANIMATION LOGIC ---
        if (cinematicPotion == null) cinematicPotion = FindObjectOfType<CinematicPotion>(true);
        if (brewController == null) brewController = FindObjectOfType<CauldronBrewController>(true);

        if (cinematicPotion != null)
        {
            cinematicPotion.gameObject.SetActive(true);
            cinematicPotion.PlayBrewingAnimation();
        }

        // Wait for it to float up AND finish its wild spin before exploding
        float climaxDelay = 2.5f;
        if (cinematicPotion != null)
        {
            climaxDelay = cinematicPotion.timeGoingUp + cinematicPotion.timeShakingAtTop;
        }
        
        yield return new WaitForSeconds(climaxDelay);

        if (brewController != null)
        {
            brewController.gameObject.SetActive(true);
            brewController.TriggerBoom();
        }
        // -----------------------------------

        if (wizard != null)
        {
            yield return wizard.StartCoroutine(wizard.SayWithVoice(new string[]
            {
                "YES! The potion glows with power!",
                "Every ingredient was perfect!",
                "The dragon will be healed!"
            }));
        }

        yield return new WaitForSeconds(6f);

        if (amuletPrefab != null && amuletSpawnPoint != null)
        {
            yield return StartCoroutine(AmuletSequence());
        }
        else
        {
            FantasyPhaseManager.Instance.AdvancePhase();
        }
    }

    private IEnumerator AmuletSequence()
    {
        GameObject amulet = Instantiate(
            amuletPrefab,
            amuletSpawnPoint.position,
            Quaternion.identity
        );

        // Float up
        Vector3 startPos = amulet.transform.position;
        float floatDuration = 1.5f;
        float elapsed = 0f;

        while (elapsed < floatDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / floatDuration;
            amulet.transform.position = startPos + Vector3.up * t * 1.5f;
            amulet.transform.Rotate(Vector3.up, 180f * Time.deltaTime);
            yield return null;
        }

        yield return new WaitForSeconds(1f);

        // Fly toward robot with arc
        Vector3 flyStart = amulet.transform.position;
        float flyDuration = 0.8f;
        elapsed = 0f;

        while (elapsed < flyDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / flyDuration;
            float smooth = t * t;

            Vector3 pos = Vector3.Lerp(flyStart, robotTransform.position, smooth);
            float arc = 4f * 1f * t * (1f - t);
            pos.y += arc;

            amulet.transform.position = pos;
            amulet.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.3f, smooth);
            yield return null;
        }

        Destroy(amulet);

        yield return new WaitForSeconds(1f);

        if (wizard != null)
        {
            yield return wizard.StartCoroutine(wizard.SayWithVoice(new string[]
            {
                "The amulet is yours!",
                "You have proven yourself, adventurer.",
                "The dragon will remember your kindness."
            }));
        }

        yield return new WaitForSeconds(6f);

        FantasyPhaseManager.Instance.AdvancePhase();
    }

    private IEnumerator FailSequence(CauldronResult result)
    {
        Debug.Log("[CAULDRON] FailSequence START");
        int wrongCount = result.total - result.correct;
        Debug.Log($"[CAULDRON] wrongCount={wrongCount}, total={result.total}, correct={result.correct}");

        Debug.Log($"[CAULDRON] wizard is {(wizard != null ? wizard.name : "NULL")}");
        if (wizard != null)
        {
            failureCount++;
            Debug.Log($"[CAULDRON] Calling wizard.SayFailResult (Iteration: {failureCount})...");
            wizard.SayFailResult(failureCount, wrongCount, result.total);
            Debug.Log("[CAULDRON] wizard.SayFailResult done");
        }

        Debug.Log($"[CAULDRON] robotBrain is {(robotBrain != null ? robotBrain.name : "NULL")}");
        if (robotBrain != null)
        {
            Debug.Log("[CAULDRON] Calling robotBrain.OnCauldronFailed...");
            robotBrain.OnCauldronFailed(result);
            Debug.Log("[CAULDRON] robotBrain.OnCauldronFailed done");
        }

        Debug.Log("[CAULDRON] Waiting 4s before returning items...");
        yield return new WaitForSeconds(4f);

        Debug.Log($"[CAULDRON] Returning items for {allStations.Length} station(s)...");
        Debug.Log($"[CAULDRON] IngredientTracker is {(IngredientTracker.Instance != null ? "OK" : "NULL!")}");
        Debug.Log($"[CAULDRON] ingredientOrbit is {(ingredientOrbit != null ? "OK" : "NULL!")}");

        foreach (FantasyStationConfig station in allStations)
        {
            Debug.Log($"[CAULDRON] Processing station: {station?.stationID ?? "NULL STATION"}");
            List<ChoiceRecord> records = IngredientTracker.Instance.GetAllChoiceRecordsForStation(station.stationID);
            Debug.Log($"[CAULDRON] Station has {records?.Count ?? -1} choice records");
            foreach (ChoiceRecord record in records)
            {
                Debug.Log($"[CAULDRON] Rejecting orb: data={record.data?.displayName ?? "NULL"}, grabbable={(record.grabbable != null ? record.grabbable.name : "NULL")}");
                ingredientOrbit.RejectOrb(
                    record.data,
                    record.grabbable,
                    cauldronTarget.position
                );
            }

            IngredientTracker.Instance.ClearStation(station.stationID);
            Debug.Log($"[CAULDRON] Station {station.stationID} cleared");

            yield return new WaitForSeconds(delayBetweenRejects);
        }

        Debug.Log("[CAULDRON] FailSequence COMPLETE");
        brewing = false;
    }
}