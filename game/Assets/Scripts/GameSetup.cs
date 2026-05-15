using UnityEngine;

/// <summary>
/// Global game setup applied at startup:
///  - Application.runInBackground (physics continues on ALT-TAB)
///  - Tall invisible arena barriers (prevent ball escape)
///  - Visual enhancements: obstacle colors, floor tint, lighting contrast
/// Attach to a persistent GameObject (e.g. NetworkManager).
/// </summary>
public class GameSetup : MonoBehaviour
{
    [Header("Arena Boundaries")]
    public float arenaHalfSize = 45f;
    public float barrierHeight = 50f;
    public float barrierThickness = 1f;

    [Header("Visuals")]
    public bool enhanceVisuals = true;

    void Awake()
    {
        // --- Keep physics and network running on focus loss ---
        Application.runInBackground = true;
        Application.targetFrameRate = 60;

        // Barriers removed — respawn system handles falls (Y < -10)
    }

    void Start()
    {
        if (enhanceVisuals)
            EnhanceVisuals();
    }

    // --- Barrier creation ---

    private void CreateBarrier(string name, Vector3 position, Vector3 size)
    {
        var go = new GameObject(name);
        go.transform.position = position;
        var col = go.AddComponent<BoxCollider>();
        col.size = size;
        // No Renderer = invisible. Static collider = immovable wall.
    }

    // --- Visual enhancements ---

    private void EnhanceVisuals()
    {
        TintFloor();
        ColorObstacles();
        ColorWallsAndGrids();
        EnhanceLighting();
    }

    private void TintFloor()
    {
        var plane = GameObject.Find("Plane");
        if (plane == null) return;
        var rend = plane.GetComponent<Renderer>();
        if (rend == null) return;

        var mat = new Material(rend.sharedMaterial);
        // Soft blue-gray instead of flat white
        Color floorColor = new Color(0.70f, 0.74f, 0.82f, 1f);
        SetMatColor(mat, floorColor);
        rend.material = mat;
    }

    private void ColorObstacles()
    {
        Color[] palette =
        {
            new Color(0.42f, 0.55f, 0.75f), // Steel blue
            new Color(0.60f, 0.45f, 0.68f), // Muted purple
            new Color(0.48f, 0.68f, 0.55f), // Sage green
            new Color(0.74f, 0.52f, 0.42f), // Warm terracotta
            new Color(0.68f, 0.65f, 0.44f), // Sandy gold
            new Color(0.44f, 0.62f, 0.72f), // Slate teal
        };

        for (int i = 1; i <= 18; i++)
        {
            var obs = GameObject.Find($"Obs_{i}");
            if (obs == null) continue;
            var rend = obs.GetComponent<Renderer>();
            if (rend == null) continue;

            var mat = new Material(rend.sharedMaterial);
            SetMatColor(mat, palette[i % palette.Length]);
            rend.material = mat;
        }
    }

    private void ColorWallsAndGrids()
    {
        Color wallColor = new Color(0.50f, 0.54f, 0.62f);
        foreach (string name in new[] { "Wall_North", "Wall_South", "Wall_East", "Wall_West" })
        {
            var wall = GameObject.Find(name);
            if (wall == null) continue;
            var rend = wall.GetComponent<Renderer>();
            if (rend == null) continue;
            var mat = new Material(rend.sharedMaterial);
            SetMatColor(mat, wallColor);
            rend.material = mat;
        }

        Color gridColor = new Color(0.58f, 0.61f, 0.68f);
        for (int i = 1; i <= 4; i++)
        {
            foreach (string dir in new[] { "NS", "EW" })
            {
                var grid = GameObject.Find($"Grid_{dir}_{i}");
                if (grid == null) continue;
                var rend = grid.GetComponent<Renderer>();
                if (rend == null) continue;
                var mat = new Material(rend.sharedMaterial);
                SetMatColor(mat, gridColor);
                rend.material = mat;
            }
        }
    }

    private void EnhanceLighting()
    {
        // Directional light: warm white, stronger, soft shadows
        var lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (var light in lights)
        {
            if (light.type == LightType.Directional)
            {
                light.color = new Color(1f, 0.96f, 0.90f);          // Warm white
                light.intensity = 1.6f;
                light.shadows = LightShadows.Soft;
                light.shadowStrength = 0.75f;
            }
        }

        // Ambient: cool tint for contrast with warm direct light
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.32f, 0.36f, 0.48f);
    }

    // --- Utility ---

    private static void SetMatColor(Material mat, Color color)
    {
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color"))
            mat.color = color;
    }
}
