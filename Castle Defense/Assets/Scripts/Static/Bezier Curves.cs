using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class BezierCurves
{
    //==================  Function - BezierPositions()  ======================================//
    public static Vector3[] BezierPositions(LineRenderer lineRenderer, int pointCount, Vector3 pStart, Vector3 pEnd, Vector3 h0, Vector3 h1)
    {
        lineRenderer.positionCount = pointCount;
        lineRenderer.startWidth = 0.5f;
        lineRenderer.endWidth = 0.5f;

        Vector3[] positions = new Vector3[pointCount];

        for (int i = 0; i < pointCount; i++)
        {
            float t = (float)i / (pointCount - 1);
            positions[i] = CubicPointPosition(t, pStart, pEnd, h0, h1);
            lineRenderer.SetPosition(i, positions[i]);
        }

        return positions;
    }

    //==================  Function - CubicPointPosition()  ======================================//
    public static Vector3 CubicPointPosition(float t, Vector3 p0, Vector3 p3, Vector3 h0, Vector3 h1)
    {
        Vector3 p1 = p0 + h0;
        Vector3 p2 = p3 + h1;

        return (Mathf.Pow(1 - t, 3) * p0) + (3 * Mathf.Pow(1 - t, 2) * t * p1) + (3 * (1 - t) * Mathf.Pow(t, 2) * p2) + (Mathf.Pow(t, 3) * p3);
    }

    //==================  Function - CubicPointPerpendicular()  ======================================//
    public static Vector3 CubicPointPerpendicular(Vector3 point, float t, Vector3 p0, Vector3 p3, Vector3 h0, Vector3 h1)
    {
        Vector3 pBefore = CubicPointPosition(t - 0.01f, p0, p3, h0, h1);

        Vector3 heading = (point - pBefore).normalized;

        Vector2 perpendicular = Vector2.Perpendicular(new Vector2(heading.x, heading.z));

        return new Vector3(perpendicular.x, 0, perpendicular.y);
    }

    //==================  Function - BezierDistance()  ======================================//
    public static float BezierDistance(LineRenderer line)
    {
        Vector3[] points = new Vector3[line.positionCount];
        line.GetPositions(points);

        float distance = 0;

        for (int i = 1; i < points.Length; i++)
            distance += Vector3.Distance(points[i - 1], points[i]);

        return distance;
    }

    //==================  Function - GetBezierHeadings()  ======================================//
    public static Headings GetBezierHeadings(HUD_AdvancedWalling.ProtoWall p, HUD_AdvancedWalling.CurveSegment cS, HUD_AdvancedWalling.CurvePoint p0, HUD_AdvancedWalling.CurvePoint p1, bool rotateh1)
    {
        Headings h = new Headings();
        float distance = BezierCurves.BezierDistance(cS.lines[0]);

        h.h0 = Quaternion.AngleAxis(p.wallRotation, Vector3.up) * p0.transform.forward;
        h.h1 = Quaternion.AngleAxis(p.wallRotation - 180f, Vector3.up) * p1.transform.forward;

        /*
        if (distance * 2 > p0.scale + p1.scale) {
            h.h0 *= p0.scale;
            h.h1 *= p1.scale;
        }
        */

        h.h0 *= distance / 2;
        h.h1 *= distance / 2;

        if (Vector3.Angle(h.h0, h.h1) > 180)
            h.h1 = Quaternion.AngleAxis(180f, Vector3.up) * h.h1;

        return h;
    }

    public class Headings {
        public Vector3 h0;
        public Vector3 h1;
    }

    public class Quadratic
    {
        private static Vector3 GetMidPoint(Vector3 p0, Vector3 p1, Vector3 p0_heading, Vector3 p1_heading)
        {
            Vector3 pMid = Vector2.zero;

            Vector2 h0 = new Vector2(p0_heading.x, p0_heading.z).normalized;

            Vector2 h1 = new Vector2(-p1_heading.x, -p1_heading.z).normalized;

            //----------  Linear Equations  -----------------------------------------------------------//
            float m0;
            float c0;

            float m1;
            float c1;

            if (h0.x == 0)
                m0 = 10 * Mathf.Sign(h0.y);
            else if (h0.y == 0)
                m0 = 0;
            else
                m0 = h0.y / h0.x;

            c0 = p0.z - p0.x * m0;

            if (h1.x == 0)
                m1 = 10 * Mathf.Sign(h1.y);
            else if (h1.y == 0)
                m1 = 0;
            else
                m1 = h1.y / h1.x;

            c1 = p1.z - p1.x * m1;

            pMid.x = (c1 - c0) / (m0 - m1);
            pMid.z = m0 * pMid.x + c0;

            Debug.Log(pMid);

            //--------------  Return  --------------------//
            return pMid;
        }

        public static Vector3 QuadraticPointPosition(float t, Vector3 p0, Vector3 p1, Vector3 p2)
        {
            Vector3 pos = Mathf.Pow(1 - t, 2) * p0 + 2 * (1 - t) * t * p1 + Mathf.Pow(t, 2) * p2;

            return pos;
        }
    }
}