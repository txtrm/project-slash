using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    private Vector2 moveInput;
    private PlayerInput.OnFootActions onFoot;

    private PlayerLook look;
    private PlayerMotor motor;
    private PlayerInput playerInput;

    void Awake()
    {
        motor = GetComponent<PlayerMotor>();
        look = GetComponent<PlayerLook>();

        playerInput = new PlayerInput();
        onFoot = playerInput.OnFoot;

        onFoot.Jump.performed += ctx => motor.Jump();
        onFoot.CrouchSlide.performed += ctx => motor.Crouch();                                
    }

    void FixedUpdate()
    {
        motor.ProcessMove(onFoot.Movement.ReadValue<Vector2>());
    }

    private void OnEnable()
    {
        onFoot.Enable();

        // listen for Move input
        onFoot.Movement.performed += ctx =>
        {
            moveInput = ctx.ReadValue<Vector2>();
        };

        onFoot.Movement.canceled += ctx =>
        {
            moveInput = Vector2.zero;
            motor.isSprinting = false; // stop sprinting immediately when movement stops
        };

        // listen for sprint key
        onFoot.Sprint.performed += ctx =>
        {
            if (moveInput != Vector2.zero)
                motor.isSprinting = true; // only sprint if moving
        };

        // Listen for look input and pass to PlayerLook
        onFoot.Look.performed += ctx =>
        {
            look.ProcessLook(ctx.ReadValue<Vector2>());
        };
        onFoot.Look.canceled += ctx =>
        {
            look.ProcessLook(Vector2.zero);
        };
    }
    
    private void OnDisable() {
        onFoot.Disable();
    }
}
