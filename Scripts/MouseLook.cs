using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
public class MouseLook : MonoBehaviour
{
    private PlayerAudioScript p_Audio;
    private PlayerMovement p_Movement;
    private PlayerHealthManager p_Health;
    private CharacterController charCont;

    private bool overrideMouseLook = false;
    public bool canLook = true;

    private Vector2 targetSensitivity;
    private Vector2 targetDirection;
    private Vector2 targetCharacterDirection;
    private Vector2 _mouseAbsolute;
    private Vector2 _smoothMouse;


    public Vector2 clampInDegrees = new Vector2(360, 180);

    private float rotAmount;

    [Header("Values")]

    [SerializeField]
    private Vector2 lookSensitivity = new Vector2(2, 2);
    [SerializeField]
    private Vector2 smoothing = new Vector2(3, 3);

    [SerializeField]
    private float lerpSpeed = 1f;

    [SerializeField]
    private Transform mouseLooker;
    [SerializeField]
    private Transform playerBody;

    [SerializeField]
    private Transform toBob;

    [Space]
    public float headOffset;
    public float heighTransitionSpeed = 8f;
    [Space]
    public float leanTransitionSpeed = 7f;

    // Start is called before the first frame update
    void Awake()
    {
        p_Audio = GetComponent<PlayerAudioScript>();
        p_Movement = GetComponent<PlayerMovement>();
        p_Health = GetComponent<PlayerHealthManager>();
        charCont = GetComponent<CharacterController>();

        //we load the saved sensitity
        float sensMultiplier = PlayerPrefs.GetFloat("sensitivity", 1f);
        targetSensitivity = lookSensitivity * sensMultiplier;
    }

    

    // Update is called once per frame
    void Update()
    {   
        if (overrideMouseLook == false && canLook && !p_Health.IsPaused())
        {
            PerformMouseLook();
            DoHeadBob();
        }
    }

    private float rawX;
    private float rawY;
    public void MouseInput(InputAction.CallbackContext context)
    {
        Vector2 contextVector = context.ReadValue<Vector2>().normalized;
         rawX = contextVector.x;
        rawY = contextVector.y;
    }

    void PerformMouseLook()
    {
     
        var targetOrientation = Quaternion.Euler(targetDirection);
        var targetCharacterOrientation = Quaternion.Euler(targetCharacterDirection);

        // Get raw mouse input for a cleaner reading on more sensitive mice.
           var mouseDelta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
        //var mouseDelta = new Vector2(rawX, rawY);

        // Scale input against the sensitivity setting and multiply that against the smoothing value.
        mouseDelta = Vector2.Scale(mouseDelta, new Vector2(targetSensitivity.x * smoothing.x, targetSensitivity.y * smoothing.y));

        // Interpolate mouse movement over time to apply smoothing delta.
        _smoothMouse.x = Mathf.Lerp(_smoothMouse.x, mouseDelta.x, 1f / smoothing.x);
        _smoothMouse.y = Mathf.Lerp(_smoothMouse.y, mouseDelta.y, 1f / smoothing.y);

        // Find the absolute mouse movement value from point zero.
        _mouseAbsolute += _smoothMouse;

        // Clamp and apply the local x value first, so as not to be affected by world transforms.

        if (clampInDegrees.x < 360) _mouseAbsolute.x = Mathf.Clamp(_mouseAbsolute.x, -clampInDegrees.x * 0.5f, clampInDegrees.x * 0.5f);

        // Then clamp and apply the global y value.
        if (clampInDegrees.y < 360) _mouseAbsolute.y = Mathf.Clamp(_mouseAbsolute.y, -clampInDegrees.y * 0.5f, clampInDegrees.y * 0.5f);

     //   Debug.Log("Mouse abs x " + _mouseAbsolute.x);

        if (doVectorClamp)
        {
            _mouseAbsolute.x = Mathf.Clamp(_mouseAbsolute.x, minClamp, maxClamp);
        }

        useLean = Mathf.Lerp(useLean, targetLeanAngle, Time.deltaTime * leanTransitionSpeed);
        Quaternion useQuart = (Quaternion.AngleAxis(-_mouseAbsolute.y, targetOrientation * Vector3.right) * targetOrientation) * Quaternion.AngleAxis(rotAmount, Vector3.forward) * Quaternion.AngleAxis(useLean,Vector3.forward);

        mouseLooker.localRotation = useQuart;

        // If there's a character body that acts as a parent to the camera
        var yRotation = Quaternion.AngleAxis(_mouseAbsolute.x, Vector3.up);
        playerBody.transform.rotation = yRotation * targetCharacterOrientation;

        float lookValUp = mouseLooker.transform.localEulerAngles.x;
        if (lookValUp > 100f) //this is to fix the retrieving of an obtuse angle 
        {
            lookValUp = (360f - lookValUp) * -1f;
        }

        //update position
        mouseLooker.transform.localPosition = Vector3.Lerp(mouseLooker.transform.localPosition, new Vector3(0f, targetHeight, 0f), heighTransitionSpeed * Time.deltaTime);
    }

