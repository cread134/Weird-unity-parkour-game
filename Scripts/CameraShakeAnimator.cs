using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraShakeAnimator : MonoBehaviour
{
    public void ShortCamShake()
    {
        EZCameraShake.CameraShaker.Instance.ShakeOnce(1.5f, 0.8f, 0.1f, 0.2f);
    }

    public void LongCamShake()
    {
        EZCameraShake.CameraShaker.Instance.ShakeOnce(3f, 0.8f, 0.4f, 1.5f);
    }
}
