using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds a compact 80×80 tutorial arena at runtime with connected zones:
///
///   Zone A (South)      — Movement basics + jump gaps
///   Zone B (Center-S)   — Bounce (GelBleu) training
///   Zone C (West)       — Speed (GelOrange) training
///   Zone D (NW)         — Moving platforms
///   Zone E (NE)         — Sticky walls (GelViolet) training
///   Zone F (North)      — Combo challenge (all gels + platforms)
///   Zone G (Center)     — Central tower (final challenge)
///
///   Spawn: (0, 2, -30)  — south edge
///
/// Ball capabilities reference (from PlayerController):
///   Max jump height      ≈ 5.1 units  (JumpForce=10, gravity=9.81)
///   Horizontal jump range ≈ 6-8 units at cruising speed
///   GelBleu bounce       = perfect elasticity (bounciness 1.0, combine=Max)
///   GelOrange speed      = ×3.83 multiplier (~19 u/s)
///   GelViolet sticky     = surface-relative movement, jump = surface normal
///   Ball radius          = 0.5, min platform 2×2 for playability
///
/// Loads PhysicMaterials from Resources/ folder.
/// Attach to a persistent GameObject (e.g. NetworkManager).
/// </summary>
public class ArenaZoneBuilder : MonoBehaviour
{
    private PhysicsMaterial _matGelBleu;
    private PhysicsMaterial _matGelOrange;
    private PhysicsMaterial _matGelViolet;
    private PhysicsMaterial _matBouncy;
    private PhysicsMaterial _matNormal;
    private Material _baseMat;

    private readonly List<MovingPlatform> _movingPlatforms = new List<MovingPlatform>();

    // ── Color palette ──
    private static readonly Color ColFloor     = new Color(0.28f, 0.29f, 0.34f, 1f);
    private static readonly Color ColBleu      = new Color(0.2f, 0.5f, 1f, 0.85f);
    private static readonly Color ColBleuLt    = new Color(0.3f, 0.65f, 1f, 0.9f);
    private static readonly Color ColOrange    = new Color(1f, 0.55f, 0.1f, 0.85f);
    private static readonly Color ColOrangeLt  = new Color(1f, 0.65f, 0.2f, 0.9f);
    private static readonly Color ColViolet    = new Color(0.6f, 0.2f, 0.8f, 0.8f);
    private static readonly Color ColVioletLt  = new Color(0.7f, 0.35f, 0.9f, 0.85f);
    private static readonly Color ColNormal    = new Color(0.42f, 0.43f, 0.50f, 0.9f);
    private static readonly Color ColNormalLt  = new Color(0.52f, 0.53f, 0.60f, 0.95f);
    private static readonly Color ColDark      = new Color(0.22f, 0.22f, 0.28f, 0.95f);
    private static readonly Color ColGold      = new Color(1f, 0.84f, 0f, 0.95f);
    private static readonly Color ColWall      = new Color(0.35f, 0.35f, 0.42f, 0.95f);
    private static readonly Color ColPath      = new Color(0.38f, 0.40f, 0.48f, 0.9f);
    private static readonly Color ColSignBleu  = new Color(0.15f, 0.4f, 0.9f, 0.95f);
    private static readonly Color ColSignOrange= new Color(0.9f, 0.45f, 0.05f, 0.95f);
    private static readonly Color ColSignViolet= new Color(0.5f, 0.15f, 0.7f, 0.95f);
    private static readonly Color ColSignGrey  = new Color(0.5f, 0.5f, 0.55f, 0.95f);
    private static readonly Color ColSignGold  = new Color(0.9f, 0.75f, 0f, 0.95f);
    private static readonly Color ColGuide     = new Color(0.5f, 0.5f, 0.6f, 0.5f);

    // ── Constants ──
    private const float HALF = 40f;         // Arena half-size
    private const float WALL_H = 12f;       // Perimeter wall height
    private const float WALL_T = 1f;        // Perimeter wall thickness

