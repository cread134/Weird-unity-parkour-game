using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;

public class ConnectionManager : MonoBehaviour
{
    private PlayerMovement p_Movement;
    private WeaponSway wSway;
    private PlayerAudioScript p_Audio;
    private PlayerHealthManager p_Health;
    private ObjectPooler objPooler;
    [Header("Debug menu")]
    public bool useDebugui = false;
    public GameObject debugmenuHolder;
    public TextMeshProUGUI castStateText;
    public TextMeshProUGUI secondStateText;
    public TextMeshProUGUI fullConnectionText;

    [Header("Audio")]
    public AudioClip beamSound;
    public AudioClip gunDrawSound;
    public AudioClip[] castSounds;
    public AudioClip[] secondCastSounds;
    public AudioClip[] interactSounds;
    public AudioClip[] attackPulseSounds;
    public AudioClip[] retractedSounds;

    [Header("Full connection settings")]
    public float enemyPullForce;

    [Header("Animations")]
    public AnimationClip shootClip;
    public AnimationClip grappleClip;
    public AnimationClip noHitClip;
    public AnimationClip interact;
    public AnimationClip interactAlternate;
    public AnimationClip damageOrb;
    public AnimationClip secondCast;
    public AnimationClip useTelekinesisClip;

    [Header("GunSettings")]
    public Animator handsAnimator;
    public ParticleSystem gunParticles;
    public bool requireResources = false;
    public LayerMask gunMask;
    public Transform bulletSpawnPosition;
    public float bulletDamage = 100f;
    public GameObject canShootIndicator;
    [Space]
    public float resourceRateIncreaseSpeed = 3f;
    public float minResourcesPerSecond = 4f;
    public float maxResourcesPerSecond = 10f;
    public float maxResources = 100f;
    public float resourcesPerShot = 100f;
    public float connectAddResources = 20f;
    private float currentResources;
    private float currentResourceRate;
    private float lastResources;

    [Space]
    public float gunCooldown = 2f;
    private float lastGun;
    public float gunTime = 1.5f;
    public float gunAccuracy = 0.1f;
    public int bulletsPerShot = 1;
    public float timeToCalculateShot = 0.7f;

    [Space]
    public float shotInterval;
    private bool canSecondaryshot = false;
    private float lastShot;
    public float putAwayTime = 0.65f;
    public float secondaryShotTime = 1.1f;
    public float timeToGetToAway = 0.6f;

    [Space]
    public AudioClip[] shootSounds;

    public Slider resourceSlider;

    [Header("Telekenesis settings")]
    public float telekinesisForceAmount = 15f;
    public float telekinesisRange = 15f;
    public float telekkinesisUsetime = 0.5f;

    [Header("components")]
    public GameObject thrownRepresentor;
    public LayerMask checkMask;
    public LayerMask blockMask;
    [Space]
    public LineRenderer castLineRenderer;
    public LineRenderer secondCastLine;
    public ParticleSystem handParticles;
    [Space]
    public Transform cameraTransForm;
    public Transform fromCastPoint;
    public Transform prefabHolder;
    [Space]
    public GameObject castObjPrefab;
    public GameObject pulseObjectPrefab;
    public GameObject hitmarker;
    [Space]
    public float castDelay;
    public float enemyStunTime = 2f;
    public float allowedCastTime = 3f;
    public float secondaryPulseTime = 0.8f;
    public float fullconnectionCastTime;
    public float castRange;
    [Space]
    public float healthTime = 0.5f;
    [Space]
    public ParticleSystem grappleParticle;
    public AudioClip[] grappleSounds;
    [Space]
    public float retractTime = 0.5f;
    public Transform retractPos;
    [Space]
    public int bezierIterations = 10;
    public float wobbleMultiplier = 0.1f;
    public float curveSmoothNess = 2f;
    public float shakeMagnitudeMultiplier = 0.1f;
    [Space]
    public float secondCastTime = 0.6f;
    private bool secondCasting = false;
    
    public void SetDefaultValues()
    {
        resourceSlider.maxValue = maxResources;
        resourceSlider.value = 0f;
        currentResourceRate = minResourcesPerSecond;
        currentResources = 0f;

        casting = false;
        hasConnection = false;
        secondReceiver = null;
        secondCasting = false;
        currentConnection = null;

        secondCastLine.enabled = false;
        SetCastLineActive(false);

    }

    public void LevelEnded()
    {
        StopAllCoroutines();
        if(currentCastInstance != null)
        {
            Destroy(currentCastInstance);
        }
    }

    public void ResetAnimator()
    {

        StopAllCoroutines();
        casting = false;
        isBlocking = false;
        hasConnection = false;
        sendingPulse = false;
        secondCasting = false;
        isRetracting = false;

        canSecondaryshot = false;

        thrownRepresentor.SetActive(true);

        handsAnimator.Play("Anim_Arms_Draw", 0, 0f);
        handsAnimator.SetBool("Hitting", false);
        handsAnimator.SetBool("Casting", false);
    }

    // Start is called before the first frame update
    void Start()
    {
        if (useDebugui)
        {
            debugmenuHolder.SetActive(true);
        }
        else
        {
            debugmenuHolder.SetActive(false);
        }

        p_Movement = GetComponent<PlayerMovement>();
        wSway = GetComponent<WeaponSway>();
        p_Audio = GetComponent<PlayerAudioScript>();

        SetCastLineActive(false);

        castLineRenderer.positionCount = bezierIterations + 1;

        p_Health = GetComponent<PlayerHealthManager>();
        objPooler = p_Health.GetObjectPooler();
    }

    private Vector3 lastConPoint;
    // Update is called once per frame
    private bool isCastDelay;

