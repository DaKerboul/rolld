using UnityEngine;

/// <summary>
/// Global startup settings. Scene geometry and materials are set directly in the Editor.
/// </summary>
public class GameSetup : MonoBehaviour
{
    void Awake()
    {
        Application.runInBackground = true;
        Application.targetFrameRate = 60;
    }
}
