using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class HUD_SelectionBox
{
    public struct SelectionBoxVars
    {
        public Vector3 p1;

        public MeshCollider selectionBox;
        public Mesh selectionMesh;

        public Vector2[] corners;
        public Vector3[] verts;
    }

    public static SelectionBoxVars CreateSelectionBoxObj(SelectionBoxVars sBv, GameObject HUD)
    {
        sBv.selectionBox = HUD.gameObject.AddComponent<MeshCollider>();
        sBv.selectionBox.convex = true;
        sBv.selectionBox.isTrigger = true;

        sBv.p1 = Input.mousePosition;

        return sBv;
    }

    public static Vector2[] GetBoundingBox(Vector2 p1, Vector2 p2)
    {
        Vector2 newP1;
        Vector2 newP2;
        Vector2 newP3;
        Vector2 newP4;

        if (p1.x < p2.x) //if p1 is to the left of p2
        {
            if (p1.y > p2.y) // if p1 is above p2
            {
                newP1 = p1;
                newP2 = new Vector2(p2.x, p1.y);
                newP3 = new Vector2(p1.x, p2.y);
                newP4 = p2;
            }
            else //if p1 is below p2
            {
                newP1 = new Vector2(p1.x, p2.y);
                newP2 = p2;
                newP3 = p1;
                newP4 = new Vector2(p2.x, p1.y);
            }
        }
        else //if p1 is to the right of p2
        {
            if (p1.y > p2.y) // if p1 is above p2
            {
                newP1 = new Vector2(p2.x, p1.y);
                newP2 = p1;
                newP3 = p2;
                newP4 = new Vector2(p1.x, p2.y);
            }
            else //if p1 is below p2
            {
                newP1 = p2;
                newP2 = new Vector2(p1.x, p2.y);
                newP3 = new Vector2(p2.x, p1.y);
                newP4 = p1;
            }

        }

        Vector2[] corners = { newP1, newP2, newP3, newP4 };
        return corners;
    }
    
    //generate a mesh from the 4 bottom points
    public static Mesh GenerateSelectionMesh(Vector3[] corners)
    {
        Vector3[] verts = new Vector3[8];
        int[] tris = { 0, 1, 2, 2, 1, 3, 4, 6, 0, 0, 6, 2, 6, 7, 2, 2, 7, 3, 7, 5, 3, 3, 5, 1, 5, 0, 1, 1, 4, 0, 4, 5, 6, 6, 5, 7 }; //map the tris of our cube

        for (int i = 0; i < 4; i++)
        {
            verts[i] = corners[i];
        }

        for (int j = 4; j < 8; j++)
        {
            verts[j] = corners[j - 4] + Vector3.up * 100.0f;
        }

        Mesh selectionMesh = new Mesh();
        selectionMesh.vertices = verts;
        selectionMesh.triangles = tris;

        return selectionMesh;
    }

    public static void UpdateBoxSelect(SelectionBoxVars sBv, LayerMask ground)
    {
        sBv.verts = new Vector3[4];
        Vector3 p2 = Input.mousePosition;

        if (sBv.p1.x > p2.x + 5 || p2.x > sBv.p1.x + 5)
            if (sBv.p1.y > p2.y + 5 || p2.y > sBv.p1.y + 5)
            {
                sBv.corners = GetBoundingBox(sBv.p1, p2);

                for (int i = 0; i < sBv.corners.Length; i++)
                {
                    Ray ray = Camera.main.ScreenPointToRay(sBv.corners[i]);
                    RaycastHit hit;
                    if (Physics.Raycast(ray, out hit, 50000.0f, ground))
                    {
                        sBv.verts[i] = new Vector3(hit.point.x, hit.point.y, hit.point.z);
                        Debug.DrawLine(Camera.main.ScreenToWorldPoint(sBv.corners[i]), hit.point, Color.red, 1.0f);
                    }
                }

                //generate the mesh
                sBv.selectionMesh = GenerateSelectionMesh(sBv.verts);

                sBv.selectionBox.sharedMesh = sBv.selectionMesh;

            }
    }

}
