using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletRepresentor : MonoBehaviour
{
    public float metresPerSecond = 100f;

    public TrailRenderer tRenderer;
    bool spawned = false;

    public void SpawnBullet(Vector3 targetPosition)
    {
        tRenderer.Clear();
        spawned = true;
        StartCoroutine(MoveToPosition(transform, targetPosition, Vector3.Distance(transform.position, targetPosition) / metresPerSecond));
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

        ReachedPosition();
    }

    void ReachedPosition()
    {
        spawned = false;
    }
}
