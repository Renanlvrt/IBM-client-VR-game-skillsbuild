using System;
using UnityEngine;

[Serializable]
public class QuizData
{
    public string description;
    public QuestionData[] questions;
}

[Serializable]
public class NpcData
{
    public string name;
    public string role;
    public string personality;
    public string[] rules;
    public string voiceId;            // ElevenLabs voice ID (set in world JSON)
    public string systemPrompt;       // Full LLM system prompt
    public string[] dialogueOptions;  // The questions player can ask
    public string[] thinkingMessages; // AI "thinking" status messages
}

[Serializable]
public class QuestionData
{
    public string question;
    public string[] options;     // expected length 4
    public int correctIndex = -1; // 0..3, may be absent in JSON
    public string correct_answer; // alternative: matched against options to derive correctIndex
    public string topic;
    public string learning_goal;
}
