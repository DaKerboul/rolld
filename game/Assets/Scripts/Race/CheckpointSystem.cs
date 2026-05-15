using UnityEngine;
using System.Collections;

/// <summary>
/// Manages race checkpoints and the finish line.
/// Place checkpoint GameObjects in order in the Inspector array.
/// Each checkpoint needs a Collider set to "Is Trigger".
/// The last checkpoint in the array is treated as the finish line.
/// Attach to a persistent manager GameObject in the race scene.
/// </summary>
public class CheckpointSystem : MonoBehaviour
{
    public static CheckpointSystem Instance { get; private set; }

    [Header("Checkpoints (in order — last one = finish line)")]
    public Collider[] checkpoints;

    [Header("Visuals")]
    [Tooltip("Material to apply to active (next) checkpoint")]
    public Material checkpointActiveMaterial;
    [Tooltip("Material to apply to passed checkpoints")]
    public Material checkpointPassedMaterial;
    [Tooltip("Material to apply to finish line")]
    public Material finishLineMaterial;

    private int _localCheckpointIndex = 0;  // how many checkpoints this local player passed
    private Renderer[] _checkpointRenderers;
    private bool _finished = false;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        _checkpointRenderers = new Renderer[checkpoints.Length];
        for (int i = 0; i < checkpoints.Length; i++)
        {
            _checkpointRenderers[i] = checkpoints[i].GetComponent<Renderer>();

            // Tag checkpoints with their index for trigger identification
            checkpoints[i].gameObject.name = $"Checkpoint_{i}";
        }

        // Tell HUD total checkpoints
        GameHUD.TotalCheckpoints = checkpoints.Length;
        UpdateCheckpointVisuals();
    }

    /// <summary>Called by CheckpointTrigger on each checkpoint object.</summary>
    public void OnLocalPlayerHitCheckpoint(int index)
    {
        if (_finished) return;
        // Must hit checkpoints in order
        if (index != _localCheckpointIndex) return;

        _localCheckpointIndex++;
        NetworkManager.Instance?.SendCheckpoint(_localCheckpointIndex);

        Debug.Log($"[Checkpoint] Reached {_localCheckpointIndex}/{checkpoints.Length}");

        // Update HUD
        GameHUD.Instance?.SetCheckpoint(_localCheckpointIndex, checkpoints.Length);

        UpdateCheckpointVisuals();

        if (_localCheckpointIndex >= checkpoints.Length)
        {
            _finished = true;
            Debug.Log("[Checkpoint] FINISH LINE reached!");
            StartCoroutine(FinishFlash());
        }
        else
        {
            StartCoroutine(FlashCheckpoint(index));
        }
    }

    public void ResetForRound()
    {
        _localCheckpointIndex = 0;
        _finished = false;
        UpdateCheckpointVisuals();
    }

    private void UpdateCheckpointVisuals()
    {
        for (int i = 0; i < checkpoints.Length; i++)
        {
            if (_checkpointRenderers[i] == null) continue;

            if (i < _localCheckpointIndex)
            {
                // Passed
                if (checkpointPassedMaterial != null)
                    _checkpointRenderers[i].material = checkpointPassedMaterial;
                _checkpointRenderers[i].enabled = false; // hide passed checkpoints
            }
            else if (i == _localCheckpointIndex)
            {
                // Active (next to hit)
                _checkpointRenderers[i].enabled = true;
                if (i == checkpoints.Length - 1 && finishLineMaterial != null)
                    _checkpointRenderers[i].material = finishLineMaterial;
                else if (checkpointActiveMaterial != null)
                    _checkpointRenderers[i].material = checkpointActiveMaterial;
            }
            else
            {
                // Upcoming (not yet active)
                _checkpointRenderers[i].enabled = true;
            }
        }
    }

    private IEnumerator FlashCheckpoint(int index)
    {
        if (index < 0 || index >= checkpoints.Length) yield break;
        var rend = _checkpointRenderers[index];
        if (rend == null) yield break;

        Color orig = rend.material.HasProperty("_BaseColor")
            ? rend.material.GetColor("_BaseColor")
            : rend.material.color;

        for (int i = 0; i < 3; i++)
        {
            SetRendererColor(rend, new Color(0.3f, 1f, 0.5f));
            yield return new WaitForSeconds(0.08f);
            SetRendererColor(rend, orig);
            yield return new WaitForSeconds(0.08f);
        }
    }

    private IEnumerator FinishFlash()
    {
        float t = 0f;
        while (t < 2f)
        {
            t += Time.deltaTime;
            yield return null;
        }
        // Finish confirmed via network — GameManager/EliminationOverlay handles the "Qualifié!" overlay
    }

    private static void SetRendererColor(Renderer rend, Color c)
    {
        if (rend.material.HasProperty("_BaseColor")) rend.material.SetColor("_BaseColor", c);
        else rend.material.color = c;
    }
}
