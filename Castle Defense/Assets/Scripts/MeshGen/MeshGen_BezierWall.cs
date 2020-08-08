using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// UpdateBezier() 'p' used for p.currentSegment.line, p.currentSegment.transform.position, 

public class MeshGen_BezierWall : MonoBehaviour
{
    //=========================  Variables  =========================================//
    enum MeshType { visual, collider }

    List<QueuedSegment> queuedSegments = new List<QueuedSegment>();

    //=========================  Function - BeginGeneratingQueuedWalls()  =========================================//
    public void GenerateNextWall()
    {
        StartCoroutine(ModifyMesh(MeshType.visual, queuedSegments[0].cS, queuedSegments[0].segmentData, queuedSegments[0].p0, queuedSegments[0].p1, queuedSegments[0].h, queuedSegments[0]));
    }

    //=========================  Function - UpdateBezier()  =========================================//
    public static void UpdateBezier(HUD_AdvancedWalling.ProtoWall p, HUD_AdvancedWalling.CurveSegment cS, HUD_AdvancedWalling.CurvePoint p0, HUD_AdvancedWalling.CurvePoint p1, bool rotateh1, bool actualPlacement)
    {   
        BezierCurves.Headings h = BezierCurves.GetBezierHeadings(p, cS, p0, p1, rotateh1);

        cS.transform.position = BezierCurves.CubicPointPosition(0.5f, p0.transform.position, p1.transform.position, h.h0, h.h1);

        SegmentData segmentData = GetSegmentData(cS.lines[0], p0, p1, actualPlacement);

        MeshGen_BezierWall meshGen = GameObject.Find("World - MeshGen").GetComponent<MeshGen_BezierWall>();
        
        if (!actualPlacement)
            meshGen.StartCoroutine(meshGen.ModifyMesh(MeshType.visual, cS, segmentData, p0, p1, h, null));
        else {
            meshGen.StartCoroutine(meshGen.ModifyMesh(MeshType.collider, cS, segmentData, p0, p1, h, null));   //Add mesh collider

            QueuedSegment queuedSegment = new QueuedSegment();
            queuedSegment.cS = cS;
            queuedSegment.h = h;
            queuedSegment.p0 = p0;
            queuedSegment.p1 = p1;
            queuedSegment.segmentData = segmentData;
            meshGen.queuedSegments.Add(queuedSegment);
        }
    }

    //=========================  Function - ModifyMesh()  =========================================//
    IEnumerator ModifyMesh(MeshType meshType, HUD_AdvancedWalling.CurveSegment cS, SegmentData segmentData, HUD_AdvancedWalling.CurvePoint p0, HUD_AdvancedWalling.CurvePoint p1, BezierCurves.Headings h, QueuedSegment queuedSegment)
    {
        Mesh mesh;
        float width;
        if (meshType == MeshType.visual) {
            mesh = segmentData.mesh_visual;
            width = segmentData.meshWidth;
        }
        else {
            mesh = segmentData.mesh_wallCollider;
            width = segmentData.meshWidth_wallCollider;
        }

        Vector3[] vertPosArr = new Vector3[mesh.vertexCount];
        Vector2[] uvPosArr = new Vector2[mesh.vertexCount];

        int uvMultiplier = Mathf.RoundToInt(segmentData.distance / width);

        for (int i = 0; i < mesh.vertexCount; i++)
        {
            if (mesh.vertexCount > 200 && i != 0 && i % 100 == 0)
                yield return 0;

            //-----------------  t value  ------------------------------------------//
            float t = mesh.vertices[i].x / width + 0.5f;

            //-----------------  X coordinates  ------------------------------------//
            Vector3 bezPos = BezierCurves.CubicPointPosition(t, p0.transform.position, p1.transform.position, h.h0, h.h1);
            vertPosArr[i] = bezPos;

            //-----------------  Y coordinates  ------------------------------------//
            float normalisedHeight = mesh.vertices[i].y / segmentData.meshHeight;
            vertPosArr[i].y = bezPos.y + (p0.height * (1 - t) + p1.height * t) * normalisedHeight + segmentData.meshHeight / 2;
            
            //-----------------  Z coordinates  ------------------------------------//
            Vector3 perp;
            perp = BezierCurves.CubicPointPerpendicular(bezPos, t, p0.transform.position, p1.transform.position, h.h0, h.h1);
            vertPosArr[i] += perp * mesh.vertices[i].z * cS.thickness;

            //-----------------  Correct local coordinates  ------------------------//
            vertPosArr[i] -= cS.transform.position;

            //-----------------  UV coordinates  -----------------------------------//
            uvPosArr[i].Set(mesh.uv[i].x * uvMultiplier, mesh.uv[i].y);
        }

        mesh.vertices = vertPosArr;
        mesh.RecalculateBounds();
        mesh.uv = uvPosArr;

        if (meshType == MeshType.visual)
            cS.GetComponent<MeshFilter>().sharedMesh = mesh;
        else {
            MeshCollider mCol;
            if (cS.gameObject.GetComponent<MeshCollider>() == null)
                mCol = cS.gameObject.AddComponent<MeshCollider>();
            else
                mCol = cS.gameObject.GetComponent<MeshCollider>();

            mCol.sharedMesh = mesh;
        }

        if (queuedSegment != null) {
            queuedSegments.Remove(queuedSegment);

            if (queuedSegments.Count > 0)
                GenerateNextWall();
        }
    }
    
