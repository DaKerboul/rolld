using UnityEngine;

/// <summary>
/// Centralized Dear ImGui–style skin for Unity's OnGUI / IMGUI.
/// Provides cached textures, pre-built GUIStyles, and helper draw methods
/// that replicate the classic Dear ImGui dark theme.
///
/// Usage:  Call ImGuiSkin.Init() once (auto-called on first use),
///         then use the static styles and helpers from any OnGUI method.
/// </summary>
public static class ImGuiSkin
{
    // ════════════════════════════════════════════
    //  PALETTE  (Dear ImGui "Dark" defaults)
    // ════════════════════════════════════════════
    public static readonly Color ColWindowBg    = new(0.06f, 0.06f, 0.10f, 0.94f);  // #10101A
    public static readonly Color ColFrameBg     = new(0.12f, 0.12f, 0.18f, 1f);     // #1E1E2E
    public static readonly Color ColFrameHover  = new(0.18f, 0.18f, 0.26f, 1f);
    public static readonly Color ColHeader      = new(0.16f, 0.20f, 0.36f, 1f);     // #283A5C
    public static readonly Color ColHeaderHover = new(0.22f, 0.28f, 0.48f, 1f);
    public static readonly Color ColButton      = new(0.16f, 0.20f, 0.36f, 1f);
    public static readonly Color ColButtonHover = new(0.22f, 0.28f, 0.48f, 1f);
    public static readonly Color ColButtonActive= new(0.10f, 0.30f, 0.60f, 1f);
    public static readonly Color ColAccent      = new(0.26f, 0.59f, 0.98f, 1f);     // #4296FA
    public static readonly Color ColAccentDark  = new(0.20f, 0.42f, 0.78f, 1f);
    public static readonly Color ColText        = new(0.92f, 0.92f, 0.92f, 1f);
    public static readonly Color ColTextDim     = new(0.55f, 0.55f, 0.60f, 1f);
    public static readonly Color ColBorder      = new(0.28f, 0.28f, 0.36f, 1f);
    public static readonly Color ColSeparator   = new(0.28f, 0.28f, 0.36f, 0.6f);
    public static readonly Color ColOverlay     = new(0f, 0f, 0f, 0.65f);
    public static readonly Color ColGreen       = new(0.3f, 1f, 0.3f, 1f);
    public static readonly Color ColRed         = new(1f, 0.35f, 0.35f, 1f);
    public static readonly Color ColYellow      = new(1f, 0.85f, 0.25f, 1f);
    public static readonly Color ColGold        = new(1f, 0.84f, 0f, 1f);

    // ════════════════════════════════════════════
    //  CACHED TEXTURES  (1×1)
    // ════════════════════════════════════════════
    public static Texture2D TexWindowBg   { get; private set; }
    public static Texture2D TexFrameBg    { get; private set; }
    public static Texture2D TexFrameHover { get; private set; }
    public static Texture2D TexHeader     { get; private set; }
    public static Texture2D TexHeaderHover{ get; private set; }
    public static Texture2D TexButton     { get; private set; }
    public static Texture2D TexButtonHover{ get; private set; }
    public static Texture2D TexButtonActive{ get; private set; }
    public static Texture2D TexAccent     { get; private set; }
    public static Texture2D TexAccentDark { get; private set; }
    public static Texture2D TexBorder     { get; private set; }
    public static Texture2D TexOverlay    { get; private set; }
    public static Texture2D TexTransparent{ get; private set; }

    // HUD-specific
    public static Texture2D TexHudStrip   { get; private set; }

    // ════════════════════════════════════════════
    //  STYLES  (lazy-initialized)
    // ════════════════════════════════════════════
    private static bool _inited;

    // Window / panel
    public static GUIStyle WindowTitle   { get; private set; }
    public static GUIStyle WindowSubtitle{ get; private set; }

    // Section headers
    public static GUIStyle SectionHeader { get; private set; }

    // Labels
    public static GUIStyle Label         { get; private set; }
    public static GUIStyle LabelDim      { get; private set; }
    public static GUIStyle LabelBold     { get; private set; }
    public static GUIStyle LabelCenter   { get; private set; }
    public static GUIStyle LabelRich     { get; private set; }

    // Fields (key-value pairs)
    public static GUIStyle FieldKey      { get; private set; }
    public static GUIStyle FieldValue    { get; private set; }

    // Buttons
    public static GUIStyle Button        { get; private set; }
    public static GUIStyle ButtonAccent  { get; private set; }
    public static GUIStyle ButtonSmall   { get; private set; }

    // TextField
    public static GUIStyle TextField     { get; private set; }