    private bool playingBeam = false;
    void Update()
    {
        if (casting && !secondCasting && !isCastDelay)
        {
            float divided = Vector3.Distance(fromCastPoint.position, currentCast.transform.position) / castRange;
            DrawConLine(fromCastPoint.position, currentCast.transform.position, Mathf.Lerp(1.5f, 0f, divided), 3f);
        }
        else
        {
            if (hasConnection)
            {
                if (currentConnection.transform == null)
                {
                    RetractCast(lastConPoint);
                }

                if (secondCasting)
                {
                    DrawConLine(currentCast.transform.position, currentConnection.connectPoint.position, 0.1f, 4f);
                }
                else
                {
                    DrawConLine(fromCastPoint.position, currentConnection.connectPoint.position, 0.1f, 4f);
                    lastConPoint = currentConnection.connectPoint.position;

                    //we check that there is no interfearnace
                   // float distBetween = Vector3.Distance(transform.position, currentConnection.transform.position);
                  //  RaycastHit hit;
                   // if (Physics.Raycast(cameraTransForm.position, cameraTransForm.forward, out hit, distBetween + 0.1f, blockMask))
                   // {
                    //    if (hit.transform.gameObject != currentConnection.transform.gameObject)
                    //    {
                     //       hasConnection = false;
                       ///     RetractCast(currentConnection.connectPoint.position);
                        //    Debug.Log("cast interfearance");
                      //  }
                    //}
                }

                //do resources
                currentResourceRate = Mathf.Lerp(currentResourceRate, maxResourcesPerSecond, resourceRateIncreaseSpeed * Time.deltaTime);

                //we checkto add resources
                if(Time.time > lastResources && !isRetracting)
                {
                    // do the adding
                    lastResources = Time.time + 0.1f;
                    currentResources = Mathf.Clamp(currentResources + (currentResourceRate / 10f), 0f, maxResources);
                    resourceSlider.value = currentResources;
                }

            }
        }

        if(casting || hasConnection)
        {
        }
        else
        {
            if(playingBeam == true)
            {
                p_Audio.StopPlaying();
                playingBeam = false;
            }
        }

        if (!hasConnection)
        {
            currentResourceRate = minResourcesPerSecond;
        }

        if(Time.time > lastGun && currentResources > (resourcesPerShot - 0.01f))
        {
            canShootIndicator.SetActive(true);
        }
        else
        {
            canShootIndicator.SetActive(false);
        }

        if (isRetracting)
        {
            retractPos.position = Vector3.Lerp(retractPos.position, fromCastPoint.position, 20f * Time.deltaTime);

            float dist = Vector3.Distance(retractPos.position, fromCastPoint.position);
            if (dist < 0.5f)
            {
                if (retractCoroutine != null)
                {
                    StopCoroutine(retractCoroutine);

                    SetCastLineActive(false);
                    isRetracting = false;
                }
            }
            float divided = 1f - (Vector3.Distance(fromCastPoint.position, retractPos.position) / castRange);

            DrawConLine(fromCastPoint.position, retractPos.position, Mathf.Lerp(1f, 0f, divided), 4f);
        }

        if(Time.time > lasthitmarker && hitmarker.activeSelf == true)
        {
            hitmarker.SetActive(false);
        }

        if(secondReceiver != null)
        {
            if (isRetracting == true)
            {
                DissolveSecondRecLine();
                secondReceiver = null;
            }
            else
            {
                if (secondCastLine.enabled == false)
                {
                    secondCastLine.enabled = true;
                }
                DrawSecondRecLine(currentConnection.connectPoint.position, secondReceiver.connectPoint.position);            
            }
        }
        else
        {
            //disable line
            if(secondCastLine.enabled == true)
            {
                secondCastLine.enabled = false;
            }
            //disallow full connection
            fullConnection = false;
        }

        //we do for debug menu
        if (useDebugui)
        {
            fullConnectionText.text = "Full connection " + fullConnection.ToString();
            castStateText.text = "Casting " + casting.ToString();
            bool hasReceivor = secondReceiver != null;
            secondStateText.text = "second cast " + secondCasting.ToString() + "receivor is " + hasReceivor.ToString();
        }
    }

    void DrawSecondRecLine(Vector3 start, Vector3 end)
    {
        secondCastLine.SetPosition(0, start);
        secondCastLine.SetPosition(1, end);
    }

    void DissolveSecondRecLine()
    {
        secondCastLine.enabled = false;
    }
    void DrawConLine(Vector3 start, Vector3 end, float shakeMagnitude, float shakeSpeed)
    {
        castLineRenderer.SetPosition(0, start);
        castLineRenderer.SetPosition(1, end);

        float lerpJump = 1f / bezierIterations;

        float curDistance = Vector3.Distance(start, end);
        Vector3 midPoint = cameraTransForm.position + (cameraTransForm.forward * curDistance / 2f);

        for (int i = 0; i < bezierIterations + 1; i++)
        {
            float amount = i * lerpJump;

            Vector3 offset = Vector3.zero;
            if (i != 0 && i != bezierIterations)
            {
                offset = new Vector3(Mathf.Cos(shakeSpeed * (Time.time + i)), ExpSin(shakeMagnitude, curveSmoothNess, i, bezierIterations), 0f) * shakeMagnitude * wobbleMultiplier * shakeMagnitudeMultiplier; //to make shake and wobble
            }

            Vector3 lerpA = Vector3.Lerp(start, end, amount);
            Vector3 lerpB = Vector3.Lerp(midPoint, end, amount);

            Vector3 lerpBetween = Vector3.Lerp(lerpA, lerpB, amount) + offset;

            castLineRenderer.SetPosition(i, lerpBetween);
        }
    }

    float ExpSin(float amplitude, float periodIntensity, int index, int maxIndex)
    {
        float div = (float)index / maxIndex;
        float coefficient = amplitude / div;

        return coefficient * Mathf.Sin(((2f * Mathf.PI) / periodIntensity) * div);
    }

    private bool hasConnection = false;
    private bool casting = false;
    private bool isRetracting;
    #region Input
    public void CastInput(InputAction.CallbackContext context)
    {
        if (p_Health.IsDead() || p_Health.IsPaused() || p_Movement.BlockingAction()) return;
        if (context.performed)
        {
            if (!hasConnection && !casting && !isRetracting && !sendingPulse && !isBlocking && !secondCasting)
            {
                CastConnection();
            }

            if(hasConnection && !casting && !isRetracting && !sendingPulse && !isBlocking && !secondCasting)
            {
                
                SecondCast();
            }
        }
    }

