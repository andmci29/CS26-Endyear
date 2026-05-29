using UnityEngine;
using UnityEngine.InputSystem;

public class FighterController : MonoBehaviour
{
    // -------------------------------------------------------
    // Fields
    // -------------------------------------------------------
    [Header("Player")]
    public int playerNumber = 1;

    [Header("State")]
    public FighterState currentState = FighterState.Idle;

    [Header("References")]
    public Animator animator;
    public Rigidbody rb;

    [Header("Combat References")]
    public BoxCollider hitbox;
    public Transform hurtboxRoot;
    public AttackData[] attacks;    // [0] light, [1] heavy, [2] launcher

    [Header("Opponent")]
    public FighterController opponent;

    [Header("Stats")]
    public FighterData data;
    public float currentHealth;

    [Header("Movement Feel")]
    [Tooltip("Instant horizontal speed while in the air.")]
    public float airSpeed = 10f;
    [Tooltip("Seconds — jump input remembered before landing")]
    public float jumpBufferTime = 0.12f;

    [Space]
    [Tooltip("Multiplies gravity. Higher numbers make the character fall/rise much faster.")]
    public float gravityMultiplier = 4f;
    [Tooltip("Additional multiplier applied ONLY when falling down for a sharp, snappy peak.")]
    public float fallMultiplier = 1.3f;

    // Private — state
    private float maxHealth;
    private bool facingRight = true;
    private Vector2 moveInput;

    // Private — grounded
    private bool isGrounded;

    // Private — jump buffer
    private float jumpBufferTimer;
    private float knockbackTimer;

    // Private — input
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction blockAction;
    private InputAction lightAttackAction;
    private InputAction heavyAttackAction;
    private InputAction specialAction;
    private InputAction superAction;

    // -------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------

    private FighterInputActions actions; // promote to field so we can dispose it

    private void Awake()
    {
        actions = new FighterInputActions(); // store reference

        if (playerNumber == 1)
        {
            actions.Fighter.Enable();
            moveAction = actions.Fighter.Move;
            jumpAction = actions.Fighter.Jump;
            blockAction = actions.Fighter.Block;
            lightAttackAction = actions.Fighter.LightAttack;
            heavyAttackAction = actions.Fighter.HeavyAttack;
            specialAction = actions.Fighter.Special;
            superAction = actions.Fighter.Super;
        }
        else
        {
            actions.FighterP2.Enable();
            moveAction = actions.FighterP2.Move;
            jumpAction = actions.FighterP2.Jump;
            blockAction = actions.FighterP2.Block;
            lightAttackAction = actions.FighterP2.LightAttack;
            heavyAttackAction = actions.FighterP2.HeavyAttack;
            specialAction = actions.FighterP2.Special;
            superAction = actions.FighterP2.Super;
        }
    }

    private void OnDestroy()
    {
        actions.Fighter.Disable();
        actions.FighterP2.Disable();
        actions.Dispose();
    }

    private void Start()
    {
        maxHealth = data.maxHealth;
        currentHealth = maxHealth;

        if (hitbox != null)
            hitbox.gameObject.SetActive(false);
    }

    private void Update()
    {
        ReadInput();
        UpdateTimers();
        FaceOpponent();

        switch (currentState)
        {
            case FighterState.Idle: HandleIdle(); break;
            case FighterState.Moving: HandleMoving(); break;
            case FighterState.Attacking: HandleAttacking(); break;
            case FighterState.Blocking: HandleBlocking(); break;
            case FighterState.Knockback: HandleKnockback(); break;
            case FighterState.KO: HandleKO(); break;
        }
    }

    private void FixedUpdate()
    {
        ApplyMovement();
    }

    // -------------------------------------------------------
    // Input
    // -------------------------------------------------------

    void ReadInput()
    {
        moveInput = moveAction.ReadValue<Vector2>();

        if (jumpAction.WasPressedThisFrame())
            jumpBufferTimer = jumpBufferTime;
    }

    // -------------------------------------------------------
    // Timers
    // -------------------------------------------------------

    void UpdateTimers()
    {
        jumpBufferTimer -= Time.deltaTime;

        // FIXED: Added '&& currentState != FighterState.Blocking' to lock jumping while guarding
        if (jumpBufferTimer > 0f
            && isGrounded
            && currentState != FighterState.Attacking
            && currentState != FighterState.KO
            && currentState != FighterState.Knockback
            && currentState != FighterState.Blocking)
        {
            Jump();
            jumpBufferTimer = 0f;
        }
    }

    // -------------------------------------------------------
    // Grounded
    // -------------------------------------------------------

    private void OnCollisionStay(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.normal.y > 0.7f)
            {
                isGrounded = true;
                return;
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        isGrounded = false;
    }

    // -------------------------------------------------------
    // Movement & Physics
    // -------------------------------------------------------

    void ApplyMovement()
    {
        if (currentState == FighterState.Attacking
            || currentState == FighterState.Knockback
            || currentState == FighterState.KO
            || currentState == FighterState.Blocking)
            return;

        if (isGrounded)
        {
            float targetSpeed = moveInput.x * data.moveSpeed;
            rb.linearVelocity = new Vector3(targetSpeed, rb.linearVelocity.y, 0f);
        }
        else
        {
            float targetAirSpeed = moveInput.x * airSpeed;
            rb.linearVelocity = new Vector3(targetAirSpeed, rb.linearVelocity.y, 0f);

            if (rb.linearVelocity.y > 0)
            {
                rb.AddForce(Physics.gravity * (gravityMultiplier - 1f), ForceMode.Acceleration);
            }
            else if (rb.linearVelocity.y < 0)
            {
                rb.AddForce(Physics.gravity * ((gravityMultiplier * fallMultiplier) - 1f), ForceMode.Acceleration);
            }
        }

        if (currentState == FighterState.Idle && Mathf.Abs(moveInput.x) > 0.01f)
            TransitionTo(FighterState.Moving);
        else if (currentState == FighterState.Moving && Mathf.Abs(moveInput.x) < 0.01f)
            TransitionTo(FighterState.Idle);
    }