    //=========================  Function - AddWidthPlane()  =========================================//
    static SegmentData GetSegmentData(LineRenderer line, HUD_AdvancedWalling.CurvePoint p0, HUD_AdvancedWalling.CurvePoint p1, bool actualPlacement)
    {
        //------------------  MeshOptions  -----------------------------------------------------------------------------------//
        World_GenericVars.Meshes meshOptions = GameObject.Find("World - Generic variables").GetComponent<World_GenericVars>().meshes;

        //------------------  SegmentData, height, & distance  --------------------------------------------------------------//
        SegmentData segmentData = new SegmentData();

        segmentData.distance = BezierCurves.BezierDistance(line);

        if (p0.height > p1.height)  segmentData.meshHeight = p0.height;
        else                        segmentData.meshHeight = p1.height;

        //------------------  choose meshes based on meshWidth ----------------------------------------------------------------------------//
        segmentData.mesh_wallCollider = (Mesh)Object.Instantiate(meshOptions.genericWallThick);
        segmentData.meshWidth_wallCollider = 20;

        if (actualPlacement) {
            segmentData.mesh_visual = (Mesh)Object.Instantiate(meshOptions.sixteenXeight);
            segmentData.meshWidth = 4;
        }
        else {
            segmentData.mesh_visual = (Mesh)Object.Instantiate(meshOptions.genericWall);
            segmentData.meshWidth = 20;
        }

        TileMesh(segmentData);

        //------------------  SegmentData meshWidth  ----------------------------------------------------------------------------//
        return segmentData;
    }

    static void TileMesh(SegmentData segmentData)
    {
        int tiling;

        tiling = Mathf.RoundToInt(segmentData.distance / segmentData.meshWidth);

        if (tiling < 1)
            tiling = 1;

        Debug.Log("tiling == " + tiling);

        CombineInstance[] combine = new CombineInstance[tiling];
        
        for (int i = 0; i < tiling; i++)
        {
            Mesh meshCopy = (Mesh)Object.Instantiate(segmentData.mesh_visual);
            combine[i].mesh = new Mesh();

            Transform transform = new GameObject().transform;
            transform.position = Vector3.zero;

            if (tiling > 1)
               transform.position = -Vector3.right * segmentData.meshWidth * (tiling - 1) / 2;

            transform.position += Vector3.right * segmentData.meshWidth * i;

            combine[i].mesh = meshCopy;
            combine[i].transform = transform.localToWorldMatrix;

            Destroy(transform.gameObject);
        }

        segmentData.mesh_visual = (Mesh)Object.Instantiate(segmentData.mesh_wallCollider);
        segmentData.mesh_visual.CombineMeshes(combine);

        segmentData.meshWidth *= tiling;
    }

    //=========================  Class - SegmentData  =========================================//
    class SegmentData
    {
        public float    distance;
        public float    meshWidth;
        public float    meshWidth_wallCollider;
        public float    meshHeight;
        public Mesh     mesh_visual;
        public Mesh     mesh_wallCollider;
    }

    class QueuedSegment
    {
        public HUD_AdvancedWalling.CurveSegment cS;
        public SegmentData                      segmentData;
        public HUD_AdvancedWalling.CurvePoint   p0;
        public HUD_AdvancedWalling.CurvePoint   p1;
        public BezierCurves.Headings            h;
    }
}
