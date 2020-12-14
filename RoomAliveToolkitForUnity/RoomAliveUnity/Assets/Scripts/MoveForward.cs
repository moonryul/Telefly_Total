using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MoveForward : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private bool isBtnDown = false;
    private GameObject butterfly;
    private Animator butterflyAnim;
    public Rigidbody rb;
    private float forwardSpeed = 25.0f;

    void Start()
    {
        butterfly = GameObject.FindGameObjectWithTag("butterfly");
        butterflyAnim = butterfly.GetComponent<Animator>();
    }

    void FixedUpdate()
    {
        ExecuteMoveForward();
    }

    private void ExecuteMoveForward()
    {
        if (isBtnDown || Input.GetKey(KeyCode.Space))
        {
            this.rb.isKinematic = false;

            butterflyAnim.SetBool("IsDoubleFlapping", false);
            butterflyAnim.SetBool("TurnLeft", false);
            butterflyAnim.SetBool("TurnRight", false);
            butterflyAnim.SetBool("GoForward", true);
            butterflyAnim.SetBool("IsSlowFlapping", false);
            butterflyAnim.SetBool("IsTouched", false);

            this.rb.AddRelativeForce(Vector3.forward * forwardSpeed);
            //this.rb.MovePosition(rb.position + forwardSpeed * Time.deltaTime);

            Debug.Log("Forward");
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isBtnDown = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isBtnDown = false;
    }

}