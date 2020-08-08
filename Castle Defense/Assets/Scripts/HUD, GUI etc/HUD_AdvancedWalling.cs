using UnityEngine;
using UnityEngine.EventSystems;

public static class HUD_AdvancedWalling
{
    //==========================  Enums  =================================================================// 
    //enum WallingPhase { bezierPlacement, bezierModification, pathways, gates, drawbridge, hollows, machicolations, crenellations }
    public enum WallingPhase { none, hovering, scaleDragging }

    //==========================  Function - CreateStarterBezierUI()  ====================================//
    public static void CreateStarterBezierUI(ProtoWall p) {
        //-----------------------------  2 - Add first curve point  --------------------------------------//
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit, 100.0f, LayerMask.GetMask("Ground")))
            AddNewCurvePoint(p, hit.point, null);

        p.scaleDragData = new ScaleDragData();
    }

    //==========================  Function - WallingUpdate()  ============================================//
    public static void WallingUpdate(ProtoWall p, HUD.AudioGUI audioGUI) {
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        // Raycast on Unist&Buildings && phase == none
        if (p.phase == WallingPhase.none && p.complete && Physics.Raycast(ray, out hit, 100.0f, LayerMask.GetMask("Units&Buildings"))) {
            if (Input.GetMouseButton(0)) {
                if (hit.collider.GetComponent<CurveSegment>() != null) {
                    p.phase = WallingPhase.scaleDragging;
                    p.scaleDragData.segment = hit.collider.GetComponent<CurveSegment>();
                    p.scaleDragData.valOG = p.scaleDragData.segment.thickness;
                    p.scaleDragData.hitHeading = hit.normal;

                    Physics.Raycast(ray, out hit, 100.0f, LayerMask.GetMask("Ground"));
                    p.scaleDragData.hitPoint = hit.point;
                }
            }
        }
        // Raycast on UI && phase != scaleDragging
        if (Physics.Raycast(ray, out hit, 100.0f, LayerMask.GetMask("UI")) && p.phase != WallingPhase.scaleDragging) {
            if (!EventSystem.current.IsPointerOverGameObject()) {
                if (hit.collider.GetComponent<CurvePoint>() != null) {
                    if (Input.GetMouseButtonDown(0))
                    {
                        audioGUI.audioSrc.clip = audioGUI.click_confirm;
                        audioGUI.audioSrc.Play();

                        //-----------------  Click on point, complete loop of walls  ---------------//
                        if (!p.complete)
                            if (p.currentSegment.acceptable) {
                                if (hit.collider.GetComponent<CurvePoint>() != p.currentPoint && hit.collider.GetComponent<CurvePoint>() != p.currentPoint.prev) {
                                    p.currentSegment.p1 = hit.collider.GetComponent<CurvePoint>();
                                    Object.Destroy(p.currentPoint.gameObject);
                                    MeshGen_BezierWall.UpdateBezier(p, p.currentSegment, p.currentSegment.p0, hit.collider.GetComponent<CurvePoint>(), true, true);
                                    p.currentPoint = null;
                                    p.currentSegment = null;
                                    p.complete = true;
                                }
                            }
                            else {
                                audioGUI.audioSrc.clip = audioGUI.click_null;
                                audioGUI.audioSrc.Play();
                            }

                        //-----------------  Click on point, restart walling process  ---------------//
                        else {
                            p.complete = false;

                            hit.collider.GetComponent<CurvePoint>().prev = null;
                            AddNewCurvePoint(p, hit.point, hit.collider.GetComponent<CurvePoint>());
                        }
                    }

                    //-----------------  If hovering over point, GetActiveSegmentPositions()  ---------------//
                    if (p.currentPoint != null && hit.collider.GetComponent<CurvePoint>() != p.currentPoint && hit.collider.GetComponent<CurvePoint>() != p.currentPoint.prev) {
                        GetActiveSegmentPositions(p, p.currentSegment.p0, hit.collider.GetComponent<CurvePoint>(), true);
                        MeshGen_BezierWall.UpdateBezier(p, p.currentSegment, p.currentSegment.p0, hit.collider.GetComponent<CurvePoint>(), true, false);
                    }
                }
            }
        }
        //-------------------------  Raycast on "Ground"  --------------------------------//
        else if (Physics.Raycast(ray, out hit, 100.0f, LayerMask.GetMask("Ground"))) {
            //-------------------  Scale dragging  -------------------------//
            if (p.phase == WallingPhase.scaleDragging) {
                if (Input.GetMouseButton(0)) {
                    p.scaleDragData.segment.thickness = p.scaleDragData.valOG + Vector3.Project(hit.point - p.scaleDragData.hitPoint, p.scaleDragData.hitHeading).magnitude;
                    MeshGen_BezierWall.UpdateBezier(p, p.scaleDragData.segment, p.scaleDragData.segment.p0, p.scaleDragData.segment.p1, false, true);
                }

                if (Input.GetMouseButtonUp(0))
                    p.phase = WallingPhase.none;
            }
            else if (!p.complete) {
                //------------------------  Update active point  ----------------------------//
                if (!Input.GetMouseButton(0))
                    UpdatePointPosition(p.currentPoint, hit.point);

                //------------------------  Update active segment  --------------------------//
                if (p.currentSegment != null) {
                    GetActiveSegmentPositions(p, p.currentSegment.p0, p.currentSegment.p1, false);
                    MeshGen_BezierWall.UpdateBezier(p, p.currentSegment, p.currentSegment.p0, p.currentSegment.p1, false, false);
                }

                if (!EventSystem.current.IsPointerOverGameObject()) {
                    //------------------------  If we are left-clicking: rotate current curvePoint  ------------------//
                    if (Input.GetMouseButtonDown(0))
                        p.rotationOffset = Vector3.SignedAngle(p.currentPoint.transform.forward, hit.point - p.currentPoint.transform.position, Vector3.up);
                    else if (Input.GetMouseButton(0))
                        UpdatePointRotation(p, hit.point);

                    if (p.currentSegment == null || p.currentSegment.acceptable) {
                        //------------------------  If we left-clicked: create new curvePoint  ---------------------------//
                        if (Input.GetMouseButtonUp(0) && p.currentPoint != null) {
                            audioGUI.audioSrc.clip = audioGUI.click_confirm;
                            audioGUI.audioSrc.Play();

                            if (p.currentSegment != null) {
                                MeshGen_BezierWall.UpdateBezier(p, p.currentSegment, p.currentSegment.p0, p.currentSegment.p1, false, true);
                                p.currentSegment.gameObject.layer = LayerMask.NameToLayer("Units&Buildings");
                            }

                            CurvePoint origin = p.currentPoint;
                            AddNewCurvePoint(p, hit.point, p.currentPoint);
                            p.currentPoint.prev = origin;
                        }

                        //------------------------  If we right-clicked: finish protowall  --------------------------//
                        if (Input.GetMouseButtonDown(1) && p.currentPoint.prev != null) {
                            audioGUI.audioSrc.clip = audioGUI.click_confirm;
                            audioGUI.audioSrc.Play();

                            MeshGen_BezierWall.UpdateBezier(p, p.currentSegment, p.currentSegment.p0, p.currentSegment.p1, false, true);
                            p.currentSegment.gameObject.layer = LayerMask.NameToLayer("Units&Buildings");
                            p.currentPoint.GetComponent<Collider>().enabled = true;

                            p.complete = true;
                            p.currentPoint = null;
                            p.currentSegment = null;
                        }
                    }
                    else if (Input.GetMouseButtonDown(0)) {
                        audioGUI.audioSrc.clip = audioGUI.click_null;
                        audioGUI.audioSrc.Play();
                    }
                }
            }
        }
    }

    //==========================  Function - AddNewCurvePoint()  =========================================//
    static void AddNewCurvePoint(ProtoWall p, Vector3 hitPos, CurvePoint previous)
    {
        if (p.currentPoint != null)
            p.currentPoint.GetComponent<Collider>().enabled = true;

        CurvePoint newPoint = GameObject.CreatePrimitive(PrimitiveType.Plane).AddComponent<CurvePoint>();

        //-----------------------------  pointObj  ---------------------------------------//
        newPoint.gameObject.name = "curvePoint";
        newPoint.gameObject.layer = LayerMask.NameToLayer("UI");
        newPoint.transform.localScale = Vector3.one * 0.5f;
        newPoint.transform.position = hitPos;

        newPoint.gameObject.GetComponent<MeshRenderer>().sharedMaterial = GameObject.Find("World - Generic variables").GetComponent<World_GenericVars>().materials.circleBlueprintMaterial;

        newPoint.GetComponent<Collider>().enabled = false;

        if (previous != null)
            newPoint.transform.rotation = previous.transform.rotation;

        //-----------------------------  Vertical animated line  ---------------------------------------//
        newPoint.line = newPoint.gameObject.AddComponent<LineRenderer>();

        newPoint.line.SetPosition(0, hitPos);
        newPoint.line.SetPosition(1, hitPos + Vector3.up * 10);

        newPoint.line.sharedMaterial = GameObject.Find("World - Generic variables").GetComponent<World_GenericVars>().materials.lineBlueprintMaterial;
        newPoint.line.widthMultiplier = 0.5f;

        //-----------------------------  Handles  ---------------------------------------//
        newPoint.scale = 15f;

        if (previous == null)
            newPoint.height = 10f;
        else
            newPoint.height = previous.height;

        //-----------------------------  Add newPoint  ---------------------------------------//
        if (previous != null)
            newPoint.prev = previous;
        else
            newPoint.prev = null;

        p.currentPoint = newPoint;
        if (p.currentPoint == p.currentPoint.prev)
            Debug.Log("ERROR: p.currentPoint == p.currentPoint.prev");

        //-----------------------------  Segment curve  ---------------------------------------//
        if (p.currentPoint.prev != null) {
            float thickness;
            if (p.currentSegment != null)
                thickness = p.currentSegment.thickness;
            else
                thickness = 1f;

            p.currentSegment = new GameObject().AddComponent<CurveSegment>();
            p.currentSegment.gameObject.name = "Wall segment";
            p.currentSegment.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");

            //--------------  Lines  ------------------//
            Material lineMaterial = GameObject.Find("World - Generic variables").GetComponent<World_GenericVars>().materials.lineBlueprintMaterial;
            p.currentSegment.lines = new LineRenderer[11];
            for (int i = 0; i < 7; i++)
            {
                p.currentSegment.lines[i] = new GameObject().AddComponent<LineRenderer>();
                p.currentSegment.lines[i].name = "LineObj";
                p.currentSegment.lines[i].transform.parent = p.currentSegment.transform;
                p.currentSegment.lines[i].gameObject.layer = LayerMask.NameToLayer("UI");
                p.currentSegment.lines[i].sharedMaterial = lineMaterial;
                p.currentSegment.lines[i].widthMultiplier = 0.25f;

                if (i == 0) p.currentSegment.lines[i].positionCount = 20;
                else        p.currentSegment.lines[i].positionCount = 2;
            }
            
            p.currentSegment.p0 = p.currentPoint.prev;
            p.currentSegment.p1 = p.currentPoint;

            p.currentSegment.gameObject.AddComponent<MeshFilter>();
            p.currentSegment.gameObject.AddComponent<MeshRenderer>().sharedMaterial = GameObject.Find("World - Generic variables").GetComponent<World_GenericVars>().materials.outlineMaterials[0];

            p.currentSegment.thickness = thickness;
        }
    }

    //==========================  Function - GetActiveSegmentPositions()  ================================//
    static void GetActiveSegmentPositions(ProtoWall p, CurvePoint p0, CurvePoint p1, bool rotateh1)
    {
        if (p0.prev == null)
            p.wallRotation = Vector3.SignedAngle(p0.transform.forward, p1.transform.position - p0.transform.position, Vector3.up);

        BezierCurves.Headings h = BezierCurves.GetBezierHeadings(p, p.currentSegment, p0, p1, rotateh1);

        p.currentSegment.lines[0].SetPositions(BezierCurves.BezierPositions(
            p.currentSegment.lines[0],
            p.currentSegment.lines[0].positionCount,
            p.currentSegment.p0.gameObject.transform.position,
            p.currentSegment.p1.gameObject.transform.position,
            h.h0,
            h.h1
        ));

        Vector3 perp;
        Vector3 bezPos;
        float t;

        t = 0.25f;
        bezPos = BezierCurves.CubicPointPosition(t, p0.transform.position, p1.transform.position, h.h0, h.h1);
        perp = BezierCurves.CubicPointPerpendicular(bezPos, t, p0.transform.position, p1.transform.position, h.h0, h.h1);
        p.currentSegment.lines[1].SetPosition(0, bezPos + perp);
        p.currentSegment.lines[1].SetPosition(1, bezPos + perp + Vector3.up * 20);
        p.currentSegment.lines[2].SetPosition(0, bezPos - perp);
        p.currentSegment.lines[2].SetPosition(1, bezPos - perp + Vector3.up * 20);

        t = 0.50f;
        bezPos = BezierCurves.CubicPointPosition(t, p0.transform.position, p1.transform.position, h.h0, h.h1);
        perp = BezierCurves.CubicPointPerpendicular(bezPos, t, p0.transform.position, p1.transform.position, h.h0, h.h1);
        p.currentSegment.lines[3].SetPosition(0, bezPos + perp);
        p.currentSegment.lines[3].SetPosition(1, bezPos + perp + Vector3.up * 20);
        p.currentSegment.lines[4].SetPosition(0, bezPos - perp);
        p.currentSegment.lines[4].SetPosition(1, bezPos - perp + Vector3.up * 20);

        t = 0.75f;
        bezPos = BezierCurves.CubicPointPosition(t, p0.transform.position, p1.transform.position, h.h0, h.h1);
        perp = BezierCurves.CubicPointPerpendicular(bezPos, t, p0.transform.position, p1.transform.position, h.h0, h.h1);
        p.currentSegment.lines[5].SetPosition(0, bezPos + perp);
        p.currentSegment.lines[5].SetPosition(1, bezPos + perp + Vector3.up * 20);
        p.currentSegment.lines[6].SetPosition(0, bezPos - perp);
        p.currentSegment.lines[6].SetPosition(1, bezPos - perp + Vector3.up * 20);

        //-------------------------  Error check for excessive Bezier warping  ------------------------------//
        CheckForExcessiveBezierWarping(p.currentSegment);
    }

    //==========================  Function - CheckForExcessiveBezierWarping()  ============================//
    static void CheckForExcessiveBezierWarping(CurveSegment cS)
    {
        cS.acceptable = true;
        Vector3 hPrev = cS.lines[0].GetPosition(1) - cS.lines[0].GetPosition(0);

        for (int i = 2; i < cS.lines[0].positionCount; i++)
        {
            Vector3 hCur = cS.lines[0].GetPosition(i) - cS.lines[0].GetPosition(i - 1);

            if (Vector3.Angle(hCur, hPrev) > 15)
            {
                cS.acceptable = false;
                GameObject.Find("World - Generic variables").GetComponent<World_GenericVars>().materials.outlineMaterials[0].color = Color.red;

                break;
            }

            hPrev = hCur;
        }

        if (cS.acceptable)
        {
            GameObject.Find("World - Generic variables").GetComponent<World_GenericVars>().materials.outlineMaterials[0].color = Color.white;
        }
    }

    //==========================  Function - UpdatePointPosition()  ======================================//
    static void UpdatePointPosition(CurvePoint cP, Vector3 hitPos)
    {
        cP.line.SetPosition(0, hitPos);
        cP.line.SetPosition(1, hitPos + Vector3.up * 15);
        cP.gameObject.transform.position = hitPos;
    }

    //==========================  Function - UpdatePointRotation()  ======================================//
    static void UpdatePointRotation(ProtoWall p, Vector3 hitPos)
    {
        p.currentPoint.transform.LookAt(new Vector3(hitPos.x, p.currentPoint.gameObject.transform.position.y, hitPos.z));
        p.currentPoint.transform.Rotate(Vector3.up, p.rotationOffset);

        //Debug.Log(p.rotationOffset);
    }

    //==========================  Class - ProtoWall  ==================================================//
    public class ProtoWall {
        public CurvePoint       currentPoint;
        public CurveSegment     currentSegment;
        public float            wallRotation;
        public bool             complete;
        public float            rotationOffset;
        public WallingPhase     phase;
        public ScaleDragData    scaleDragData;
    }

    //==========================  Class - CurvePoint  =================================================//
    public class CurvePoint : MonoBehaviour
    {
        public float height;
        public LineRenderer line;
        public CurvePoint prev;
        public float scale;
    }

    //==========================  Class - CurveSegment  ===============================================//
    public class CurveSegment : MonoBehaviour
    {
        public CurvePoint       p0;
        public CurvePoint       p1;
        public LineRenderer[]   lines;
        public float            thickness;
        public bool             acceptable;
    }

    //==========================  Class - ScaleDragData  ==============================================//
    public class ScaleDragData {
        public Vector3      hitPoint;
        public Vector3      hitHeading;
        public float        valOG;
        public CurveSegment segment;
    }

}