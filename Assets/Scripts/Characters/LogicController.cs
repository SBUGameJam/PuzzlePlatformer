using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class LogicController : MonoBehaviour
{
    public float moveSpeed = 6f;
    public bool allowVerticalMove = true;

    public KeyCode interactKey = KeyCode.E;
    public float interactDistance = 1.3f;
    public LayerMask interactMask;

    public KeyCode scanKey = KeyCode.Q;
    public float scanRadius = 4f;
    public LayerMask scanMask;

    private Rigidbody2D rb;
    private bool controllable = true;

    private float moveX;
    private float moveY;

    private bool facingRight = true;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    private void Update()
    {
        if (!controllable)
        {
            moveX = 0f;
            moveY = 0f;
            return;
        }

        moveX = Input.GetAxisRaw("Horizontal");
        moveY = allowVerticalMove ? Input.GetAxisRaw("Vertical") : 0f;

        UpdateFacing(moveX);

        if (Input.GetKeyDown(interactKey))
            TryInteract();

        if (Input.GetKeyDown(scanKey))
            TryScan();
    }

    private void FixedUpdate()
    {
        if (!controllable) return;
        rb.velocity = new Vector2(moveX * moveSpeed, moveY * moveSpeed);
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

    private void TryInteract()
    {
        Vector2 dir = (Mathf.Abs(moveX) > 0.01f) ? new Vector2(Mathf.Sign(moveX), 0f) : Vector2.right;

        RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, interactDistance, interactMask);
        if (!hit.collider) return;

        var interactable = hit.collider.GetComponent<IInteractable>();
        if (interactable != null)
            interactable.Interact(this);
    }

    private void TryScan()
    {
        if (GameManager.I == null) return;
        if (!GameManager.I.TrySpendLogicSpecial()) return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, scanRadius, scanMask);
        for (int i = 0; i < hits.Length; i++)
        {
            var reveal = hits[i].GetComponent<IRevealable>();
            if (reveal != null) reveal.Reveal(0f);
        }
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
            GameManager.I?.RegisterDeathAndRespawn("Logic");
        else if (other.CompareTag("Portal"))
            GameManager.I?.NotifyEnteredPortal("Logic");
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Portal"))
            GameManager.I?.NotifyExitedPortal("Logic");
    }
}

public interface IInteractable
{
    void Interact(LogicController logic);
}

public interface IRevealable
{
    void Reveal(float seconds);
}
