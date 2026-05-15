using System.Collections;
using UnityEngine;

/// <summary>
/// Full-screen overlay for elimination, qualification, and game-end events.
/// Fade in → hold → fade out automatically.
/// </summary>
public class EliminationOverlay : MonoBehaviour
{
    private enum OverlayType { None, Eliminated, Qualified, GameEnd }

    private OverlayType _type = OverlayType.None;
    private float _alpha = 0f;
    private string _winnerName = "";

    private static Texture2D _bgTex;

    void Start()
    {
        if (_bgTex == null)
        {
            _bgTex = new Texture2D(1, 1);
            _bgTex.SetPixel(0, 0, Color.white);
            _bgTex.Apply();
        }
    }

    public void ShowEliminated() => StartCoroutine(ShowOverlay(OverlayType.Eliminated, 3f));
    public void ShowQualified()  => StartCoroutine(ShowOverlay(OverlayType.Qualified, 2.5f));
    public void ShowGameEnd(string winner)
    {
        _winnerName = winner;
        StartCoroutine(ShowOverlay(OverlayType.GameEnd, 6f));
    }

    private IEnumerator ShowOverlay(OverlayType type, float holdTime)
    {
        _type = type;

        // Fade in
        float t = 0f;
        while (t < 0.3f)
        {
            t += Time.deltaTime;
            _alpha = Mathf.Clamp01(t / 0.3f);
            yield return null;
        }
        _alpha = 1f;

        // Hold
        yield return new WaitForSeconds(holdTime);

        // Fade out
        t = 0f;
        while (t < 0.4f)
        {
            t += Time.deltaTime;
            _alpha = 1f - Mathf.Clamp01(t / 0.4f);
            yield return null;
        }

        _alpha = 0f;
        _type = OverlayType.None;
    }

    void OnGUI()
    {
        if (_type == OverlayType.None || _alpha < 0.01f) return;

        // Background tint
        Color bgColor = _type switch
        {
            OverlayType.Eliminated => new Color(0.7f, 0.05f, 0.05f, _alpha * 0.55f),
            OverlayType.Qualified  => new Color(0.05f, 0.55f, 0.15f, _alpha * 0.45f),
            OverlayType.GameEnd    => new Color(0.05f, 0.05f, 0.3f,  _alpha * 0.6f),
            _ => Color.clear
        };
        GUI.color = bgColor;
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _bgTex);
        GUI.color = Color.white;

        // Main text
        var mainStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 72,
            fontStyle = FontStyle.Bold,
        };

        string mainText = _type switch
        {
            OverlayType.Eliminated => "ÉLIMINÉ !",
            OverlayType.Qualified  => "QUALIFIÉ !",
            OverlayType.GameEnd    => "VICTOIRE !",
            _ => ""
        };

        Color textColor = _type switch
        {
            OverlayType.Eliminated => new Color(1f, 0.3f, 0.2f, _alpha),
            OverlayType.Qualified  => new Color(0.3f, 1f, 0.5f, _alpha),
            OverlayType.GameEnd    => new Color(1f, 0.85f, 0.1f, _alpha),
            _ => Color.clear
        };
        mainStyle.normal.textColor = textColor;
        GUI.Label(new Rect(0, Screen.height * 0.35f, Screen.width, 100f), mainText, mainStyle);

        // Sub text
        var subStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 26,
            fontStyle = FontStyle.Bold,
        };
        subStyle.normal.textColor = new Color(1f, 1f, 1f, _alpha * 0.85f);

        string subText = _type switch
        {
            OverlayType.Eliminated => "Meilleure chance la prochaine fois !",
            OverlayType.Qualified  => "Tu passes au round suivant !",
            OverlayType.GameEnd    => $"Gagnant : {_winnerName}",
            _ => ""
        };
        GUI.Label(new Rect(0, Screen.height * 0.35f + 100f, Screen.width, 50f), subText, subStyle);

        // Emoji accent
        var emojiStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 48,
        };
        emojiStyle.normal.textColor = new Color(1f, 1f, 1f, _alpha * 0.7f);
        string emoji = _type switch
        {
            OverlayType.Eliminated => "💀",
            OverlayType.Qualified  => "✅",
            OverlayType.GameEnd    => "🏆",
            _ => ""
        };
        GUI.Label(new Rect(0, Screen.height * 0.35f - 80f, Screen.width, 70f), emoji, emojiStyle);
    }
}