    void Start()
    {
        LoadMaterials();
        if (_baseMat == null) FindBaseMaterial();

        BuildFloorAndWalls();
        BuildPaths();
        BuildZoneA_Movement();
        BuildZoneB_Bounce();
        BuildZoneC_Speed();
        BuildZoneD_MovingPlatforms();
        BuildZoneE_StickyWalls();
        BuildZoneF_Combo();
        BuildZoneG_CentralTower();

        Debug.Log("[ArenaZoneBuilder] 80x80 tutorial arena built successfully.");
    }

    void Update()
    {
        for (int i = 0; i < _movingPlatforms.Count; i++)
        {
            var mp = _movingPlatforms[i];
            if (mp.go == null) continue;
            mp.t += Time.deltaTime * mp.speed;
            float ping = Mathf.PingPong(mp.t, 1f);
            mp.go.transform.position = Vector3.Lerp(mp.posA, mp.posB, ping);
        }
    }

    void OnDestroy()
    {
        _movingPlatforms.Clear();
    }

    // ═══════════════════════════════════════════
    //  FLOOR, WALLS & PATHS
    // ═══════════════════════════════════════════

    private void BuildFloorAndWalls()
    {
        // Main floor
        CreateZone("Arena_Floor", new Vector3(0, -0.25f, 0),
            new Vector3(HALF * 2f, 0.5f, HALF * 2f), _matNormal, ColFloor);

        // Perimeter walls (high to prevent escape)
        float h2 = WALL_H / 2f;
        float full = HALF * 2f + 2f;
        CreateZone("Wall_N", new Vector3(0, h2, HALF),   new Vector3(full, WALL_H, WALL_T), _matNormal, ColWall);
        CreateZone("Wall_S", new Vector3(0, h2, -HALF),  new Vector3(full, WALL_H, WALL_T), _matNormal, ColWall);
        CreateZone("Wall_E", new Vector3(HALF, h2, 0),   new Vector3(WALL_T, WALL_H, full), _matNormal, ColWall);
        CreateZone("Wall_W", new Vector3(-HALF, h2, 0),  new Vector3(WALL_T, WALL_H, full), _matNormal, ColWall);
    }

    /// <summary>Ground-level paths connecting all zones, with directional color markers.</summary>
    private void BuildPaths()
    {
        float pathH = 0.06f;
        float pathY = 0.03f;

        // Spawn -> Zone A (already at spawn)
        // Zone A -> Zone B (south to center-south)
        CreateZone("Path_A_B", new Vector3(0, pathY, -22f), new Vector3(3f, pathH, 10f), _matNormal, ColPath);
        // Zone B -> Zone G center
        CreateZone("Path_B_G", new Vector3(0, pathY, -8f), new Vector3(3f, pathH, 10f), _matNormal, ColPath);
        // Zone G -> Zone C (center to west)
        CreateZone("Path_G_C", new Vector3(-10f, pathY, 0f), new Vector3(12f, pathH, 3f), _matNormal, ColPath);
        // Zone C -> Zone D (west to northwest)
        CreateZone("Path_C_D", new Vector3(-28f, pathY, 12f), new Vector3(3f, pathH, 16f), _matNormal, ColPath);
        // Zone G -> Zone E (center to northeast)
        CreateZone("Path_G_E", new Vector3(12f, pathY, 10f), new Vector3(16f, pathH, 3f), _matNormal, ColPath);
        // Zone D/E -> Zone F (north)
        CreateZone("Path_D_F", new Vector3(-12f, pathY, 28f), new Vector3(16f, pathH, 3f), _matNormal, ColPath);
        CreateZone("Path_E_F", new Vector3(12f, pathY, 28f), new Vector3(16f, pathH, 3f), _matNormal, ColPath);
    }

    // ═══════════════════════════════════════════
    //  ZONE A — MOVEMENT BASICS + JUMP GAPS (South)
    //  Origin: (0, 0, -32)
    // ═══════════════════════════════════════════

