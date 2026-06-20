using UnityEngine;

public class IngredientPickup : MonoBehaviour
{
    public IngredientData data;
    public string stationID;
    [Tooltip("How many ingredients can be picked from this station. Default 1.")]
    public int maxPerStation = 1;
}