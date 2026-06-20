using System;
using UnityEngine;

public class GameState : MonoBehaviour
{
    
    public enum PlayerState { Hoverboard, Station };

    public static bool interactionInputEnabled = true;
    public static Station nearbyStation;
    public static Station currentStation;

    public static event Action<PlayerState> OnStateChanged;

    private static PlayerState _currentState = PlayerState.Hoverboard;
    public static PlayerState currentState
    {
        get => _currentState;
        set
        {
            if (_currentState != value)
            {
                _currentState = value;
                OnStateChanged?.Invoke(_currentState);
            }
        }
    }


}