    private void BuildZoneA_Movement()
    {
        // Sign
        CreateSign("Sign_A", new Vector3(-4f, 1.5f, -35f), ColSignGrey);

        // Spawn platform (raised)
        CreateZone("A_Spawn", new Vector3(0, 0.4f, -32f), new Vector3(8f, 0.8f, 6f), _matNormal, ColNormalLt);

        // Straight corridor with slight turns to learn rolling
        CreateZone("A_Corr1", new Vector3(0, 0.15f, -27f), new Vector3(4f, 0.3f, 5f), _matNormal, ColNormal);
        CreateZone("A_Corr2", new Vector3(3f, 0.15f, -23f), new Vector3(4f, 0.3f, 4f), _matNormal, ColNormal);
        CreateZone("A_Corr3", new Vector3(0f, 0.15f, -19.5f), new Vector3(4f, 0.3f, 4f), _matNormal, ColNormal);

        // Low guide walls for the corridor
        CreateZone("A_GuideL1", new Vector3(-2.5f, 0.6f, -27f), new Vector3(0.3f, 0.9f, 5f), _matNormal, ColGuide);
        CreateZone("A_GuideR1", new Vector3(2.5f, 0.6f, -27f), new Vector3(0.3f, 0.9f, 5f), _matNormal, ColGuide);

        // Jump gaps: 3 gaps of increasing difficulty
        // Gap 1: 2 units gap
        CreateZone("A_Plat1", new Vector3(-3f, 0.15f, -16f), new Vector3(4f, 0.3f, 4f), _matNormal, ColNormalLt);
        // (gap of 2 units)
        CreateZone("A_Plat2", new Vector3(-3f, 0.15f, -10f), new Vector3(4f, 0.3f, 4f), _matNormal, ColNormalLt);

        // Gap 2: 3 units gap
        // (gap of 3 units)
        CreateZone("A_Plat3", new Vector3(-3f, 0.15f, -3f), new Vector3(4f, 0.3f, 4f), _matNormal, ColNormalLt);

        // Gap 3: 4.5 units gap (needs charged jump)
        // (gap of 4.5 units)
        CreateZone("A_Plat4", new Vector3(-3f, 0.15f, 5f), new Vector3(4f, 0.3f, 4f), _matNormal, ColNormalLt);

        // Return ramp to ground
        CreateRamp("A_Ramp", new Vector3(-3f, 1.0f, 9f), new Vector3(4f, 0.3f, 5f),
            -12f, Vector3.right, _matNormal, ColNormal);

        Debug.Log("[ArenaZoneBuilder] Zone A (Movement) built.");
    }

    // ═══════════════════════════════════════════
    //  ZONE B — BOUNCE TRAINING (Center-South)
    //  Origin: (10, 0, -15)
    // ═══════════════════════════════════════════

    private void BuildZoneB_Bounce()
    {
        Vector3 o = new Vector3(10f, 0f, -15f);

        // Sign
        CreateSign("Sign_B", o + new Vector3(-3f, 1.5f, -3f), ColSignBleu);

        // Entry pad
        CreateZone("B_Entry", o + new Vector3(0, 0.15f, 0), new Vector3(5f, 0.3f, 5f), _matNormal, ColNormalLt);

        // Bounce pads with targets at increasing heights
        // Pad 1 (small) -> target at 3m
        CreateZone("B_Pad1", o + new Vector3(0, 0.2f, 5f), new Vector3(3f, 0.25f, 3f), _matGelBleu, ColBleu);
        CreateZone("B_Tgt1", o + new Vector3(0, 3f, 9f), new Vector3(4f, 0.4f, 4f), _matNormal, ColNormalLt);

        // Pad 2 (medium) -> target at 4.5m
        CreateZone("B_Pad2", o + new Vector3(0, 3.2f, 9f), new Vector3(3.5f, 0.25f, 3.5f), _matGelBleu, ColBleuLt);
        CreateZone("B_Tgt2", o + new Vector3(4f, 5.5f, 12f), new Vector3(4f, 0.4f, 4f), _matNormal, ColNormalLt);

        // Pad 3 on target 2 -> top at 8m
        CreateZone("B_Pad3", o + new Vector3(4f, 5.7f, 12f), new Vector3(3f, 0.2f, 3f), _matGelBleu, ColBleu);
        CreateZone("B_Tgt3", o + new Vector3(0, 8f, 15f), new Vector3(5f, 0.4f, 5f), _matNormal, ColNormalLt);

        // Bounce staircase: fall from 8m -> bounce up further
        CreateZone("B_Stair_Bnc", o + new Vector3(-5f, 0.2f, 15f), new Vector3(4f, 0.25f, 4f), _matGelBleu, ColBleu);
        CreateZone("B_Stair_Mid", o + new Vector3(-5f, 5f, 19f), new Vector3(4f, 0.4f, 4f), _matNormal, ColNormalLt);
        CreateZone("B_Stair_Top", o + new Vector3(-5f, 8.5f, 23f), new Vector3(5f, 0.4f, 4f), _matNormal, ColNormalLt);

        // Return to ground via gentle ramp
        CreateRamp("B_Return", o + new Vector3(-5f, 4f, 27f), new Vector3(4f, 0.3f, 8f),
            -20f, Vector3.right, _matNormal, ColNormal);

        Debug.Log("[ArenaZoneBuilder] Zone B (Bounce) built.");
    }

