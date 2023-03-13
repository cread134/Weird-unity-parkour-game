using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class Obstacle : ScriptableObject
{
    public enum ObstacleType { obstacle, utility}

    [Header("Settings")]
    public GameObject obstaclePrefab;
    public ObstacleType obstacleType;
}
