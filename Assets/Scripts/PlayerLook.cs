using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class PlayerLook : MonoBehaviour
{
    public Camera cam;

    private float xRotation = 0f;

    public float xSens = 30f;
    public float ySens = 30f;

    public void ProcessLook(Vector2 input)
    {
        float mouseX = input.x;
        float mouseY = input.y;
        // calculate camera rotation for looking up and down
        xRotation -= (mouseY * Time.deltaTime) * ySens / 15f; // Divide by 15 to match horizontal sensitivity
        xRotation = Mathf.Clamp(xRotation, -1.5f, 1.5f);
        // apply to camera transform
        cam.transform.localRotation = quaternion.Euler(xRotation, 0, 0);
        // rotate player to look right and left
        transform.Rotate(Vector3.up * (mouseX * Time.deltaTime) * xSens);
    }
}
