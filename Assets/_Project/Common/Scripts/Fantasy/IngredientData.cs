using UnityEngine;

[CreateAssetMenu(fileName = "Ingredient", menuName = "Game/Ingredient Data")]
public class IngredientData : ScriptableObject
{
    public string ingredientID;
    public string displayName;
    public Color orbColor = Color.white;
    public GameObject prefab;
}