    // ═══════════════════════════════════════════
    //  ZONE C — SPEED TRAINING (West)
    //  Origin: (-25, 0, -5)
    // ═══════════════════════════════════════════

    private void BuildZoneC_Speed()
    {
        Vector3 o = new Vector3(-25f, 0f, -5f);

        // Sign
        CreateSign("Sign_C", o + new Vector3(8f, 1.5f, -2f), ColSignOrange);

        // Entry
        CreateZone("C_Entry", o + new Vector3(0, 0.15f, 0), new Vector3(5f, 0.3f, 5f), _matNormal, ColNormalLt);

        // Speed strip 1 -> short, straight, with ramp launch
        CreateZone("C_Strip1", o + new Vector3(0, 0.12f, 5f), new Vector3(4f, 0.15f, 8f), _matGelOrange, ColOrange);
        // Guide walls
        CreateZone("C_GuideL1", new Vector3(o.x - 2.5f, 0.5f, o.z + 5f), new Vector3(0.3f, 1f, 8f), _matNormal, ColGuide);
        CreateZone("C_GuideR1", new Vector3(o.x + 2.5f, 0.5f, o.z + 5f), new Vector3(0.3f, 1f, 8f), _matNormal, ColGuide);

        // Ramp at end of strip 1
        CreateRamp("C_Ramp1", o + new Vector3(0, 0.8f, 10.5f), new Vector3(4f, 0.25f, 4f),
            20f, Vector3.right, _matGelOrange, ColOrangeLt);

        // Landing platform (12m ahead, 2m up)
        CreateZone("C_Land1", o + new Vector3(0, 2f, 18f), new Vector3(5f, 0.4f, 5f), _matNormal, ColNormalLt);

        // Speed strip 2 -> longer with a curve
        CreateZone("C_Strip2a", o + new Vector3(0, 2.1f, 22f), new Vector3(4f, 0.15f, 5f), _matGelOrange, ColOrange);
        CreateZone("C_Strip2b", o + new Vector3(4f, 2.1f, 26f), new Vector3(5f, 0.15f, 4f), _matGelOrange, ColOrangeLt);
        CreateZone("C_Strip2c", o + new Vector3(8f, 2.1f, 30f), new Vector3(4f, 0.15f, 5f), _matGelOrange, ColOrange);
        // Guide walls for curve
        CreateZone("C_GuideO", o + new Vector3(-2.5f, 2.6f, 22f), new Vector3(0.3f, 1f, 5f), _matNormal, ColGuide);
        CreateZone("C_GuideI", o + new Vector3(10.5f, 2.6f, 30f), new Vector3(0.3f, 1f, 5f), _matNormal, ColGuide);

        // Final landing with another ramp
        CreateRamp("C_Ramp2", o + new Vector3(8f, 3f, 34f), new Vector3(4f, 0.25f, 4f),
            25f, Vector3.right, _matGelOrange, ColOrangeLt);
        CreateZone("C_Land2", o + new Vector3(8f, 4.5f, 38f), new Vector3(6f, 0.4f, 5f), _matNormal, ColNormalLt);

        Debug.Log("[ArenaZoneBuilder] Zone C (Speed) built.");
    }

