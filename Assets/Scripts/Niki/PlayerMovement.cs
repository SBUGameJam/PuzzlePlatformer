using UnityEngine;

/// <summary>
/// PlayerMovement (both characters stay VISIBLE)
/// - We do NOT SetActive(false) on the characters
/// - Instead we disable physics (Rigidbody2D.simulated) + collisions (Collider2D.enabled)
/// - Only the "active" character receives movement + jump input
/// 
/// Controls:
/// - A/D or Left/Right: Move
/// - Space: Jump
/// - Tab: Switch character
/// 
/// Notes:
/// - Put BOTH Logic and Emotion on Tag "Player" (or at least the active one)
/// - Ground objects must be on Layer "Ground"
/// - Both characters must have: Rigidbody2D + Collider2D (BoxCollider2D recommended)
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    public GameObject logicCharacter;
    public GameObject emotionCharacter;

    [Header("Movement")]
    public float moveSpeed = 6f;
    public float jumpForce = 12f;

    [Header("Ground Check")]
    [Tooltip("Ray length downwards for grounded check.")]
    public float groundRayLength = 0.8f;

    [Header("Emotion Special")]
    [Tooltip("Double jump = 1 extra jump in air.")]
    public int emotionExtraJumps = 1;

    [Tooltip("If true, using Emotion extra jump registers Emotion action for AIDirector.")]
    public bool scoreEmotionExtraJump = true;

    private Rigidbody2D activeRb;
    private Collider2D activeCol;
    private bool isLogicActive = false;

    private int jumpsLeft;

    void Awake()
    {
        // Basic validation to avoid null refs
        if (logicCharacter == null || emotionCharacter == null)
        {
            Debug.LogError("PlayerSwitcher: Assign logicCharacter and emotionCharacter in Inspector.");
        }
    }

    void Start()
    {
        // Ensure both are visible (SpriteRenderer stays enabled by default)
        // Activate Emotion by default
        ActivateEmotion();
    }

    void Update()
    {
        // Switch character
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (isLogicActive) ActivateEmotion();
            else ActivateLogic();

            AIDirector.Instance?.RegisterSwitch();
        }

        // Jump input
        if (Input.GetKeyDown(KeyCode.Space))
        {
            HandleJump();
        }
    }

    void FixedUpdate()
    {
        // Horizontal movement
        float x = Input.GetAxisRaw("Horizontal");
        if (activeRb != null)
        {
            activeRb.velocity = new Vector2(x * moveSpeed, activeRb.velocity.y);
        }
    }

    private void HandleJump()
    {
        if (activeRb == null) return;

        if (IsGrounded())
        {
            activeRb.velocity = new Vector2(activeRb.velocity.x, jumpForce);
            jumpsLeft = isLogicActive ? 0 : emotionExtraJumps;
            return;
        }

        // Extra jumps only for Emotion
        if (!isLogicActive && jumpsLeft > 0)
        {
            activeRb.velocity = new Vector2(activeRb.velocity.x, jumpForce);
            jumpsLeft--;

            if (scoreEmotionExtraJump)
                AIDirector.Instance?.RegisterEmotionAction(1);
        }
    }

    private void ActivateLogic()
    {
        isLogicActive = true;

        EnableCharacter(logicCharacter, enablePhysics: true);
        EnableCharacter(emotionCharacter, enablePhysics: false);

        activeRb = logicCharacter.GetComponent<Rigidbody2D>();
        activeCol = logicCharacter.GetComponent<Collider2D>();
        jumpsLeft = 0;

        // Optional: visually indicate active/inactive
        SetVisualActive(logicCharacter, true);
        SetVisualActive(emotionCharacter, false);
    }

    private void ActivateEmotion()
    {
        isLogicActive = false;

        EnableCharacter(emotionCharacter, enablePhysics: true);
        EnableCharacter(logicCharacter, enablePhysics: false);

        activeRb = emotionCharacter.GetComponent<Rigidbody2D>();
        activeCol = emotionCharacter.GetComponent<Collider2D>();
        jumpsLeft = emotionExtraJumps;

        // Optional: visually indicate active/inactive
        SetVisualActive(emotionCharacter, true);
        SetVisualActive(logicCharacter, false);
    }

    /// <summary>
    /// Keeps the character visible, but disables physics/collision when inactive.
    /// </summary>
    private void EnableCharacter(GameObject character, bool enablePhysics)
    {
        if (character == null) return;

        var rb = character.GetComponent<Rigidbody2D>();
        if (rb != null) rb.simulated = enablePhysics;

        var col = character.GetComponent<Collider2D>();
        if (col != null) col.enabled = enablePhysics;

        // Important: DO NOT disable SpriteRenderer
        // This keeps it visible even when inactive.
    }

    /// <summary>
    /// Optional: make inactive character semi-transparent to show it's not controlled.
    /// Safe to remove if you don't want it.
    /// </summary>
    private void SetVisualActive(GameObject character, bool active)
    {
        if (character == null) return;

        var sr = character.GetComponent<SpriteRenderer>();
        if (sr == null) return;

        var c = sr.color;
        c.a = active ? 1f : 0.35f;   // inactive looks "ghosted"
        sr.color = c;
    }

    private bool IsGrounded()
    {
        if (activeRb == null) return false;

        // Raycast straight down from active character position
        Vector2 origin = activeRb.position;
        int groundMask = LayerMask.GetMask("Ground");

        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, groundRayLength, groundMask);
        return hit.collider != null;
    }
}
