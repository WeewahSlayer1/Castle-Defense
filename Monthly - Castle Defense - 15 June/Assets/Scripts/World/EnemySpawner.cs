using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] GameObject spawnObj;
    [SerializeField] Transform  hierarchySoldiers;
    [SerializeField] int        spawnCount;
    [SerializeField] Spawn[]    spawns;
    [SerializeField] Mesh[]     meshOptions;
    [SerializeField] int        meshChildIndex;

    float spawnTimer = 0;
    const float spawnInterval = 60;
    int meshOptionIndex = 0;

    //=========================  Update()  =======================================================================//
    void Update()
    {
        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0)
            SpawnEnemies(spawnCount);
    }

    //=========================  SpawnEnemies()  =================================================================//
    public void SpawnEnemies(int num)
    {
        spawnTimer = spawnInterval;

        if (spawns.Length > 0)
        {
            for (int i = 0; i < num; i++)
            {
                Spawn spawn = spawns[Random.Range(0, spawns.Length)];

                Vector3 spawnPos =
                    spawn.area.transform.position
                    + Random.Range(-spawn.area.size.x, spawn.area.size.x) * spawn.area.transform.right / 2
                    + Random.Range(-spawn.area.size.z, spawn.area.size.z) * spawn.area.transform.forward / 2;


                GameObject character = Object.Instantiate(spawnObj, spawnPos, Quaternion.identity, hierarchySoldiers);
                character.transform.GetChild(meshChildIndex).GetComponent<SkinnedMeshRenderer>().sharedMesh = meshOptions[meshOptionIndex];
                meshOptionIndex++;
                if (meshOptionIndex == meshOptions.Length)
                    meshOptionIndex = 0;
            }
        }
    }

    //=========================  Struct - Spawn  ================================================================//
    [System.Serializable]
    public struct Spawn
    {
        public string Name;
        public BoxCollider area; 
    }
}
