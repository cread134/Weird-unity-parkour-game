using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class CardData : ScriptableObject
{
    public string cardName;
    [TextArea]public string description;

    public float useTime;
    public AnimationClip associatedAnimation;
}