    public void CancelCastInput(InputAction.CallbackContext context)
    {
        if (p_Health.IsDead() || p_Health.IsPaused() || p_Movement.BlockingAction()) return;

        if (context.performed)
        {
            if (hasConnection && !sendingPulse && !isRetracting && !casting && !isBlocking && !secondCasting)
            {
                CancelConnection();
            }
            else
            {
                if (!hasConnection && !sendingPulse && !isRetracting && !casting && !secondCasting && Time.time > lastGun) 
                {
                    if ((currentResources > resourcesPerShot - 0.01f) && requireResources)
                    {
                        if (canSecondaryshot && Time.time > lastShot)
                        {
                            SecondaryShot();
                        }
                        else
                        {
                            if (!isBlocking)
                            {
                                ShootGun();
                            }
                        }
                    }
                    else
                    {
                        if (!requireResources)
                        {
                            if (canSecondaryshot && Time.time > lastShot)
                            {
                                SecondaryShot();
                            }
                            else
                            {
                                if (!isBlocking)
                                {
                                    ShootGun();
                                }
                            }
                        }
                    }
                }
            }

        }
    }

    public void PulseInput(InputAction.CallbackContext context)
    {
        if (p_Health.IsDead() || p_Health.IsPaused() || p_Movement.BlockingAction()) return;
        if (context.performed)
        {
            if(!casting && !isRetracting && !sendingPulse && !isBlocking && !secondCasting)
            {
                if (hasConnection)
                {
                    if (connectionCoroutine != null)
                    {
                        StopCoroutine(connectionCoroutine);
                    }
                    CaluclatePulseChoice();
                }
                else
                {
                    if(secondReceiver != null)
                    {
                        //we do just between the two
                        CalculateSecondaryPulse();
                    }
                }
            }
        }
    }
    #endregion

    #region telekensis object

    void CalculateTelekinesObject(GameObject teleKenesisObject,Transform toTarget, bool useTarget)
    {
        if(useTarget)
        {
        Debug.Log("DidTelekenessis to " + toTarget.name);
            Vector3 point = toTarget.gameObject.GetComponent<CastReceiver>().connectPoint.position;
            teleKenesisObject.GetComponent<TelekenesisObject>().SendTowardsTarget(telekinesisForceAmount, point);
        }
        else
        {
            //find target
            Vector3 targ = FindTelekinesissTarget();
            teleKenesisObject.GetComponent<TelekenesisObject>().SendTowardsTarget(telekinesisForceAmount, targ);
            handsAnimator.Play(useTelekinesisClip.name, 0, 0f);
        }

        AudioClip targClip = attackPulseSounds[Random.Range(0, attackPulseSounds.Length)];
        p_Audio.PlayPlayerSound(targClip);

        handsAnimator.SetBool("Hitting", false);
        handsAnimator.SetBool("Casting", false);
    }

    Vector3 FindTelekinesissTarget()
    {
        Vector3 targ;
        RaycastHit hit;
        if(Physics.Raycast(cameraTransForm.position, cameraTransForm.forward, out hit, telekinesisRange, gunMask, QueryTriggerInteraction.Ignore))
        {
            targ = hit.point;
        }
        else
        {
            targ = cameraTransForm.position + (cameraTransForm.forward * telekinesisRange);
        }
        return targ;
    }
    #endregion

    #region Gun handling
    void ShootGun()
    {

        handsAnimator.Play(shootClip.name, 0, 0f);

        p_Audio.PlayPlayerSound(gunDrawSound);

        handsAnimator.SetBool("Hitting", false);

        //in case  of blocked
        if (blockCoroutine != null)
        {
            StopCoroutine(blockCoroutine);
        }
        blockCoroutine = StartCoroutine(GunCoroutine());

        currentResources = Mathf.Clamp(currentResources - resourcesPerShot, 0f, maxResources);
        resourceSlider.value = currentResources;
    }

    IEnumerator GunCoroutine()
    {
        isBlocking = true;

        yield return new WaitForSeconds(timeToCalculateShot);
        canSecondaryshot = true;
        lastShot = Time.time + shotInterval;
        CalculateShot();
        yield return new WaitForSeconds(timeToGetToAway);
        canSecondaryshot = false;
        yield return new WaitForSeconds(gunTime - timeToCalculateShot - timeToGetToAway);

        canSecondaryshot = false;

        isBlocking = false;
        lastGun = Time.time + gunCooldown;
    }

    void CalculateShot()
    {
        //visuals
        gunParticles.Play();
        AudioClip targClip = shootSounds[Random.Range(0, shootSounds.Length)];
        p_Audio.PlayPlayerSound(targClip);

        EZCameraShake.CameraShaker.Instance.ShakeOnce(3f, 0.8f, 0.4f, 0.5f);

        //do actual shooting
        for (int i = 0; i < bulletsPerShot; i++) //for shotguning
        {
            //check the barrel isn't blocked by something
            RaycastHit hit;
            Vector3 dir = (cameraTransForm.position + cameraTransForm.forward * 10f) - bulletSpawnPosition.position;
            if (Physics.Raycast(fromCastPoint.position, dir, out hit, 0.3f, gunMask, QueryTriggerInteraction.Ignore))
            {
                GameObject repInstance = objPooler.SpawnFromPool("BulletRepresentor", bulletSpawnPosition.position, Quaternion.LookRotation(dir));
                repInstance.GetComponent<BulletRepresentor>().SpawnBullet(hit.point);
                objPooler.SpawnFromPool("Bulletparticle", hit.point, Quaternion.LookRotation(hit.normal));
                ShotHit(hit.transform.gameObject, hit.point, dir);
            }
            else
            {
                //we do normal shot
                Vector3 shotDirection = ForwardVector(gunAccuracy);
                RaycastHit bulhit;
                if (Physics.Raycast(cameraTransForm.position, shotDirection, out bulhit, 100f, gunMask, QueryTriggerInteraction.Ignore))
                {
                    objPooler.SpawnFromPool("Bulletparticle", bulhit.point, Quaternion.LookRotation(hit.normal));
                    ShotHit(bulhit.transform.gameObject, bulhit.point, shotDirection);

                    //make the bullet representores
                    GameObject repInstance = objPooler.SpawnFromPool("BulletRepresentor", bulletSpawnPosition.position, Quaternion.LookRotation(shotDirection));
                    repInstance.GetComponent<BulletRepresentor>().SpawnBullet(bulhit.point);

                }
                else
                {
                    Vector3 pos = cameraTransForm.position + (shotDirection * 100f);
                    GameObject repInstance = objPooler.SpawnFromPool("BulletRepresentor", bulletSpawnPosition.position, Quaternion.LookRotation(shotDirection));
                    repInstance.GetComponent<BulletRepresentor>().SpawnBullet(pos);
                }
            }
        }
    }

