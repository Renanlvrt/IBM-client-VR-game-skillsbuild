using UnityEngine;

public class StationZone : MonoBehaviour
{
    private Station station;

    void Start()
    {
        station = GetComponent<Station>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameState.nearbyStation = station;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // KBM player walks out — seamless exit, no fade
            if (!GameSettings.isVR
                && GameState.currentState == GameState.PlayerState.Station
                && GameState.currentStation == station)
            {
                station.ExitStationSeamless(other.transform);
            }

            GameState.nearbyStation = null;
        }
    }
}