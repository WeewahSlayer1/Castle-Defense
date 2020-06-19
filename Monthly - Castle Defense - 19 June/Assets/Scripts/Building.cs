using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Building : MonoBehaviour
{
    public Unit.Team team;
    public Unit_Squad squad;

    public GameObject BuildMenuScroll;
    public Transform UIScroll;
    public List<Vector3> SpawnWaypoints;

    public int UnitLimit;
    public int UnitColumns;

    [System.NonSerialized]
    public float lastSpawnTime;

    [System.NonSerialized]
    public float spawnInterval = 0.5f;
}