    void ShotHit(GameObject hit, Vector3 point, Vector3 direction)
    {

        if(hit.GetComponent<CastReceiver>() != null)
        {
            CastReceiver cRec = hit.GetComponent<CastReceiver>();
            if (secondReceiver != null)
            {
                if (cRec == currentConnection)
                {
                    if (secondReceiver.gameObject.GetComponent<I_DamageAble>() != null)
                    {
                        GameObject repInstance = objPooler.SpawnFromPool("BulletRepresentor", currentConnection.connectPoint.position, Quaternion.LookRotation(cameraTransForm.forward));
                        repInstance.GetComponent<BulletRepresentor>().SpawnBullet(secondReceiver.connectPoint.position);

                        secondReceiver.gameObject.GetComponent<I_DamageAble>().TakeDamage(bulletDamage, point, direction);
                    }
                }
                else
                {
                    if (cRec == secondReceiver)
                    {
                        if (currentConnection.gameObject.GetComponent<I_DamageAble>() != null)
                        {
                            GameObject repInstance = objPooler.SpawnFromPool("BulletRepresentor", secondReceiver.connectPoint.position, Quaternion.LookRotation(cameraTransForm.forward));
                            repInstance.GetComponent<BulletRepresentor>().SpawnBullet(currentConnection.connectPoint.position);

                            currentConnection.gameObject.GetComponent<I_DamageAble>().TakeDamage(bulletDamage, point, direction);
                        }
                    }
                }

            }
        }

        if(hit.GetComponent<I_DamageAble>() != null)
        {
            hit.GetComponent<I_DamageAble>().TakeDamage(bulletDamage, point, direction);
            DoHitMarker();
        }

        //we make particles
    }

    void SecondaryShot()
    {
        //stop current gun coroutine
        if (blockCoroutine != null)
        {
            StopCoroutine(blockCoroutine);
        }
        currentResources = Mathf.Clamp(currentResources - resourcesPerShot, 0f, maxResources);
        resourceSlider.value = currentResources;

        blockCoroutine = StartCoroutine(SecondaryShotIenumerator());
    }

    IEnumerator SecondaryShotIenumerator()
    {
        isBlocking = true;
        lastShot = Time.time + shotInterval;
        handsAnimator.Play("Anim_Arms_ShootSecondary", 0, 0f);
        CalculateShot();
        yield return new WaitForSeconds(secondaryShotTime);
        canSecondaryshot = false;
        yield return new WaitForSeconds(putAwayTime);
        isBlocking = false;
    }

    private Vector3 ForwardVector(float accuracy)
    {
        //make rotation based on accuracy to determine where shot will go
        Vector3 forwardVector = Vector3.forward;
        float deviation = Random.Range(0f, accuracy);
        float angle = Random.Range(0f, 360f);
        forwardVector = Quaternion.AngleAxis(deviation, Vector3.up) * forwardVector;
        forwardVector = Quaternion.AngleAxis(angle, Vector3.forward) * forwardVector;
        forwardVector = cameraTransForm.transform.rotation * forwardVector;

        return forwardVector;
    }
    #endregion

    private GameObject currentCastInstance;
    void CastConnection()
    {
        Debug.Log("sent primary cast");

        StartCoroutine(CastDelay());
        handsAnimator.SetBool("Casting", true);
        handParticles.Play();
        handsAnimator.SetTrigger("SendCast");

     casting = true;
    }

    IEnumerator CastDelay()
    {
        isCastDelay = true;
        yield return new WaitForSeconds(castDelay);
        if (!playingBeam)
        {
            playingBeam = true;
            p_Audio.PlayLoopingSound(beamSound);
        }

        AudioClip targClip = castSounds[Random.Range(0, castSounds.Length)];
        p_Audio.PlayPlayerSound(targClip);

        //find postition
        Vector3 targPos = Vector3.zero;
        RaycastHit chhit;
        Vector3 dir = cameraTransForm.position + (cameraTransForm.forward * 0.7f);

        if (Physics.Raycast(fromCastPoint.position, dir, out chhit, 0.7f, checkMask, QueryTriggerInteraction.Ignore))
        {
            targPos = chhit.point;
        }
        else
        {
            targPos = cameraTransForm.position + (cameraTransForm.forward * castRange);
        }

        SetCastLineActive(true);

        GameObject castInstance = Instantiate(castObjPrefab, fromCastPoint.position, fromCastPoint.rotation, prefabHolder);
        currentCastInstance = castInstance;

        CastObject cObject = castInstance.GetComponent<CastObject>();
        currentCast = castInstance;
        cObject.SetInitialValues(targPos, this, castRange);

        Debug.DrawLine(transform.position, targPos, Color.red, 5f);
        isCastDelay = false;
    }
    //to send to catchup
    void SecondCast()
    {
        Debug.Log("sending secondary cast");

        handsAnimator.Play(secondCast.name, 0, 0f);
        handsAnimator.SetBool("Casting", false);

        //find postition
        Vector3 targPos = Vector3.zero;
        RaycastHit chhit;
        Vector3 dir = cameraTransForm.position + (cameraTransForm.forward * 0.7f);

        if (Physics.Raycast(fromCastPoint.position, dir, out chhit, 0.7f, checkMask, QueryTriggerInteraction.Ignore))
        {
            targPos = chhit.point;
        }
        else
        {
            targPos = cameraTransForm.position + (cameraTransForm.forward * castRange);
        }

        SetCastLineActive(true);

        GameObject castInstance = Instantiate(castObjPrefab, fromCastPoint.position, fromCastPoint.rotation, prefabHolder);

        CastObject cObject = castInstance.GetComponent<CastObject>();
        currentCast = castInstance;
        cObject.SetInitialValues(targPos, this, castRange);

        Debug.DrawLine(transform.position, targPos, Color.red, 5f);

        if (connectionCoroutine != null)
        {
            StopCoroutine(connectionCoroutine);
            connectionCoroutine = StartCoroutine(StartedConnection());
        }

        handsAnimator.SetBool("Hitting", false);

        AudioClip targClip = secondCastSounds[Random.Range(0, secondCastSounds.Length)];
        p_Audio.PlayPlayerSound(targClip);

        secondCasting = true;
        casting = true;
    }

