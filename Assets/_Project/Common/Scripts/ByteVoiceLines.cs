using UnityEngine;

public static class ByteVoiceLines
{
    public static string[] GetRandomTemplate(string category)
    {
        string[] templates = category switch
        {
            "INSPECT_CORRECT" => InspectCorrect,
            "INSPECT_INCORRECT" => InspectIncorrect,
            "CAULDRON_FAIL_T1" => CauldronFailT1,
            "CAULDRON_FAIL_T2" => CauldronFailT2,
            "CAULDRON_SUCCESS" => CauldronSuccess,
            _ => null
        };
        return templates;
    }

    public static string GetRandomFilled(string category, System.Collections.Generic.Dictionary<string, string> replacements)
    {
        string[] templates = GetRandomTemplate(category);
        if (templates == null || templates.Length == 0) return null;

        string template = templates[Random.Range(0, templates.Length)];
        foreach (var kvp in replacements)
        {
            template = template.Replace(kvp.Key, kvp.Value);
        }
        return template;
    }

    public static string GetRandom(string[] array)
    {
        if (array == null || array.Length == 0) return null;
        return array[Random.Range(0, array.Length)];
    }

    public static string[] InspectCorrect = {
        "{\"Hint\": \"{itemName} fits the ritual. It belongs here.\", \"Emotion\": \"Pleased\", \"Additional\": \"None\"}",
        "{\"Hint\": \"{itemName} from {itemBiome} is compatible with the potion.\", \"Emotion\": \"Helpful\", \"Additional\": \"None\"}",
        "{\"Hint\": \"I detect a match. {itemName} is a correct ingredient.\", \"Emotion\": \"Encouraging\", \"Additional\": \"None\"}",
        "{\"Hint\": \"{itemName} aligns with the cauldron's data. Good find.\", \"Emotion\": \"Pleased\", \"Additional\": \"None\"}",
        "{\"Hint\": \"The history of {itemLore} confirms this item is correct.\", \"Emotion\": \"Helpful\", \"Additional\": \"None\"}"
    };

    public static string[] InspectIncorrect = {
        "{\"Hint\": \"{itemName} is unstable. It might harm the ritual.\", \"Emotion\": \"Concerned\", \"Additional\": \"None\"}",
        "{\"Hint\": \"Warning: {itemName} could disrupt the healing brew.\", \"Emotion\": \"Concerned\", \"Additional\": \"None\"}",
        "{\"Hint\": \"My scans suggest {itemName} is not for this ritual.\", \"Emotion\": \"Concerned\", \"Additional\": \"None\"}",
        "{\"Hint\": \"{itemName} carries a risk. Use it with caution.\", \"Emotion\": \"Neutral\", \"Additional\": \"None\"}",
        "{\"Hint\": \"The data for {itemName} shows it is a hazard.\", \"Emotion\": \"Concerned\", \"Additional\": \"None\"}"
    };

    public static string[] CauldronFailT1 = {
        "{\"Hint\": \"{wrongCount} items in the mixture are incorrect.\", \"Emotion\": \"Helpful\", \"Additional\": \"None\"}",
        "{\"Hint\": \"The ritual failed. {wrongCount} ingredients are mismatched.\", \"Emotion\": \"Concerned\", \"Additional\": \"None\"}",
        "{\"Hint\": \"I count {wrongCount} errors in the current brew.\", \"Emotion\": \"Neutral\", \"Additional\": \"None\"}",
        "{\"Hint\": \"{wrongCount} discordant elements detected in the cauldron.\", \"Emotion\": \"Concerned\", \"Additional\": \"None\"}",
        "{\"Hint\": \"Analysis result: {wrongCount} ingredients must be replaced.\", \"Emotion\": \"Helpful\", \"Additional\": \"None\"}"
    };

    public static string[] CauldronFailT2 = {
        "{\"Hint\": \"An item from {wrongBiome} is disrupting the balance.\", \"Emotion\": \"Helpful\", \"Additional\": \"None\"}",
        "{\"Hint\": \"I trace the error to an ingredient from {wrongBiome}.\", \"Emotion\": \"Concerned\", \"Additional\": \"None\"}",
        "{\"Hint\": \"Check your items from {wrongBiome}. One is incorrect.\", \"Emotion\": \"Helpful\", \"Additional\": \"None\"}",
        "{\"Hint\": \"The {wrongBiome} item in the mix is incompatible.\", \"Emotion\": \"Concerned\", \"Additional\": \"None\"}",
        "{\"Hint\": \"My sensors flag a source from the {wrongBiome}.\", \"Emotion\": \"Neutral\", \"Additional\": \"None\"}"
    };

    public static string[] CauldronSuccess = {
        "{\"Hint\": \"Ritual complete. The healing potion is ready.\", \"Emotion\": \"Proud\", \"Additional\": \"None\"}",
        "{\"Hint\": \"All items are aligned. Success confirmed.\", \"Emotion\": \"Pleased\", \"Additional\": \"None\"}",
        "{\"Hint\": \"The cauldron is stable. The cure is finished.\", \"Emotion\": \"Proud\", \"Additional\": \"None\"}",
        "{\"Hint\": \"Perfect balance achieved. The potion is restorative.\", \"Emotion\": \"Encouraging\", \"Additional\": \"None\"}",
        "{\"Hint\": \"My sensors report a successful brew. Well done.\", \"Emotion\": \"Pleased\", \"Additional\": \"None\"}"
    };
}
