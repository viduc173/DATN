using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class HandAnimated : MonoBehaviour
{
    private Animator handAnimator;
    private HandInputValue handInput;    
    void Start()
    {
        handAnimator = GetComponent<Animator>();
        handInput = GetComponent<HandInputValue>();
    }

    // Update is called once per frame
    void Update()
    {
        handAnimator.SetFloat("Trigger", handInput.triggerValue);
        handAnimator.SetFloat("Grip", handInput.gridValue);
    }
}