    // ═══════════════════════════════════════════
    //  ZONE D — MOVING PLATFORMS (Northwest)
    //  Origin: (-28, 0, 18)
    // ═══════════════════════════════════════════

    private void BuildZoneD_MovingPlatforms()
    {
        Vector3 o = new Vector3(-28f, 0f, 18f);

        // Sign
        CreateSign("Sign_D", o + new Vector3(4f, 1.5f, -2f), ColSignGrey);

        // Entry platform
        CreateZone("D_Entry", o + new Vector3(0, 0.2f, 0), new Vector3(6f, 0.4f, 6f), _matNormal, ColNormalLt);

        // 3 horizontal sliders (oscillate along X, spaced along Z, rising)
        for (int i = 0; i < 3; i++)
        {
            Vector3 a = o + new Vector3(-2f, 1.5f + i * 2f, 5f + i * 6f);
            Vector3 b = a + new Vector3(8f, 0f, 0f);
            var go = CreateZone("D_Slide" + i, a, new Vector3(4f, 0.5f, 4f), _matNormal, ColDark);
            AddMovingPlatform(go, a, b, 0.3f + i * 0.1f);
        }

        // Mid-platform
        CreateZone("D_Mid", o + new Vector3(2f, 7f, 20f), new Vector3(5f, 0.4f, 5f), _matNormal, ColNormalLt);

        // 2 vertical lifts
        for (int i = 0; i < 2; i++)
        {
            Vector3 a = o + new Vector3(-4f + i * 8f, 7.5f, 24f + i * 5f);
            Vector3 b = a + new Vector3(0f, 5f, 0f);
            var go = CreateZone("D_Lift" + i, a, new Vector3(4f, 0.5f, 4f), _matNormal, ColDark);
            AddMovingPlatform(go, a, b, 0.22f + i * 0.1f);
        }

        // End platform (high, with view)
        CreateZone("D_End", o + new Vector3(2f, 12f, 32f), new Vector3(6f, 0.4f, 6f), _matNormal, ColNormalLt);

        // Bouncy descent pad at ground level
        CreateZone("D_Desc", o + new Vector3(2f, 0.2f, 32f), new Vector3(4f, 0.25f, 4f), _matGelBleu, ColBleu);

        Debug.Log("[ArenaZoneBuilder] Zone D (Moving Platforms) built.");
    }

    // ═══════════════════════════════════════════
    //  ZONE E — STICKY WALLS (Northeast)
    //  Origin: (24, 0, 15)
    // ═══════════════════════════════════════════

