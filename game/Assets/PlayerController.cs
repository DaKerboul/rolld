using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class PlayerController : MonoBehaviour
{
    // Reference to the Player Input component
    private bool isJumpPressed = false;
    private float jumpPressTime = 0f;
    private bool _isLocalPlayer = false;
    public float maxJumpHoldTime = 0.5f; // Ex. limite la puissance du saut

    public float JumpForce = 5f; // Force applied when jumping

    public float MovementSpeed = 25f; // Speed of player movement
    public float BoostSpeed = 2f; // Multiplicateur de vitesse sur GelOrange

    [Header("Steering Feel")]
    [Tooltip("Damps velocity perpendicular to input — higher = sharper turns")]
    public float turnDamping = 7f;
    [Tooltip("Horizontal friction when no input is held")]
    public float idleDrag = 3f;

    [Header("Bump Collision")]
    public float bumpForce = 4f; // Impulse force when bumping a remote player
    public float bumpCooldown = 0.25f; // Minimum time between bumps from the same player
    private Dictionary<int, float> _lastBumpTime = new Dictionary<int, float>();

    // Ajout des états pour chaque direction
    private bool isForwardHeld = false;
    private bool isBackwardsHeld = false;
    private bool isLeftHeld = false;
    private bool isRightHeld = false;
    private bool isOnGelOrange = false; // Indique si la boule est sur GelOrange
    private bool isOnGelViolet = false; // Indique si la boule est sur GelViolet (sticky)
    private Vector3 stickyNormal = Vector3.up; // Normale de la surface sticky en contact
    public float StickyForce = 20f; // Force qui plaque la balle contre la surface GelViolet
    private float originalDrag = 0f; // Sauvegarde du drag original du Rigidbody

    [Header("Limits")]
    public float maxVelocity = 120f; // Velocity cap to prevent infinite acceleration
    public float respawnY = -10f; // Y threshold for respawn
    private Vector3 _spawnPos = new Vector3(0f, 3f, -30f);
    private Rigidbody _rb;

    // Squash & stretch
    private bool _isSquashing = false;
    private Transform _meshTransform; // Reference to visual mesh for squash effect

    // Fall warning
    private static Texture2D _fallWarningTex;
    private float _fallWarningAlpha = 0f;



    public GameObject CameraReference; // Référence à la caméra (drag & drop dans l'inspecteur)

    // --- Local player floating name label ---
    private GameObject _nameLabelObj;
    private TextMesh _nameLabel;

    // --- Jump power (exposed for HUD) ---
    public bool IsJumpCharging => isJumpPressed && IsGrounded();
    public float JumpChargeNormalized => Mathf.Clamp01(jumpPressTime / maxJumpHoldTime);

    // --- Shared font for TextMesh labels (WebGL-safe) ---
    private static Font _labelFont;
    public static Font LabelFont
    {
        get
        {
            if (_labelFont == null)
                _labelFont = Resources.Load<Font>("LiberationSans");
            return _labelFont;
        }
    }

    void Start()
    {
        Debug.Log("PlayerController script initialized.");
        _rb = GetComponent<Rigidbody>();
        _meshTransform = transform;
    }

    public void SetSpawnPosition(Vector3 pos) => _spawnPos = pos;

    /// <summary>
    /// Called by LobbyUI after connecting. Sets up the local player
    /// with a floating name label and a 50% color tint.
    /// </summary>
    public void SetupLocalPlayer(string playerName, Color playerColor)
    {
        _isLocalPlayer = true;
        // --- Color tint (multiply blend to keep original pattern visible) ---
        var ballRenderer = GetComponent<Renderer>();
        if (ballRenderer != null)
        {
            var mat = new Material(ballRenderer.sharedMaterial);
            Color original = Color.white;
            if (mat.HasProperty("_BaseColor"))  original = mat.GetColor("_BaseColor");
            else if (mat.HasProperty("_Color")) original = mat.GetColor("_Color");

            // Multiply tint: keeps the original pattern while colorizing
            // Strength 0.7 = strong color, 0.3 original preserved
            float strength = 0.7f;
            Color tint = new Color(
                Mathf.Lerp(original.r, original.r * playerColor.r * 2f, strength),
                Mathf.Lerp(original.g, original.g * playerColor.g * 2f, strength),
                Mathf.Lerp(original.b, original.b * playerColor.b * 2f, strength),
                original.a
            );

            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", tint);
            if (mat.HasProperty("_Color"))     mat.color = tint;
            ballRenderer.material = mat;
        }

        // --- Floating name label (parented to Player root, NOT the sphere) ---
        if (_nameLabelObj != null) Destroy(_nameLabelObj);

        _nameLabelObj = new GameObject("LocalNameLabel");
        // Parent to the Player root (one level up) so ball rotation doesn't affect it
        _nameLabelObj.transform.SetParent(transform.parent, false);
        _nameLabelObj.transform.localScale = Vector3.one * 0.1f;

        _nameLabel = _nameLabelObj.AddComponent<TextMesh>();
        _nameLabel.text = playerName;
        _nameLabel.fontSize = 144;
        _nameLabel.characterSize = 0.15f;
        _nameLabel.anchor = TextAnchor.MiddleCenter;
        _nameLabel.alignment = TextAlignment.Center;
        _nameLabel.color = playerColor;
        if (LabelFont != null) _nameLabel.font = LabelFont;

        var renderer = _nameLabel.GetComponent<MeshRenderer>();
        if (LabelFont != null && LabelFont.material != null)
            renderer.material = LabelFont.material;
        else
        {
            var textShader = Shader.Find("GUI/Text Shader") ?? Shader.Find("Unlit/Texture");
            if (textShader != null) renderer.material = new Material(textShader);
        }

        // --- Speed trail renderer ---
        var trail = gameObject.GetComponent<TrailRenderer>();
        if (trail == null) trail = gameObject.AddComponent<TrailRenderer>();
        trail.time = 0.4f;
        trail.startWidth = 0.3f;
        trail.endWidth = 0.02f;
        trail.material = new Material(Shader.Find("Sprites/Default"));
        trail.startColor = new Color(1f, 0.7f, 0.2f, 0.7f);
        trail.endColor = new Color(1f, 0.4f, 0.1f, 0f);
        trail.minVertexDistance = 0.1f;
        trail.autodestruct = false;
        trail.emitting = true;

        Debug.Log($"[Player] Local setup: {playerName}, tint={playerColor}");
    }

    void LateUpdate()
    {
        // Keep local name label floating above the ball and facing the camera
        if (_nameLabelObj != null)
        {
            _nameLabelObj.transform.position = transform.position + Vector3.up * 1.5f;
            var cam = Camera.main;
            if (cam != null)
            {
                // Billboard locked to Y axis — only rotate around vertical
                Vector3 lookDir = _nameLabelObj.transform.position - cam.transform.position;
                lookDir.y = 0f;
                if (lookDir.sqrMagnitude > 0.001f)
                    _nameLabelObj.transform.rotation = Quaternion.LookRotation(lookDir);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Toggle cursor lock/unlock avec clic droit (disabled when keybind menu is open)
        if (!KeyBindingUI.IsVisible && Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                Debug.Log("Cursor UNLOCKED");
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                Debug.Log("Cursor LOCKED");
            }
        }

        // Player update logic can be added here
        if (isJumpPressed)
        {
            jumpPressTime += Time.deltaTime;
            if (jumpPressTime > maxJumpHoldTime)
            {
                jumpPressTime = maxJumpHoldTime; // Clamp to max hold time
            }
        }

        // --- Respawn if fallen off the map ---
        if (transform.position.y < respawnY)
        {
            if (_rb != null)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
                _rb.useGravity = true;
            }
            transform.position = _spawnPos;
            isOnGelViolet = false;
            isOnGelOrange = false;
            Debug.Log("[Player] Respawned after falling.");
            return;
        }

        // --- Fall warning (tint screen red when low) ---
        float fallTarget = (transform.position.y < -3f) ? Mathf.Clamp01((-3f - transform.position.y) / 7f) : 0f;
        _fallWarningAlpha = Mathf.Lerp(_fallWarningAlpha, fallTarget, Time.deltaTime * 5f);

        // Mouvement continu selon les directions maintenues
        Rigidbody rb = _rb;
        if (rb != null)
        {
            float currentSpeed = MovementSpeed;
            if (isOnGelOrange)
            {
                currentSpeed *= BoostSpeed;
            }

            // Détermination des directions selon la caméra
            Vector3 forward = Vector3.forward;
            Vector3 right = Vector3.right;
            if (CameraReference != null)
            {
                Vector3 camForward = CameraReference.transform.forward;
                Vector3 camRight = CameraReference.transform.right;

                if (isOnGelViolet)
                {
                    // PROJECT onto sticky surface plane for surface-relative movement
                    forward = Vector3.ProjectOnPlane(camForward, stickyNormal).normalized;
                    right = Vector3.ProjectOnPlane(camRight, stickyNormal).normalized;
                    // Fallback if projection is degenerate
                    if (forward.sqrMagnitude < 0.01f)
                        forward = Vector3.ProjectOnPlane(Vector3.forward, stickyNormal).normalized;
                    if (right.sqrMagnitude < 0.01f)
                        right = Vector3.ProjectOnPlane(Vector3.right, stickyNormal).normalized;
                }
                else
                {
                    // Normal mode: project onto horizontal plane
                    camForward.y = 0;
                    camForward.Normalize();
                    forward = camForward;
                    camRight.y = 0;
                    camRight.Normalize();
                    right = camRight;
                }
            }

            Vector3 inputDir = Vector3.zero;
            if (isForwardHeld)  inputDir += forward;
            if (isBackwardsHeld) inputDir -= forward;
            if (isRightHeld)   inputDir += right;
            if (isLeftHeld)    inputDir -= right;

            if (inputDir.sqrMagnitude > 0.01f)
            {
                inputDir.Normalize();
                rb.AddForce(inputDir * currentSpeed * Time.deltaTime, ForceMode.VelocityChange);

                // Counter-force on the lateral component (makes turns sharper)
                Vector3 horizVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                Vector3 perp = horizVel - Vector3.Project(horizVel, inputDir);
                if (perp.sqrMagnitude > 0.01f)
                    rb.AddForce(-perp * turnDamping * Time.deltaTime, ForceMode.VelocityChange);
            }
            else if (!isOnGelViolet)
            {
                // Gradual horizontal slow-down when no key is held
                Vector3 horizVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                rb.AddForce(-horizVel * idleDrag * Time.deltaTime, ForceMode.VelocityChange);
            }

            // GelViolet : colle la balle à la surface (sticky)
            if (isOnGelViolet)
            {
                // Désactive la gravité Unity sur le Rigidbody
                rb.useGravity = false;

                // Applique une gravité inversée vers la surface
                rb.AddForce(-stickyNormal * Physics.gravity.magnitude, ForceMode.Acceleration);

                // Force de placage supplémentaire
                rb.AddForce(-stickyNormal * StickyForce, ForceMode.Acceleration);

                // Annule la vélocité qui s'éloigne de la surface (empêche le rebond)
                float velocityAwayFromSurface = Vector3.Dot(rb.linearVelocity, stickyNormal);
                if (velocityAwayFromSurface > 0)
                {
                    rb.linearVelocity -= stickyNormal * velocityAwayFromSurface;
                }
            }
            else
            {
                rb.useGravity = true;
            }

            // --- Velocity cap ---
            if (rb.linearVelocity.magnitude > maxVelocity)
            {
                rb.linearVelocity = rb.linearVelocity.normalized * maxVelocity;
            }
        }
    }

    void OnCollisionStay(Collision collision)
    {
        Collider col = collision.collider;
        if (col == null || col.sharedMaterial == null) return;

        if (col.sharedMaterial.name.Contains("GelOrange"))
        {
            isOnGelOrange = true;
        }

        if (col.sharedMaterial.name.Contains("GelViolet"))
        {
            if (!isOnGelViolet)
            {
                originalDrag = _rb != null ? _rb.linearDamping : 0f;
                if (_rb != null) _rb.linearDamping = 1f;
            }
            isOnGelViolet = true;
            Vector3 avgNormal = Vector3.zero;
            foreach (ContactPoint contact in collision.contacts)
                avgNormal += contact.normal;
            stickyNormal = avgNormal.normalized;
        }
    }

    void OnCollisionExit(Collision collision)
    {
        Collider col = collision.collider;
        if (col != null && col.sharedMaterial != null)
        {
            if (col.sharedMaterial.name.Contains("GelOrange"))
            {
                isOnGelOrange = false;
            }
            if (col.sharedMaterial.name.Contains("GelViolet"))
            {
                isOnGelViolet = false;
                // Restaure le drag original et réactive la gravité
                Rigidbody rb = GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.useGravity = true;
                    rb.linearDamping = originalDrag;
                }
            }
        }
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            // Touche appuyée
            isJumpPressed = true;
            jumpPressTime = 0f;
            Debug.Log("Jump Started");
        }
        else if (context.performed)
        {
            // Action validée (utile pour saut immédiat aussi)
            Debug.Log("Jump Performed");
        }
        else if (context.canceled)
        {
            // Touche relâchée
            float jumpForceFactor = Mathf.Clamp01(jumpPressTime / maxJumpHoldTime);
            if (IsGrounded())
            {
                PerformJump(jumpForceFactor * JumpForce);
                Debug.Log($"Jump Released after {jumpPressTime}s -> Force factor: {jumpForceFactor}");
            }
            else
            {
                Debug.Log("Jump Released but not grounded.");
            }

            // Reset jump state so gauge goes back to 0
            isJumpPressed = false;
            jumpPressTime = 0f;
        }
    }

    public void PerformJump(float force)
    {
        if (_rb == null) return;
        Vector3 jumpDir = isOnGelViolet ? stickyNormal : Vector3.up;
        _rb.AddForce(jumpDir * force, ForceMode.Impulse);
    }

    private bool IsGrounded()
    {
        // On sticky surface: raycast toward the surface (opposite of normal)
        if (isOnGelViolet)
            return Physics.Raycast(transform.position, -stickyNormal, 1.1f);
        // Normal: raycast downward
        return Physics.Raycast(transform.position, Vector3.down, 1.1f);
    }

    public void OnForward(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            isForwardHeld = true;
            Debug.Log("Forward Action Started");
        }
        else if (context.performed)
        {
            // Forward action performed
            Debug.Log("Forward Action Performed");
        }
        else if (context.canceled)
        {
            isForwardHeld = false;
            Debug.Log("Forward Action Canceled");
        }
    }

    public void OnBackwards(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            isBackwardsHeld = true;
            Debug.Log("Backwards Action Started");
        }
        else if (context.performed)
        {
            Debug.Log("Backwards Action Performed");
        }
        else if (context.canceled)
        {
            isBackwardsHeld = false;
            Debug.Log("Backwards Action Canceled");
        }
    }

    public void OnLeft(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            isLeftHeld = true;
            Debug.Log("Left Action Started");
        }
        else if (context.performed)
        {
            Debug.Log("Left Action Performed");
        }
        else if (context.canceled)
        {
            isLeftHeld = false;
            Debug.Log("Left Action Canceled");
        }
    }

    public void OnRight(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            isRightHeld = true;
            Debug.Log("Right Action Started");
        }
        else if (context.performed)
        {
            Debug.Log("Right Action Performed");
        }
        else if (context.canceled)
        {
            isRightHeld = false;
            Debug.Log("Right Action Canceled");
        }
    }

    // --- Bump collision with remote players ---
    void OnTriggerEnter(Collider other)
    {
        HandleBump(other);
    }

    void OnTriggerStay(Collider other)
    {
        HandleBump(other);
    }

    private void HandleBump(Collider other)
    {
        var remote = other.GetComponent<RemotePlayerController>();
        if (remote == null) return;

        int id = other.gameObject.GetInstanceID();
        if (_lastBumpTime.TryGetValue(id, out float lastTime) && Time.time - lastTime < bumpCooldown)
            return;

        _lastBumpTime[id] = Time.time;

        // Repulsion direction: from remote toward local player
        Vector3 dir = (transform.position - other.transform.position).normalized;
        // Add slight upward component so the ball lifts off the ground
        dir = (dir + Vector3.up * 0.3f).normalized;

        if (_rb != null)
            _rb.AddForce(dir * bumpForce, ForceMode.Impulse);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!_isLocalPlayer) return;
        // Squash & stretch on landing/impact
        if (collision.relativeVelocity.magnitude > 2f && !_isSquashing)
        {
            StartCoroutine(SquashStretch());
        }
    }

    private IEnumerator SquashStretch()
    {
        _isSquashing = true;
        Vector3 original = Vector3.one;
        Vector3 squash = new Vector3(1.2f, 0.7f, 1.2f);
        float t = 0f;
        float dur = 0.08f;
        // Squash
        while (t < dur)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(original, squash, t / dur);
            yield return null;
        }
        // Stretch back
        t = 0f;
        dur = 0.12f;
        while (t < dur)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(squash, original, t / dur);
            yield return null;
        }
        transform.localScale = original;
        _isSquashing = false;
    }

    void OnDestroy()
    {
        // Clean up name label (it's not parented to the ball)
        if (_nameLabelObj != null) Destroy(_nameLabelObj);
    }

    // ========================
    //  JUMP POWER GAUGE (HUD)
    // ========================
    private static Texture2D _gaugeBarTex;
    private static Texture2D _gaugeBgTex;
    private static Texture2D _gaugeGlowTex;
    private float _gaugeDisplayAlpha = 0f;   // smooth fade-in/out
    private float _lastChargeValue = 0f;     // for smooth lerp

    void OnGUI()
    {
        if (!_isLocalPlayer) return;

        // Target alpha: 1 when charging, 0 otherwise (keep visible briefly after release)
        float targetAlpha = IsJumpCharging ? 1f : 0f;
        _gaugeDisplayAlpha = Mathf.MoveTowards(_gaugeDisplayAlpha, targetAlpha, Time.deltaTime * 6f);

        // Smooth the charge value for a fluid bar animation
        float chargeTarget = IsJumpCharging ? JumpChargeNormalized : 0f;
        _lastChargeValue = Mathf.Lerp(_lastChargeValue, chargeTarget, Time.deltaTime * 12f);

        if (_gaugeDisplayAlpha < 0.01f) return;

        // Gauge dimensions
        float barWidth = 400f;
        float barHeight = 22f;
        float x = (Screen.width - barWidth) / 2f;
        float y = Screen.height - 80f;

        // Label
        var labelStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 12,
            fontStyle = FontStyle.Bold
        };
        labelStyle.normal.textColor = new Color(1f, 1f, 1f, _gaugeDisplayAlpha * 0.9f);
        GUI.Label(new Rect(x, y - 26f, barWidth, 24f), "JUMP POWER", labelStyle);

        // Ensure textures
        if (_gaugeBgTex == null)
        {
            _gaugeBgTex = new Texture2D(1, 1);
            _gaugeBgTex.SetPixel(0, 0, new Color(0.08f, 0.08f, 0.12f, 1f));
            _gaugeBgTex.Apply();
        }
        if (_gaugeBarTex == null)
        {
            _gaugeBarTex = new Texture2D(1, 1);
            _gaugeBarTex.SetPixel(0, 0, Color.white);
            _gaugeBarTex.Apply();
        }
        if (_gaugeGlowTex == null)
        {
            _gaugeGlowTex = new Texture2D(1, 1);
            _gaugeGlowTex.SetPixel(0, 0, Color.white);
            _gaugeGlowTex.Apply();
        }

        Color prevBg = GUI.backgroundColor;
        Color prevColor = GUI.color;

        // Background (dark panel)
        float pad = 4f;
        Rect bgRect = new Rect(x - pad, y - pad, barWidth + pad * 2f, barHeight + pad * 2f);
        GUI.color = new Color(1f, 1f, 1f, _gaugeDisplayAlpha * 0.85f);
        GUI.DrawTexture(bgRect, _gaugeBgTex);

        // Border
        float b = 1f;
        Color borderColor = new Color(0.35f, 0.35f, 0.45f, _gaugeDisplayAlpha * 0.7f);
        GUI.color = borderColor;
        GUI.DrawTexture(new Rect(bgRect.x, bgRect.y, bgRect.width, b), _gaugeBarTex);                     // top
        GUI.DrawTexture(new Rect(bgRect.x, bgRect.yMax - b, bgRect.width, b), _gaugeBarTex);               // bottom
        GUI.DrawTexture(new Rect(bgRect.x, bgRect.y, b, bgRect.height), _gaugeBarTex);                     // left
        GUI.DrawTexture(new Rect(bgRect.xMax - b, bgRect.y, b, bgRect.height), _gaugeBarTex);              // right

        // Filled bar with gradient color (blue -> cyan -> yellow -> red)
        float fill = _lastChargeValue;
        Color barColor;
        if (fill < 0.5f)
            barColor = Color.Lerp(new Color(0.2f, 0.6f, 1f), new Color(0.1f, 0.95f, 0.85f), fill * 2f);
        else
            barColor = Color.Lerp(new Color(0.1f, 0.95f, 0.85f), new Color(1f, 0.3f, 0.2f), (fill - 0.5f) * 2f);

        Rect barRect = new Rect(x, y, barWidth * fill, barHeight);
        GUI.color = new Color(barColor.r, barColor.g, barColor.b, _gaugeDisplayAlpha);
        GUI.DrawTexture(barRect, _gaugeBarTex);

        // Glow overlay on the filled portion (bright center highlight)
        Color glowColor = new Color(1f, 1f, 1f, _gaugeDisplayAlpha * 0.2f * fill);
        GUI.color = glowColor;
        float glowH = barHeight * 0.4f;
        GUI.DrawTexture(new Rect(x, y + (barHeight - glowH) * 0.3f, barWidth * fill, glowH), _gaugeGlowTex);

        // Pulsing edge glow when near max
        if (fill > 0.85f)
        {
            float pulse = 0.4f + 0.6f * Mathf.Abs(Mathf.Sin(Time.time * 6f));
            GUI.color = new Color(1f, 0.3f, 0.15f, _gaugeDisplayAlpha * 0.35f * pulse);
            GUI.DrawTexture(bgRect, _gaugeBarTex);
        }

        // Percentage text
        var pctStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 11,
            fontStyle = FontStyle.Bold
        };
        pctStyle.normal.textColor = new Color(1f, 1f, 1f, _gaugeDisplayAlpha * 0.95f);
        GUI.Label(new Rect(x, y, barWidth, barHeight), Mathf.RoundToInt(fill * 100f) + "%", pctStyle);

        GUI.color = prevColor;
        GUI.backgroundColor = prevBg;



        // ========================
        //  FALL WARNING (red tint)
        // ========================
        if (_fallWarningAlpha > 0.01f)
        {
            if (_fallWarningTex == null)
            {
                _fallWarningTex = new Texture2D(1, 1);
                _fallWarningTex.SetPixel(0, 0, Color.white);
                _fallWarningTex.Apply();
            }
            GUI.color = new Color(0.9f, 0.1f, 0.05f, _fallWarningAlpha * 0.35f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _fallWarningTex);

            // Warning text
            var warnStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 24,
                fontStyle = FontStyle.Bold
            };
            warnStyle.normal.textColor = new Color(1f, 0.3f, 0.2f, _fallWarningAlpha);
            GUI.Label(new Rect(0, Screen.height * 0.35f, Screen.width, 40f), "\u26A0 DANGER - CHUTE !", warnStyle);
        }

        // ========================
        //  SPEED INDICATOR
        // ========================
        if (_rb != null)
        {
            float speed = _rb.linearVelocity.magnitude;
            var speedStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleRight,
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };
            float speedAlpha = Mathf.Clamp01(speed / 5f) * 0.8f;
            Color speedCol = Color.Lerp(new Color(0.8f, 0.8f, 0.8f), new Color(1f, 0.5f, 0.1f), Mathf.Clamp01(speed / 30f));
            speedStyle.normal.textColor = new Color(speedCol.r, speedCol.g, speedCol.b, Mathf.Max(0.3f, speedAlpha));
            GUI.Label(new Rect(Screen.width - 160f, Screen.height - 50f, 140f, 24f),
                $"{speed:F1} m/s", speedStyle);
        }

        GUI.color = Color.white;
    }
}
