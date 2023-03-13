using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class PlayerMovement : MonoBehaviour
{
    public enum MoveState {idle, walk, run, airborne, crouchIdle, crouchWalk, sliding, wallrunning, climbing, vaulting}
    private MoveState currentMoveState;
    private MoveState targetMoveState;

    private CharacterController charController;
    private PlayerHealthManager p_Health;
    private PlayerAudioScript p_audio;
    private MouseLook p_MouseLook;
    private WeaponSway w_Sway;

    public  bool canMove = true;
    private bool canJump = true;
    private bool runPressed = false;

    private bool grounded = false;
    private bool lastGrounded = false;
    private bool overrideGravity = false;

    private Vector3 velocity;

    private float useSpeed;
    private float targetStepOffset;

    [Header("Values")]
    public TextMeshProUGUI stateText;

    public Transform lookCam;
    public Transform body;

    public float groundedCheckRadius = 0.1f;
    public LayerMask groundedLayerMask;

    [Space]
    public float gravity = -9.8f;
    public float jumpheight = 1f;
    public float jumpVelMultiplier = 1f;
    [Space]
    public float blockairControlMultiplier = 0.4f;
    [Space]
    private float targetSpeed;
    public float acceleration;
    [Header("State settings")]
    public float standingHeight;
    public float crouchingHeight;
    [Space]
    private float headBobAmplitude;
    private float headBobSpeed;
    [Space]
    public float walkSpeed = 12f;
    public float runSpeed = 20f;
    public float crouchSpeed;
    [Space]
    public float walkBobAmplitude;
    public float walkBobSpeed;
    public float walkLean = 3f;
    [Space]
    public float runBobAmplitude;
    public float runBobSpeed;
    [Space]
    public float idleBobAmplitude;
    public float idleBobSpeed;
    [Space]
    public float idleCrouchBobAmplitude;
    public float idleCrouchBobSpeed;
    [Space]
    public float crouchBobAmplitude;
    public float crouchBobSpeed;

    [Header("Sliding")]
    public float slideBobSpeed;
    public float slideBobAmplitude;
    public float slideHeight;
    [Space]
    public float slideHorizontalVelocityThreshold;
    public float targetSlideSpeed;
    public float slideTime;
    public float slideGraceTime = 0.3f;
    [Space]
    public float slideControlSpeed = 1.5f;
    public float startSlideMultiplier = 5f;
    public float slideSpeedClamp = 30f;
    [Space]
    public float downwardSlideForce = 7f;
    public float slideClampAngle = 75f;
    [Space]
    public float slideRampMultiplier;
    public float slideUpwardRampMultiplier = 15f;
    [Space]
    public AudioClip[] slideSounds;
    [Space]
    public float slideLeanAngle = -15f;

    [Header("Wallrunning")]
    public float wallrunHeadBobSpeed;
    public float wallrunHeadBobAmplitude;
    [Space]
    public float wallRunCheckDistance;
    public float wallrunCheckAngle = 35f;
    [Space]
    public float wallRunCheckHeightOffset = 0.2f;
    public float wallRunSpeedMax = 12f;
    public float wallRunSpeedMin = 8f;
    public float wallRunTime = 2f;
    public float wallRunGravity = 3f;

    public float minWallRunHeight = 0.5f;

    public float wallrunJumpMultiplier = 10f;
    public float wallrunDirectionConsideration = 0.3f;

    private RaycastHit leftWallHit;
    private RaycastHit rightWallHit;

    public float wallRunLeanAngle = 15f;
  

    [Header("Climbing")]
    public float climbingBobSpeed;
    public float climbingBobAmplitude;
    [Space]
    public float climbingSpeedMax;
    public float climbingSpeedMin;
    public float climbTime;
    public float climbStaminaRechargeTime;
    [Space]
    public Slider climbSlider;
    [Space]
    public float climbCheckAngle = 15f;

    private bool climbing;
    [Space]
    public float wallRunResetTime = 0.2f;
    [Space]
    public float wallRunStickForce = 1.5f;
    [Space]
    public float climbJumpMultiplier = 5f;
    public float climbRechargeCooldown = 0.5f;
    public float climbStaminaThreshold = 0.25f;
    [Space]
    public ParticleSystem climbParticles;

    [Header("Vaulting")]
    public float kneeHeight;
    public float vaultTime;
    [Space]
    public float vaultCheckDistance;
    public float vaultFromHeadOffsest = 0.2f;
    public float vaultDownHitOffset = 0.1f;
    public float vaultFromEdgeDistance = 0.1f;
    public float downCastCheckLength;
    [Space]
    public TextMeshProUGUI vaultOverallCheckText;
    [Space]
    public float vaultHighVaultCheckLenght = 0.6f;
    [Space]
    public AnimationClip midVaultClip;
    public AnimationClip highVaultClip;
    public float highVaultTime = 0.7f;
    [Space]
    public float vaultMidVelocityMultiplier = 1.5f;
    [Space]
    public float vaultAddVelocityThreshold;
    [Space]
    public float vaultFinishOffset = 0.15f;
    public float vaultFinishVelocityMultipler = 1.5f;
    [Space]
    public float vaultGraceTime = 0.3f;
    private float lastLift;
    [Space]
    public ParticleSystem vaultParticles;

    [Header("Audio")]
    public AudioClip[] jumpSounds;
    public AudioClip[] landSounds;

    [Header("Animation")]
    public Animator armAnimator;
    public Animator bodyRepresentorAnimator;
    public GameObject bodyRepresentor;

    [Header("Vfx")]
    public ParticleSystem slideParticles;
    public ParticleSystem slideParticlesRight;
    public ParticleSystem speedParticles;
    [Space]
    public float speedParticleMagnitudeMin = 0.2f;
    public float maxSpeedParticleMagnitudeThreshold = 0.4f;
    [Space]
    public int minSpeedParticleEmmision = 15;
    public int maxSpeedParticleEmmision = 30;

    [Header("Control Dashing")]
    public float dashTime = 0.5f;
    public float dashCooldown;
    [Space]
    public GameObject dashIndicator;
    [Space]
    public AudioClip[] dashSounds;
    [Space]
    public LayerMask dashCheckMask;
    public float dashCheckRadius = 0.5f;
    public float dashDistance = 1f;
    [Space]
    public ParticleSystem leftDashParticles;
    public ParticleSystem rightDashParticles;
    public ParticleSystem frontDashParticles;
    public ParticleSystem backDashParticles;
    

    private float targetGravity;

    public void SetDefaultValues()
    {
        SetMoveState(MoveState.idle);
    }

    private void Awake()
    {
        p_Health = GetComponent<PlayerHealthManager>();
        w_Sway = GetComponent<WeaponSway>();
        charController = GetComponent<CharacterController>();
        p_MouseLook = GetComponent<MouseLook>();
        charController = GetComponent<CharacterController>();
        p_audio = GetComponent<PlayerAudioScript>();

        StopAllCoroutines();

        isDashing = false;
        velocity.y = -2f;

        bodyRepresentor.SetActive(false);
    }

    // Start is called before the first frame update
    void Start()
    {
        //initialise values
        targetGravity = gravity;

        targetStepOffset = charController.stepOffset;

        p_MouseLook.SetHeadbobValues(headBobAmplitude, headBobSpeed);


        SetMoveState(MoveState.idle);


    }

    // Update is called once per frame
    void Update()
    {
        if (!p_Health.IsPaused())
        {
            UpdateMovement();
        }
    }

    public float grappleVelMultiplier = 0.7f;

    private Vector3 floorDirection;
    private Vector3 floorNormal;
    void UpdateMovement()
    {
        if (canMove && !isDashing && !moveStateDoingOverride)
        {
            MovePlayer();
        }

        //update values
        grounded = Physics.CheckSphere(transform.position, groundedCheckRadius, groundedLayerMask);

        //find floor direction
        if (grounded)
        {
            RaycastHit groundHit;
            if(Physics.Raycast(transform.position, Vector3.down, out groundHit, groundedCheckRadius, groundedLayerMask, QueryTriggerInteraction.Ignore))
            {
                Vector3 left = -body.right;
                floorNormal = groundHit.normal;
                Vector3 normal = floorNormal;

                Vector3 floorDir = -Vector3.Cross(left, normal);
                floorDirection = floorDir;
              
                Debug.DrawLine(transform.position, transform.position + floorDir.normalized, Color.red);
            }
        }
        else
        {
            floorDirection = Vector3.zero;
            floorNormal = Vector3.up;

            //check wallrunn
            if(!sliding && zMove > 0 && currentMoveState != MoveState.crouchIdle && currentMoveState != MoveState.crouchWalk && !wallrunning)
            {
                CalculateForWallRun();
            }
        }


        targetVelocity = Vector3.Lerp(targetVelocity, Vector3.zero, velocityLerpSpeed * Time.deltaTime);
        if (climbing)
        {
            gravity = 0f;
        }
        else
        {
            if (targetVelocity.magnitude > 0.1f)
            {
                gravity = targetGravity * grappleVelMultiplier;
            }
            else
            {
                if (wallrunning)
                {
                    gravity = wallRunGravity;
                }
                else
                {
                    gravity = targetGravity;
                }
            }

            if (Time.time > lastDash && !isDashing)
            {
                dashIndicator.SetActive(true);
            }
            else
            {
                dashIndicator.SetActive(false);
            }
        }

        //work gravity baby!
        if (overrideGravity == false && !isDashing && !isVaulting)
        {
            velocity.y += gravity * Time.deltaTime;
            charController.Move((velocity + targetVelocity) * Time.deltaTime);
        }

        //stop accel ramping
        if (grounded && velocity.y < 0f)
        {
            velocity.y = -5f;
        }

        //accelarate to speed
        useSpeed = Mathf.Lerp(useSpeed, targetSpeed, acceleration * Time.deltaTime);

        //fixes weird edge jitter
        if (grounded == false)
        {
            charController.stepOffset = 0f;
        }
        else
        {
            charController.stepOffset = targetStepOffset;
        }

        //we do the sliding business
        if (sliding)
        {
            //determine for slopes
            if (Vector3.Dot(floorDirection, Vector3.down) > 0f)//downwards slope
            {
                float dot = Vector3.Dot(body.forward, floorDirection);
                float dotVal = 1 / dot; //convert so steeper slopes equals higher value
                float speedAdditive = dotVal * slideRampMultiplier;

               currentSlideSpeed = Mathf.Clamp(currentSlideSpeed += speedAdditive * Time.deltaTime, 0f, maxSlideSpeed);

                //reset index 
                slideIndex = 0f;
                maxSlideSpeed = currentSlideSpeed;

                //set body rotation
                Vector3 rightDirection = Vector3.Cross(startSlideDirection, Vector3.up);
                Vector3 left = rightDirection;
                Vector3 normal = floorNormal;

                Vector3 floorDir = -Vector3.Cross(left, normal);

                bodyRepresentor.transform.rotation = Quaternion.LookRotation(floorDir);

                bodyRepresentorAnimator.SetFloat("SlideSpeed", 1f);
            }
            else
            {
                if(Vector3.Dot(floorDirection, Vector3.down) < 0f)
                {
                    float dot = Vector3.Dot(body.forward, floorDirection);
                    float dotVal = 1 / dot; //convert so steeper slopes equals higher value
                    float speedAdditive = dotVal * slideUpwardRampMultiplier;

                    currentSlideSpeed = Mathf.Clamp(currentSlideSpeed -= speedAdditive * Time.deltaTime, 0f, maxSlideSpeed); //we subtract the speed

                    //reset index 
                    slideIndex = 0f;
                    maxSlideSpeed = currentSlideSpeed;

                    //set body rotation
                    Vector3 rightDirection = Vector3.Cross(startSlideDirection, Vector3.up);
                    Vector3 left = rightDirection;
                    Vector3 normal = floorNormal;

                    Vector3 floorDir = -Vector3.Cross(left, normal);

                    bodyRepresentor.transform.rotation = Quaternion.LookRotation(floorDir);

                    bodyRepresentorAnimator.SetFloat("SlideSpeed", 1f);

                }
                else
                {
                    //de accelerate
                    slideIndex += Time.deltaTime / slideTime;

                    currentSlideSpeed = Mathf.Lerp(maxSlideSpeed, 0f, slideIndex);

                    bodyRepresentor.transform.rotation = Quaternion.LookRotation(startSlideDirection);

                    float outOf = (currentSlideSpeed / maxSlideSpeed);
                    bodyRepresentorAnimator.SetFloat("SlideSpeed", outOf);
                }
            }

            Vector3 slideMove = (startSlideDirection * currentSlideSpeed);
            slideMove.y -= downwardSlideForce;
            charController.Move(slideMove * Time.deltaTime);
            

            //set body rotation
          

            //check slide
            bool canContinue = SlideCheck();
            if (!canContinue)
            {
                StopSliding();
            }
        }
        else
        {
            if(lastCrouchPressed > Time.time && crouchedPressed == true)
            {
                AttemptSlide();
            }
        }

        if (wallrunning)
        {
            WallRunMovement();
        }

        //do climbing values
        if (climbing)
        {
            ClimbMovement();
        }

        if (!climbing && curClimbStamina < 1f && Time.time > lastClimb) 
        {
            curClimbStamina = Mathf.Clamp01(curClimbStamina + (Time.deltaTime / climbStaminaRechargeTime));
            climbSlider.value = curClimbStamina;
        }

        if(!sliding && currentMoveState != MoveState.crouchIdle && currentMoveState != MoveState.crouchWalk)
        {
            CheckForVault();
        }

        if (!isVaulting)
        {
            if (!sliding && currentMoveState != MoveState.crouchIdle && currentMoveState != MoveState.crouchWalk && !isDashing && canDownVaultHit && (canVaultKnee || canVaulthead))
            {
                bool jDown = jumpDown;
                if(Time.time < lastLift)
                {
                    jDown = true;
                }
                if (zMove > 0 && jDown == true)
                {
                    StartVault();
                }else
                {
                    if (climbing)
                    {
                        StartVault();
                    }
                }
            }
        }

        MoveStateMachine();//determine what move type to do

    }

    private Vector3 moveDamper;
    private Vector3 moveVal;
    public void MovePlayer()
    {
        Vector3 move = body.transform.right * xMove + body.transform.forward * zMove;
        move.y = 0f;
        moveVal = move;
        moveDamper = Vector3.Lerp(moveDamper, move, acceleration * Time.deltaTime);
        float toUseS = useSpeed;
        if (blockAirControl)
        {
            toUseS *= blockairControlMultiplier;
        }
        charController.Move(moveDamper * toUseS * Time.deltaTime);

        if (!sliding)
        {
            if (xMove > 0)
            {
                p_MouseLook.SetCamLeanAngle(-walkLean);
            }
            else
            {

                if (xMove < 0)
                {
                    p_MouseLook.SetCamLeanAngle(walkLean);
                }
                else
                {
                    {
                        p_MouseLook.SetCamLeanAngle(0f);
                    }
                }
            }
        }
    }

    void MoveStateMachine()
    {
        //set move states

        if ((currentMoveState == MoveState.crouchIdle || currentMoveState == MoveState.crouchWalk) && grounded)
        {
            if (!HeadIsFree())
            {
                Debug.Log("HeadBlocked");
                //set crouch walk
                if (MoveInputPressed())
                {
                    if (currentMoveState != MoveState.crouchWalk)
                    {
                        targetMoveState = MoveState.crouchWalk;
                        currentMoveState = MoveState.crouchWalk;

                        TransitionedFromState(currentMoveState);
                        SetMoveState(MoveState.crouchWalk);


                    }
                }
                return;
            }
        }


        if (CheckToClimb())
        {
            if (wallrunning)
            {
                StopWallrunning();
            }

            StartClimbing();
            if (currentMoveState != MoveState.climbing)
            {
                TransitionedFromState(currentMoveState);
                currentMoveState = MoveState.climbing;
                SetMoveState(currentMoveState);
            }
        }
        if (!isVaulting) 
        {
            if (climbing)
            {
                if (!CheckToClimb())
                {
                    StopClimbing();
                }
            }
            else
            {

                if (sliding)//overide movement for sliding
                {
                    if (currentMoveState != MoveState.sliding)
                    {
                        TransitionedFromState(currentMoveState);
                        currentMoveState = MoveState.sliding;
                        SetMoveState(currentMoveState);
                    }
                }
                else
                {
                    if (!wallrunning)
                    {
                        if (grounded == false)
                        {
                            //set to airborne
                            if (currentMoveState != MoveState.airborne)
                            {
                                TransitionedFromState(currentMoveState);
                                SetMoveState(MoveState.airborne);
                            }
                        }
                        else
                        {
                            if (MoveInputPressed() == false)
                            {
                                //check idle states
                                if (crouchedPressed)
                                {
                                    if (currentMoveState != MoveState.crouchIdle)
                                    {
                                        targetMoveState = MoveState.crouchIdle;

                                        TransitionedFromState(currentMoveState);
                                        SetMoveState(MoveState.crouchIdle);
                                    }
                                }
                                else
                                {
                                    if (currentMoveState != MoveState.idle)
                                    {
                                        targetMoveState = MoveState.idle;

                                        TransitionedFromState(currentMoveState);
                                        SetMoveState(MoveState.idle);
                                    }
                                }
                            }
                            else
                            {
                                if (runPressed && zMove > 0)
                                {
                                    if (currentMoveState != MoveState.run)
                                    {
                                        targetMoveState = MoveState.run;
                                        TransitionedFromState(currentMoveState);
                                        SetMoveState(MoveState.run);
                                    }
                                }
                                else
                                {
                                    if (crouchedPressed)
                                    {
                                        //set crouch walk
                                        if (currentMoveState != MoveState.crouchWalk)
                                        {
                                            targetMoveState = MoveState.crouchWalk;

                                            TransitionedFromState(currentMoveState);
                                            SetMoveState(MoveState.crouchWalk);
                                        }
                                    }
                                    else
                                    {
                                        //set normal walk
                                        if (currentMoveState != MoveState.walk)
                                        {
                                            targetMoveState = MoveState.walk;

                                            TransitionedFromState(currentMoveState);
                                            SetMoveState(MoveState.walk);
                                        }
                                    }
                                }
                            }

                        }
                    }
                    else
                    {
                        if (wallrunning)
                        {
                            //set normal walk
                            if (currentMoveState != MoveState.wallrunning)
                            {
                                targetMoveState = MoveState.wallrunning;

                                TransitionedFromState(currentMoveState);
                                SetMoveState(MoveState.wallrunning);
                            }
                        }
                    }
                }
            }
        } 

        //show state text
        stateText.text = currentMoveState.ToString();
    }

    bool HeadIsFree() //check if we can exit crouch
    {
        return !Physics.Raycast(lookCam.position, Vector3.up, 0.7f, groundedLayerMask, QueryTriggerInteraction.Ignore);
    }

    private bool MoveInputPressed()
    {
        if(xMove == 0 && zMove == 0)
        {
            return false;
        }

        return true;
    }

    private bool jumpDown = false;
    public void JumpInput(InputAction.CallbackContext context)
    {
        if (p_Health.IsPaused()) return;

        if (context.performed)
        {
            jumpDown = true;
        }
        if (context.canceled)
        {
            jumpDown = false;
            lastLift = Time.time + vaultGraceTime;
        }
        if (currentMoveState == MoveState.crouchIdle || currentMoveState == MoveState.crouchWalk)
        {
                if (HeadIsFree())
                {
                    crouchedPressed = false;
                    TransitionedFromState(currentMoveState);
                    SetMoveState(MoveState.idle);
                }
            return;
        }

        if (!isVaulting)
        {
            if (context.performed && canJump && grounded == true && overrideGravity == false && !isDashing)
            {
                DoJump(Vector3.zero);
            }

            if (context.performed && wallrunning)
            {
                if (wallRunBlockCoroutine != null)
                {
                    StopCoroutine(wallRunBlockCoroutine);
                }

                wallRunBlockCoroutine = StartCoroutine(BlockWallRunIenmerator());

                Vector3 jumpDir = (wallNormal * wallrunJumpMultiplier) + (body.forward * wallrunDirectionConsideration);
                DoJump(jumpDir);

                StopWallrunning();
                DoBlockAirControl(0.5f);
            }

            if (context.performed && climbing)
            {
                StopClimbing();
                if (climbBlockCoroutine != null)
                {
                    StopCoroutine(climbBlockCoroutine);
                }

                climbBlockCoroutine = StartCoroutine(ClimbCooldown());

                Vector3 jumpDir = climbNormal * climbJumpMultiplier;

                DoJump(jumpDir);

                DoBlockAirControl(0.5f);
            }
        }

        if (sliding)
        {
            StopSliding();
        }

    }
    
    public void RunInput(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            runPressed = true;
        }

        if (context.canceled)
        {
            runPressed = false;
        }
    }

    private float lastCrouchPressed = 0f;
    private bool crouchedPressed = false;
    public void CrouchInput(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            crouchedPressed = true;
            if (!sliding)
            {
                 lastCrouchPressed = Time.time + slideGraceTime;
                AttemptSlide();
            }
        }

        if (context.canceled)
        {
            crouchedPressed = false;

            if (sliding)
            {
                StopSliding();
            }
        }
    }

    private bool blockAirControl = false;
    private Coroutine blockAirCoroutine;
    public void DoBlockAirControl(float blocktime)
    {
        if(blockAirCoroutine != null)
        {
            StopCoroutine(blockAirCoroutine);
        }

        blockAirCoroutine = StartCoroutine(BlockAirIenum(blocktime));
    }

    private IEnumerator BlockAirIenum(float blocktime)
    {
        blockAirControl = true;
        yield return new WaitForSeconds(blocktime);
        blockAirControl = false;
    }

    void DoJump(Vector3 addVector)
    {
            armAnimator.SetTrigger("Jump");

            velocity.y = Mathf.Sqrt(jumpheight * -2f * gravity);
            velocity += (jumpVelMultiplier * moveVelocity) + (moveVelocity * jumpVelMultiplier);
            velocity += addVector;

            w_Sway.JumpSway();
            PlayJumpSound();
        EZCameraShake.CameraShaker.Instance.ShakeOnce(2.5f, 1f, 0.3f, 1.5f);
        grounded = false;
        
    }

    #region vaulting

    private RaycastHit kneeHit;
    private RaycastHit headVaultHit;

    private RaycastHit downVaultHit;

    private bool canVaulthead;
    private bool canVaultKnee;

    private bool canDownVaultHit;
    private float currentVaultDownDistance;
    public void CheckForVault()
    {
        Vector3 kneePos = transform.position;
        kneePos.y += kneeHeight;
        canVaultKnee = Physics.Raycast(kneePos, body.forward, out kneeHit, vaultCheckDistance, groundedLayerMask, QueryTriggerInteraction.Ignore);

        if (canVaultKnee)
        {
            Debug.DrawLine(kneePos, kneeHit.point, Color.red);
        }

        Vector3 headPos = lookCam.position;
        headPos.y += vaultFromHeadOffsest;
        canVaulthead = Physics.Raycast(headPos, body.forward, out headVaultHit, vaultCheckDistance, groundedLayerMask, QueryTriggerInteraction.Ignore);
        if (canVaulthead)
        {
            Debug.DrawLine(headPos, headVaultHit.point, Color.red);
        }

        Vector3 fromDownPos = lookCam.position + (body.forward * vaultCheckDistance);
        fromDownPos.y += vaultDownHitOffset;
        if (canVaulthead) //set position to be the edge
        {
            Vector3 baseVec = headVaultHit.point + (body.forward * vaultCheckDistance);
            baseVec.y = lookCam.position.y + vaultDownHitOffset;
        }
        else
        {
            if (canVaultKnee)
            {
                Vector3 baseVec = kneeHit.point + (body.forward * vaultCheckDistance);
                baseVec.y = lookCam.position.y + vaultDownHitOffset;
            }
        }

        float useCastLength = downCastCheckLength;
        if (sliding)
        {
            useCastLength *= 0.3f;
        }
        canDownVaultHit = Physics.Raycast(fromDownPos, Vector3.down, out downVaultHit, useCastLength, groundedLayerMask, QueryTriggerInteraction.Ignore);
        if (canDownVaultHit)
        {
            currentVaultDownDistance = Vector3.Distance(fromDownPos, downVaultHit.point);
            Debug.DrawLine(fromDownPos, downVaultHit.point, Color.green);           
        }
        else
        {
            Vector3 downPosition = fromDownPos;
            downPosition.y -= downCastCheckLength;
            Debug.DrawLine(fromDownPos, downPosition, Color.magenta);
        }

        vaultOverallCheckText.text = "Vault head: " + canVaulthead.ToString() + " Vault knee " + canVaultKnee.ToString() + " Overall check " + canDownVaultHit.ToString();
    }

    private bool isVaulting = false;
    void StartVault()
    {
        TransitionedFromState(currentMoveState);
        SetMoveState(MoveState.vaulting);

        EZCameraShake.CameraShaker.Instance.ShakeOnce(5f, 1f, 0.7f, 1.5f);

        isVaulting = true;

        if(climbBlockCoroutine != null)
        {
            StopCoroutine(climbBlockCoroutine);
        }
        climbBlockCoroutine = StartCoroutine(ClimbCooldown());

        if(vaultCoroutine != null)
        {
            StopCoroutine(vaultCoroutine);
        }
        vaultCoroutine = StartCoroutine(VaultIenumerator());
    }
    private Coroutine vaultCoroutine;
    IEnumerator VaultIenumerator()
    {
        charController.detectCollisions = false;

        var currentPos = transform.position;
        var t = 0f;

        Vector3 vaultTarget = downVaultHit.point;
        vaultTarget.y += vaultFinishOffset;
        Vector3 startVel = velocity;

        velocity = Vector3.zero;

        //determine modifiers
        float useVaultTime = vaultTime;

        Vector3 fromDownPos = lookCam.position + (body.forward * vaultCheckDistance);
        fromDownPos.y += vaultDownHitOffset;
        if (canVaulthead) //set position to be the edge
        {
            Vector3 baseVec = headVaultHit.point + (body.forward * vaultCheckDistance);
            baseVec.y = lookCam.position.y + vaultDownHitOffset;
        }
        else
        {
            if (canVaultKnee)
            {
                Vector3 baseVec = kneeHit.point + (body.forward * vaultCheckDistance);
                baseVec.y = lookCam.position.y + vaultDownHitOffset;
            }
        }

        float downDist = Vector3.Distance(fromDownPos, downVaultHit.point);

        bool doingMidVault = false;
        if(downDist < vaultHighVaultCheckLenght) //do high vault
        {
            useVaultTime = highVaultTime;
            armAnimator.CrossFade(highVaultClip.name, 0.1f, 0);
        }
        else
        {
            //mid vault
            doingMidVault = true;
            armAnimator.CrossFade(midVaultClip.name, 0.1f, 0);
        }

        while (t < 1) // change positions
        {
            t += Time.deltaTime / useVaultTime;
            transform.position = Vector3.Lerp(currentPos, vaultTarget, t);
            yield return null;
        }

        //add velocity
        Vector3 horiZontalVel = new Vector3(moveVelocity.x, 0f, moveVelocity.z);
        //Debug.Log("horizontal velocity " + horiZontalVel.magnitude);
        if (horiZontalVel.magnitude > vaultAddVelocityThreshold)
        {
            float velMag = startVel.magnitude;
            if (doingMidVault) velMag *= vaultMidVelocityMultiplier;
            AddVelocity(velMag * transform.forward * vaultFinishVelocityMultipler);
        }
        //reset coll
        charController.detectCollisions = true;

        //finish vaulting
        StopVaulting();
    } 

    void StopVaulting()
    {
        isVaulting = false;
    }
