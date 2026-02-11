using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.ProBuilder;

public class ViewBobbing : MonoBehaviour
{
    [Range(0.001f, 2f)]
    public float Amount = 0.002f;

    [Range(1f, 30f)]
    public float Frequency = 10.0f;

    [Range(0f, 100f)]
    public float Smooth = 10.0f;

    Vector3 StartPos;
    [HideInInspector]
    public float moveAmount;

    void Start()
    {
        StartPos = transform.localPosition;
    }

    void LateUpdate()
    {
        CheckForHeadBobTrigger();
        StopHeadBob();
    }
    
    public void CheckForHeadBobTrigger()
    {
        if (moveAmount > 0.1f && Amount > 0f)
        {
            StartHeadBob();
        }
    }

    private void StartHeadBob()
    {
        float y = Mathf.Sin(Time.time * Frequency) * Amount * 1.4f;
        float x = Mathf.Cos(Time.time * Frequency / 2f) * Amount * 1.6f;

        Vector3 targetPos = StartPos + new Vector3(x, y, 0);
        transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            targetPos,
            Smooth * Time.deltaTime
        );
    }

    private void StopHeadBob()
    {
        transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            StartPos,
            Smooth * Time.deltaTime
        );
    }
}