    // -------------------------------------------------------
    // Facing
    // -------------------------------------------------------

    void FaceOpponent()
    {
        if (opponent == null) return;
        bool opponentIsRight = opponent.transform.position.x > transform.position.x;
        if (opponentIsRight && !facingRight) Flip();
        else if (!opponentIsRight && facingRight) Flip();
    }

    void Flip()
    {
        facingRight = !facingRight;
        float yRotation = facingRight ? 0f : 180f;
        transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
    }

    // -------------------------------------------------------
    // State handlers
    // -------------------------------------------------------

    void HandleIdle()
    {
        if (lightAttackAction.WasPressedThisFrame()) StartAttack(0);
        if (heavyAttackAction.WasPressedThisFrame()) StartAttack(1);
        if (blockAction.WasPressedThisFrame()) TransitionTo(FighterState.Blocking);
    }

    void HandleMoving()
    {
        if (lightAttackAction.WasPressedThisFrame()) StartAttack(0);
        if (heavyAttackAction.WasPressedThisFrame()) StartAttack(1);
        if (blockAction.WasPressedThisFrame()) TransitionTo(FighterState.Blocking);
    }

    void HandleAttacking() { }

    void HandleBlocking()
    {
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        if (!blockAction.IsPressed())
            TransitionTo(FighterState.Idle);
    }

    void HandleKnockback()
    {
        knockbackTimer -= Time.deltaTime;
        if (knockbackTimer <= 0f)
            TransitionTo(FighterState.Idle);
    }

    void HandleKO()
    {
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
    }

    // -------------------------------------------------------
    // Jump
    // -------------------------------------------------------

    void Jump()
    {
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, data.jumpForce, 0f);
    }

    // -------------------------------------------------------
    // Attack
    // -------------------------------------------------------

    void StartAttack(int attackIndex)
    {
        if (attacks == null || attackIndex >= attacks.Length) return;
        TransitionTo(FighterState.Attacking);
    }

    // -------------------------------------------------------
    // Combat — damage
    // -------------------------------------------------------

    public void TakeDamage(AttackData attack)
    {
        if (currentState == FighterState.KO) return;

        float damage = attack.damage;
        if (currentState == FighterState.Blocking)
            damage *= attack.blockDamageMultiplier;

        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0f);

        if (opponent != null)
        {
            float direction = transform.position.x > opponent.transform.position.x ? 1f : -1f;
            rb.linearVelocity = new Vector3(
                attack.knockbackForce.x * direction,
                attack.knockbackForce.y,
                0f
            );
        }

        if (currentHealth <= 0f)
            TransitionTo(FighterState.KO);
        else
        {
            knockbackTimer = data.knockbackDuration; // set timer on hit
            TransitionTo(FighterState.Knockback);
        }
    }

    // -------------------------------------------------------
    // Hitboxes
    // -------------------------------------------------------

    public void EnableHitboxForAttack(int attackIndex)
    {
        if (hitbox == null || attacks == null || attackIndex >= attacks.Length) return;

        AttackData attack = attacks[attackIndex];

        // Flip the offset X based on facing direction
        Vector3 offset = attack.hitboxOffset;
        if (!facingRight) offset.x *= -1f;

        hitbox.center = offset;
        hitbox.size = attack.hitboxSize;
        hitbox.GetComponent<Hitbox>().attackData = attack;
        hitbox.gameObject.SetActive(true);
    }

    public void DisableHitbox()
    {
        if (hitbox != null)
            hitbox.gameObject.SetActive(false);
    }

    public void OnAttackEnd()
    {
        DisableHitbox();
        TransitionTo(FighterState.Idle);
    }

    // -------------------------------------------------------
    // State machine
    // -------------------------------------------------------

    public void TransitionTo(FighterState newState)
    {
        if (!CanTransitionTo(newState)) return;
        OnExitState(currentState);
        currentState = newState;
        OnEnterState(currentState);
    }

    void OnEnterState(FighterState state)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;

        switch (state)
        {
            case FighterState.Idle: animator.SetTrigger("Idle"); break;
            case FighterState.Moving: animator.SetTrigger("Move"); break;
            case FighterState.Attacking: animator.SetTrigger("Attack"); break;
            case FighterState.Blocking: animator.SetTrigger("Block"); break;
            case FighterState.Knockback: animator.SetTrigger("Knockback"); break;
            case FighterState.KO: animator.SetTrigger("KO"); break;
        }
    }

    void OnExitState(FighterState state)
    {
        if (state == FighterState.Knockback)
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
    }

    public bool CanTransitionTo(FighterState newState)
    {
        switch (currentState)
        {
            case FighterState.KO:
                return false;

            case FighterState.Attacking:
                return newState == FighterState.Knockback
                    || newState == FighterState.KO;

            case FighterState.Knockback:
                return newState == FighterState.Idle
                    || newState == FighterState.KO;

            default:
                return true;
        }
    }
}