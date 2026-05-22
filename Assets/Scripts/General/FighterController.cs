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
    public BoxCollider hitbox;       // single hitbox — reshaped per attack
    public Transform hurtboxRoot;   // always-on hurtbox lives here
    public AttackData[] attacks;    // [0] light, [1] heavy, [2] launcher — assign in Inspector

    [Header("Opponent")]
    public FighterController opponent;

    [Header("Stats")]
    public FighterData data;
    public float currentHealth;

    // Private
    private float maxHealth;
    private bool isGrounded;
    private bool facingRight = true;
    private Vector2 moveInput;

    // Input actions — cached from PlayerInput on the same GameObject
    private PlayerInput playerInput;
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

    private void Awake()
    {
        var actions = new FighterInputActions();

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
        Debug.Log($"Move: {moveInput} | Jump: {jumpAction.WasPressedThisFrame()} | Light: {lightAttackAction.WasPressedThisFrame()} | IsGrounded: {isGrounded}");
        CheckGrounded();
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

    // -------------------------------------------------------
    // Input
    // -------------------------------------------------------

    void ReadInput()
    {
        moveInput = moveAction.ReadValue<Vector2>();
    }

    // -------------------------------------------------------
    // Ground check
    // -------------------------------------------------------

    void CheckGrounded()
    {
        isGrounded = Mathf.Abs(rb.linearVelocity.y) < 0.05f;
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
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);

        if (moveInput.x != 0f)
            TransitionTo(FighterState.Moving);

        if (jumpAction.WasPressedThisFrame() && isGrounded)
            Jump();

        if (lightAttackAction.WasPressedThisFrame())
            StartAttack(0);

        if (heavyAttackAction.WasPressedThisFrame())
            StartAttack(1);

        if (blockAction.WasPressedThisFrame())
            TransitionTo(FighterState.Blocking);
    }

    void HandleMoving()
    {
        rb.linearVelocity = new Vector3(moveInput.x * data.moveSpeed, rb.linearVelocity.y, 0f);

        if (moveInput.x == 0f)
            TransitionTo(FighterState.Idle);

        if (jumpAction.WasPressedThisFrame() && isGrounded)
            Jump();

        if (lightAttackAction.WasPressedThisFrame())
            StartAttack(0);

        if (heavyAttackAction.WasPressedThisFrame())
            StartAttack(1);

        if (blockAction.WasPressedThisFrame())
            TransitionTo(FighterState.Blocking);
    }

    void HandleAttacking()
    {
        // Horizontal movement locked during attacks
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        // Attack completion driven by Animation Event calling OnAttackEnd()
    }

    void HandleBlocking()
    {
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);

        if (!blockAction.IsPressed())
            TransitionTo(FighterState.Idle);
    }

    void HandleKnockback()
    {
        // Knockback velocity applied by TakeDamage
        // Animation Event on knockback clip calls TransitionTo(Idle) when recovery ends
    }

    void HandleKO()
    {
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        // RoundManager watches for this state to end the round
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
        // Animation Event on the attack clip calls EnableHitboxForAttack(attackIndex)
        // and OnAttackEnd() — those drive the rest
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

        // Apply knockback force away from attacker
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
            TransitionTo(FighterState.Knockback);

        // Hit-stop and screen shake plug in here next step
    }

    // -------------------------------------------------------
    // Combat — hitbox control (called by Animation Events)
    // -------------------------------------------------------

    public void EnableHitboxForAttack(int attackIndex)
    {
        if (hitbox == null || attacks == null || attackIndex >= attacks.Length) return;

        AttackData attack = attacks[attackIndex];
        hitbox.center = attack.hitboxOffset;
        hitbox.size = attack.hitboxSize;
        hitbox.GetComponent<Hitbox>().attackData = attack;
        hitbox.gameObject.SetActive(true);
    }

    public void DisableHitbox()
    {
        if (hitbox != null)
            hitbox.gameObject.SetActive(false);
    }

    // Called by Animation Event at the end of an attack clip
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
        // Reset velocity when leaving knockback so the fighter doesn't slide
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