    void CaluclatePulseChoice()
    {
        if (fullConnection == false)
        {
            switch (currentConnection.connectionType)
            {
                case CastReceiver.ConnectionType.enemy:
                    SendAttackPulse();
                    break;
                case CastReceiver.ConnectionType.movePoint:
                    GrapplePull(currentConnection, Vector3.zero, false, true);
                    break;
                case CastReceiver.ConnectionType.healthPoint:
                    PullHealth(currentConnection);
                    break;
                case CastReceiver.ConnectionType.telekinesisObject:
                    CalculateTelekinesObject(currentConnection.gameObject,null, false);
                    break;
            }
            hasConnection = false;
        }
        else
        {
            if (blockCoroutine != null)
            {
                StopCoroutine(blockCoroutine);
            }
            blockCoroutine = StartCoroutine(BlockCoroutine(fullconnectionCastTime));
            //we have full connection so we choose what to do for each
            switch (currentConnection.connectionType)
            {
                case CastReceiver.ConnectionType.enemy:

                    switch (secondReceiver.connectionType)
                    {
                        case CastReceiver.ConnectionType.enemy:
                            SendEnemyStun(secondReceiver, currentConnection, enemyStunTime);
                            break;
                        case CastReceiver.ConnectionType.movePoint:
                            PullEnemyToPoint(secondReceiver, currentConnection, 1f);
                            break;
                        case CastReceiver.ConnectionType.healthPoint:
                            SendDamageOrbToEnemy(secondReceiver, currentConnection, true);
                            break;
                        case CastReceiver.ConnectionType.telekinesisObject:
                            CalculateTelekinesObject(secondReceiver.gameObject, currentConnection.transform, true);
                            SendEnemyStun(currentConnection,null, enemyStunTime / 2f);
                            break;
                    }
                    break;
                case CastReceiver.ConnectionType.movePoint:

                    switch (secondReceiver.connectionType)
                    {
                        case CastReceiver.ConnectionType.enemy:
                            PullEnemyToPoint(currentConnection, secondReceiver, 1f);
                            break;
                        case CastReceiver.ConnectionType.movePoint:
                            //pull ourselves between thetwo
                            GrapplePull(currentConnection, secondReceiver.transform.position, true, false);
                            break;
                        case CastReceiver.ConnectionType.healthPoint:
                            PullHealth(secondReceiver);
                            break;
                        case CastReceiver.ConnectionType.telekinesisObject:
                            CalculateTelekinesObject(secondReceiver.gameObject, currentConnection.transform, true);
                            break;
                    }


                    break;
                case CastReceiver.ConnectionType.healthPoint:

                    switch (secondReceiver.connectionType)
                    {
                        case CastReceiver.ConnectionType.enemy:
                            SendDamageOrbToEnemy(secondReceiver, currentConnection, false);
                            break;
                        case CastReceiver.ConnectionType.movePoint:
                            PullHealth(currentConnection);
                            GrapplePull(secondReceiver, Vector3.zero, false, false);
                            break;
                        case CastReceiver.ConnectionType.healthPoint:
                            PullHealth(secondReceiver);
                            PullHealth(currentConnection);
                            break;
                        case CastReceiver.ConnectionType.telekinesisObject:
                            CalculateTelekinesObject(secondReceiver.gameObject, currentConnection.transform, true);
                            PullHealth(currentConnection);
                            break;
                    }
                    break;
                case CastReceiver.ConnectionType.telekinesisObject:
                    switch (secondReceiver.connectionType)
                    {
                        case CastReceiver.ConnectionType.enemy:
                            CalculateTelekinesObject(currentConnection.gameObject, secondReceiver.transform, true);
                            SendEnemyStun(secondReceiver, null, enemyStunTime / 2f);
                            break;
                        case CastReceiver.ConnectionType.movePoint:
                            CalculateTelekinesObject(currentConnection.gameObject, secondReceiver.transform, true);
                            break;
                        case CastReceiver.ConnectionType.healthPoint:
                            CalculateTelekinesObject(currentConnection.gameObject, secondReceiver.transform, true);
                            PullHealth(secondReceiver);
                            break;
                        case CastReceiver.ConnectionType.telekinesisObject:
                            CalculateTelekinesObject(secondReceiver.gameObject, currentConnection.transform, true);
                            CalculateTelekinesObject(currentConnection.gameObject, secondReceiver.transform, true);
                            break;
                    }
                    break;
            }

            handsAnimator.Play(interact.name, 0, 0f);
            fullConnection = false;
            
            hasConnection = false;
            //cancel the receiver
                secondReceiver = null;      
            DissolveLine();
            DissolveSecondRecLine();
        }
        handsAnimator.SetBool("Hitting", false);
        handsAnimator.SetBool("Casting", false);
    }

