using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class World_GenericVars : MonoBehaviour
{
    //=====================  Variables  ================================================//
    public Mats             materials;
    public Meshes           meshes;
    public UnitUIelements   unitUIelements;

    float mixer = 0;

    Vector2 lineOffset;
    const   float   circleSpeed = 1.0f;
            float   circleStep = 0;
            float   circleLerp = 0.25f;

    //=====================  Function - Start()  ========================================//
    private void Start()
    {
        for (int i = 0; i < materials.outlineMaterials.Count; i++)
            materials.outlineMaterials[i].SetFloat("_Mixer", 0);
    }

    //=====================  Function - Update  ========================================//
    public void Update()
    {
        //------------------  Line Noise variation  ---------------------------//
        lineOffset -= new Vector2 (1, 1) * 0.01f * circleSpeed;
        materials.lineBlueprintMaterial.SetTextureOffset("_Noise1", lineOffset);

        //------------------  Circle alpha  -------------------------------------//
        circleStep += 0.05f;
        if (circleStep > 2 * Mathf.PI) circleStep = 0;

        materials.circleBlueprintMaterial.SetColor("_Color", new Color(0, 0, 0, (Mathf.Sin(circleStep) + 1) / 2));

        //------------------  Circle size  -------------------------------------//
        circleLerp += 0.025f * circleSpeed / Mathf.PI;
        if (circleLerp > 1)
            circleLerp = 0;
        float circleTiling = Mathf.Lerp(1.0f, 0.25f, circleLerp);
        materials.circleBlueprintMaterial.SetTextureScale("_Noise1", new Vector2(circleTiling, circleTiling));
        float circleOffset = Mathf.Lerp(0.0f, 0.375f, circleLerp);
        materials.circleBlueprintMaterial.SetTextureOffset("_Noise1", new Vector2(circleOffset, circleOffset));
    }

    //=====================  Function - Update  ========================================//
    public void Mixing(float endV)
    {
        StopCoroutine(CoRoutine_Mixing(0, 0));

        StartCoroutine(CoRoutine_Mixing(mixer, endV));
    }

    //=====================  IEnumerator - CoRoutine_Mixing  ===========================//
    IEnumerator CoRoutine_Mixing(float startV, float endV)
    {
        while (mixer != endV) {
            if (Mathf.Sign(endV - startV) == 1) {
                yield return new WaitForSeconds(0.025f);
                mixer += 0.1f;

                if (mixer > endV)   mixer = endV;
            }
            else {
                yield return new WaitForSeconds(0.025f);
                mixer -= 0.1f;

                if (mixer < endV)   mixer = endV;
            }
            
            for (int i = 0; i < materials.outlineMaterials.Count; i++)
                materials.outlineMaterials[i].SetFloat("_Mixer", mixer);
        }
    }

    //=============  Struct - Mats  ==============================================//
    [System.Serializable]
    public struct Mats
    {
        public Material lineMovementTrailMaterial;
        public Material protoMaterial;
        public Material lineBlueprintMaterial;
        public Material lineAirTrail;
        public List<Material> outlineMaterials;
        public Material circleBlueprintMaterial;

        public GameObject unitChevron;
    }

    //=============  Struct - Meshes  ==============================================//
    [System.Serializable]
    public class Meshes
    {
        public Mesh twoXtwo;
        public Mesh twoXfour;
        public Mesh fourXtwo;
        public Mesh fourXfour;
        public Mesh fourXeight;
        public Mesh eightXfour;
        public Mesh eightXeight;
        public Mesh eightXsixteen;
        public Mesh sixteenXeight;
        public Mesh sixteenXsixteen;

        public Mesh genericWall;
        public Mesh genericWallThick;
    }
    
    //=============  Struct - Unit control UI  ==============================================//
    [System.Serializable]
    public struct UnitUIelements
    {
        public GameObject selectionCircleInfantry;
        public GameObject selectionCircleWagon;
    }
}