    // HUD strip
    public static GUIStyle HudLabel      { get; private set; }

    // Status
    public static GUIStyle StatusGreen   { get; private set; }
    public static GUIStyle StatusRed     { get; private set; }

    // Hint/footer
    public static GUIStyle Hint          { get; private set; }
    public static GUIStyle Footer        { get; private set; }

    // Scroll
    public static GUIStyle ScrollView    { get; private set; }

    // ════════════════════════════════════════════
    //  INIT
    // ════════════════════════════════════════════

    public static void Init()
    {
        if (_inited) return;
        _inited = true;

        // --- Textures ---
        TexWindowBg    = MakeTex(ColWindowBg);
        TexFrameBg     = MakeTex(ColFrameBg);
        TexFrameHover  = MakeTex(ColFrameHover);
        TexHeader      = MakeTex(ColHeader);
        TexHeaderHover = MakeTex(ColHeaderHover);
        TexButton      = MakeTex(ColButton);
        TexButtonHover = MakeTex(ColButtonHover);
        TexButtonActive= MakeTex(ColButtonActive);
        TexAccent      = MakeTex(ColAccent);
        TexAccentDark  = MakeTex(ColAccentDark);
        TexBorder      = MakeTex(ColBorder);
        TexOverlay     = MakeTex(ColOverlay);
        TexTransparent = MakeTex(Color.clear);
        TexHudStrip    = MakeTex(new Color(0.04f, 0.04f, 0.08f, 0.85f));
    }

    /// <summary>Call at the top of every OnGUI that uses this skin.</summary>
    public static void EnsureReady()
    {
        if (!_inited) Init();
        // Build styles lazily (needs GUI.skin to exist, so must be inside OnGUI)
        if (WindowTitle == null) BuildStyles();
    }

    static void BuildStyles()
    {
        var pad4  = new RectOffset(4, 4, 2, 2);
        var pad6  = new RectOffset(6, 6, 4, 4);
        var pad8  = new RectOffset(8, 8, 4, 4);

        // ── Window Title ──
        WindowTitle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 22,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            padding   = pad6,
            richText  = true
        };
        WindowTitle.normal.textColor = ColAccent;