    #region Just secondary effects
    void CalculateSecondaryPulse()
    {
        if (currentConnection == null) return;

        CastReceiver cRec = secondReceiver;
        DissolveLine();
        secondReceiver = null;

        hasConnection = false;

        switch (cRec.connectionType)
        {
            case CastReceiver.ConnectionType.enemy:

                switch (currentConnection.connectionType)
                {
                    case CastReceiver.ConnectionType.enemy:
                        float stunTime = enemyStunTime * 0.5f; //because not full conection only does half the time
                        SendEnemyStun(currentConnection, cRec, stunTime);
                        break;
                    case CastReceiver.ConnectionType.movePoint:
                        PullEnemyToPoint(currentConnection, cRec, 0.5f);
                        break;
                    case CastReceiver.ConnectionType.healthPoint:
                        PullHealth(currentConnection);
                        SendEnemyStun(cRec, null, enemyStunTime * 0.5f);

                        SendDamageOrbToEnemy(currentConnection, cRec, false);
                        break;
                    case CastReceiver.ConnectionType.telekinesisObject:
                        CalculateTelekinesObject(currentConnection.gameObject, cRec.transform, true);
                        break;
                }

                break;
            case CastReceiver.ConnectionType.movePoint:

                switch (currentConnection.connectionType)
                {
                    case CastReceiver.ConnectionType.enemy:
                        PullEnemyToPoint(cRec, currentConnection, 0.5f);
                        break;
                    case CastReceiver.ConnectionType.movePoint:
                        //pull towards pounts
                        Vector3 vectarget = cRec.connectPoint.transform.position;

                        vectarget = cRec.connectPoint.transform.position + ((currentConnection.connectPoint.position - cRec.connectPoint.transform.position) * 0.5f);

                        Vector3 differential = vectarget - transform.position;
                        p_Movement.AddVelocity(differential * grappleStrength);

                        EZCameraShake.CameraShaker.Instance.ShakeOnce(1.5f, 1f, 0.1f, 0.5f);

                        wSway.JumpSway();

                        grappleParticle.Play();

                        //sounds
                        AudioClip targClip = grappleSounds[Random.Range(0, grappleSounds.Length)];
                        p_Audio.PlayPlayerSound(targClip);
                        break;
                    case CastReceiver.ConnectionType.healthPoint:
                        PullHealth(currentConnection);
                        GrapplePull(cRec, Vector3.zero, false, false);
                        break;
                    case CastReceiver.ConnectionType.telekinesisObject:
                        CalculateTelekinesObject(currentConnection.gameObject, cRec.transform, true);
                        break;
                }
                break;
            case CastReceiver.ConnectionType.healthPoint:

                switch (currentConnection.connectionType)
                {
                    case CastReceiver.ConnectionType.enemy:
                        PullHealth(cRec);
                        SendEnemyStun(currentConnection, null, enemyStunTime * 0.5f);
                        break;
                    case CastReceiver.ConnectionType.movePoint:
                        PullHealth(cRec);
                        GrapplePull(currentConnection, Vector3.zero, false, false);
                        break;
                    case CastReceiver.ConnectionType.healthPoint:
                        PullHealth(cRec);
                        PullHealth(currentConnection);

                        SendDamageOrbToEnemy(cRec, currentConnection, false);
                        break;
                    case CastReceiver.ConnectionType.telekinesisObject:
                        CalculateTelekinesObject(currentConnection.gameObject, cRec.transform, true);
                        break;
                }
                break;
            case CastReceiver.ConnectionType.telekinesisObject:
                switch (currentConnection.connectionType)
                {
                    case CastReceiver.ConnectionType.enemy:

                        CalculateTelekinesObject(cRec.gameObject, currentConnection.transform, true);
                        break;
                    case CastReceiver.ConnectionType.movePoint:
                        CalculateTelekinesObject(cRec.gameObject, currentConnection.transform, true);
                        break;
                    case CastReceiver.ConnectionType.healthPoint:
                        CalculateTelekinesObject(cRec.gameObject, currentConnection.transform, true);
                        break;
                    case CastReceiver.ConnectionType.telekinesisObject:
                        CalculateTelekinesObject(currentConnection.gameObject, cRec.transform, true);
                        CalculateTelekinesObject(cRec.gameObject, currentConnection.transform, true);
                        break;
                }
                break;
        }
        handsAnimator.Play(interactAlternate.name, 0, 0f);
        handsAnimator.SetBool("Hitting", false);
        handsAnimator.SetBool("Casting", false);
        if (blockCoroutine != null)
        {
            StopCoroutine(blockCoroutine);
        }
        blockCoroutine = StartCoroutine(BlockCoroutine(secondaryPulseTime));
    }
    #endregion

    #region Full Connection Effects
    void SendEnemyStun(CastReceiver enemyOne, CastReceiver enemyTwo, float amount)
    {

        handsAnimator.SetBool("Hitting", false);
        enemyOne.GetComponent<EnemyBrain>().StunEnemy(amount);

        GameObject particleInstance = objPooler.SpawnFromPool("StunEffect", enemyOne.connectPoint.position, Quaternion.identity);
        ParticleSystem pSys = particleInstance.GetComponent<ParticleSystem>();
        var main = pSys.main;
        main.duration = amount;
        pSys.Play();

        if (enemyTwo != null)
        {
            enemyTwo.GetComponent<EnemyBrain>().StunEnemy(amount);
            GameObject particleInstance2 = objPooler.SpawnFromPool("StunEffect", enemyTwo.connectPoint.position, Quaternion.identity);
            ParticleSystem pSys2 = particleInstance2.GetComponent<ParticleSystem>();
            var main2 = pSys2.main;
            main2.duration = amount;
            pSys2.Play();
        }

        AudioClip targClip = interactSounds[Random.Range(0, interactSounds.Length)];
        p_Audio.PlayPlayerSound(targClip);
    }

    void PullEnemyToPoint(CastReceiver targetPoint, CastReceiver enemy, float multipler)
    {
        Vector3 differential = targetPoint.connectPoint.position - enemy.transform.position;
        differential *= enemyPullForce * multipler;
        enemy.GetComponent<I_Knockback>().KnockBack(differential.magnitude, enemy.connectPoint.position, differential);

        AudioClip targClip = interactSounds[Random.Range(0, interactSounds.Length)];
        p_Audio.PlayPlayerSound(targClip);
    }

