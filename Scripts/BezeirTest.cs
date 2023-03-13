using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BezeirTest : MonoBehaviour
{

    public LineRenderer lRenderer;
    public Transform startPos;
    public Transform endPoint;
    public Transform midPoint;

    public int iterations = 5;

    // Update is called once per frame

    private void Start()
    {
        lRenderer.positionCount = iterations + 1;
    }

    void Update()
    {
        GenerateCurve();
    }

    void GenerateCurve()
    {
        float lerpJump = 1f / iterations;
        
        for (int i = 0; i < iterations + 1; i++)
        {
            float amount = i * lerpJump;

            Vector3 lerpA = Vector3.Lerp(startPos.position, midPoint.position, amount);
            Vector3 lerpB = Vector3.Lerp(midPoint.position, endPoint.position, amount);
            Vector3 lerpBetween = Vector3.Lerp(lerpA, lerpB, amount);

            lRenderer.SetPosition(i, lerpBetween);
        }
    }
}