        WindowSubtitle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 13,
            alignment = TextAnchor.MiddleCenter,
            padding   = pad4,
        };
        WindowSubtitle.normal.textColor = ColTextDim;

        // ── Section Header ──
        SectionHeader = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 12,
            fontStyle = FontStyle.Bold,
            padding   = new RectOffset(6, 4, 4, 4),
            margin    = new RectOffset(0, 0, 6, 2),
            richText  = true,
        };
        SectionHeader.normal.background = TexHeader;
        SectionHeader.normal.textColor  = ColAccent;

        // ── Labels ──
        Label = new GUIStyle(GUI.skin.label) { fontSize = 13, richText = true };
        Label.normal.textColor = ColText;

        LabelDim = new GUIStyle(Label);
        LabelDim.normal.textColor = ColTextDim;

        LabelBold = new GUIStyle(Label) { fontStyle = FontStyle.Bold };

        LabelCenter = new GUIStyle(Label) { alignment = TextAnchor.MiddleCenter };

        LabelRich = new GUIStyle(Label) { richText = true, wordWrap = true };

        // ── Field key/value ──
        FieldKey = new GUIStyle(Label) { fontSize = 12 };
        FieldKey.normal.textColor = ColTextDim;

        FieldValue = new GUIStyle(Label) { fontSize = 12 };

        // ── Buttons ──
        Button = new GUIStyle(GUI.skin.button)
        {
            fontSize  = 13,
            fontStyle = FontStyle.Bold,
            padding   = pad8,
            margin    = new RectOffset(2, 2, 2, 2),
            border    = new RectOffset(1, 1, 1, 1),
        };
        Button.normal.background  = TexButton;
        Button.normal.textColor   = ColText;
        Button.hover.background   = TexButtonHover;
        Button.hover.textColor    = Color.white;
        Button.active.background  = TexButtonActive;
        Button.active.textColor   = Color.white;
        Button.focused.background = TexButtonHover;

        ButtonAccent = new GUIStyle(Button)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
        };
        ButtonAccent.normal.background  = TexAccentDark;
        ButtonAccent.normal.textColor   = Color.white;
        ButtonAccent.hover.background   = TexAccent;
        ButtonAccent.active.background  = TexButtonActive;

        ButtonSmall = new GUIStyle(Button) { fontSize = 11, padding = pad4 };

        // ── TextField ──
        TextField = new GUIStyle(GUI.skin.textField)
        {
            fontSize  = 14,
            padding   = new RectOffset(8, 8, 6, 6),
            border    = new RectOffset(2, 2, 2, 2),
        };
        TextField.normal.background  = TexFrameBg;
        TextField.normal.textColor   = ColText;
        TextField.focused.background = TexFrameHover;
        TextField.focused.textColor  = Color.white;
        TextField.hover.background   = TexFrameHover;
        // Cursor color follows textColor

        // ── HUD Strip ──
        HudLabel = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 13,
            richText  = true,
            alignment = TextAnchor.MiddleLeft,
            padding   = new RectOffset(10, 10, 0, 0),
        };
        HudLabel.normal.textColor = ColText;

        // ── Status ──
        StatusGreen = new GUIStyle(Label) { fontStyle = FontStyle.Bold };
        StatusGreen.normal.textColor = ColGreen;

        StatusRed = new GUIStyle(Label) { fontStyle = FontStyle.Bold };
        StatusRed.normal.textColor = ColRed;

        // ── Hint / Footer ──
        Hint = new GUIStyle(Label)
        {
            fontSize  = 13,
            fontStyle = FontStyle.Italic,
            alignment = TextAnchor.MiddleCenter,
        };
        Hint.normal.textColor = ColYellow;

        Footer = new GUIStyle(Label)
        {
            fontSize  = 11,
            alignment = TextAnchor.MiddleCenter,
        };
        Footer.normal.textColor = new Color(1, 1, 1, 0.3f);

        // ── ScrollView ──
        ScrollView = new GUIStyle(GUI.skin.scrollView);
        ScrollView.normal.background = TexFrameBg;
    }

    // ════════════════════════════════════════════
    //  DRAWING HELPERS
    // ════════════════════════════════════════════

    /// <summary>Draw a full-screen darkened overlay.</summary>
    public static void DrawOverlay()
    {
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), TexOverlay);
    }

    /// <summary>Draw a window/panel background with a subtle border.</summary>
    public static void DrawWindowBg(Rect rect)
    {
        // Border (1px)
        GUI.DrawTexture(new Rect(rect.x - 1, rect.y - 1, rect.width + 2, rect.height + 2), TexBorder);
        // Fill
        GUI.DrawTexture(rect, TexWindowBg);
    }

    /// <summary>Draw the HUD strip background.</summary>
    public static void DrawHudStripBg(float height)
    {
        GUI.DrawTexture(new Rect(0, 0, Screen.width, height), TexHudStrip);
    }

    /// <summary>Begin a centered window panel. Returns content Rect (inset by padding).</summary>
    public static Rect BeginWindow(float width, float height, string title)
    {
        float x = (Screen.width - width) / 2f;
        float y = (Screen.height - height) / 2f;
        return BeginWindowAt(x, y, width, height, title);
    }

    /// <summary>Begin a window at a specific position.</summary>
    public static Rect BeginWindowAt(float x, float y, float width, float height, string title)
    {
        DrawWindowBg(new Rect(x, y, width, height));

        // Title bar
        float titleH = 32;
        GUI.DrawTexture(new Rect(x, y, width, titleH), TexHeader);
        GUI.Label(new Rect(x, y, width, titleH), title, WindowTitle);

        // Content area
        float pad = 16;
        Rect content = new(x + pad, y + titleH + 8, width - pad * 2, height - titleH - pad - 8);
        GUILayout.BeginArea(content);
        return content;
    }

    /// <summary>End a window started with BeginWindow/BeginWindowAt.</summary>
    public static void EndWindow()
    {
        GUILayout.EndArea();
    }

    /// <summary>Draw a section header bar (e.g. "CONNECTION", "LOCAL PLAYER").</summary>
    public static void DrawSectionHeader(string text)
    {
        GUILayout.Label(text, SectionHeader);
    }

    /// <summary>Draw a key-value field row.</summary>
    public static void DrawField(string key, string value)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(key + ":", FieldKey, GUILayout.Width(85));
        GUILayout.Label(value, FieldValue);
        GUILayout.EndHorizontal();
    }

    /// <summary>Draw a thin horizontal separator line.</summary>
    public static void Separator()
    {
        GUILayout.Space(4);
        Rect r = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(1));
        GUI.DrawTexture(r, TexBorder);
        GUILayout.Space(4);
    }

    // ─── Internal ───

    static Texture2D MakeTex(Color c)
    {
        var t = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        t.SetPixel(0, 0, c);
        t.Apply();
        t.hideFlags = HideFlags.HideAndDontSave;
        return t;
    }
}