    void SendDamageOrbToEnemy(CastReceiver damagePoint, CastReceiver enemy, bool destroyPoint)
    {

        HealthPoint hPoint = damagePoint.GetComponent<HealthPoint>();
        hPoint.SetInitialValues(this, true, enemy.transform.gameObject);

        AudioClip targClip = interactSounds[Random.Range(0, interactSounds.Length)];
        p_Audio.PlayPlayerSound(targClip);
    }
    #endregion

    #region localEffects
    void PullHealth(CastReceiver targ)
    {


        hasConnection = false;

        DissolveLine();


        HealthPoint hPoint = targ.GetComponent<HealthPoint>();
        hPoint.SetInitialValues(this, false, transform.gameObject);
        if (blockCoroutine != null)
        {
            StopCoroutine(blockCoroutine);
        }
        blockCoroutine = StartCoroutine(BlockCoroutine(healthTime));
        handsAnimator.Play(noHitClip.name, 0, 0f);
        handsAnimator.SetBool("Hitting", false);
        handsAnimator.SetBool("Casting", false);

        AudioClip targClip = interactSounds[Random.Range(0, interactSounds.Length)];
        p_Audio.PlayPlayerSound(targClip);
    }


    public float grappleStrength = 1f;
    void GrapplePull(CastReceiver target,Vector3 secondVector, bool useSecondVector, bool useAnimation)
    {

        hasConnection = false;
        if(blockCoroutine != null)
        {
            StopCoroutine(blockCoroutine);
        }
        blockCoroutine =  StartCoroutine(BlockCoroutine(grappleTime));

        float usestrength = grappleStrength;

        //pull towards pount
        Vector3 vectarget = target.connectPoint.transform.position;

        if (useSecondVector)//makes go between the two points
        {
            usestrength *= 1.5f; //makes velocity bigger for pull
            vectarget = target.connectPoint.transform.position + ((secondVector - target.connectPoint.transform.position) * 0.5f);
        }

        Vector3 differential = vectarget - transform.position;
        p_Movement.AddVelocity(differential * usestrength);

        EZCameraShake.CameraShaker.Instance.ShakeOnce(1.5f, 1f, 0.1f, 0.5f);

        wSway.JumpSway();

        grappleParticle.Play();

        //sounds
        AudioClip targClip = grappleSounds[Random.Range(0, grappleSounds.Length)];
        p_Audio.PlayPlayerSound(targClip);

        DissolveLine();

        handsAnimator.Play(grappleClip.name, 0, 0f);
        handsAnimator.SetBool("Hitting", false);
        handsAnimator.SetBool("Casting", false);
    }
    void SendAttackPulse()
    {

        GameObject target = currentConnection.gameObject;

        EnemyBrain e_Brain = target.GetComponent<EnemyBrain>();

        if (e_Brain.IsDead()) return;

        hasConnection = false;

        StartCoroutine(PulseCoroutine());

        GameObject pulseInstance = Instantiate(pulseObjectPrefab, fromCastPoint.position, Quaternion.LookRotation(transform.forward), prefabHolder);
        PulseObject p_Object = pulseInstance.GetComponent<PulseObject>();

        p_Object.SetInitialValues(target, this);
        DissolveLine();

        AudioClip targClip = attackPulseSounds[Random.Range(0, attackPulseSounds.Length)];
        p_Audio.PlayPlayerSound(targClip);

        handsAnimator.Play(damageOrb.name, 0, 0f);
        handsAnimator.SetBool("Hitting", false);
        handsAnimator.SetBool("Casting", false);
    }

    public float grappleTime = 0.7f;
    private bool isBlocking = false;

    private Coroutine blockCoroutine;
    IEnumerator BlockCoroutine(float time)
    {
        isBlocking = true;
        yield return new WaitForSeconds(time);
        isBlocking = false;
    }
    IEnumerator PulseCoroutine()
    {
        sendingPulse = true;
        yield return new WaitForSeconds(pulseTime);
        sendingPulse = false;
    }
    public float pulseTime = 0.5f;

    private bool sendingPulse = false;
    #endregion

    void DissolveLine()
    {
        SetCastLineActive(false);
    }

    void CancelConnection()
    {

        if (connectionCoroutine != null)
        {
            StopCoroutine(connectionCoroutine);
        }

        hasConnection = false;

        RetractCast(currentConnection.connectPoint.transform.position);

        handsAnimator.SetBool("Hitting", false);
        handsAnimator.SetBool("Casting", false);
    }


    private GameObject currentCast;

    void SetCastLineActive(bool value)
    {
        if (value)
        {
            castLineRenderer.enabled = true;
        }
        else
        {
            castLineRenderer.enabled = false;
            handParticles.Stop();
        }
    }

