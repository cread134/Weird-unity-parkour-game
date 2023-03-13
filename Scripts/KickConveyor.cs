using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KickConveyor : MonoBehaviour
{
    public KickManager kickManager;

    public void KickCam()
    {
        kickManager.KickCam();
    }

    public void CalculateKick()
    {
        kickManager.DoKickCalculation();
    }
    public void KickSound()
    {
        kickManager.DoKickSound();
    }
}