    private void BuildZoneE_StickyWalls()
    {
        Vector3 o = new Vector3(24f, 0f, 15f);

        // Sign
        CreateSign("Sign_E", o + new Vector3(-5f, 1.5f, -2f), ColSignViolet);

        // Entry pad
        CreateZone("E_Entry", o + new Vector3(0, 0.15f, 0), new Vector3(5f, 0.3f, 5f), _matNormal, ColNormalLt);

        // === Intro wall: simple vertical climb (4m high) ===
        // Wall face pointing -X (ball approaches from the left)
        CreateZone("E_Wall1", o + new Vector3(3f, 3f, 3f), new Vector3(0.5f, 6f, 5f), _matGelViolet, ColViolet);
        // Platform at top of wall
        CreateZone("E_Top1", o + new Vector3(3f, 6.2f, 3f), new Vector3(4f, 0.4f, 5f), _matNormal, ColNormalLt);

        // === L-shaped sticky: wall -> turn -> wall ===
        // Vertical wall going up (face -Z)
        CreateZone("E_Wall2a", o + new Vector3(0, 3.5f, 8f), new Vector3(4f, 7f, 0.5f), _matGelViolet, ColViolet);
        // Ceiling connecting to second wall (face down, -Y)
        CreateZone("E_Ceil", o + new Vector3(0, 7.1f, 10f), new Vector3(4f, 0.5f, 4f), _matGelViolet, ColVioletLt);
        // Second wall going down (face +Z)
        CreateZone("E_Wall2b", o + new Vector3(0, 3.5f, 12f), new Vector3(4f, 7f, 0.5f), _matGelViolet, ColViolet);
        // Landing after L traverse
        CreateZone("E_Land2", o + new Vector3(0, 0.2f, 14f), new Vector3(5f, 0.4f, 4f), _matNormal, ColNormalLt);

        // === Vertical tunnel: two sticky walls face-to-face, zigzag up ===
        // Left wall
        CreateZone("E_Tun_L", o + new Vector3(-3.5f, 6f, 18f), new Vector3(0.5f, 12f, 4f), _matGelViolet, ColViolet);
        // Right wall
        CreateZone("E_Tun_R", o + new Vector3(3.5f, 6f, 18f), new Vector3(0.5f, 12f, 4f), _matGelViolet, ColVioletLt);
        // Small ledges alternating sides to help zigzag
        for (int i = 0; i < 4; i++)
        {
            float side = (i % 2 == 0) ? -2.5f : 2.5f;
            float h = 2f + i * 2.8f;
            CreateZone("E_Tun_Ledge" + i, o + new Vector3(side, h, 18f),
                new Vector3(2f, 0.3f, 3f), _matNormal, ColNormalLt);
        }
        // Top of tunnel
        CreateZone("E_TunTop", o + new Vector3(0, 12.2f, 18f), new Vector3(5f, 0.4f, 5f), _matNormal, ColNormalLt);

        Debug.Log("[ArenaZoneBuilder] Zone E (Sticky Walls) built.");
    }

    // ═══════════════════════════════════════════
    //  ZONE F — COMBO CHALLENGE (North)
    //  Origin: (0, 0, 30)
    // ═══════════════════════════════════════════

    private void BuildZoneF_Combo()
    {
        Vector3 o = new Vector3(0f, 0f, 30f);

        // Sign
        CreateSign("Sign_F", o + new Vector3(-5f, 1.5f, -4f), ColSignGold);

        // Entry
        CreateZone("F_Entry", o + new Vector3(0, 0.15f, 0), new Vector3(5f, 0.3f, 5f), _matNormal, ColNormalLt);

        // Step 1: Speed strip launch
        CreateZone("F_Speed", o + new Vector3(0, 0.12f, 4f), new Vector3(3f, 0.15f, 6f), _matGelOrange, ColOrange);
        CreateZone("F_GuidL", o + new Vector3(-2f, 0.5f, 4f), new Vector3(0.3f, 0.7f, 6f), _matNormal, ColGuide);
        CreateZone("F_GuidR", o + new Vector3(2f, 0.5f, 4f), new Vector3(0.3f, 0.7f, 6f), _matNormal, ColGuide);

        // Step 2: Ramp -> bounce pad
        CreateRamp("F_Ramp", o + new Vector3(0, 0.6f, 8.5f), new Vector3(3f, 0.25f, 3f),
            22f, Vector3.right, _matGelOrange, ColOrangeLt);
        CreateZone("F_BncPad", o + new Vector3(0, 0.2f, 13f), new Vector3(4f, 0.25f, 4f), _matGelBleu, ColBleu);

        // Step 3: High platform with sticky wall leading higher
        CreateZone("F_MidPlat", o + new Vector3(0, 4.5f, 17f), new Vector3(5f, 0.4f, 4f), _matNormal, ColNormalLt);
        CreateZone("F_StickyWall", o + new Vector3(-3f, 7f, 17f), new Vector3(0.5f, 5f, 4f), _matGelViolet, ColViolet);

        // Step 4: Moving platform to final
        Vector3 mpA = o + new Vector3(0, 9.5f, 17f);
        Vector3 mpB = o + new Vector3(0, 9.5f, 23f);
        var mp = CreateZone("F_MovPlat", mpA, new Vector3(4f, 0.5f, 4f), _matNormal, ColDark);
        AddMovingPlatform(mp, mpA, mpB, 0.25f);

        // Step 5: Gold finish platform
        CreateZone("F_Finish", o + new Vector3(0, 10f, 27f), new Vector3(6f, 0.5f, 5f), _matNormal, ColGold);
        CreateZone("F_FinBnc", o + new Vector3(0, 10.3f, 27f), new Vector3(3f, 0.2f, 3f), _matGelBleu, ColBleuLt);

        Debug.Log("[ArenaZoneBuilder] Zone F (Combo) built.");
    }

