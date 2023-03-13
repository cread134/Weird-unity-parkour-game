using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyBrain : MonoBehaviour, I_DamageAble,I_Knockback
{

    private NavMeshPath path;

    public bool showGizmos = false;
    private Vector3 lasttargetPosition;
    private Vector3 targetVelocity;

    public GameObject target;

    private Rigidbody rb;
    public Animator animator;

    [Header("Audio settings")]
    public AudioSource a_Source;
    [Space]
    public AudioClip[] deathSounds;
    public AudioClip[] attackSounds;

    [Header("Life settings")]
    public float deathForceMultiplier = 0.3f;
    public float maxHealth = 50f;
    private float curHealth;
    [Space]
    public string hitAnimation;
    public string knockbackAnimation;
    [Space]
    public float knockBackMultiplier = 1f;
    [Space]
    public ParticleSystem dieParticles;

    [Header("Movement")]
    public LayerMask groundedMask;
    public float getUpTime = 2f;
    public bool lookTowardsTarget = true;
    public float lookSpeed = 8f;
    public bool blockMoveOnAttack = true;
    public float moveRange = 1f;
    public bool canMove = true;
    public MoveType moveType;
    public float acceleration = 8f;
    public enum MoveType {chaseDown, randomArea }

    [Header("Random area settings")]
    public float moveSetInterval = 5f;
    public float randWalkRadius = 8f;

    [Space]
    public float moveSpeed = 1f;

    [Header("Navigation")]
    public float pathThreshold = 1f;
    public float pathRefreshInterval = 0.5f;

    [Header("Ragdoll settings")]
    public float ragdollPointAffectorDistance = 0.5f;
    public float ragdollPointAffectorForce = 5f;
    public Collider[] ragdollColliders;
    public Rigidbody[] ragdollRigidBodies;

    [Header("Attack settings")]
    public Transform attackOrigin;
    public float attackRange = 1.5f;
    [Space]
    public float attackTime = 0.5f;
    public float attackInterval = 3f;

    public enum AttackType { ranged, melee}

    [Header("Ranged Settings")]
    public LayerMask rangedCheckMask;
    public ParticleSystem rangedParticle;
    public GameObject projectile;
    public Transform projectileSpawnPosition;
    public Transform checkFromPosition;
    public float predictionMultiplier = 1f;
    public float rangedDamage;
    [Space]
    public float projectileLaunchVelocity = 1f; //units per second
    [Space]
    public float accuracy = 0.3f;
    public float rangedPrepTime = 0.5f;
    public int attacksPerSet = 1;

    [Header("Melee Settings")]
    public float meleeDamage;
    public AttackType attackType;
    public LayerMask meleeLayermask;
    public float meleeRadius = 0.5f;
    public float timeToRegisterAttack = 0.3f;
    public float timeToLunge = 0.4f;
    [Space]
    public float meleeRange = 1f;
    [Space]
    public float meleeLunge = 10f;


    private float lastAttack;
    public void SetTarget(GameObject settarget)
    {
        target = settarget;
    }

    // Start is called before the first frame update
    void Awake()
    {
        path = new NavMeshPath();

        //disable ragdoll colliders
        foreach (Collider col in ragdollColliders)
        {
            col.enabled = false;    
        }
        foreach (Rigidbody rigid in ragdollRigidBodies)
        {
            rigid.isKinematic = true;
        }

        lastPosition = transform.position;
        curHealth = maxHealth;
        rb = GetComponent<Rigidbody>();
    }

    private GameHandler gameHandler;
    private bool spawned = false;
    public void Spawn(GameHandler gHandler, float spawnDuration)
    {
        gameHandler = gHandler;
        StartCoroutine(Spawning(spawnDuration));
    }

    IEnumerator Spawning(float duration)
    {
        yield return new WaitForSeconds(duration);
        spawned = true;
    }

    private Vector2 velocity;
    private Vector2 lastVelocity;
    private Vector2 smoothDeltaPosition;
    private Vector3 lastPosition;


    private bool playerDead = false;
    // Update is called once per frame
    void Update()
    {
        if (!spawned || dead || playerDead) return;

        //check for attack
        distToTarget = Vector3.Distance(target.transform.position, transform.position);

        if(distToTarget <= attackRange && Time.time > lastAttack && !attacking && !isStunned && !isGettingUp)
        {
            switch (attackType)
            {
                case AttackType.ranged:
                    if (CheckForRangedAttack())
                    {
                        Debug.Log("Enemy Ranged attack");
                        DoRangedAttack();
                    }
                    break;
                case AttackType.melee:
                DoMeleeAttack();
                    break;
            }
        }

        //look towards
        if (lookTowardsTarget && !attacking && !isStunned && !isGettingUp)
        {
            Vector3 lookDir = target.transform.position - transform.position;
            lookDir.y = 0f;

            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(lookDir), lookSpeed * Time.deltaTime);
        }

        //calculate animation movement
         Vector3  moveVector = transform.position - lastPosition;

        float dx = Vector3.Dot(transform.right, moveVector);
        float dy = Vector3.Dot(transform.forward, moveVector);
        Vector2 deltaPosition = new Vector2(dx, dy);

        // Low-pass filter the deltaMove
        float smooth = Mathf.Min(1.0f, Time.deltaTime / 0.15f);
        smoothDeltaPosition = Vector2.Lerp(smoothDeltaPosition, deltaPosition, smooth);

        if (Time.deltaTime > 1e-5f)
        {
            velocity = smoothDeltaPosition / Time.deltaTime;
        }

        velocity = velocity.normalized;
        //we double smmothh
        Vector2 smoothed = Vector2.Lerp(lastVelocity, velocity, 3f * Time.deltaTime);
        lastVelocity = velocity;

        animator.SetFloat("MoveX", smoothed.x);
        animator.SetFloat("MoveY", smoothed.y);

        lastPosition = transform.position;

    }
    void FixedUpdate()
    {
        if (!spawned || dead || playerDead) return;

        curGrounded = Grounded();
        //check for movement
        bool allowMove = !blockMove;

        if (blockMoveOnAttack && attacking)
        {
            allowMove = false;
        }

        if (canMove && allowMove && !isStunned && !isGettingUp && !pauseMovement)
        {
            switch (moveType)
            {
                case MoveType.chaseDown:
                    //set up navigation

                    float distancetoTarg = Vector3.Distance(target.transform.position, transform.position);
                    if (distancetoTarg > moveRange)
                    {
                        animator.SetBool("Moving", true);
                        if (distancetoTarg > pathThreshold && Time.time > lastPath)
                        {
                            GenerateNMeshPath(target.transform.position);
                        }
                        if (distancetoTarg < pathThreshold)
                        {
                            movetarget = target.transform.position;
                        }
                        else
                        {
                            movetarget = currentPathPositions[curIndex];
                            //we go down the chain of points 
                            if (Vector3.Distance(transform.position, movetarget) < 0.5f)
                            {
                                curIndex++;
                            }
                        }

                        //perform movement
                        ChaseDownMovement();
                    }
                    else
                    {
                        animator.SetBool("Moving", false);
                    }
                    break;
                case MoveType.randomArea:

                    animator.SetBool("Moving", true);

                    RandomAreaMovement();
                    break;
            }
        }
        else
        {
            animator.SetBool("Moving", false);
        }
        

        //caclulate velocity
        targetVelocity = target.transform.position - lasttargetPosition;
        lasttargetPosition = target.transform.position;

        //handling grounding
        if(curGrounded == true && lastGrounded == false)
        {
            HitGround();
        }

        lastGrounded = Grounded();
    }

    private bool curGrounded = false;
    private bool lastGrounded = false;
    bool Grounded()
    {
        if (Physics.Raycast(transform.position, Vector3.down, 0.3f, groundedMask))
        {
            return true;
        }
        return false;
    }

    private float lastSetTime;
    private Vector3 movetarget;
    private int curIndex = 0;
    private int pathCornerCount;
    private float lastPath;
    private Vector3[] currentPathPositions = new Vector3[15];
    void GenerateNMeshPath(Vector3 inputTarget)
    {
        if (!Grounded()) return;

        //we generate path
        lastPath = Time.time + pathRefreshInterval;
        NavMesh.CalculatePath(transform.position, inputTarget, NavMesh.AllAreas, path);
        path.GetCornersNonAlloc(currentPathPositions);
        pathCornerCount = path.corners.Length;
        //set the values
        curIndex = 0;
        movetarget = currentPathPositions[0];

        if (showGizmos)
        {
            for (int i = 1; i < currentPathPositions.Length; i++)
            {
                Debug.DrawLine(currentPathPositions[i], currentPathPositions[i - 1], Color.green, pathRefreshInterval);
            }
        }
    }

    void HitGround()
    {
      
    }

    private bool blockMove = false;
    float distToTarget;

    private Vector3 followMoveVector;
    void ChaseDownMovement()
    {     
        if (distToTarget > moveRange)
        {
            Vector3 moveDir = (movetarget - transform.position).normalized;

            Vector3 moveVector = transform.position + (moveDir * moveSpeed * Time.deltaTime);

            followMoveVector = Vector3.Lerp(transform.position, moveVector, acceleration * Time.deltaTime);
            rb.MovePosition(followMoveVector);
        }
        else
        {

        }
    }

    void RandomAreaMovement()
    {

        if(Time.time > lastSetTime)
        {
            lastSetTime = Time.time + moveSetInterval;

            //get random point
            Vector3 randomDirection = Random.insideUnitSphere * randWalkRadius;
            if (Vector3.Distance(transform.position, target.transform.position) < attackRange)
            {
                randomDirection += target.transform.position;
            }
            else {
                randomDirection += transform.position;
            }

            //sample pos
            NavMeshHit hit;
            NavMesh.SamplePosition(randomDirection, out hit, randWalkRadius, NavMesh.AllAreas);
            Vector3 finalPosition = hit.position;
            GenerateNMeshPath(finalPosition);
        }

        //if we hit the position right away we don't bug out
        if (curIndex < pathCornerCount)
        {
            movetarget = currentPathPositions[curIndex];
            //we go down the chain of points 
            if (Vector3.Distance(transform.position, movetarget) < 0.5f)
            {
                curIndex++;
            }

            //move towards point
            Vector3 moveDir = (movetarget - transform.position).normalized;

            Vector3 moveVector = transform.position + (moveDir * moveSpeed * Time.deltaTime);

            followMoveVector = Vector3.Lerp(transform.position, moveVector, acceleration * Time.deltaTime);
            rb.MovePosition(followMoveVector);
        }
    }

    #region damage
    private float lastDamage;
    public void TakeDamage(float amount, Vector3 point, Vector3 direction)
    {
        if (dead) return;
        lastDamage = amount;
        curHealth = Mathf.Clamp(curHealth - amount, 0f, maxHealth);
        if(curHealth <= 0)
        {
            Die(amount, point, direction);
        }
        
        Debug.Log("Enemy Died");
    }

    public void DisruptAttack()
    {
        if (attacking && attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attacking = false;
            lastAttack = Time.time + attackInterval;
        }

        if (movementBlockCoroutine != null)
        {
            StopCoroutine(movementBlockCoroutine);
        }

        movementBlockCoroutine = StartCoroutine(BlockMovement(1.5f));
        //animation
        animator.ResetTrigger("MeleeAttack");
        animator.ResetTrigger("RangedAttack");
        animator.SetBool("DoingRangedAttack", false);

        rb.velocity = Vector3.zero;
        animator.Play(hitAnimation, 0, 0.15f);
    }

    #endregion

    #region ranged attacks
    bool CheckForRangedAttack()
    {
        Vector3 targPos = target.transform.position;
        targPos.y += 1.5f;
        if (Physics.Raycast(checkFromPosition.position, targPos - projectileSpawnPosition.position, Mathf.Clamp(Vector3.Distance(targPos, projectileSpawnPosition.position) - 3f,0f, 100f), rangedCheckMask))
        {
            return false;
        }

        return true;
    }


    Vector3 CalculateTargetPosition()
    {
        //do projectile motion

        float targetdistance = Vector3.Distance(target.transform.position, projectileSpawnPosition.position);
         //theta = cos^-1( x/t * v0)

        float tVal = targetdistance / projectileLaunchVelocity;

        Vector3 velAdd = target.transform.position + (targetVelocity * predictionMultiplier * tVal);
        velAdd.y += 0.9f;
        velAdd.y = Mathf.Clamp(velAdd.y, transform.position.y, transform.position.y + 3f);
        return velAdd;
    }

    void DoRangedAttack()
    {
        animator.SetTrigger("RangedAttack");

        attackCoroutine = StartCoroutine(RangedAttackIenumerator());
    }

    IEnumerator RangedAttackIenumerator()
    {
        attacking = true;
        animator.SetBool("DoingRangedAttack", true);
        yield return new WaitForSeconds(rangedPrepTime);
        for (int i = 0; i < attacksPerSet; i++)
        {
            if(attacksPerSet > 0)
            {
                animator.SetTrigger("RangedAttackInterval");
            }

            if(rangedParticle != null)
            {
                rangedParticle.Play();
            }

            if (attackSounds != null)
            {
                PlayAttackSound();
            }

            CreateProjectile();
            yield return new WaitForSeconds(attackTime);
        }
        animator.SetBool("DoingRangedAttack", false);
        attacking = false;
        lastAttack = Time.time + attackInterval;
    }

    void CreateProjectile()
    {
        Vector3 targPos = CalculateTargetPosition();
        
        Vector3 baseDirection = projectileSpawnPosition.position - targPos;

        Debug.DrawRay(projectileSpawnPosition.position, baseDirection, Color.green, 5f);

        GameObject projectileInstance = Instantiate(projectile, projectileSpawnPosition.position, Quaternion.LookRotation(baseDirection));
        EnemyProjectile e_Projectile = projectileInstance.GetComponent<EnemyProjectile>();

        e_Projectile.SpawnProjectile(projectileLaunchVelocity, rangedDamage, baseDirection);
    }
    #endregion

    void PlayAttackSound()
    {
        AudioClip targClip = attackSounds[Random.Range(0, attackSounds.Length)];
        a_Source.PlayOneShot(targClip);
    }

    #region melee attacking
    private bool attacking = false;
    void DoMeleeAttack()
    {
        attacking = true;

        animator.SetTrigger("MeleeAttack");

        attackCoroutine = StartCoroutine(MeleeIenumerator());
    }

    void DoLunge()
    {
        Vector3 lungVector = (target.transform.position - transform.position).normalized * meleeLunge;
        rb.AddForce(lungVector, ForceMode.Impulse);
    }

    public bool IsDead()
    {
        return dead;
    }

    private Coroutine attackCoroutine;
    IEnumerator MeleeIenumerator()
    {
        yield return new WaitForSeconds(timeToLunge);
        DoLunge();
        yield return new WaitForSeconds(timeToRegisterAttack - timeToLunge);
        RegisterMeleeAttack();
        if (attackSounds != null)
        {
            PlayAttackSound();
        }
        yield return new WaitForSeconds(attackTime - timeToRegisterAttack - timeToLunge);
        attacking = false;
        lastAttack = Time.time + attackInterval;
    }
    public void RegisterMeleeAttack()
    {
        RaycastHit hit;
        if (Physics.SphereCast(attackOrigin.position, meleeRadius,transform.forward, out hit, meleeRange, meleeLayermask))
        {
            if (hit.transform.gameObject.GetComponent<I_DamageAble>() != null)
            {
                hit.transform.gameObject.GetComponent<I_DamageAble>().TakeDamage(meleeDamage, hit.point, transform.forward);
            }
        }
    }

    #endregion

    #region death
    private bool dead;
    void Die(float damage,Vector3 point, Vector3 direction)
    {
        dead = true;
        gameHandler.EnemyDied(this);
        EnableRagdoll(point, direction, lastDamage * deathForceMultiplier);
        rb.AddForce(direction.normalized * lastDamage * deathForceMultiplier, ForceMode.Impulse);

        //particles
        dieParticles.transform.position = point;
        dieParticles.Play();

        //play sound
        AudioClip targClip = deathSounds[Random.Range(0, deathSounds.Length)];
        PlaySound(targClip);

        StartCoroutine(ToDestroy());
    }
    IEnumerator ToDestroy()
    {
        yield return new WaitForSeconds(5f);
        Destroy(gameObject);
    }
    #endregion

    #region ragdoll settings
    void EnableRagdoll(Vector3 point, Vector3 direction, float force)
    {

        animator.enabled = false;
        //we do the ragdoll
        //disable ragdoll colliders
        foreach (Collider col in ragdollColliders)
        {
            col.enabled = true;
        }
        foreach (Rigidbody rigid in ragdollRigidBodies)
        {
            rigid.isKinematic = false;
            //adds force yo
            float dist = Vector3.Distance(rigid.transform.position, point);
            if (dist <= ragdollPointAffectorDistance)
            {               
                rigid.AddExplosionForce(ragdollPointAffectorForce, point, ragdollPointAffectorDistance, ragdollPointAffectorForce, ForceMode.Impulse);
            }
        }

    }

    void DeactivateRagdoll()
    {
        
    }
    void GetUpFromRagdoll()
    {
        DeactivateRagdoll();
        StartCoroutine(GetUp());
    }
    private bool isGettingUp;
    IEnumerator GetUp()
    {
        isGettingUp = true;
        yield return new WaitForSeconds(getUpTime);
        isGettingUp = false;
    }

    #endregion

    #region effectors
    public void KnockBack(float amount, Vector3 point, Vector3 direction)
    {
        if(knockBackIenumerator != null)
        {
            StopCoroutine(knockBackIenumerator);
        }
        if(attacking && attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attacking = false;
            lastAttack = Time.time + attackInterval;
        }
        rb.velocity = Vector3.zero;

        //animation
        animator.ResetTrigger("MeleeAttack");
        animator.ResetTrigger("RangedAttack");

        animator.Play(knockbackAnimation, 0, 0f);

        rb.AddForce(direction.normalized * amount * knockBackMultiplier, ForceMode.Impulse);

        knockBackIenumerator = StartCoroutine(KnockBackCoroutine());
    }

    private Coroutine knockBackIenumerator;
    public float knockBackTime = 0.5f;
    IEnumerator KnockBackCoroutine()
    {
        blockMove = true;
        yield return new WaitForSeconds(knockBackTime);
        blockMove = false;
    }

    private bool pauseMovement = false;
    Coroutine movementBlockCoroutine;
    IEnumerator BlockMovement(float duration)
    {
        pauseMovement = true;
        yield return new WaitForSeconds(duration);
        pauseMovement = false;
    }

    public void StunEnemy(float time)
    {
        DisruptAttack();
        StartCoroutine(Stunned(time));
    }
    private bool isStunned = false;
    IEnumerator Stunned(float time)
    {
        isStunned = true;
        yield return new WaitForSeconds(time);
        isStunned = false;
    }
    #endregion

    #region audio

    void PlaySound(AudioClip clip)
    {
        a_Source.PlayOneShot(clip);
    }

    #endregion

    public void SetPlayerDead()
    {
        playerDead = true;
        StopAllCoroutines();
        animator.Play("EnemyAnim_Twerk", 0, 0f);
    }
    private void OnDrawGizmosSelected()
    {
        if (showGizmos)
        {
            Gizmos.DrawWireSphere(attackOrigin.position + (transform.forward * meleeRange), meleeRadius);
            if (spawned)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(CalculateTargetPosition(), 0.5f);
            }
        }
    }
}
