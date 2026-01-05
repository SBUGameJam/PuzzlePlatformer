using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EmotionController : MonoBehaviour
{
    public float moveSpeed = 7f;

    public float firstJumpForce = 5f;
    public float secondJumpForce = 2f;
    public int maxJumps = 2;

    public Transform groundCheck;
    public float groundCheckRadius = 0.15f;
    public LayerMask groundMask;

    public float dashSpeed = 18f;
    public float dashDuration = 0.12f;
    public float dashCooldown = 0.35f;

    public KeyCode dashKey = KeyCode.LeftShift;
    public KeyCode swapKey = KeyCode.R;

    public string enemyTag = "Enemy";
    public float stompRayLength = 0.25f;
    public Vector2 stompRayOffset = new Vector2(0f, -0.1f);
    public float stompBounceForce = 3.5f;

    private Rigidbody2D rb;
    private bool controllable = true;

    private float moveX;
    private int jumpsUsed;

    private bool isDashing;
    private bool dashReady = true;

    private bool facingRight = true;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    private void Update()
    {
        if (!controllable)
        {
            moveX = 0f;
            return;
        }

        moveX = Input.GetAxisRaw("Horizontal");
        UpdateFacing(moveX);

        bool isGrounded = false;
        if (groundCheck != null)
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundMask);

        if (isGrounded)
            jumpsUsed = 0;

        if (Input.GetKeyDown(KeyCode.Space))
            TryJump();

        if (Input.GetKeyDown(dashKey))
            TryDash();

        if (Input.GetKeyDown(swapKey))
            TrySwap();
    }

    private void FixedUpdate()
    {
        if (!controllable) return;

        if (!isDashing)
            rb.velocity = new Vector2(moveX * moveSpeed, rb.velocity.y);

        if (rb.velocity.y <= 0f)
            TryStompRayKill();
    }

    private void UpdateFacing(float xInput)
    {
        if (Mathf.Abs(xInput) < 0.01f) return;

        bool wantRight = xInput > 0f;
        if (wantRight == facingRight) return;

        facingRight = wantRight;

        Vector3 s = transform.localScale;
        s.x = Mathf.Abs(s.x) * (facingRight ? 1f : -1f);
        transform.localScale = s;
    }

    private void TryJump()
    {
        if (jumpsUsed >= maxJumps) return;

        float force = (jumpsUsed == 0) ? firstJumpForce : secondJumpForce;

        jumpsUsed++;
        rb.velocity = new Vector2(rb.velocity.x, 0f);
        rb.AddForce(Vector2.up * force, ForceMode2D.Impulse);
    }

    private void TryDash()
    {
        if (isDashing || !dashReady) return;
        StartCoroutine(DashRoutine());
    }

    private System.Collections.IEnumerator DashRoutine()
    {
        dashReady = false;
        isDashing = true;

        float dir = (Mathf.Abs(moveX) > 0.01f) ? Mathf.Sign(moveX) : (facingRight ? 1f : -1f);
        float g = rb.gravityScale;
        rb.gravityScale = 0f;

        rb.velocity = new Vector2(dir * dashSpeed, 0f);

        yield return new WaitForSeconds(dashDuration);

        rb.gravityScale = g;
        isDashing = false;

        yield return new WaitForSeconds(dashCooldown);
        dashReady = true;
    }

    private void TrySwap()
    {
        if (GameManager.I == null) return;
        if (!GameManager.I.TrySpendScore(GameManager.I.emotionSwapCost)) return;
        GameManager.I.SwapCharactersPositions();
    }

    private void TryStompRayKill()
    {
        Vector2 origin = (Vector2)transform.position + stompRayOffset;
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, stompRayLength);

        if (!hit.collider) return;
        if (!hit.collider.CompareTag(enemyTag)) return;

        var killable = hit.collider.GetComponent<IKillable>();
        if (killable != null) killable.Kill();
        else Destroy(hit.collider.gameObject);

        rb.velocity = new Vector2(rb.velocity.x, 0f);
        rb.AddForce(Vector2.up * stompBounceForce, ForceMode2D.Impulse);
    }

    public void SetControllable(bool canControl)
    {
        controllable = canControl;
        if (!canControl) rb.velocity = Vector2.zero;
    }

    public void TeleportTo(Vector3 position)
    {
        rb.velocity = Vector2.zero;
        transform.position = position;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Kill"))
            GameManager.I?.RegisterDeathAndRespawn("Emotion");
        else if (other.CompareTag("Portal"))
            GameManager.I?.NotifyEnteredPortal("Emotion");
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Portal"))
            GameManager.I?.NotifyExitedPortal("Emotion");
    }
}
