using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class HUD_Building
{
    //======================  Enums  =============================//
    public enum BuildPhase { none, basicMoving, basicWalling, advancedWalling}
    
    public static BuildingVars BuildUpdate(BuildingVars buildingVars, bool overlap, LayerMask layerMaskGround, HUD.ScrollAudio scrollAudio, HUD.AudioGUI audioGUI, LayerMask layerMaskNotGround)
    {
        if (buildingVars.currentBuildObj != null)
            if (Input.GetMouseButtonDown(1) && !overlap)
            //-------------------------  Finish placement  ---------------------------------------------------------------------//
            {
                //Apply material
                buildingVars.currentBuildObj.GetComponent<Renderer>().material = buildingVars.currentBuildAsset.mat_Final;

                for (int i = 0; i < buildingVars.currentBuildObj.transform.childCount; i++)
                    if (buildingVars.currentBuildObj.transform.GetChild(i).GetComponent<Renderer>() != null)
                        buildingVars.currentBuildObj.transform.GetChild(i).GetComponent<Renderer>().material = buildingVars.currentBuildAsset.mat_Final;

                //Swap box collider for mesh Collider
                Object.Destroy(buildingVars.currentBuildObj.GetComponent<BoxCollider>());
                buildingVars.currentBuildObj.GetComponent<MeshCollider>().enabled = true;

                // Activate spriteSheet timelapse
                if (buildingVars.currentBuildObj.GetComponent<SpriteSheet>() != null)
                {
                    buildingVars.currentBuildObj.GetComponent<SpriteSheet>().enabled = true;
                    buildingVars.currentBuildObj.GetComponent<SpriteSheet>().Initialise();
                }

                //Place final wall node
                if (buildingVars.buildPhase == BuildPhase.basicWalling)
                {
                    Object.Instantiate(buildingVars.currentBuildAsset.buildingObj, buildingVars.currentNode.position + (buildingVars.currentBuildObj.transform.position - buildingVars.currentNode.position) * 2, Quaternion.identity, buildingVars.hierarchy_buildings);
                    buildingVars.currentNode = null;
                }

                //reset currentBuildObj, currentBuildAsset, & buildphase
                buildingVars.currentBuildObj = null;
                buildingVars.currentBuildAsset = null;
                buildingVars.buildPhase = BuildPhase.none;

                //Audio
                buildingVars.buildAudioSrc.clip = audioGUI.click_whoosh;
                buildingVars.buildAudioSrc.Play();
            }
            else
            {
                RaycastHit hit;
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out hit, 100.0f, layerMaskGround))
                {
                    //Collision detection
                    Collider[] colliders = Physics.OverlapBox(hit.point, buildingVars.currentBuildObj.GetComponent<BoxCollider>().size / 2, buildingVars.currentBuildObj.transform.rotation, layerMaskNotGround, QueryTriggerInteraction.Collide);
                    
                    if (colliders.Length > 1) overlap = true;
                    else overlap = false;

                    if (overlap)
                        buildingVars.currentBuildAsset.mat_Proto.color = new Color(1, 0, 0, buildingVars.currentBuildAsset.mat_Proto.color.a);
                    else
                        buildingVars.currentBuildAsset.mat_Proto.color = new Color(1, 1, 1, buildingVars.currentBuildAsset.mat_Proto.color.a);
                    
                    //-------------------------  Moving & rotating  -----------------------------------------------------------//
                    if (buildingVars.buildPhase != BuildPhase.basicWalling)
                        if (!Input.GetMouseButton(0))           //---------------  Moving  -----------------------------------------//
                            buildingVars.currentBuildObj.transform.position = hit.point;
                        else
                        {
                            if (buildingVars.currentBuildAsset.wall.wallObj == null) //-----------------  Rotating  ------------------------//
                                buildingVars.currentBuildObj.transform.LookAt(hit.point);
                            else                                        //-----------------  Walling  ------------------------//
                            {
                                buildingVars.buildPhase = BuildPhase.basicWalling;

                                buildingVars.currentBuildObj.GetComponent<Renderer>().material = buildingVars.currentBuildAsset.mat_Final;
                                for (int i = 0; i < buildingVars.currentBuildObj.transform.childCount; i++)
                                    buildingVars.currentBuildObj.transform.GetChild(i).GetComponent<Renderer>().material = buildingVars.currentBuildAsset.mat_Final;

                                buildingVars.currentNode = buildingVars.currentBuildObj.transform;
                                buildingVars.currentBuildObj = Object.Instantiate(buildingVars.currentBuildAsset.wall.wallObj, buildingVars.hierarchy_buildings);
                                buildingVars.currentBuildObj.GetComponent<Renderer>().material = buildingVars.currentBuildAsset.mat_Proto;
                            }
                        }
                    else  //-----------------  Walling  ------------------------//
                        if (Vector3.Distance(hit.point, buildingVars.currentNode.position) > buildingVars.currentBuildAsset.wall.wallLength)
                    {
                        buildingVars.currentBuildObj.transform.position = buildingVars.currentNode.position + Vector3.Normalize(hit.point - buildingVars.currentNode.position) * (buildingVars.currentBuildAsset.wall.wallLength / 2);
                        buildingVars.currentBuildObj.transform.LookAt(hit.point);
                        buildingVars.currentBuildObj.transform.rotation = Quaternion.Euler(0, buildingVars.currentBuildObj.transform.eulerAngles.y - 90, 0);
                        //Debug.Log("buildingVars.currentBuildObj.transform.rotation: " + buildingVars.currentBuildObj.transform.rotation);

                        if (Input.GetMouseButtonDown(0) && !overlap)
                        {
                            buildingVars.buildAudioSrc.clip = audioGUI.click_whoosh;
                            buildingVars.buildAudioSrc.Play();

                            buildingVars.currentNode = Object.Instantiate(buildingVars.currentBuildAsset.buildingObj, buildingVars.currentNode.position + Vector3.Normalize(hit.point - buildingVars.currentNode.position) * (buildingVars.currentBuildAsset.wall.wallLength), Quaternion.identity, buildingVars.hierarchy_buildings).transform;

                            buildingVars.currentBuildObj.GetComponent<Renderer>().material = buildingVars.currentBuildAsset.mat_Final;
                            for (int i = 0; i < buildingVars.currentBuildObj.transform.childCount; i++)
                                buildingVars.currentBuildObj.transform.GetChild(i).GetComponent<Renderer>().material = buildingVars.currentBuildAsset.mat_Final;

                            //Instantiate new object
                            buildingVars.currentBuildObj = Object.Instantiate(buildingVars.currentBuildAsset.wall.wallObj, buildingVars.hierarchy_buildings);

                            buildingVars.currentBuildObj.transform.position = buildingVars.currentNode.position + Vector3.Normalize(hit.point - buildingVars.currentNode.position) * (buildingVars.currentBuildAsset.wall.wallLength / 2);
                            buildingVars.currentBuildObj.transform.LookAt(hit.point);
                            buildingVars.currentBuildObj.transform.rotation = Quaternion.Euler(0, buildingVars.currentBuildObj.transform.eulerAngles.y - 90, 0);

                            buildingVars.currentBuildObj.GetComponent<Renderer>().material = buildingVars.currentBuildAsset.mat_Proto;
                        }
                    }
                }
            }

        return buildingVars;
    }

    //==============  Struct BuildingVars  =================================================//
    [System.Serializable]
    public struct BuildingVars
    {
        [System.NonSerialized]
        public BuildingAsset currentBuildAsset;
        [System.NonSerialized]
        public GameObject currentBuildObj;
        [System.NonSerialized]
        public Transform currentNode;
        [System.NonSerialized]
        public AudioSource buildAudioSrc;
        [System.NonSerialized]
        public BuildPhase buildPhase;

        public Transform hierarchy_buildings;
    }
}