using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

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
    [Header("Camera")]
    public CinemachineImpulseSource impulseSource;
    public bool CanBeJuggled => isLaunched && !hasBeenJuggled;


    // Private — state
    private float maxHealth;
    private bool facingRight = true;
    private Vector2 moveInput;

    // Private — grounded
    private bool isGrounded;

    // Private — jump buffer & timers
    private float jumpBufferTimer;
    private float knockbackTimer;
    private float attackTimer;
    private float hitboxTimer;
    private int currentAttackIndex = -1;
    private bool isHitStopped = false;

    // FIX: Promoted to class field to safely buffer knockback values during hit-stop freezes
    private Vector3 hitStopStoredVelocity;

    // Private — input
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction blockAction;
    private InputAction lightAttackAction;
    private InputAction heavyAttackAction;
    private InputAction specialAction;
    private InputAction superAction;
    // Private — launcher
    private bool isLaunched = false;
    private bool hasBeenJuggled = false; // prevents more than one follow-up


    // -------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------

    private FighterInputActions actions;

    private void Awake()
    {
        actions = new FighterInputActions();

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
        if (actions != null)
        {
            actions.Fighter.Disable();
            actions.FighterP2.Disable();
            actions.Dispose();
        }
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
                isLaunched = false;  // back on ground — no longer launched
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
        // FIX: Moved custom gravity processing outside the state switch gate.
        // This ensures airborne knockback arcs utilize your sharp gravityMultiplier and fallMultiplier!
        if (!isGrounded && currentState != FighterState.KO)
        {
            if (rb.linearVelocity.y > 0)
            {
                rb.AddForce(Physics.gravity * (gravityMultiplier - 1f), ForceMode.Acceleration);
            }
            else if (rb.linearVelocity.y < 0)
            {
                rb.AddForce(Physics.gravity * ((gravityMultiplier * fallMultiplier) - 1f), ForceMode.Acceleration);
            }
        }

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
        }

        if (currentState == FighterState.Idle && Mathf.Abs(moveInput.x) > 0.01f)
            TransitionTo(FighterState.Moving);
        else if (currentState == FighterState.Moving && Mathf.Abs(moveInput.x) < 0.01f)
            TransitionTo(FighterState.Idle);
    }

    public IEnumerator HitStopCoroutine(float duration)
    {
        isHitStopped = true;

        // Freeze physics safely using the class-scoped tracking variable
        hitStopStoredVelocity = rb.linearVelocity;
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;

        yield return new WaitForSecondsRealtime(duration);

        // Restore physics
        rb.isKinematic = false;
        rb.linearVelocity = hitStopStoredVelocity;

        isHitStopped = false;
    }

    public void ApplyHitStop(float duration)
    {
        if (isHitStopped) return;
        StartCoroutine(HitStopCoroutine(duration));
    }

    // -------------------------------------------------------
    // Facing
    // -------------------------------------------------------

    void FaceOpponent()
    {
        if (currentState == FighterState.Knockback || currentState == FighterState.KO) return;

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

    public void RegisterJuggleHit()
    {
        hasBeenJuggled = true; // close the juggle window after one hit
    }

    // -------------------------------------------------------
    // State handlers
    // -------------------------------------------------------

    void HandleIdle()
    {
        if (lightAttackAction.WasPressedThisFrame())
        {
            // If opponent is launched, count this as the juggle hit
            if (opponent != null && opponent.CanBeJuggled)
                opponent.RegisterJuggleHit();

            StartAttack(0);
        }

        if (heavyAttackAction.WasPressedThisFrame()) StartAttack(1);

        // Launcher — index 2, only usable grounded
        if (specialAction.WasPressedThisFrame() && isGrounded) StartAttack(2);

        if (blockAction.WasPressedThisFrame()) TransitionTo(FighterState.Blocking);
    }

    void HandleMoving()
    {
        if (lightAttackAction.WasPressedThisFrame())
        {
            // If opponent is launched, count this as the juggle hit
            if (opponent != null && opponent.CanBeJuggled)
                opponent.RegisterJuggleHit();

            StartAttack(0);
        }

        if (heavyAttackAction.WasPressedThisFrame()) StartAttack(1);

        // Launcher — index 2, only usable grounded
        if (specialAction.WasPressedThisFrame() && isGrounded) StartAttack(2);

        if (blockAction.WasPressedThisFrame()) TransitionTo(FighterState.Blocking);
    }

    void HandleAttacking()
    {
        if (isHitStopped) return;

        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);

        attackTimer -= Time.deltaTime;
        hitboxTimer -= Time.deltaTime;

        if (hitboxTimer <= 0f)
            DisableHitbox();

        if (attackTimer <= 0f)
            OnAttackEnd();
    }

    void HandleKnockback()
    {
        if (isHitStopped) return;

        knockbackTimer -= Time.deltaTime;
        if (knockbackTimer <= 0f)
            TransitionTo(FighterState.Idle);
    }

    void HandleBlocking()
    {
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        if (!blockAction.IsPressed())
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

        currentAttackIndex = attackIndex;
        AttackData attack = attacks[attackIndex];

        attackTimer = attack.attackDuration;
        hitboxTimer = attack.hitboxActiveTime;

        TransitionTo(FighterState.Attacking);
        EnableHitboxForAttack(attackIndex);
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

        // Detect launcher by high upward force
        bool isLauncherHit = attack.knockbackForce.y >= 10f;

        if (opponent != null)
        {
            float direction = transform.position.x > opponent.transform.position.x ? 1f : -1f;
            Vector3 knockbackVelocity = new Vector3(
                attack.knockbackForce.x * direction,
                attack.knockbackForce.y,
                0f
            );

            if (isHitStopped)
                hitStopStoredVelocity = knockbackVelocity;
            else
                rb.linearVelocity = knockbackVelocity;
        }

        if (isLauncherHit)
        {
            isLaunched = true;
            hasBeenJuggled = false; // reset juggle counter on fresh launch
        }

        TriggerScreenShake(attack.screenShakeMagnitude);

        if (currentHealth <= 0f)
            TransitionTo(FighterState.KO);
        else
        {
            knockbackTimer = data.knockbackDuration;
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
                    || newState == FighterState.KO
                    || newState == FighterState.Idle;

            case FighterState.Knockback:
                return newState == FighterState.Idle
                    || newState == FighterState.KO;

            default:
                return true;
        }
    }

    public void TriggerScreenShake(float magnitude)
    {
        if (impulseSource == null) return;
        impulseSource.GenerateImpulse(magnitude);
    }
}