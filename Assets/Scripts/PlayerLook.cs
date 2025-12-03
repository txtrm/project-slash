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
    [Tooltip("Scale applied to raw mouse delta (pixels). Separate X/Y multipliers let you tune horizontal and vertical sensitivity independently.")]
    public float mouseXMultiplier = 0.002f;
    public float mouseYMultiplier = 0.004f;
    [Tooltip("Clamp maximum degrees change from mouse in a single frame to avoid spikes.")]
    public float maxMouseDelta = 8f;

    public void ProcessLook(Vector2 input)
    {
        float mouseX = input.x;
        float mouseY = input.y;

        // Detect likely mouse (large deltas) vs gamepad stick (small -1..1 values)
        bool isMouse = Mathf.Abs(mouseX) > 1f || Mathf.Abs(mouseY) > 1f;

        float deltaX;
        float deltaY;

        if (isMouse)
        {
            // Mouse delta is in pixels/frame — scale using per-axis multipliers
            deltaX = mouseX * xSens * mouseXMultiplier;
            deltaY = mouseY * ySens * mouseYMultiplier;

            // Clamp to avoid huge single-frame jumps from mouse (e.g., jitter or raw input spikes)
            deltaX = Mathf.Clamp(deltaX, -maxMouseDelta, maxMouseDelta);
            deltaY = Mathf.Clamp(deltaY, -maxMouseDelta, maxMouseDelta);
        }
        else
        {
            // Gamepad stick provides -1..1; multiply by sensitivity (deg/sec) and frame time
            deltaX = mouseX * xSens * Time.deltaTime;
            deltaY = mouseY * ySens * Time.deltaTime;
        }

        // calculate camera rotation for looking up and down (degrees)
        xRotation -= deltaY;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);

        // apply to camera transform
        cam.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // rotate player to look right and left
        transform.Rotate(Vector3.up * deltaX);
    }
}
