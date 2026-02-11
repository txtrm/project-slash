using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class attack : MonoBehaviour
{
    public Animator anim;
    public InputAction attackAction;

    private void OnEnable()
    {
        attackAction.Enable();
        attackAction.performed += OnAttack;
    }

    private void OnDisable()
    {
        attackAction.performed -= OnAttack;
        attackAction.Disable();
    }

    private void OnAttack(InputAction.CallbackContext context)
    {
        anim.SetTrigger("Attack");
        Debug.Log("hdfsalköjads");
    }
}
