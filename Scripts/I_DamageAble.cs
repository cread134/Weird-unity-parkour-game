using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface I_DamageAble
{
    void TakeDamage(float amount, Vector3 point, Vector3 direction);
}
