using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit_Squad : MonoBehaviour
{
    public Unit.Team    team;
    public List<Unit>   unitList;
    public bool         enemiesAcquired;
    float               timeTillNextUpdate;
    const float         timeBetweenUpdates = 5;
    public Transform    squadTransform;

    public Formation    formation;

    //============ Update()  ===============================//
    private void Update()
    {
        //
        if (unitList.Count == 0)
            Destroy(this.gameObject);

        //------------------------------  Acquire or nullify enemyUnit  -------------------------------------------------//
        timeTillNextUpdate -= Time.deltaTime;

        if (timeTillNextUpdate <= 0 )
        {
            // ---------------  Rest timer  -------------------------------------------------------------------------//
            timeTillNextUpdate = timeBetweenUpdates;

            // ---------------  Update squadPos  ---------------------------------------------------------------------//
            //squadTransform.position = SquadPos();

            // ---------------  Target acquisition -------------------------------------------------------------------//
            if (team != Unit.Team.none)
            {
                // ---------------  Get a list of squads in area  --------------------------------------------//
                Unit_Squad enemySquad = GetEnemySquad(UnityEngine.Object.FindObjectsOfType<Unit_Squad>());

                // ---------------  Assign enemies to individual units  --------------------------------------//
                if (enemySquad != null)
                {
                    Debug.Log("EnemySquad != null");
                    for (int i = 0; i < unitList.Count; i++)
                    {
                        if (unitList[i].enemyUnit == null)
                        {
                            unitList[i].enemyUnit = GetEnemyBot(enemySquad, unitList[i]);
                            if (unitList[i].enemyUnit != null)
                                unitList[i].AssignEnemy(unitList[i].enemyUnit);
                        }
                        else
                        {
                            if (unitList[i].enemyUnit.currentState == Unit.UnitState.dying)
                            {
                                unitList[i].enemyUnit = null;

                                if (unitList[i].currentState != Unit.UnitState.attacking)
                                    unitList[i].navMeshAgent.SetDestination(this.transform.position);
                            }
                        }
                    }
                }
            }

            /*
            // ---------------  Formation ----------------------------------------------------------------------------//
            if (unitList.Count < 10)
            {
                formation.columns = 5;
                formation.spacing = 1;
                formation.random = 0.25f;
            }
            
            for (int i = 0; i < unitList.Count; i++)
                if (unitList[i].currentState == Unit.UnitState.notAttacking)
                {
                    Vector3 objective = FormationPos(formation.columns, i, formation.spacing, unitList.Count, squadTransform);
                    if (Vector3.Distance(unitList[i].transform.position, objective) > 1)
                        unitList[i].AssignObjective(objective);
                }
            */
        }
    }
    
    //============ Function - SquadPos()  ===============================//
    Vector3 SquadPos ()
    {
        Vector3 squadPos = Vector3.zero;
        for (int i = 0; i < unitList.Count; i++)
            squadTransform.position += unitList[i].transform.position;

        squadTransform.position /= unitList.Count;

        return squadPos;
    }
    
    //============ Function - FormationPos()  ===============================//
    public static Vector3 FormationPos(int columns, int index, float spacing, float random, int formationSize, Transform squadTransform)
    {
        Vector3 localPos = Vector3.zero;
        
        int rows = (int)Mathf.Ceil((float)formationSize / (float)columns);

        int row         = (int)Mathf.Ceil(index / columns);
        int column      = index - row * columns;

        // Y position
        float yLength   = (rows - 1) * spacing;
        localPos.y      = yLength / 2 - spacing * row;
        
        // X position
        float xWidth    = (columns - 1) * spacing;
        localPos.x      = -xWidth / 2 + spacing * column;

        Vector3 globalPos = squadTransform.position + squadTransform.transform.right * localPos.x + squadTransform.transform.forward * localPos.y;

        Vector3 randomVec = new Vector3(Random.Range(-random, random), 0, Random.Range(-random, random));
        globalPos += randomVec;


        return globalPos;
    }

    //============ Function - CreateSquad()  ===============================//
    public static Unit_Squad CreateSquad(Transform hierarchyUnits, Unit.Team team)
    {
        Unit_Squad squad = new GameObject().AddComponent<Unit_Squad>();
        squad.transform.position = Vector3.zero;
        squad.transform.rotation = Quaternion.identity;
        squad.name = team +  " squad " + (1 + hierarchyUnits.childCount).ToString();
        squad.transform.parent = hierarchyUnits;
        squad.unitList = new List<Unit>();
        squad.squadTransform = new GameObject().transform;
        squad.squadTransform.transform.parent = squad.transform;
        squad.squadTransform.name = "SquadTransform";
        squad.team = team;

        squad.formation.columns = 5;
        squad.formation.spacing = 2;

        return squad;
    }

    //============ Function - GetEnemySquad  ===============================//
    Unit_Squad GetEnemySquad(Unit_Squad[] enemySquads)
    {
        Unit_Squad[] squads = UnityEngine.Object.FindObjectsOfType<Unit_Squad>();

        Unit_Squad closestSquad = null;
        float shortestDistance = float.MaxValue;

        for (int i = 0; i < squads.Length; i++)
            if (squads[i] != this && squads[i].team != team)
            {
                float distance = Vector3.Distance(squadTransform.position, squads[i].squadTransform.position);
                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    closestSquad = squads[i];
                }
            }

        return closestSquad;
    }

    //============ Function - GetEnemyUnit  ===============================//
    Unit GetEnemyBot(Unit_Squad enemySquad, Unit thisBot)
    {
        Unit[] enemyUnits = new Unit[enemySquad.transform.childCount];
        for (int i = 0; i < enemyUnits.Length; i++)
            enemyUnits[i] = enemySquad.transform.GetChild(i).GetComponent<Unit>();

        Unit closestEnemy = null;
        float shortestDistance = float.MaxValue;


        for (int i = 0; i < enemyUnits.Length; i++)
            if(enemyUnits[i] != thisBot && enemyUnits[i].currentState != Unit.UnitState.dying)
            {
                float distance = Vector3.Distance(thisBot.transform.position, enemyUnits[i].transform.position);
                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    closestEnemy = enemyUnits[i];
                }
            }

        return closestEnemy;
    }

    //============ Function - AssignSquadTarget()  ===============================//
    public void AssignSquadTarget( Vector3 squadPos)
    {
        squadTransform.position = squadPos;

        for (int i = 0; i < unitList.Count; i++)
        {
            Vector3 target = FormationPos(formation.columns, i, formation.spacing, unitList[i].formationRandom, unitList.Count, squadTransform);

            if (unitList.Count > 3)
                unitList[i].StartCoroutine(unitList[i].CoRoutine_AssignObjective(target));
            else
                unitList[i].AssignObjective(target);
        }
    }

    //============ Struct - Formation  ===============================//
    public struct Formation
    {
        public int columns;
        public float spacing;
    }
}
