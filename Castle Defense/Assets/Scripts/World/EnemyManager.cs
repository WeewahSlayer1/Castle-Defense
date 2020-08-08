using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    //========================  Variables  ============================================//
    [SerializeField] EnemySpawner spawner;
    List<Unit_Squad> squads = new List<Unit_Squad>();
    const int   unitCount = 100;

    Unit.Team team = Unit.Team.orc;

    const int   squadSizeMin = 10;
    const int   squadSizeMax = 30;
    int         nextSquadSize = squadSizeMax;
    int         deadUnits = 0;

    float       timeTillNextUpdate;
    const float timeBetweenUpdates = 1.0f;
    
    //========================  Function - Start()  ============================================//
    private void Start() {
        int i = Random.Range(0, spawner.spawns.Length);
        spawner.SpawnEnemies(unitCount, i);
        squads.Add(spawner.spawns[i].squad);
    }
    
    //========================  Function - Update()  ============================================//
    private void Update()
    {
        timeTillNextUpdate -= Time.deltaTime;

        if (timeTillNextUpdate <= 0) {
            timeTillNextUpdate = timeBetweenUpdates;

            SendSquads();

            int currentUnitCount = 0;

            foreach (Unit_Squad squad in squads)
                foreach (Unit u in squad.unitList)
                    currentUnitCount++;

            if (currentUnitCount + nextSquadSize < unitCount)
                ReplenishForces(nextSquadSize);
        }
    }

    //========================  Function - SendSquads()  ===================================//
    void SendSquads()
    {
        foreach (Unit_Squad squad in squads) {
            if (squad.enemySquad == null) {
                squad.GetEnemySquad();

                if (squad.enemySquad != null && squad.enemySquad.unitList.Count > 0) {
                    squad.squadTarget.position = squad.enemySquad.SquadPos();
                    Vector3 lookAt = squad.squadTarget.position + (squad.squadTarget.position - squad.squadTransform.position).normalized;
                    lookAt.y = squad.squadTarget.position.y;
                    squad.squadTarget.LookAt(lookAt);
                    squad.MoveSquadToFormationForMarch(squad.squadTarget);

                    foreach (Unit u in squad.unitList)
                        u.anims[0].SetBool("Guard", true);
                }
            }
        }
    }



    //========================  Function - ReplenishForces()  ===================================//
    void ReplenishForces(int num)
    {
        int spawnIndex = Random.Range(0, spawner.spawns.Length);
        spawner.ResetSpawnSquad(spawnIndex);
        squads.Add(spawner.spawns[spawnIndex].squad);

        for (int i = 0; i < num; i++) {
            spawner.SpawnEnemy(spawnIndex, num, i);
            deadUnits++;
        }

        nextSquadSize = (int)Random.Range(squadSizeMin, squadSizeMax);
    }
}