    // ═══════════════════════════════════════════
    //  ZONE G — CENTRAL TOWER (Center)
    //  Origin: (0, 0, 5)
    // ═══════════════════════════════════════════

    private void BuildZoneG_CentralTower()
    {
        Vector3 o = new Vector3(0f, 0f, 5f);

        // Base (accessible from ground)
        CreateZone("G_Base", o + new Vector3(0, 0.25f, 0), new Vector3(10f, 0.5f, 10f), _matNormal, ColNormal);

        // Access ramp from south
        CreateRamp("G_Ramp", o + new Vector3(0, 0.8f, -6f), new Vector3(4f, 0.3f, 5f),
            -10f, Vector3.right, _matNormal, ColNormal);

        // Level 1: Bounce pad -> L2
        CreateZone("G_L1_Bnc", o + new Vector3(0, 0.5f, 0), new Vector3(3.5f, 0.25f, 3.5f), _matGelBleu, ColBleu);

        // Level 2 (3.5m): platform + speed strip
        CreateZone("G_L2", o + new Vector3(0, 3.5f, 0), new Vector3(8f, 0.4f, 8f), _matNormal, ColNormalLt);
        CreateZone("G_L2_Spd", o + new Vector3(0, 3.7f, 0), new Vector3(6f, 0.15f, 2f), _matGelOrange, ColOrange);
        CreateRamp("G_L2_Ramp", o + new Vector3(3f, 4.5f, 3f), new Vector3(3f, 0.25f, 3f),
            25f, Vector3.right, _matGelOrange, ColOrangeLt);

        // Level 3 (7m): platform + moving platform to L4
        CreateZone("G_L3", o + new Vector3(0, 7f, 0), new Vector3(7f, 0.4f, 7f), _matNormal, ColNormalLt);
        CreateZone("G_L3_Bnc", o + new Vector3(2f, 7.25f, 2f), new Vector3(3f, 0.2f, 3f), _matGelBleu, ColBleuLt);

        // Moving platform L3->L4
        Vector3 mp3a = o + new Vector3(5f, 8f, 0);
        Vector3 mp3b = o + new Vector3(0, 8f, 5f);
        var m3 = CreateZone("G_MP3", mp3a, new Vector3(3.5f, 0.5f, 3.5f), _matNormal, ColDark);
        AddMovingPlatform(m3, mp3a, mp3b, 0.2f);

        // Level 4 (10.5m): platform + sticky wall to L5
        CreateZone("G_L4", o + new Vector3(0, 10.5f, 0), new Vector3(7f, 0.4f, 7f), _matNormal, ColNormalLt);
        CreateZone("G_L4_Sticky", o + new Vector3(-3.8f, 13f, 0), new Vector3(0.5f, 5f, 5f), _matGelViolet, ColViolet);

        // Level 5 — SUMMIT (15.5m): gold platform
        CreateZone("G_L5", o + new Vector3(0, 15.5f, 0), new Vector3(8f, 0.5f, 8f), _matNormal, ColGold);
        CreateZone("G_L5_Bnc", o + new Vector3(0, 15.85f, 0), new Vector3(4f, 0.25f, 4f), _matGelBleu, ColBleuLt);

        // Orbital moving platform for alternative L3->L4 access
        Vector3 orb_a = o + new Vector3(-6f, 9f, 0);
        Vector3 orb_b = o + new Vector3(0, 9f, -6f);
        var orb = CreateZone("G_Orb", orb_a, new Vector3(3.5f, 0.5f, 3.5f), _matNormal, ColDark);
        AddMovingPlatform(orb, orb_a, orb_b, 0.18f);

        Debug.Log("[ArenaZoneBuilder] Zone G (Central Tower) built.");
    }