#endregion

    #region sliding
    void AttemptSlide()
    {
        bool canSlide = SlideCheck();
        
        if (canSlide)
        {
            StartSlide();
            TransitionedFromState(currentMoveState);
            SetMoveState(MoveState.sliding);
        }
    }

    bool SlideCheck()
    {

        Debug.Log("Checkto slie");
        if (!grounded)
        {
            return false;
        }
        if(climbing || isVaulting)
        {
            return false;
        }
        Vector3 horiZontalVel = new Vector3(moveVelocity.x, 0f, moveVelocity.z);
        //Debug.Log("horizontal velocity " + horiZontalVel.magnitude);
        if (horiZontalVel.magnitude < slideHorizontalVelocityThreshold)
        {
            return false;
        }

        return true;
    }

    private bool sliding = false;
    private Vector3 startSlideDirection;
    private float currentSlideSpeed = 0f;
    private float maxSlideSpeed = 0f;
    float slideIndex = 0f;
    void StartSlide()
    {
        //set slide values

        Debug.Log("Start slide");

        slideIndex = 0f;

        Vector3 horiZontalVel = new Vector3(moveVelocity.x, 0f, moveVelocity.z);
        currentSlideSpeed = targetSlideSpeed + (horiZontalVel.magnitude * startSlideMultiplier);
        maxSlideSpeed = currentSlideSpeed;

        startSlideDirection = body.forward;
        
        p_MouseLook.ClampLookToVector(slideClampAngle);

        EZCameraShake.CameraShaker.Instance.ShakeOnce(2.5f, 1f, 0.3f, 1.5f);

        //do animation
        EnableRepresentorBody();
        bodyRepresentorAnimator.Play("Anim_BodyRepresentor_SlideStart", 0, 0f);

        //sound
        AudioClip targClip = slideSounds[Random.Range(0, slideSounds.Length)];

        p_audio.PlayPlayerSound(targClip);

        sliding = true;
    }
    void StopSliding()
    {
        sliding = false;

        DisableRepresentorBody(0.5f);

        p_MouseLook.CancelLookClamp();
        bodyRepresentorAnimator.SetTrigger("StopSlide");
        if (!HeadIsFree())
        {
            TransitionedFromState(currentMoveState);
            SetMoveState(MoveState.crouchIdle);
        }
        MoveStateMachine(); //recalculate target movestate
    }
    #endregion

    #region wallrunning

    private bool wallrunning;
    private bool wallRight;
    private bool wallLeft;

    private void CalculateForWallRun()
    {
        //check for wallrun
        CheckForWall();

        if((wallLeft || wallRight) && zMove > 0 && !blockWallrun)
        {        
            //check for above min distance
            if (AboveGround() && CheckWallRunAngle())
            {
                if (!wallrunning)
                {
                    StartWallRunning(); // start the wall run
                }
            }
            else
            {
                if (wallrunning)
                {
                    StopWallrunning();
                }
            }
        }
        else
        {
            if (wallrunning)
            {
                StopWallrunning();
            }
        }
    }
    private bool CheckWallRunAngle()
    {
        float dot = Vector3.Dot(-wallNormal, lookCam.forward);
        float angle = Mathf.Acos(dot) * Mathf.Rad2Deg;

        if(Mathf.Abs(angle) >= wallrunCheckAngle)
        {
            return true;
        }
        return false;
    }

    private void CheckForWall()
    {
        Vector3 wallCheckFrom = transform.position;
        wallCheckFrom.y += wallRunCheckHeightOffset;
        wallRight = Physics.Raycast(transform.position, body.right, out rightWallHit, wallRunCheckDistance, groundedLayerMask, QueryTriggerInteraction.Ignore);
        wallLeft = Physics.Raycast(transform.position, -body.right, out leftWallHit, wallRunCheckDistance, groundedLayerMask, QueryTriggerInteraction.Ignore);

    }

    private bool AboveGround() //check we have cleared the ground
    {
        return !Physics.Raycast(transform.position, Vector3.down, minWallRunHeight, groundedLayerMask, QueryTriggerInteraction.Ignore);
    }

    private void StartWallRunning()
    {
        wallrunning = true;
        armAnimator.ResetTrigger("Jump");//stop the jump trigger 
        ResetWallRunIndex();
        MoveStateMachine();
    }

    private void StopWallrunning()
    {
        wallrunning = false;
        MoveStateMachine();
    }

    private Vector3 wallNormal;

    private float currentWallRunSpeed;
    private void WallRunMovement()
    {
        //calculate index
        wallRunIndex += Time.deltaTime / wallRunTime;

        currentWallRunSpeed = Mathf.Lerp(wallRunSpeedMax, wallRunSpeedMin, wallRunIndex);

        float outOf = (currentWallRunSpeed / wallRunSpeedMax);

        //we prioritise wall right
        wallNormal = wallRight ? rightWallHit.normal : leftWallHit.normal;

        Vector3 wallUpCross = Vector3.Cross(-body.forward, wallNormal); //so we always go forward
        Vector3 wallForward = Vector3.Cross(wallUpCross, wallNormal);

   
        Vector3 finalVector = wallForward * currentWallRunSpeed;
        finalVector += wallRunStickForce * -wallNormal; //pushes into wall

        charController.Move(finalVector * Time.deltaTime);

        if (wallRight) //do lean
        {
            p_MouseLook.SetCamLeanAngle(wallRunLeanAngle);
            armAnimator.SetBool("WallRunRight", true);
        }
        else
        {
            p_MouseLook.SetCamLeanAngle(-wallRunLeanAngle);
            armAnimator.SetBool("WallRunRight", false);
        }

        CheckForWall();
        if (!wallLeft && !wallRight)
        {
            StopWallrunning();
            return;
        }
        if(zMove <= 0)
        {
            StopWallrunning();
            return;
        }
        if (grounded)
        {
            StopWallrunning();
            return;
        }
    }

    private float wallRunIndex;
    private void ResetWallRunIndex()
    {
        if (!doingWallReset) // to prevent frame skips or cheesing of speed slowdown
        {
            currentWallRunSpeed = wallRunSpeedMax;
            wallRunIndex = 0f;
        }
    }

    private void LeftWallRunWall()
    {
        if (wallResetCoroutine != null)
        {
            StopCoroutine(wallResetCoroutine);
        }

        wallResetCoroutine = StartCoroutine(WallRunResetIenumerator());
    }


    Coroutine wallResetCoroutine;
    private bool doingWallReset = false;
    private IEnumerator WallRunResetIenumerator()
    {
        doingWallReset = true;
        yield return new WaitForSeconds(wallRunResetTime);
        doingWallReset = false;
        ResetWallRunIndex();
    }

    private bool blockWallrun = false;
    private Coroutine wallRunBlockCoroutine;
    private IEnumerator BlockWallRunIenmerator()
    {
        blockWallrun = true;
        yield return new WaitForSeconds(0.3f);
        blockWallrun = false;
    }
    #endregion

    #region climbing
    private Vector3 climbNormal;
    RaycastHit climbHit;
    private float lastClimb;

    bool CheckToClimb()
    {
        if (blockClimb)
        {
            return false;
        }

        if (isVaulting)
        {
            return false;
        }

        if(curClimbStamina <= 0f)
        {
            return false;
        }

        if (!climbing)
        {
            if (curClimbStamina <= climbStaminaThreshold)
            {
                return false;
            }
        }

        if(zMove <= 0)
        {
            return false;  
        }
        if (grounded) return false;

        bool canClimb = Physics.Raycast(lookCam.transform.position, body.forward, out climbHit, wallRunCheckDistance, groundedLayerMask, QueryTriggerInteraction.Ignore);
        if (canClimb)
        {
            climbNormal = climbHit.normal;

            float dot = Vector3.Dot(body.forward, -climbHit.normal);
            float angle = Mathf.Acos(dot) * Mathf.Rad2Deg;

            if (Mathf.Abs(angle) <= climbCheckAngle)
            {
                return true;
            }
            else
            {
                Debug.Log("Check angle " + angle);
            }
        }

        return false;
    }

    void StartClimbing()
    {
        climbing = true;
    }

    void StopClimbing()
    {
        climbing = false;

        lastClimb = Time.time + climbRechargeCooldown;
    }

    private float curClimbStamina = 0f;
    void ClimbMovement()
    {
        if(curClimbStamina > 0f)
        {
            curClimbStamina -= Time.deltaTime / climbTime;
            climbSlider.value = curClimbStamina;
        }

        float useSpeed = Mathf.Lerp(climbingSpeedMax, climbingSpeedMin, curClimbStamina / 1f);
      
        Vector3 useMoveVector = new Vector3(0f, useSpeed, 0f);

        charController.Move(useMoveVector * Time.deltaTime);
    }

    bool blockClimb = false;
    private Coroutine climbBlockCoroutine;
    IEnumerator ClimbCooldown()
    {
        blockClimb = true;
        yield return new WaitForSeconds(0.5f);
        blockClimb = false;
    }
    #endregion

    #region dash
    public void DashInput(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            //check to dash
            if (canMove && !isDashing && Time.time > lastDash)
            {
                Dash();
            }
        }
    }
    void Dash()
    {

        EZCameraShake.CameraShaker.Instance.ShakeOnce(3f, 1.5f, 0.1f, 1f);

        AudioClip targetSound = dashSounds[Random.Range(0, dashSounds.Length)];
        p_audio.PlayPlayerSound(targetSound);

        //do particles
        ParticleSystem useParticles = frontDashParticles;
        if (xMove > 0) useParticles = rightDashParticles;
        if (xMove < 0) useParticles = leftDashParticles;
        if (zMove < 0) useParticles = backDashParticles;

        useParticles.Play();

        //do direction
        Vector3 lookDir = lookCam.forward * zMove;
        Vector3 rightDir = lookCam.right;
        lookDir.y = 0f;
        rightDir.y = 0f;

        lookDir += rightDir * xMove;


        Vector3 origin = transform.position + new Vector3(0f, dashCheckRadius + 1f, 0f);
        Vector3 targPoint = transform.position + (lookDir * dashDistance);

        //we do cast to check we wont go through a wall all something
        float checkDist = dashDistance - dashCheckRadius;

        RaycastHit hit;
        if (Physics.SphereCast(origin, dashCheckRadius, lookDir, out hit, checkDist, dashCheckMask))
        {
            float doDist = Vector3.Distance(hit.point, origin) - dashCheckRadius;
            targPoint = transform.position + (lookDir * doDist);
        }

        //reset velocity
        if (!grounded)
        {
            float velMag = velocity.magnitude;
            velocity = lookCam.forward * velMag;
        }
        else
        {
            velocity = Vector3.zero;
        }

        //anim
        armAnimator.Play("Anim_Arms_Dash", 0, 0f);

        //coroutine
        StartCoroutine(MoveToPosition(transform, targPoint, dashTime));

        StartCoroutine(DashIenumerator()); 
    }
    public IEnumerator MoveToPosition(Transform transform, Vector3 position, float timeTo)
    {
        var currentPos = transform.position;
        var t = 0f;
        while (t < 1)
        {
            t += Time.deltaTime / timeTo;
            transform.position = Vector3.Lerp(currentPos, position, t);
            yield return null;
        }
    }

    private bool isDashing = false;
    private float lastDash;
    IEnumerator DashIenumerator()
    {
        isDashing = true;
        yield return new WaitForSeconds(dashTime);
        isDashing = false;

        lastDash = Time.time + dashCooldown;
    }
    #endregion
     
    #region body representor
    void EnableRepresentorBody()
    {
        if (disableBodyCoroutine != null)
        {
            StopCoroutine(disableBodyCoroutine);
        }

        bodyRepresentor.SetActive(true);      
    }

    void DisableRepresentorBody(float disableTime)
    {
        if(disableBodyCoroutine != null)
        {
            StopCoroutine(disableBodyCoroutine);
        }
        disableBodyCoroutine = StartCoroutine(DisableBodyIenumerator(disableTime));
    }

    private Coroutine disableBodyCoroutine;
    IEnumerator DisableBodyIenumerator(float disableTime)
    {
        yield return new WaitForSeconds(disableTime);
        bodyRepresentor.SetActive(false);
    }
    #endregion

    public bool IsGrounded() => grounded;

    public bool IsMoving()
    {
        if(curSpeed > 0)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public Vector3 LocalMoveVelocity()
    {
        return new Vector3(xMove, 0, zMove);
    }

    void PlayJumpSound()
    {
        AudioClip toPlay = jumpSounds[Random.Range(0, jumpSounds.Length)];
        p_audio.PlayPlayerSound(toPlay);
    }

    private float xMove;
    private float zMove;

    private bool hasMoveInput = false;
    public void RetrieveMovementValues(InputAction.CallbackContext iMove)
    { 

        if(iMove.ReadValue<Vector2>().magnitude != 0)
        {
            hasMoveInput = true;
        }
        else
        {
            hasMoveInput = false;
        }
        xMove = iMove.ReadValue<Vector2>().x;
        zMove = iMove.ReadValue<Vector2>().y;
    }

    private float curSpeed;
    private Vector3 lastPos;
    private Vector3 curPos;

    private Vector3 moveVelocity;
    private void FixedUpdate()
    {
        if (p_Health.IsPaused()) return;
        //handle speed
        lastPos = curPos;

        curSpeed = Vector3.Distance(lastPos, transform.position) / Time.fixedDeltaTime;
        curSpeed = Mathf.RoundToInt(curSpeed);

        curPos = transform.position;

        moveVelocity = curPos - lastPos;
        SetSpeedParticles(moveVelocity.magnitude);

        //for grounded 
        if (grounded && lastGrounded != grounded)
        {
            //hit ground
            Land();
        }


        if(!grounded && lastGrounded == grounded)
        {
            if (sliding)
            {
                StopSliding();
            }
        }
        if (lastGrounded != grounded) { armAnimator.SetBool("Grounded", grounded); }

        lastGrounded = grounded;

       // Debug.Log("Char velocity " + moveVelocity + " magnitude " + moveVelocity.magnitude);
    }

    void Land()
    {
        armAnimator.SetTrigger("Land");

        EZCameraShake.CameraShaker.Instance.ShakeOnce(2.5f, 1f, 0.3f, 1.5f);
        //we hit the ground
        velocity.x = 0;
        velocity.z = 0;

        //play sound
        AudioClip toPlay = landSounds[Random.Range(0, landSounds.Length)];
        p_audio.PlayPlayerSound(toPlay);

        w_Sway.LandSway();

        if (wallrunning)
        {
            StopWallrunning();
        }

        ResetWallRunIndex();
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, groundedCheckRadius);
    }

    private Vector3 targetVelocity;
    public float velocityLerpSpeed = 10f;

    public void AddVelocity(Vector3 velocity)
    {
        targetVelocity += velocity;
    }

    private bool moveStateDoingOverride = false;
    void SetMoveState(MoveState moveState)
    {
        switch (moveState)
        {
            case MoveState.idle:
                p_MouseLook.SetPlayerHeight(standingHeight);

                currentMoveState = moveState;

                //set headbob values
                headBobAmplitude = idleBobAmplitude;
                headBobSpeed = idleBobSpeed;

                p_MouseLook.SetHeadbobValues(headBobAmplitude, headBobSpeed);

                //set speed
                targetSpeed = 0f;

                p_MouseLook.SetPlayFootSteps(false);

                moveStateDoingOverride = false;
                break;
            case MoveState.walk:
                p_MouseLook.SetPlayerHeight(standingHeight);

                currentMoveState = moveState;

                //set headbob values
                headBobAmplitude = walkBobAmplitude;
                headBobSpeed = walkBobSpeed;

                p_MouseLook.SetHeadbobValues(headBobAmplitude, headBobSpeed);

                //set speed
                targetSpeed = walkSpeed;

                p_MouseLook.SetPlayFootSteps(true);

                moveStateDoingOverride = false;
                break;
            case MoveState.run:
                p_MouseLook.SetPlayerHeight(standingHeight);

                currentMoveState = moveState;

                //set headbob values
                headBobAmplitude = runBobAmplitude;
                headBobSpeed = runBobSpeed;

                p_MouseLook.SetHeadbobValues(headBobAmplitude, headBobSpeed);

                armAnimator.SetBool("Run", true);

                //set speed
                targetSpeed = runSpeed;

                p_MouseLook.SetPlayFootSteps(true);

                moveStateDoingOverride = false;
                break;
            case MoveState.airborne:
                p_MouseLook.SetPlayerHeight(standingHeight);

                currentMoveState = moveState;

                headBobAmplitude = idleBobAmplitude;
                headBobSpeed = idleBobSpeed;

                p_MouseLook.SetHeadbobValues(headBobAmplitude, headBobSpeed);

                //set speed
                targetSpeed = walkSpeed;

                p_MouseLook.SetPlayFootSteps(false);

                moveStateDoingOverride = false;
                break;
            case MoveState.crouchIdle:
                p_MouseLook.SetPlayerHeight(crouchingHeight);

                currentMoveState = moveState;

                //set headbob values
                headBobAmplitude = idleCrouchBobAmplitude;
                headBobSpeed = idleCrouchBobSpeed;

                p_MouseLook.SetHeadbobValues(headBobAmplitude, headBobSpeed);

                armAnimator.SetBool("Crouch", true);

                //set speed
                targetSpeed = 0f;

                p_MouseLook.SetPlayFootSteps(false);
                moveStateDoingOverride = false;

                break;
            case MoveState.crouchWalk:
                p_MouseLook.SetPlayerHeight(crouchingHeight);

                currentMoveState = moveState;

                //set headbob values
                headBobAmplitude = crouchBobAmplitude;
                headBobSpeed = crouchBobSpeed;

                p_MouseLook.SetHeadbobValues(headBobAmplitude, headBobSpeed);

                armAnimator.SetBool("Crouch", true);

                //set speed
                targetSpeed = crouchSpeed;

                p_MouseLook.SetPlayFootSteps(true);

                moveStateDoingOverride = false;
                break;
            case MoveState.sliding:
                p_MouseLook.SetPlayerHeight(slideHeight);

                currentMoveState = moveState;

                //set headbob values
                headBobAmplitude = slideBobAmplitude;
                headBobSpeed = slideBobSpeed;

                p_MouseLook.SetHeadbobValues(headBobAmplitude, headBobSpeed);

                armAnimator.SetBool("Sliding", true);

                //set speed
                targetSpeed = slideControlSpeed;

                p_MouseLook.SetPlayFootSteps(false);
                p_MouseLook.SetCamLeanAngle(slideLeanAngle);

                slideParticles.Play();
                slideParticlesRight.Play();
                moveStateDoingOverride = false;

                break;
            case MoveState.wallrunning:
                p_MouseLook.SetPlayerHeight(standingHeight);

                currentMoveState = moveState;

                //set headbob values
                headBobAmplitude = wallrunHeadBobAmplitude;
                headBobSpeed = wallrunHeadBobSpeed;

                p_MouseLook.SetHeadbobValues(headBobAmplitude, headBobSpeed);
                moveStateDoingOverride = true;
                p_MouseLook.SetPlayFootSteps(true);

                armAnimator.SetBool("wallrunning", true);
                break;
            case MoveState.climbing:
                p_MouseLook.SetPlayerHeight(standingHeight);

                currentMoveState = moveState;

                //set headbob values
                headBobAmplitude = climbingBobAmplitude;
                headBobSpeed = climbingBobSpeed;

                p_MouseLook.SetHeadbobValues(headBobAmplitude, headBobSpeed);
                moveStateDoingOverride = true;
                p_MouseLook.SetPlayFootSteps(true);

                armAnimator.SetBool("climbing", true);

                climbParticles.Play();

                blockingAction = true;
                break;
            case MoveState.vaulting:
                //set headbob values
                headBobAmplitude = climbingBobAmplitude;
                headBobSpeed = climbingBobSpeed;

                p_MouseLook.SetHeadbobValues(headBobAmplitude, headBobSpeed);

                currentMoveState = moveState;

                moveStateDoingOverride = true;
                blockingAction = true;
                p_MouseLook.SetPlayFootSteps(false);
                p_MouseLook.SetCamLeanAngle(4f);
                vaultParticles.Play();
                break;
        }
    }
    private void TransitionedFromState(MoveState moveState)
    {
        switch (moveState)
        {
            case MoveState.idle:

                armAnimator.SetBool("Run", false);

                break;
            case MoveState.walk:

                p_MouseLook.SetCamLeanAngle(0f);
                break;
            case MoveState.run:

                armAnimator.SetBool("Run", false);

                p_MouseLook.SetCamLeanAngle(0f);

                break;
            case MoveState.airborne:

                break;
            case MoveState.crouchIdle:
                if (targetMoveState != MoveState.crouchWalk)
                {
                    armAnimator.SetBool("Crouch", false);
                }

                break;
            case MoveState.crouchWalk:

                if (targetMoveState != MoveState.crouchIdle)
                {
                    armAnimator.SetBool("Crouch", false);
                }
                break;
            case MoveState.sliding:

                armAnimator.SetBool("Sliding", false);
                slideParticles.Stop();
                slideParticlesRight.Stop();

                p_MouseLook.SetCamLeanAngle(0f);

                break;
            case MoveState.wallrunning:
                wallrunning = false;

                armAnimator.SetBool("wallrunning", false);
                p_MouseLook.SetCamLeanAngle(0f);

                //set left wall
                LeftWallRunWall();
                break;
            case MoveState.climbing:

                armAnimator.SetBool("climbing", false);
                climbParticles.Stop();
                blockingAction = false;
                break;
            case MoveState.vaulting:

                blockingAction = false;

                p_MouseLook.SetCamLeanAngle(0f);
     
                break;
        }
    }

    private void SetSpeedParticles(float curMagnitude)
    {
        if(curMagnitude > speedParticleMagnitudeMin)
        {
            if (speedParticles.isPlaying == false)
            {
                speedParticles.Play();
            }

            float between = curMagnitude / maxSpeedParticleMagnitudeThreshold;
            int useEmmision = (int)Mathf.Lerp(minSpeedParticleEmmision, maxSpeedParticleEmmision, between);

            var em = speedParticles.emission;

            em.rateOverTime = useEmmision;
        }
        else
        {
            if (speedParticles.isPlaying == true)
            {
                speedParticles.Stop();
            }
        }
    }

    private bool blockingAction = false;
    public bool BlockingAction()
    {
        return blockingAction;
    }
   
    public bool IsSliding()
    {
        return sliding;
    }
}
