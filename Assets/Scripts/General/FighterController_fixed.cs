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

    [Header("Combo")]
    [Tooltip("Seconds after an attack where the next combo input is accepted")]
    public float comboWindowDuration = 0.4f;
    [Tooltip("Seconds of inactivity before combo resets to step 0")]
    public float comboTimeoutDuration = 1.2f;

    // Public property — used by opponent to check juggle availability
    public bool CanBeJuggled => isLaunched && !hasBeenJuggled;

    // Private — state
    private float maxHealth;
    public bool facingRight = true;   // public so special ability scripts can read facing direction
    private Vector2 moveInput;

    // Private — grounded
    public bool isGrounded;

    // Private — timers
    private float jumpBufferTimer;
    public float knockbackTimer;
    public float attackTimer;         // public so GuitarSpecial can set it
    private float hitboxTimer;
    private int currentAttackIndex = -1;
    private bool isHitStopped = false;
    private Coroutine hitStopRoutine;
    private float hitStopEndTime;

    // Private — combat/animation tracking
    private Vector3 hitStopStoredVelocity;
    private bool wasBlockingWhenHit = false;

    // Private — launcher
    private bool isLaunched = false;
    private bool hasBeenJuggled = false;

    // Private — combo
    private int comboStep = 0;
    private float comboWindowTimer = 0f;
    private float comboTimeoutTimer = 0f;
    private bool comboInputBuffered = false;
    private int bufferedComboInput = -1;

    // Private — special ability
    private SpecialAbilityBase specialAbility;

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

        if (hitStopRoutine != null)
            StopCoroutine(hitStopRoutine);
    }

    private void Start()
    {
        maxHealth = data.maxHealth;
        currentHealth = maxHealth;

        if (hitbox != null)
            hitbox.gameObject.SetActive(false);

        // Cache any special ability component on this GameObject
        specialAbility = GetComponent<SpecialAbilityBase>();

        if (playerNumber == 1)
        {
            facingRight = true;
            transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        }
        else
        {
            facingRight = false;
            transform.rotation = Quaternion.Euler(0f, -90f, 0f);
        }
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

        // Global animator parameters updated continuously
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.SetBool("IsGrounded", isGrounded);
            animator.SetBool("IsMoving", currentState == FighterState.Moving);
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
        if (currentState == FighterState.KO)
        {
            moveInput = Vector2.zero;
            return;
        }

        moveInput = moveAction.ReadValue<Vector2>();

        if (jumpAction.WasPressedThisFrame())
            jumpBufferTimer = jumpBufferTime;
    }

    // -------------------------------------------------------
    // Timers
    // -------------------------------------------------------

    void UpdateTimers()
    {
        // Jump buffer
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

        // Combo window countdown
        if (comboWindowTimer > 0f)
            comboWindowTimer -= Time.deltaTime;

        // Combo timeout — reset if player waits too long between hits
        if (comboStep > 0)
        {
            comboTimeoutTimer -= Time.deltaTime;
            if (comboTimeoutTimer <= 0f)
                ResetCombo();
        }
    }

    // -------------------------------------------------------
    // Grounded
    // -------------------------------------------------------

    private void OnCollisionStay(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f)
            {
                isGrounded = true;
                isLaunched = false;
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
        if (!isGrounded)
        {
            if (rb.linearVelocity.y > 0)
                rb.AddForce(Physics.gravity * (gravityMultiplier - 1f), ForceMode.Acceleration);
            else if (rb.linearVelocity.y < 0)
                rb.AddForce(Physics.gravity * ((gravityMultiplier * fallMultiplier) - 1f), ForceMode.Acceleration);
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

    // -------------------------------------------------------
    // Hit-stop
    // -------------------------------------------------------

    public IEnumerator HitStopCoroutine(float duration)
    {
        isHitStopped = true;

        if (rb != null)
        {
            hitStopStoredVelocity = rb.linearVelocity;
            rb.linearVelocity = Vector3.zero;
        }

        hitStopEndTime = Time.realtimeSinceStartup + duration;

        while (Time.realtimeSinceStartup < hitStopEndTime)
            yield return null;

        if (rb != null)
            rb.linearVelocity = hitStopStoredVelocity;

        isHitStopped = false;
        hitStopRoutine = null;
    }

    public void ApplyHitStop(float duration)
    {
        if (rb == null || duration <= 0f) return;

        if (isHitStopped)
        {
            hitStopEndTime = Mathf.Max(hitStopEndTime, Time.realtimeSinceStartup + duration);
            return;
        }

        if (hitStopRoutine == null)
            hitStopRoutine = StartCoroutine(HitStopCoroutine(duration));
    }

    // -------------------------------------------------------
    // Facing
    // -------------------------------------------------------

    void FaceOpponent()
    {
        ForceDefaultFacing();
        // 1. Keep your existing guards
        if (currentState == FighterState.Knockback
            || currentState == FighterState.KO
            || currentState == FighterState.Attacking) return;

        if (opponent == null) return;

        // 2. NEW FIX: If this is the absolute beginning of the match and players are too close 
        // or just spawning in, don't let a frame glitch accidentally flip them.
        if (Mathf.Abs(opponent.transform.position.x - transform.position.x) < 0.1f)
        {
            // Enforce their absolute default rotations instead of flipping them
            ForceDefaultFacing();
            return;
        }

        // 3. Normal gameplay tracking logic continues safely below:
        bool opponentIsRight = opponent.transform.position.x > transform.position.x;
        if (opponentIsRight && !facingRight) Flip();
        else if (!opponentIsRight && facingRight) Flip();
    }

    // Helper method to keep code clean and maintain exact spawn orientation
    void ForceDefaultFacing()
    {
        if (playerNumber == 1)
        {
            facingRight = true;
            transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        }
        else if (playerNumber == 2)
        {
            facingRight = false;
            transform.rotation = Quaternion.Euler(0f, -90f, 0f);
        }
    }

    void Flip()
    {
        facingRight = !facingRight;
        float yRotation = facingRight ? 90f : -90f;
        transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
    }

    // -------------------------------------------------------
    // Launcher juggle
    // -------------------------------------------------------

    public void RegisterJuggleHit()
    {
        hasBeenJuggled = true;
    }

    // -------------------------------------------------------
    // State handlers
    // -------------------------------------------------------

    void HandleIdle()
    {
        if (lightAttackAction.WasPressedThisFrame()) TryComboAttack(0);
        if (heavyAttackAction.WasPressedThisFrame()) TryComboAttack(1);

        if (specialAction.WasPressedThisFrame() && isGrounded)
        {
            if (specialAbility != null) specialAbility.TryActivate();
            else StartAttack(2); // fallback to launcher if no special assigned
        }

        if (blockAction.WasPressedThisFrame()) TransitionTo(FighterState.Blocking);
    }

    void HandleMoving()
    {
        if (lightAttackAction.WasPressedThisFrame()) TryComboAttack(0);
        if (heavyAttackAction.WasPressedThisFrame()) TryComboAttack(1);

        if (specialAction.WasPressedThisFrame() && isGrounded)
        {
            if (specialAbility != null) specialAbility.TryActivate();
            else StartAttack(2);
        }

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

        // Open the cancel window in the last portion of the attack
        // The window opens at 55% through the attack duration
        if (attacks != null && currentAttackIndex >= 0 && currentAttackIndex < attacks.Length)
        {
            float cancelWindowStart = attacks[currentAttackIndex].attackDuration * 0.55f;
            if (attackTimer <= cancelWindowStart && comboWindowTimer <= 0f)
                comboWindowTimer = comboWindowDuration;
        }

        // If a combo input was buffered during the window, execute it when attack finishes
        if (comboWindowTimer > 0f && comboInputBuffered && attackTimer <= 0f)
        {
            comboInputBuffered = false;
            int buffered = bufferedComboInput;
            bufferedComboInput = -1;
            OnAttackEnd();
            ExecuteComboStep(buffered);
            return;
        }

        if (attackTimer <= 0f)
        {
            ResetCombo();
            OnAttackEnd();
        }
    }

    void HandleBlocking()
    {
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        if (!blockAction.IsPressed())
            TransitionTo(FighterState.Idle);
    }

    void HandleKnockback()
    {
        if (isHitStopped) return;
        knockbackTimer -= Time.deltaTime;
        if (knockbackTimer <= 0f)
            TransitionTo(FighterState.Idle);
    }

    void HandleKO()
    {
        if (isHitStopped) return;
        if (isGrounded)
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
    }

    // -------------------------------------------------------
    // Jump
    // -------------------------------------------------------

    void Jump()
    {
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, data.jumpForce, 0f);

        if (animator != null && animator.runtimeAnimatorController != null)
            animator.SetTrigger("Jump");
    }

    // -------------------------------------------------------
    // Combo system
    // -------------------------------------------------------

    void TryComboAttack(int inputType) // 0 = light, 1 = heavy
    {
        // If currently attacking, buffer the input for the cancel window
        if (currentState == FighterState.Attacking)
        {
            if (comboWindowTimer > 0f)
            {
                comboInputBuffered = true;
                bufferedComboInput = inputType;
            }
            return;
        }

        // Not attacking — execute immediately
        ExecuteComboStep(inputType);
    }

    void ExecuteComboStep(int inputType)
    {
        // Juggle check — if opponent is launched, register the juggle hit
        if (inputType == 0 && opponent != null && opponent.CanBeJuggled)
            opponent.RegisterJuggleHit();

        // Enforce strict sequence: L(0) → L(0) → H(1)
        switch (comboStep)
        {
            case 0: // fresh — only light starts the combo
                if (inputType == 0)
                {
                    comboStep = 1;
                    StartAttack(0);
                }
                else
                {
                    // Standalone heavy — no combo context
                    ResetCombo();
                    StartAttack(1);
                }
                break;

            case 1: // after first light — second light continues
                if (inputType == 0)
                {
                    comboStep = 2;
                    StartAttack(0);
                }
                else
                {
                    // Wrong input — reset and treat as standalone
                    ResetCombo();
                    StartAttack(1);
                }
                break;

            case 2: // after second light — heavy completes the combo
                if (inputType == 1)
                {
                    comboStep = 3;
                    StartAttack(1); // heavy ender
                }
                else
                {
                    // Pressed light again at step 2 — restart combo from step 1
                    comboStep = 1;
                    StartAttack(0);
                }
                break;

            case 3: // combo complete — reset and start fresh
                ResetCombo();
                ExecuteComboStep(inputType);
                break;
        }

        // Reset timeout on every step
        comboTimeoutTimer = comboTimeoutDuration;
    }

    void ResetCombo()
    {
        comboStep = 0;
        comboWindowTimer = 0f;
        comboTimeoutTimer = 0f;
        comboInputBuffered = false;
        bufferedComboInput = -1;
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

        bool wasBlocking = (currentState == FighterState.Blocking);

        float damage = attack.damage;
        if (wasBlocking)
            damage *= attack.blockDamageMultiplier;

        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0f);

        bool isLauncherHit = attack.knockbackForce.y >= 10f;

        if (rb != null)
        {
            float direction = opponent != null
                ? (transform.position.x > opponent.transform.position.x ? 1f : -1f)
                : (facingRight ? 1f : -1f);

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
            hasBeenJuggled = false;
        }

        TriggerScreenShake(attack.screenShakeMagnitude);

        if (currentHealth <= 0f)
        {
            TransitionTo(FighterState.KO);
        }
        else
        {
            wasBlockingWhenHit = wasBlocking;
            knockbackTimer = data.knockbackDuration;
            TransitionTo(FighterState.Knockback);
        }
    }

    // -------------------------------------------------------
    // Stun — used by projectiles and special moves
    // Reuses Knockback state with zero velocity so all input is locked
    // -------------------------------------------------------

    public void ApplyStun(float duration)
    {
        if (currentState == FighterState.KO) return;
        knockbackTimer = duration;
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        TransitionTo(FighterState.Knockback);
    }

    // -------------------------------------------------------
    // Hitboxes
    // -------------------------------------------------------

    public void EnableHitboxForAttack(int attackIndex)
    {
        if (hitbox == null || attacks == null || attackIndex >= attacks.Length) return;

        AttackData attack = attacks[attackIndex];
        Vector3 offset = attack.hitboxOffset;

        if (playerNumber == 1)
        {
            if (!facingRight) offset.x *= -1f;
        }
        else if (playerNumber == 2)
        {
            if (facingRight) offset.x *= -1f;
        }

        hitbox.center = offset;
        hitbox.size = attack.hitboxSize;
        hitbox.GetComponent<Hitbox>().attackData = attack;

        StartCoroutine(ActivateHitboxWithDelay(attack.hitboxDelay, attack.hitboxActiveTime));
    }

    private IEnumerator ActivateHitboxWithDelay(float delay, float activeTime)
    {
        yield return new WaitForSeconds(delay);
        hitbox.gameObject.SetActive(true);
        yield return new WaitForSeconds(activeTime);
        hitbox.gameObject.SetActive(false);
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
            case FighterState.Idle:
                animator.SetTrigger("Idle");
                break;

            case FighterState.Moving:
                // Handled continuously by IsMoving bool in Update
                break;

            case FighterState.Attacking:
                animator.SetInteger("AttackIndex", currentAttackIndex);
                animator.SetTrigger("Attack");
                break;

            case FighterState.Blocking:
                animator.SetBool("IsBlocking", true);
                break;

            case FighterState.Knockback:
                if (wasBlockingWhenHit)
                    animator.SetTrigger("BlockHit");
                else
                    animator.SetTrigger("Knockback");
                break;

            case FighterState.KO:
                animator.SetTrigger("KO");
                if (actions != null)
                {
                    if (playerNumber == 1) actions.Fighter.Disable();
                    else actions.FighterP2.Disable();
                }
                break;
        }
    }

    void OnExitState(FighterState state)
    {
        if (state == FighterState.Knockback)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            wasBlockingWhenHit = false;
        }

        if (state == FighterState.Blocking)
            if (animator != null && animator.runtimeAnimatorController != null)
                animator.SetBool("IsBlocking", false);
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

    // -------------------------------------------------------
    // Camera
    // -------------------------------------------------------

    public void TriggerScreenShake(float magnitude)
    {
        if (impulseSource == null) return;
        impulseSource.GenerateImpulse(magnitude);
    }
}