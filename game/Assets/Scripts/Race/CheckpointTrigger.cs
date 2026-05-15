using UnityEngine;

/// <summary>
/// Attach to each checkpoint GameObject (which must have a trigger Collider).
/// Set the checkpointIndex in the Inspector to match the checkpoint's position in the sequence.
/// </summary>
public class CheckpointTrigger : MonoBehaviour
{
    [Tooltip("Index in the CheckpointSystem.checkpoints array (0-based)")]
    public int checkpointIndex = 0;

    void OnTriggerEnter(Collider other)
    {
        // Only trigger for the local player (has PlayerController)
        if (other.GetComponent<PlayerController>() == null) return;
        CheckpointSystem.Instance?.OnLocalPlayerHitCheckpoint(checkpointIndex);
    }
}
