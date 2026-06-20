using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IntroUI : MonoBehaviour
{
    [SerializeField] private GameObject introPanel;
    [SerializeField] private Image logoImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private Button startButton;

    private Action _onStart;

    private void Awake()
    {
        if (startButton != null)
            startButton.onClick.AddListener(OnStartClicked);
    }

    public void Show(string worldDescription, NpcData npc, Action onStart)
    {
        _onStart = onStart;

        titleText.text = "<b>Welcome to VrBimBumBam</b>";

        bodyText.text =
            "Welcome to the <b>Main Hub</b>! From here you can travel to " +
            "<b>four unique worlds</b>, each with its own quiz and AI guide:\n\n" +
            "  <b>Cyberpunk</b> — AI Ethics\n" +
            "  <b>Ancient</b> — Ethical AI in Hiring\n" +
            "  <b>Pirate</b> — AI Agents\n" +
            "  <b>Fantasy</b> — Text to Speech\n" +
            "\n<b>How it works:</b>\n\n" +
            "1.  Each world has a quiz with <b>multiple-choice questions</b>.\n" +
            "    Read each question carefully and select your answer.\n\n" +
            "2.  If you answer incorrectly, you can:\n" +
            "      <b>Retry</b> — try the same question again\n" +
            "      <b>Ask AI</b> — get a personalised explanation from your guide\n\n" +
            "3.  You have <b>3 retries</b> per question. After 3 wrong\n" +
            "    attempts the correct answer is revealed automatically.\n\n" +
            "4.  You have <b>3 AI assists</b> in total for each quiz.\n" +
            "    Use them wisely!\n\n" +
            "5.  Press <b>Tab</b> to unlock your cursor so you can click buttons.\n" +
            "    Press <b>Tab</b> again to re-lock and look around.\n\n" +
            "<b>Good luck, explorer!</b>";

        introPanel.SetActive(true);

        // Unlock cursor so the player can click the Start button
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnStartClicked()
    {
        introPanel.SetActive(false);

        // Keep cursor unlocked — the quiz screens also need clicks.
        // The player can press Tab to re-lock when they want to look around.

        _onStart?.Invoke();
    }
}
