using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public static class HUD_UnitSelection
{
    //=============  Function - UpdateSelection()  ==========================================//
    public static void UpdateSelection(HUD hud, ClickManager.dblClickSettings clicks, LayerMask layerMaskUnitOrBuilding, LayerMask layerMaskGround, Unit.Team playerTeam, List<Unit> selectedUnits, Transform protoFormationLocRot, Transform hierarchy_units, HUD.AudioGUI audioGUI)
    {
        //---------------  Left Click  --------------------------------------------------------------------------------------------------//
        if (!EventSystem.current.IsPointerOverGameObject())
        {
            if (Input.GetMouseButtonDown(0))
            {
                RaycastHit hit;
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out hit, float.MaxValue, layerMaskUnitOrBuilding, QueryTriggerInteraction.Collide))
                {
                    //------------  Click on Unit  --------------------------------------------------//
                    if (hit.collider.GetComponent<Unit>() != null)
                    {
                        if (hit.collider.GetComponent<Unit>().currentState != Unit.UnitState.dying)
                            switch (hit.collider.GetComponent<Unit>().type)
                            {
                                case Unit.UnitType.SOLDIER:
                                    {
                                        if (hit.collider.GetComponent<Unit>().combatUnitVars.squad == null)
                                            Debug.Log("hit.collider.GetComponent<Unit>().combatUnitVars.squad == null");

                                        if (hit.collider.GetComponent<Unit>().combatUnitVars.squad.team == playerTeam)
                                        {
                                            if (!Input.GetKey(KeyCode.LeftShift))
                                            {
                                                for (int i = 0; i < selectedUnits.Count; i++)
                                                    selectedUnits[i].DeselectUnit();

                                                selectedUnits.Clear();
                                            }

                                            if (!hit.collider.GetComponent<Unit>().selected) {
                                                selectedUnits.Add(hit.collider.GetComponent<Unit>());
                                                hit.collider.GetComponent<Unit>().SelectUnit(Unit.UnitType.SOLDIER);
                                            }

                                            if (clicks.dblClick)
                                                for (int i = 0; i < hit.collider.GetComponent<Unit>().combatUnitVars.squad.unitList.Count; i++)
                                                    if (!hit.collider.GetComponent<Unit>().combatUnitVars.squad.unitList[i].selected) {
                                                        selectedUnits.Add(hit.collider.GetComponent<Unit>().combatUnitVars.squad.unitList[i]);
                                                        hit.collider.GetComponent<Unit>().combatUnitVars.squad.unitList[i].SelectUnit(Unit.UnitType.SOLDIER);
                                                    }
                                        }
                                        else if (selectedUnits.Count > 0) //Enemy unit
                                        {
                                            protoFormationLocRot.position = hit.point;
                                            protoFormationLocRot.LookAt(hit.point + (hit.point - selectedUnits[0].transform.position));

                                            SendSelectedUnits(Unit.UnitType.SOLDIER, hierarchy_units, playerTeam, selectedUnits, protoFormationLocRot, hud.formation);
                                            audioGUI.audioSrc.clip = audioGUI.click_attack; audioGUI.audioSrc.Play();
                                        }
                                    }
                                    break;
                                case Unit.UnitType.WAGON:
                                    {
                                        if (hit.collider.GetComponent<Unit>().wagonUnitVars.team == playerTeam) //Friendly unit
                                        {
                                            if (!Input.GetKey(KeyCode.LeftShift)) {
                                                for (int i = 0; i < selectedUnits.Count; i++)
                                                    selectedUnits[i].DeselectUnit();

                                                selectedUnits.Clear();
                                            }

                                            if (!hit.collider.GetComponent<Unit>().selected) {
                                                selectedUnits.Add(hit.collider.GetComponent<Unit>());
                                                hit.collider.GetComponent<Unit>().SelectUnit(Unit.UnitType.WAGON);
                                            }
                                        }
                                    }
                                    break;
                                case Unit.UnitType.WORKER:
                                    {
                                        if (hit.collider.GetComponent<Unit>().workerUnitVars.team == playerTeam) //Friendly unit
                                        {
                                            if (!Input.GetKey(KeyCode.LeftShift)) {
                                                for (int i = 0; i < selectedUnits.Count; i++)
                                                    selectedUnits[i].DeselectUnit();

                                                selectedUnits.Clear();
                                            }

                                            if (!hit.collider.GetComponent<Unit>().selected)
                                            {
                                                selectedUnits.Add(hit.collider.GetComponent<Unit>());
                                                hit.collider.GetComponent<Unit>().SelectUnit(Unit.UnitType.WORKER);
                                            }
                                        }
                                    }
                                    break;
                            }
                    }

                    //------------  Click on building/unit with a ScrollAsset  --------------------------------------------------//
                    if (hit.collider.GetComponent<ScrollAsset>() != null && hit.collider.GetComponent<ScrollAsset>().replacementScroll != null) {
                        if (hit.collider.GetComponent<ScrollAsset>().replacementScroll.name != hud.scroll.name || !hud.scroll.unravelled) {
                            hud.ClickedOnReplaceScroll(hit.collider.GetComponent<ScrollAsset>().replacementScroll);

                            if (hit.collider.GetComponent<Building>() != null && hit.collider.GetComponent<Building>().unitTrainingVars.team == playerTeam)
                                hud.activeBuilding = hit.collider.GetComponent<Building>();
                        }
                        else
                            Debug.Log("This unit's scrollAsset == our current scroll");
                    }

                    //------------  Click on invalid Unit/Building  --------------------------------------------------//
                    //else
                        //Debug.Log("hit LayerMaskCombat, but neither friendly unit nor building if-statements were satisfied");
                }
                //Place protoFormation
                else if (Physics.Raycast(ray, out hit, float.MaxValue, layerMaskGround) && !Input.GetKey(KeyCode.LeftShift)) {
                    if (selectedUnits.Count > 0) {
                        hud.protoFormation = new List<GameObject>();
                        protoFormationLocRot.position = hit.point;

                        //Spawn protoFormation;
                        for (int i = 0; i < selectedUnits.Count; i++) {
                            GameObject protoUnit = Object.Instantiate(selectedUnits[i].protoUnit);
                            hud.protoFormation.Add(protoUnit);

                            int columns = (int)hud.formation.columns;

                            if (selectedUnits.Count < hud.formation.columns)
                                columns = selectedUnits.Count;

                            hud.protoFormation[i].transform.position = Unit_Squad.FormationPos(columns, i, selectedUnits[0].combatUnitVars.type, 0, hud.protoFormation.Count, protoFormationLocRot);
                        }
                    }
                    if (selectedUnits.Count == 0 || Input.GetKey(KeyCode.LeftShift))
                        hud.selectionBoxVars = HUD_SelectionBox.CreateSelectionBoxObj(hud.selectionBoxVars, hud.gameObject);
                }
            }
            else if (Input.GetMouseButton(0))
            {
                RaycastHit hit;
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out hit, float.MaxValue, layerMaskGround)) {
                    if (selectedUnits.Count > 0 && hud.selectionBoxVars.p1 == Vector3.zero) {
                        protoFormationLocRot.LookAt(hit.point);

                        hud.formation.columns = Mathf.Clamp(hud.formation.columns + (int)Input.mouseScrollDelta.y, 1, 50);

                        if (!Input.GetKey(KeyCode.LeftShift)) {
                            int columns = (int)hud.formation.columns;

                            if (selectedUnits.Count < hud.formation.columns) columns = selectedUnits.Count;

                            for (int i = 0; i < hud.protoFormation.Count; i++) {
                                hud.protoFormation[i].transform.position = Unit_Squad.FormationPos(columns, i, selectedUnits[0].combatUnitVars.type, 0, hud.protoFormation.Count, protoFormationLocRot);
                                hud.protoFormation[i].transform.rotation = protoFormationLocRot.rotation;
                            }
                        }
                    }
                }

                if (hud.selectionBoxVars.p1 != Vector3.zero)
                    HUD_SelectionBox.UpdateBoxSelect(hud.selectionBoxVars, layerMaskGround);
            }
            //------------  Click on ground  --------------------------------------------------//
            else if (Input.GetMouseButtonUp(0)) {
                DestroyProtoformation(hud);

                if (hud.selectionBoxVars.selectionBox != null)
                    Object.Destroy(hud.selectionBoxVars.selectionBox);
                else if (!Input.GetKey(KeyCode.LeftShift)) {
                    RaycastHit hit;
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                    if (!Physics.Raycast(ray, out hit, 100f, layerMaskUnitOrBuilding)) {
                        if (Physics.Raycast(ray, out hit, float.MaxValue, layerMaskGround) && selectedUnits.Count > 0)
                            SendSelectedUnits(selectedUnits[0].type, hierarchy_units, playerTeam, selectedUnits, protoFormationLocRot, hud.formation);
                        else {
                            audioGUI.audioSrc.clip = audioGUI.click_null; audioGUI.audioSrc.Play();
                            Debug.Log("NullClick");
                        }
                    }
                }

                hud.selectionBoxVars.p1 = Vector3.zero;
            }
        }
        else if (Input.GetMouseButtonUp(0))
            DestroyProtoformation(hud);

        //---------------  Right Click  -------------------------------------------------------------------------------------------------//
        if (Input.GetMouseButtonDown(1)) {
            for (int i = 0; i < selectedUnits.Count; i++)
                selectedUnits[i].DeselectUnit();

            selectedUnits.Clear();
        }
    }

    //=============  Function - GetSelectionObj()  ==========================================//
    public static GameObject GetSelectionObj(Unit.UnitType type)
    {
        World_GenericVars.UnitUIelements unitUIelements = Object.FindObjectOfType<World_GenericVars>().unitUIelements;

        switch (type) {
            case Unit.UnitType.WAGON:
                return unitUIelements.selectionCircleWagon;

            default:
                return unitUIelements.selectionCircleInfantry;
        }
    }

    //=============  Function - DestroyProtoformation()  ==========================================//
    static void DestroyProtoformation(HUD hud)
    {
        for (int i = 0; i < hud.protoFormation.Count; i++)
            GameObject.Destroy(hud.protoFormation[i]);

        hud.protoFormation.Clear();
    }

    //=============  Function - SendSelectedUnits()  ==========================================//
    static void SendSelectedUnits(Unit.UnitType type, Transform hierarchy_units, Unit.Team playerTeam, List<Unit> selectedUnits, Transform protoFormationLocRot, Unit_Squad.Formation formation)
    {
        switch (type)
        {
            case Unit.UnitType.SOLDIER:
            {
                //---------------------------  send new squad  -------------------------------------//
                // 1 - CreateSquad()
                Unit_Squad newSquad = Unit_Squad.CreateSquad(hierarchy_units, playerTeam, formation);

                // 2 - selectedUnits to new squad
                for (int i = 0; i < selectedUnits.Count; i++) {
                    selectedUnits[i].combatUnitVars.squad.unitList.Remove(selectedUnits[i]);
                    newSquad.unitList.Add(selectedUnits[i]);
                    selectedUnits[i].combatUnitVars.squad = newSquad;
                    selectedUnits[i].transform.parent = newSquad.transform;
                }

                newSquad.squadTransform.position = newSquad.SquadPos();
                newSquad.squadTransform.rotation = selectedUnits[0].transform.rotation;

                if (newSquad.team == Unit.Team.human)
                    newSquad.line_movingToFormation.SetPosition(0, newSquad.squadTransform.position);

                // 3 - Send to new formation facing enemies
                newSquad.squadTransform.position += (protoFormationLocRot.position - newSquad.squadTransform.position).normalized * 5;
                newSquad.squadTransform.LookAt(new Vector3(protoFormationLocRot.position.x, newSquad.squadTransform.position.y, protoFormationLocRot.position.z));

                newSquad.formation = formation;

                // 4 - AssignSquadTargets()
                newSquad.MoveSquadToFormationForMarch(protoFormationLocRot);

                break;
            }

            case Unit.UnitType.WAGON:
            {
                for (int i = 0; i < selectedUnits.Count; i++) {
                    selectedUnits[i].wagonUnitVars.initialHeading = selectedUnits[i].transform.forward;
                    selectedUnits[i].wagonUnitVars.destinationHeading = protoFormationLocRot.transform.forward;
                    selectedUnits[i].AssignObjective(protoFormationLocRot.position);
                }

                break;
            }

            case Unit.UnitType.WORKER:
            {
                for (int i = 0; i < selectedUnits.Count; i++)
                    selectedUnits[i].AssignObjective(protoFormationLocRot.position);

                break;
            }
        }
    }

    //=============  Function - RemoveDeadUnitsFromSelection()  =================================//
    public static void RemoveDeadUnitsFromSelection(List<Unit> selectedUnits)
    {
        for (int i = 0; i < selectedUnits.Count; i++)
            if (selectedUnits[i].type == Unit.UnitType.SOLDIER)
                if (selectedUnits[i].currentState == Unit.UnitState.dying)
                    selectedUnits.Remove(selectedUnits[i]);
    }
}