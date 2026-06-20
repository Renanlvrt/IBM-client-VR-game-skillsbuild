using UnityEngine;
using System;

public class IntroManager : MonoBehaviour
{
    public static IntroManager Instance { get; private set; }
    public static bool introComplete { get; private set; } = false;
    public static event Action OnIntroComplete;

    private void Awake()
    {
        Instance = this;
    }

    public static void CompleteIntro()
    {
        introComplete = true;
        OnIntroComplete?.Invoke();
    }
}