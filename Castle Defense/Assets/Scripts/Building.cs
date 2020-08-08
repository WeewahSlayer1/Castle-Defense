using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Building : MonoBehaviour
{
    public UnitTrainingVars unitTrainingVars;
    public Unit_Squad       squad;

    //=============  ClickedOnScrollTrainUnit()  ===================================// Called by HUD_button
    public static Unit TrainUnit(Vector3 relativePos, GameObject unitObj, Transform hierarchy_units, Unit_Squad.Formation formation, Transform spawnerTransform, Building building, Unit_Squad squad)
    {
        UnitTrainingVars utv;
        if (building != null)
            utv = building.unitTrainingVars;
        else {
            utv = new UnitTrainingVars();
            utv.team = squad.team;
            utv.UnitLimit = int.MaxValue;
            utv.SpawnWaypoints = new List<Vector3>();
        }

        // Create Squad
        if (squad == null) {
            squad = Unit_Squad.CreateSquad(hierarchy_units, building.unitTrainingVars.team, formation);
            squad.squadTransform.position = spawnerTransform.position + relativePos.x * spawnerTransform.right + relativePos.z * spawnerTransform.forward;
            squad.squadTransform.rotation = spawnerTransform.rotation;
        }

        // If we are below UnitLimit, train units
        if (squad.unitList.Count < utv.UnitLimit) {
            //Set spawnPos
            Vector3 spawnPos = spawnerTransform.position + relativePos.x * spawnerTransform.right + relativePos.z * spawnerTransform.forward;
            Unit u = Object.Instantiate(unitObj, spawnPos, Quaternion.identity, squad.transform).GetComponent<Unit>();

            u.Initialise(squad, "Soldier #" + (squad.unitList.Count).ToString());
            squad.unitList.Add(u);
            
            if (!u.navMeshAgent.isOnNavMesh) {
                RaycastHit hit;

                if (Physics.Raycast(spawnPos, Vector3.down, out hit, 1.0f, LayerMask.GetMask("Ground")))
                    u.transform.position = hit.point;
            }

            // Set squad position
            if (utv.SpawnWaypoints.Count < 2) {
                squad.squadTransform.position = relativePos;
                spawnPos = Unit_Squad.FormationPos(Mathf.RoundToInt(squad.formation.columns), squad.unitList.Count - 1, squad.unitList[0].combatUnitVars.type, u.formationRandom, squad.unitList.Count, squad.squadTransform);
                u.navMeshAgent.SetDestination(spawnPos);
                u.transform.position = spawnPos;
            }
            else {
                for (int i = 0; i < utv.SpawnWaypoints.Count; i++) {
                    Vector3 wayPoint = spawnerTransform.position + utv.SpawnWaypoints[i].x * spawnerTransform.right + utv.SpawnWaypoints[i].z * spawnerTransform.forward;
                    u.wayPoints.Add(wayPoint);
                    u.currentState = Unit.UnitState.spawning;
                }

                if (u.wayPoints.Count > 0)
                    squad.squadTransform.position = u.wayPoints[utv.SpawnWaypoints.Count - 1];
                else
                    squad.squadTransform.position = relativePos;

                squad.squadTransform.rotation = spawnerTransform.rotation;

                //Replace last wayPoint with formationPosition
                u.wayPoints[u.wayPoints.Count - 1] = Unit_Squad.FormationPos(Mathf.RoundToInt(squad.formation.columns), squad.unitList.Count - 1, squad.unitList[0].combatUnitVars.type, u.formationRandom, squad.unitList.Count, squad.squadTransform);
            }

            //If we've just added a new row, update formationPositions of every other squad member
            if (squad.unitList.Count != 1 && (squad.unitList.Count - 1) % squad.formation.columns == 0) {
                for (int i = 0; i < squad.unitList.Count - 1; i++) {
                    Vector3 newPos = Unit_Squad.FormationPos(Mathf.RoundToInt(squad.formation.columns), i, squad.unitList[0].combatUnitVars.type, squad.unitList[i].formationRandom, squad.unitList.Count, squad.squadTransform);

                if (utv.SpawnWaypoints.Count >= 2 && squad.unitList[i].currentState == Unit.UnitState.spawning)
                    squad.unitList[i].wayPoints[squad.unitList[i].wayPoints.Count - 1] = newPos;
                else if (squad.unitList[i].currentState == Unit.UnitState.available)
                    squad.unitList[i].StartCoroutine(squad.unitList[i].CoRoutine_AssignObjective(newPos, true));
                }
            }

            return u;
        }
        else
            Debug.Log("Squad size is at limit of building.unitLimit, please relocate to another barracks");

        return null;
    }
    
    [System.Serializable]
    public struct UnitTrainingVars
    {
        public Unit.Team team;

        public List<Vector3> SpawnWaypoints;

        public int UnitLimit;
        //public int UnitColumns;

        [System.NonSerialized]
        public float lastSpawnTime;

        public float spawnInterval;

    }
    
}
