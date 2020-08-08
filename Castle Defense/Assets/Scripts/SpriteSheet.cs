using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpriteSheet : MonoBehaviour
{
    //============================  Variables  ========================================//
    Material material;

    const int sheetTiling = 16;
    const int yDepth = 10;

    Vector2 currentPos = Vector2.zero;

    float lastTime;

    const float timeIncrement = 0.05f;

    readonly Vector2 endPos = new Vector2(9, 9);


    //============================  Initialise()  =====================================//
    public void Initialise()
    {
        material = this.GetComponent<MeshRenderer>().material;
        material.SetTextureScale("_AlphaTex", new Vector2(0.0625f, 0.0625f));
        this.GetComponent<AudioSource>().Play();
        GameObject.Find("World - Generic variables").GetComponent<World_GenericVars>().materials.outlineMaterials.Add(material);
        material.SetFloat("_Mixer", 1);
    }


    //============================  Update()  ========================================//
    private void Update()
    {
        if (currentPos != endPos )
        {
            if (Time.time - lastTime >= timeIncrement)
            {
                if (currentPos.x < sheetTiling - 1)
                    currentPos.x++;
                else
                {
                    currentPos.x = 0;
                    currentPos.y++;
                }

                material.SetTextureOffset("_AlphaTex", new Vector2(currentPos.x * 0.0625f, currentPos.y * -0.0625f + 0.9375f));

                lastTime = Time.time;
            }
        }
        else
        {
            if (this.GetComponent<ScrollAsset>() != null)
                this.GetComponent<ScrollAsset>().enabled = true;

            GameObject.Find("World - Generic variables").GetComponent<World_GenericVars>().materials.outlineMaterials.Remove(material);
            Destroy(this.GetComponent<MeshRenderer>().material);
            this.GetComponent<MeshRenderer>().sharedMaterial = GameObject.Find("World - Generic variables").GetComponent<World_GenericVars>().materials.outlineMaterials[0];

            Destroy(this);
        }
    }
}
