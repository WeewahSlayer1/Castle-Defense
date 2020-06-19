using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Camera_Movement : MonoBehaviour
{
    public Transform cameraRigZoom;
    public float speed = 1;
    public float sensitivity = 1f;
    float zoom = 0;

    //=================  Update()  ========================================================//
    void Update()
    {
        //------------  Translation  -------------------------------------------------//
        Vector2 movement = Vector2.zero;
        if (Input.GetKey(KeyCode.W)) movement.y += 1;
        if (Input.GetKey(KeyCode.S)) movement.y -= 1;
        if (Input.GetKey(KeyCode.A)) movement.x -= 1;
        if (Input.GetKey(KeyCode.D)) movement.x += 1;

        movement *= speed;

        this.transform.Translate(new Vector3(movement.x, 0, movement.y), Space.World);

        //---------------  Zoom  ----------------------------------------------------//
        cameraRigZoom.transform.position = this.transform.position;

        zoom = Mathf.Clamp(zoom + Input.mouseScrollDelta.y * sensitivity, -20, 20);

        cameraRigZoom.Translate(0, 0, zoom, Space.Self);
    }
}
