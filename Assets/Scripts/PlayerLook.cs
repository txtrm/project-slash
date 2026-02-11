using UnityEngine;

public class PlayerLook : MonoBehaviour
{
    public Camera cam;

    public float xSens = 150f; // left/right
    public float ySens = 120f; // up/down
    public float maxMouseDelta = 8f;

    private float xRotation = 0f;

    // Store latest input for use in LateUpdate
    private Vector2 latestInput = Vector2.zero;

    // Called by InputManager to update look input
    public void ProcessLook(Vector2 input)
    {
        latestInput = input;
        Debug.Log($"ProcessLook called with input: {input} at frame {Time.frameCount}");
    }

    void LateUpdate()
    {
        // Use latest input to update camera rotation
        float mouseX = latestInput.x;
        float mouseY = latestInput.y;

        float deltaX = mouseX * xSens * Time.deltaTime;
        float deltaY = mouseY * ySens * Time.deltaTime;

        deltaX = Mathf.Clamp(deltaX, -maxMouseDelta, maxMouseDelta);
        deltaY = Mathf.Clamp(deltaY, -maxMouseDelta, maxMouseDelta);

        // Vertical look (camera)
        xRotation -= deltaY;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);
        cam.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Horizontal look (player)
        transform.Rotate(Vector3.up * deltaX);
    }
}