    // ═══════════════════════════════════════════
    //  SIGN HELPER (zone entrance markers)
    // ═══════════════════════════════════════════

    private void CreateSign(string name, Vector3 position, Color color)
    {
        // Tall thin panel as zone marker
        CreateZone(name, position, new Vector3(0.3f, 2.5f, 0.3f), _matNormal, color);
        // Colored cap on top
        CreateZone(name + "_Cap", position + new Vector3(0, 1.45f, 0), new Vector3(0.8f, 0.4f, 0.8f), _matNormal, color);
    }

    // ═══════════════════════════════════════════
    //  MATERIAL LOADING
    // ═══════════════════════════════════════════

    private void LoadMaterials()
    {
        _matGelBleu   = Resources.Load<PhysicsMaterial>("GelBleu");
        _matGelOrange = Resources.Load<PhysicsMaterial>("GelOrange");
        _matGelViolet = Resources.Load<PhysicsMaterial>("GelViolet");
        _matBouncy    = Resources.Load<PhysicsMaterial>("Bouncy");
        _matNormal    = Resources.Load<PhysicsMaterial>("Normal");

        if (_matGelBleu == null)   Debug.LogWarning("[ArenaZoneBuilder] GelBleu not found in Resources!");
        if (_matGelOrange == null)  Debug.LogWarning("[ArenaZoneBuilder] GelOrange not found in Resources!");
        if (_matGelViolet == null)  Debug.LogWarning("[ArenaZoneBuilder] GelViolet not found in Resources!");
        if (_matBouncy == null)     Debug.LogWarning("[ArenaZoneBuilder] Bouncy not found in Resources!");
        if (_matNormal == null)     Debug.LogWarning("[ArenaZoneBuilder] Normal not found in Resources!");
    }

    private void FindBaseMaterial()
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        _baseMat = new Material(shader);
    }

    // ═══════════════════════════════════════════
    //  ZONE HELPERS
    // ═══════════════════════════════════════════

    private GameObject CreateZone(string name, Vector3 position, Vector3 size,
        PhysicsMaterial physMat, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.position = position;
        go.transform.localScale = size;
        go.isStatic = true;

        var col = go.GetComponent<Collider>();
        if (col != null && physMat != null) col.material = physMat;

        var rend = go.GetComponent<Renderer>();
        if (rend != null)
        {
            var mat = new Material(_baseMat);
            SetMatColor(mat, color);
            if (color.a < 1f) SetMatTransparent(mat, color);
            rend.material = mat;
        }
        return go;
    }

    private GameObject CreateRamp(string name, Vector3 position, Vector3 size,
        float angle, Vector3 axis, PhysicsMaterial physMat, Color color)
    {
        var go = CreateZone(name, position, size, physMat, color);
        go.transform.rotation = Quaternion.AngleAxis(angle, axis);
        return go;
    }

    private void AddMovingPlatform(GameObject go, Vector3 posA, Vector3 posB, float speed)
    {
        go.isStatic = false;
        _movingPlatforms.Add(new MovingPlatform { go = go, posA = posA, posB = posB, speed = speed });
    }

    // ═══════════════════════════════════════════
    //  MATERIAL HELPERS
    // ═══════════════════════════════════════════

    private static void SetMatColor(Material mat, Color color)
    {
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.color = color;
    }

    private static void SetMatTransparent(Material mat, Color color)
    {
        if (mat.HasProperty("_Surface"))
        {
            mat.SetFloat("_Surface", 1);
            mat.SetFloat("_Blend", 0);
        }
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.DisableKeyword("_SURFACE_TYPE_OPAQUE");
        mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.color = color;
    }

    // ═══════════════════════════════════════════
    //  MOVING PLATFORM DATA
    // ═══════════════════════════════════════════

    private class MovingPlatform
    {
        public GameObject go;
        public Vector3 posA;
        public Vector3 posB;
        public float speed;
        public float t;
    }
}

