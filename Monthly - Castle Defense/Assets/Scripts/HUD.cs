using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HUD : MonoBehaviour
{
    public              ScrollAudio     scrollAudio;
    public              ScrollButtons   scrollButtons;
    public              UnitControl     unitControl;
    public              AudioGUI        audioGUI;
    public              CursorMods      cursorMods;
    [SerializeField]    Animator        scrollAnim;
    [SerializeField]    Transform       buildings;
    [SerializeField]    LayerMask       layerMaskGround;        //Used to place buildings
    [SerializeField]    LayerMask       layerMaskNotGround;     //Used to check building placement overlaps
    [SerializeField]    LayerMask       layerMaskSoldiers;
    [SerializeField]    LayerMask       layerMaskGroundAndSoldiers;


    bool unravelled;
    bool overlap;

    BuildingAsset   currentBuildAsset;
    GameObject      currentBuildObj;
    Transform       currentNode;
    AudioSource     buildAudioSrc;

    AudioSource     audioSrcGUI;

    Unit            currentUnit;

    enum BuildPhase {none, moving, walling}
    BuildPhase buildPhase = BuildPhase.none;

    private void Start()
    {
        audioGUI.audioSrc = this.GetComponent<AudioSource>();
    }

    //=============  Update()  ===========================//
    private void Update()
    {
        if (buildPhase == BuildPhase.none)
        {
            if (Input.GetMouseButtonDown(0))
            {
                RaycastHit hit;
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out hit, float.MaxValue, layerMaskSoldiers))
                {
                    currentUnit = hit.collider.GetComponent<Unit>();
                    Object.Instantiate(unitControl.selectionCircle, currentUnit.transform.position, currentUnit.transform.rotation, currentUnit.transform);

                    currentUnit.Select();
                }
                else
                {
                    audioGUI.audioSrc.clip = audioGUI.clip_nullClick; audioGUI.audioSrc.Play();
                }
            }

            if (Input.GetMouseButtonDown(1) && currentUnit != null && currentUnit.currentState == Unit.UnitState.notAttacking)
            {
                RaycastHit hit;
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out hit, float.MaxValue, layerMaskGroundAndSoldiers))
                {
                    if (hit.collider.GetComponent<Unit>())
                    {
                        audioGUI.audioSrc.clip = audioGUI.clip_attackClick; audioGUI.audioSrc.Play();
                        currentUnit.AssignEnemy(hit.collider.GetComponent<Unit>());
                    }
                    else
                        currentUnit.AssignObjective(hit.point);
                }
            }
        }
        else
            if (currentBuildObj != null)
            if (Input.GetMouseButtonDown(1) && !overlap)
            //-------------------------  Finish placement  ---------------------------------------------------------------------//
            {
                //Apply material
                currentBuildObj.GetComponent<Renderer>().material = currentBuildAsset.Mat_Final;
                for (int i = 0; i < currentBuildObj.transform.childCount; i++)
                    currentBuildObj.transform.GetChild(i).GetComponent<Renderer>().material = currentBuildAsset.Mat_Final;

                //Place final wall node
                if (buildPhase == BuildPhase.walling)
                {
                    Object.Instantiate(currentBuildAsset.BuildingObj, currentNode.position + (currentBuildObj.transform.position - currentNode.position) * 2, Quaternion.identity, buildings);
                    currentNode = null;
                }

                //reset currentBuildObj, currentBuildAsset, & buildphase
                currentBuildObj = null;
                currentBuildAsset = null;
                buildPhase = BuildPhase.none;

                //Audio
                buildAudioSrc.clip = scrollAudio.ding_confirm;
                buildAudioSrc.Play();
            }
            else
            {
                RaycastHit hit;
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out hit, 100.0f, layerMaskGround))
                {
                    //Collision detection
                    Collider[] colliders = Physics.OverlapBox(hit.point, currentBuildObj.GetComponent<BoxCollider>().size / 2, currentBuildObj.transform.rotation, layerMaskNotGround, QueryTriggerInteraction.Collide);
                    if (colliders.Length > 1) overlap = true;
                    else overlap = false;

                    if (overlap)
                        currentBuildAsset.Mat_Proto.color = new Color(1, 0, 0, currentBuildAsset.Mat_Proto.color.a);
                    else
                        currentBuildAsset.Mat_Proto.color = new Color(1, 1, 1, currentBuildAsset.Mat_Proto.color.a);

                    //-------------------------  Moving & rotating  -----------------------------------------------------------//
                    if (buildPhase != BuildPhase.walling)
                        if (!Input.GetMouseButton(0))           //---------------  Moving  -----------------------------------------//
                            currentBuildObj.transform.position = hit.point;
                        else
                        {
                            if (currentBuildAsset.wall.wallObj == null) //-----------------  Rotating  ------------------------//
                                currentBuildObj.transform.LookAt(hit.point);
                            else                                        //-----------------  Walling  ------------------------//
                            {
                                buildPhase = BuildPhase.walling;

                                currentBuildObj.GetComponent<Renderer>().material = currentBuildAsset.Mat_Final;
                                for (int i = 0; i < currentBuildObj.transform.childCount; i++)
                                    currentBuildObj.transform.GetChild(i).GetComponent<Renderer>().material = currentBuildAsset.Mat_Final;

                                currentNode = currentBuildObj.transform;
                                currentBuildObj = Object.Instantiate(currentBuildAsset.wall.wallObj, buildings);
                                currentBuildObj.GetComponent<Renderer>().material = currentBuildAsset.Mat_Proto;
                            }
                        }
                    else  //-----------------  Walling  ------------------------//
                        if (Vector3.Distance(hit.point, currentNode.position) > currentBuildAsset.wall.wallLength)
                    {
                        currentBuildObj.transform.position = currentNode.position + Vector3.Normalize(hit.point - currentNode.position) * (currentBuildAsset.wall.wallLength / 2);
                        currentBuildObj.transform.LookAt(hit.point);
                        currentBuildObj.transform.rotation = Quaternion.Euler(0, currentBuildObj.transform.eulerAngles.y - 90, 0);

                        if (Input.GetMouseButtonDown(0) && !overlap)
                        {
                            buildAudioSrc.clip = scrollAudio.ding_confirm;
                            buildAudioSrc.Play();

                            currentNode = Object.Instantiate(currentBuildAsset.BuildingObj, currentNode.position + Vector3.Normalize(hit.point - currentNode.position) * (currentBuildAsset.wall.wallLength), Quaternion.identity, buildings).transform;

                            currentBuildObj.GetComponent<Renderer>().material = currentBuildAsset.Mat_Final;
                            for (int i = 0; i < currentBuildObj.transform.childCount; i++)
                                currentBuildObj.transform.GetChild(i).GetComponent<Renderer>().material = currentBuildAsset.Mat_Final;

                            //Instantiate new object
                            currentBuildObj = Object.Instantiate(currentBuildAsset.wall.wallObj, buildings);

                            currentBuildObj.transform.position = currentNode.position + Vector3.Normalize(hit.point - currentNode.position) * (currentBuildAsset.wall.wallLength / 2);
                            currentBuildObj.transform.LookAt(hit.point);
                            currentBuildObj.transform.rotation = Quaternion.Euler(0, currentBuildObj.transform.eulerAngles.y - 90, 0);

                            currentBuildObj.GetComponent<Renderer>().material = currentBuildAsset.Mat_Proto;
                        }
                    }
                }
            }
    }

    //=============  ClickedOnBuild()  ===================//
    public void ClickedOnBuild(BuildingAsset buildingAsset)
    {
        //---------------------  buildAsset, BuildObj  ---------------------------//
        currentBuildAsset = buildingAsset;
        currentBuildObj = Object.Instantiate(buildingAsset.BuildingObj, Vector3.zero, Quaternion.identity, buildings);

        //---------------------  Proto Material  ---------------------------//
        currentBuildObj.GetComponent<Renderer>().material = currentBuildAsset.Mat_Proto;
        for (int i = 0; i < currentBuildObj.transform.childCount; i++)
            currentBuildObj.transform.GetChild(i).GetComponent<Renderer>().material = currentBuildAsset.Mat_Proto;

        //---------------------  Audio  ---------------------------//
        scrollAudio.scrollAudioSrc.clip = scrollAudio.ding_confirm;
        scrollAudio.scrollAudioSrc.Play();

        buildAudioSrc = currentBuildObj.AddComponent<AudioSource>();
        buildAudioSrc.clip = scrollAudio.ding_selected;
        buildAudioSrc.playOnAwake = false;
        buildAudioSrc.Play();
        
        //---------------------  BuildPhase  ---------------------------//
        buildPhase = BuildPhase.moving;

        //---------------------  EnableDisableScroll  ---------------------------//
        EnableDisableScroll(false);
    }

    //=============  ClickedOnScroll()  ==================//
    public void ClickedOnScroll()
    {
        EnableDisableScroll(!unravelled);
    }

    //=============  EnableDisableScroll()  ==============//
    void EnableDisableScroll(bool unravelOrRavel)
    {
        unravelled = unravelOrRavel;

        if (unravelled)
        {
            scrollButtons.scrollButtonOpen.SetActive(false);
            scrollButtons.scrollButtonClose.SetActive(true);
            for (int i = 0; i < scrollButtons.scrollButtons.Count; i++)
                scrollButtons.scrollButtons[i].SetActive(true);

            scrollAudio.scrollAudioSrc.clip = scrollAudio.scrollOpen;
            scrollAudio.scrollAudioSrc.Play();
        }
        else
        {
            scrollButtons.scrollButtonOpen.SetActive(true);
            scrollButtons.scrollButtonClose.SetActive(false);
            for (int i = 0; i < scrollButtons.scrollButtons.Count; i++)
                scrollButtons.scrollButtons[i].SetActive(false);

            scrollAudio.scrollAudioSrc.clip = scrollAudio.scrollClose;
            scrollAudio.scrollAudioSrc.Play();
        }

        scrollAnim.SetBool("Unravelled", unravelled);
    }

    //==============  Struct scroll buttons  =======================================//
    [System.Serializable]
    public struct ScrollButtons
    {
        public List<GameObject> scrollButtons;
        public GameObject scrollButtonOpen;
        public GameObject scrollButtonClose;
    }

    //==============  Struct Audio  =================================================//
    [System.Serializable]
    public struct ScrollAudio
    {
        public AudioClip scrollOpen;
        public AudioClip scrollClose;
        public AudioClip ding_confirm;
        public AudioClip ding2;
        public AudioClip ding_selected;
        
        public AudioSource scrollAudioSrc;
    }

    //==============  Unit control UI  =================================================//
    [System.Serializable]
    public struct UnitControl
    {
        public GameObject selectionCircle;
    }

    //==============  AudioGUI  =================================================//
    [System.Serializable]
    public struct AudioGUI
    {
        public AudioSource  audioSrc;
        public AudioClip    clip_nullClick;
        public AudioClip    clip_attackClick;
    }

    [System.Serializable]
    public struct CursorMods
    {
        public Texture2D cursorTexture;
    }

    void OnMouseEnter()
    {
        Cursor.SetCursor(cursorMods.cursorTexture, Vector2.zero, CursorMode.Auto);
    }

    void OnMouseExit()
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }
}
