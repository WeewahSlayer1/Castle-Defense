using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Unit_Human
{
    //=============  Function - EnableDisableRagdoll()  =============================//
    public static void EnableDisableRagdoll(Unit u, bool enable)
    {
        if (enable)
        {
            Rigidbody[] rbArr = u.GetComponentsInChildren<Rigidbody>();    //Disable rigidbodies
            foreach (Rigidbody rb in rbArr)
                rb.isKinematic = false;

            Object.Destroy(u.GetComponent<Rigidbody>());

            /*
            Collider[] cArr = u.GetComponentsInChildren<Collider>();    //Disable colliders
            foreach (Collider c in cArr)
                c.enabled = true;

            u.GetComponent<CapsuleCollider>().enabled = false;
            */

            for (int i = 0; i < u.anims.Length; i++)
                u.anims[i].enabled = false;
        }
        else
        {
            //---------------------------  Disable ragdoll  -----------------------------------------//
            Rigidbody[] rbArr = u.GetComponentsInChildren<Rigidbody>();    //Disable rigidbodies
            foreach (Rigidbody rb in rbArr)
                rb.isKinematic = true;

            u.GetComponent<Rigidbody>().isKinematic = false;

            /*
            Collider[] cArr = u.GetComponentsInChildren<Collider>();    //Disable colliders
            foreach (Collider c in cArr)
                c.enabled = false;

            u.GetComponent<CapsuleCollider>().enabled = true;
            */

            for (int i = 0; i < u.anims.Length; i++)
                u.anims[i].enabled = true;
        }
    }

    public static void DeathCleanUp (Unit u)
    {
        /////////////////////////////////// Create Remains from body & item meshes  //////////////////////
        GameObject corpse = new GameObject();                                                           //
                                                                                                        //
        // -----------------  Transforms & naming  -------------------------------------//              //
        corpse.transform.parent     = u.transform.parent;                                               //
        corpse.name                 = "Corpse of " + u.name;                                            //
                                                                                                        //
        // -----------------  MeshFilter, MeshRenderer  --------------------------------//              //
        MeshFilter mfc = corpse.AddComponent<MeshFilter>();                                             //
                                                                                                        //
        if (u.humanUnitVars.skinnedMeshRenderer_body == null)                                           //
            Debug.Log("ERROR: " + u.name + "humanUnitVars.skinnedMeshRenderer_body == null");           //
        if (u.humanUnitVars.skinnedMeshRenderer_item == null)                                           //
            Debug.Log("ERROR: " + u.name + "humanUnitVars.skinnedMeshRenderer_item == null");           //
                                                                                                        //
        CombineInstance[] combine = new CombineInstance[2];
        combine[0].mesh = new Mesh();
        combine[1].mesh = new Mesh();

        u.humanUnitVars.skinnedMeshRenderer_body.BakeMesh(combine[0].mesh);
        combine[0].transform = u.transform.localToWorldMatrix;

        u.humanUnitVars.skinnedMeshRenderer_item.BakeMesh(combine[1].mesh);
        combine[1].transform = u.transform.localToWorldMatrix;

        mfc.mesh.CombineMeshes(combine);
        corpse.isStatic = true;

        corpse.AddComponent<MeshRenderer>().sharedMaterial = u.humanUnitVars.skinnedMeshRenderer_body.sharedMaterial;    //
        //////////////////////////////////////////////////////////////////////////////////////////////////       

        // -----------------  Destroy Original  -------------------------------------//
        Object.Destroy(u.gameObject);
    }

    [System.Serializable]
    public struct HumanVars
    {
        public SkinnedMeshRenderer  skinnedMeshRenderer_body;    // Character mesh
        public SkinnedMeshRenderer  skinnedMeshRenderer_item;
        public GameObject           deathFX;
    }
}