    private float index;
    private float lastFootstep;

    private float bobAmount;


    //headbobbing
    private float targetSpeed;
    private float targetAmplitude;

    private Vector3 targetPos;

    [SerializeField] private float transitionSpeed = 8f;
    void DoHeadBob()
    {
        index += Time.deltaTime;

        bobAmount = Mathf.Sin(index * targetSpeed) * targetAmplitude; //le sin wave

        Vector3 targetVector = new Vector3(targetPos.x, bobAmount, targetPos.z);

        toBob.localPosition = Vector3.Lerp(toBob.localPosition, targetVector, transitionSpeed * Time.deltaTime);

        //reset time
        if (index * targetSpeed > ((Mathf.PI * 2 / 3) / targetSpeed))
        {
            index = index - (Mathf.PI * 2) / targetSpeed;

            //make the footstep noise
            if (p_Movement.IsGrounded() && p_Movement.IsMoving() == true && Time.time > lastFootstep)
            {
                if (doFootStepSound)
                {
                    p_Audio.PlayFootStepSound();
                    lastFootstep = Time.time + 0.1f;
                }
            }
        }
    }
    public float GetBobValue()
    {
        return bobAmount;
    }

    public void SetHeadbobValues(float amplitude, float speed)
    {
        targetAmplitude = amplitude;
        targetSpeed = speed;
    }

    private float targetHeight = 1.85f;
    public void SetPlayerHeight(float height)
    {
        charCont.height = height;
        charCont.center = new Vector3(0f, height / 2f, 0f);

        //set targetheihgt
        targetHeight = height - headOffset;
    }

    private bool doFootStepSound;
    public void SetPlayFootSteps(bool value)
    {
        doFootStepSound = value;
    }

    bool doVectorClamp = false;

    private float maxClamp;
    private float minClamp;
    public void ClampLookToVector(float clampAngle)
    {

        doVectorClamp = true;

        maxClamp = _mouseAbsolute.x + clampAngle;
        minClamp = _mouseAbsolute.x - clampAngle;
        if(minClamp > maxClamp)//switch to account for negative
        {
            float min = minClamp;
            minClamp = maxClamp;
            maxClamp = min;
        }
    }

    public void CancelLookClamp()
    {
        doVectorClamp = false;
    }

    private float useLean;
    private float targetLeanAngle;
    public void SetCamLeanAngle(float angle) //positive is left
    {
        targetLeanAngle = angle;
    }

    public void SetViewToDirection(Vector3 direction, float setTime)
    {
        if(setLookCoroutine != null)
        {
            StopCoroutine(setLookCoroutine);
        }

        setLookCoroutine = StartCoroutine(SetLookDirEnum(direction, setTime));
    }

    private Coroutine setLookCoroutine;

    private bool settingLook = false;
    IEnumerator SetLookDirEnum(Vector3 dir, float doTime)
    {
        settingLook = true;

        var curBodyDir = playerBody.transform.forward;
        Vector3 targetBodyDir = new Vector3(dir.x, 0f, dir.y);

        var t = 0f;
        while (t < 1)
        {
            t += Time.deltaTime / doTime;
            playerBody.transform.rotation = Quaternion.Slerp(Quaternion.LookRotation(curBodyDir), Quaternion.LookRotation( targetBodyDir), t);
            yield return null;
        }

        settingLook = false;
    }

}
