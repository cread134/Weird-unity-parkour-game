using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CastReceiver : MonoBehaviour
{
    public enum ConnectionType { enemy, movePoint, healthPoint, telekinesisObject}
    public ConnectionType connectionType;
    public Transform connectPoint;
}
