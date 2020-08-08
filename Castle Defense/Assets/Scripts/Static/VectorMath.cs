using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class VectorMath
{
    public static Vector3 Vec2closestPoint(Vector3 _initPos, Vector3 _dir, Vector3 _point)
    {
        Vector2 initPos = new Vector2(_initPos.x, _initPos.z);
        Vector2 dir = new Vector2(_dir.x, _dir.z);
        Vector2 point = new Vector2(_point.x, _point.z);

        float m_eq1 = 0;
        if (dir.x != 0 && dir.y != 0)   m_eq1 = dir.y / dir.x;
        else if (dir.y == 0)            m_eq1 = 0.0001f;
        else if (dir.x == 0)            m_eq1 = 1000;

        float c_eq1 = initPos.y - initPos.x * m_eq1;

        float m_eq2 = -1 / m_eq1;
        float c_eq2 = point.y - point.x * m_eq2;

        Vector3 closestPoint = Vector3.zero;
        closestPoint.x = (c_eq2 - c_eq1) / (m_eq1 - m_eq2);
        closestPoint.z = m_eq1 * closestPoint.x + c_eq1;
        //closestPoint.y = _dir.y * (new Vector2(closestPoint.x, closestPoint.z) - initPos).magnitude;
        closestPoint.y = _point.y;

        /*
        Debug.Log("initPos: " + initPos);
        Debug.Log("dir: " + dir);
        Debug.Log("point: " + point);
        Debug.Log("Eq1 = " + m_eq1 + " + " + c_eq1);
        Debug.Log("Eq2 = " + m_eq2 + " + " + c_eq2);
        Debug.Log("closestPoint: " + closestPoint);
        */

        return closestPoint;
    }
}