    private bool fullConnection = false;
    public void CastHitSomething(Vector3 point, GameObject hit)
    {
        casting = false;
        bool currentSecondCast = secondCasting;
        if (secondCasting)
        {
            secondCasting = false;
        }

        Debug.Log("cast hit at " + point + " " + hit);
        
        if(hit.GetComponent<CastReceiver>() != null)
        {
            DoHitMarker();

            //if we have established a second recevoir
            //we make this our connection
            CastReceiver cRec = hit.GetComponent<CastReceiver>();
            if(!secondCasting && secondReceiver != null)
            {
                //we check we hit one of the connectied
                CastReceiver secRec = secondReceiver;
                CastReceiver cur = currentConnection;
                if(cRec == secRec && cRec != cur)
                {
                    currentConnection = secRec;
                    secondReceiver = cur;
                    fullConnection = true;
                }
                else
                {
                    if(cRec == cur && cRec != secRec)
                    {
                        currentConnection = cur;
                        secondReceiver = secRec;
                        fullConnection = true;
                    }
                    else
                    {
                        //stop the second line
                        if (secondReceiverLifeCoroutine != null)
                        {
                            StopCoroutine(secondReceiverLifeCoroutine);
                        }
                        DissolveSecondRecLine();
                        secondReceiver = null;
                        fullConnection = false;
                    }
                }
            }
            //for establishing reciever
            if (hasConnection && currentSecondCast)
            {
                //handle estabilshing seconds conenction
                if(cRec == currentConnection)
                {
                    hasConnection = false;

                    //stun enemy
                    if(cRec.connectionType == CastReceiver.ConnectionType.enemy)
                    {
                        SendEnemyStun(cRec, null, enemyStunTime * 0.3f);
                    }

                    RetractCast(point);
                    return;
                }
                if(secondReceiver != null)
                {
                    DissolveSecondRecLine();
                }
                secondReceiver = cRec;
                if (secondReceiverLifeCoroutine != null)
                {
                    StopCoroutine(secondReceiverLifeCoroutine);
                }
                secondReceiverLifeCoroutine = StartCoroutine(SecondRecieverTime());
                switch (cRec.connectionType)
                {
                    case CastReceiver.ConnectionType.enemy:
                        EnemyBrain e_Brain = hit.GetComponent<EnemyBrain>();

                        if (e_Brain.IsDead())
                        {
                            RetractCast(point);
                            return;
                        }

                        break;
                    case CastReceiver.ConnectionType.movePoint:
                        break;
                    case CastReceiver.ConnectionType.healthPoint:
                        break;
                }

                //disolve current line
                SetCastLineActive(false);
                if(connectionCoroutine != null) //this is to prevent coroutine deactivating when starting again
                {
                    StopCoroutine(connectionCoroutine);
                }
                hasConnection = false; //this is so we aren't currently conected
                handsAnimator.SetBool("Hitting", false);
                handsAnimator.SetBool("Casting", false);
            }
            else
            {
                switch (cRec.connectionType)
                {
                    case CastReceiver.ConnectionType.enemy:
                        EnemyBrain e_Brain = hit.GetComponent<EnemyBrain>();

                        if (e_Brain.IsDead())
                        {
                            hasConnection = false;
                            RetractCast(point);
                            handsAnimator.SetBool("Hitting", false);
                            handsAnimator.SetBool("Casting", false);
                            return;
                        }

                        break;
                    case CastReceiver.ConnectionType.movePoint:
                        break;
                    case CastReceiver.ConnectionType.healthPoint:
                        break;
                }
                ConnectTo(hit);
            }
        }
        else
        {
            if (currentSecondCast)
            {
                DissolveLine();
            }
            else
            {
                //we retract
                RetractCast(point);
            }
        }
        handsAnimator.SetBool("Casting", false);
    }

    private Coroutine secondReceiverLifeCoroutine;
    IEnumerator SecondRecieverTime()
    {
        yield return new WaitForSeconds(secondCastTime);
        fullConnection = false;
        DissolveSecondRecLine();
        secondReceiver = null;      
    }

    private CastReceiver secondReceiver;

    public void NoCastHit(Vector3 atPosition)
    {

        handsAnimator.SetBool("Casting", false);

        if (secondCasting)
        {
            DissolveLine();
        }
        else
        {
            handsAnimator.SetTrigger("Retract");
            RetractCast(atPosition);
        }


        Debug.Log("No cast hit");
        casting = false;
        secondCasting = false;
        hasConnection = false;
    }

   public bool ConnectedToEnemy()
    {
         if(hasConnection && currentConnection.connectionType == CastReceiver.ConnectionType.enemy)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    CastReceiver currentConnection;
    void ConnectTo(GameObject target)
    {
        CastReceiver cRec = target.GetComponent<CastReceiver>();
        handsAnimator.SetBool("Hitting", true);
        handsAnimator.SetBool("Casting", false);
        //resources
        lastResources = Time.time + 0.1f;
        currentResourceRate = minResourcesPerSecond;

        currentResources = Mathf.Clamp(currentResources + connectAddResources, 0f, maxResources);
        resourceSlider.value = currentResources;

        hasConnection = true;

        currentConnection = cRec;


        connectionCoroutine = StartCoroutine(StartedConnection());
    }

    private Coroutine connectionCoroutine;

    private float connectionTarget = 0f; //for setting the denominator of connection percent
    //we also set up for visual
    IEnumerator StartedConnection()
    {
        var t = 0f;
        handsAnimator.SetFloat("ConnectPercent", t);
        while (t < 1)
        {
            t += Time.deltaTime / allowedCastTime;
            //make detailedConnection animation
             handsAnimator.SetFloat("ConnectPercent", t);
            yield return null;
        }

        if (hasConnection && !isRetracting && !isBlocking && !casting)
        {
            hasConnection = false;
            handsAnimator.SetBool("Hitting", false);
            handsAnimator.SetBool("Casting", false);
            RetractCast(lastConPoint);
        }
    }

    void RetractCast(Vector3 endPos)
    {
        Debug.Log("retracting");
        if(connectionCoroutine != null)
        {
            StopCoroutine(connectionCoroutine);
        } 
        retractPos.position = endPos;
        isRetracting = true;

        retractCoroutine =  StartCoroutine(RetractIenumerator());

        AudioClip targClip = retractedSounds[Random.Range(0, retractedSounds.Length)];
        p_Audio.PlayPlayerSound(targClip);
    }

    private Coroutine retractCoroutine;
    IEnumerator RetractIenumerator()
    {
        yield return new WaitForSeconds(retractTime);
        SetCastLineActive(false);
        isRetracting = false;
    }

    public void DamageAllReceivers(float amount)
    {
        if (!hasConnection) return;
        Debug.Log("sending damage to all receivers");

        //do damage
            if (currentConnection.GetComponent<I_DamageAble>() != null)
            {
                currentConnection.GetComponent<I_DamageAble>().TakeDamage(amount, currentConnection.connectPoint.position, currentConnection.connectPoint.position - transform.position);
            }

        if (secondReceiver != null)
        {
            if (secondReceiver.GetComponent<I_DamageAble>() != null)
            {
                secondReceiver.GetComponent<I_DamageAble>().TakeDamage(amount, secondReceiver.connectPoint.position, secondReceiver.connectPoint.position - transform.position);
            }
        }
    }

    public float hitmarkerTime = 0.2f;
    private float lasthitmarker;

    public void DoHitMarker()
    {
        lasthitmarker = Time.time + hitmarkerTime;

        hitmarker.SetActive(true);
    }
}
