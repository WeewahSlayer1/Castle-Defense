using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    //=================  Variables  =======================================================================//
    [SerializeField]    Transform   hierarchySoldiers;
    [SerializeField]    int         spawnCount;
    public              Spawn[]     spawns;
    [SerializeField]    Mesh[]      meshOptions;
    [SerializeField]    GameObject  unitObj;

    public              Unit_Squad.Formation    formation;

    [SerializeField]    float spawnTimer;
    [SerializeField]    float spawnInterval = 1000;

    [SerializeField]    Unit.Team team;

    //int               meshChildIndex;
    //int               meshOptionIndex = 0;

    //=============  Function - Update()  =======================================================================//
    void Update()
    {
        spawnTimer -= Time.deltaTime;

        if (spawnTimer <= 0)
            SpawnEnemies(spawnCount, Random.Range(0, spawns.Length));
    }

    //=============  Function - SpawnEnemies()  =================================================================//
    public void SpawnEnemies(int num, int spawn)
    {
        if (num > 0)
        {
            spawnTimer = spawnInterval;

            if (spawns.Length > 0)
            {
                if (spawns[0].squad == null)
                    ResetSpawnSquad(spawn);

                for (int i = 0; i < num; i++)
                    SpawnEnemy(spawn, num, i);
            }
        }
        else
            Debug.Log("ERROR: " + this.name + " SpawnEnemies() has 'num' value of 0");
    }

    //=============  Function - ResetSpawnSquad()  ==========================================//
    public void ResetSpawnSquad(int i)
    {
        Vector3 squadRelativePos = spawns[i].area.center + spawns[i].area.transform.right * Random.Range(-spawns[i].area.size.x, spawns[i].area.size.x) + spawns[i].area.transform.forward * Random.Range(-spawns[i].area.size.z, spawns[i].area.size.z);

        spawns[i].squad = Unit_Squad.CreateSquad(GameObject.Find("Hierarchy - Soldiers").transform, team, formation);
        spawns[i].squad.squadTransform.position = spawns[i].area.transform.position + squadRelativePos;
        spawns[i].squad.squadTransform.rotation = spawns[i].area.transform.rotation;
    }

    //=============  Function - SpawnEnemy()  ==========================================//
    public void SpawnEnemy(int spawnIndex, int num, int unitIndex)
    {
        // Train unit
        Unit u = Building.TrainUnit(spawns[spawnIndex].squad.squadTransform.position, unitObj, hierarchySoldiers, formation, spawns[spawnIndex].area.transform, null, spawns[spawnIndex].squad);

        // Assign FrontLineVars
        u.combatUnitVars.squad.FrontLineVarsAssignment(unitIndex, u.combatUnitVars.squad.unitList);
    }

    //=========================  Struct - Spawn  ================================================================//
    [System.Serializable]
    public struct Spawn
    {
        public string       Name;
        public BoxCollider  area;
        public Building     building;
        public Unit_Squad   squad;
    }
}
