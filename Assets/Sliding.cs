using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sliding : MonoBehaviour
{
    private Vector3 slideDirection;
    public PlayerMotor PM;

    public float ForceTimer = 1f;

    void Update()
    {   
        // if (PM.LerpCrouch && PM.isCrouching)
        // {
        //     ForceTimer = 1f;
        // }

        // slideDirection = transform.forward;

        // if (PM.isSprinting && PM.isCrouching)
        // {
        //     ForceTimer -= Time.deltaTime;

        //     Debug.Log("HEHEH");
        //     if (ForceTimer > 0)
        //     {
        //         PM.controller.Move(slideDirection * 10f * Time.deltaTime);
        //     }
        // }
    }
}
