using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MoveDown : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private bool isBtnDown = false;
    private GameObject butterfly;
    private Animator butterflyAnim;
    public Rigidbody rb;
    private Vector3 downSpeed = new Vector3(0, -0.8f, 0);
    
    void Start()
    {
        butterfly = GameObject.FindGameObjectWithTag("butterfly");
        butterflyAnim = butterfly.GetComponent<Animator>();
    }

    void FixedUpdate()
    {
        ExecuteMoveDown();
    }

    public void ExecuteMoveDown()
    {

        if (isBtnDown || Input.GetKey(KeyCode.S))
        {
            this.rb.isKinematic = false;

            butterflyAnim.SetBool("IsDoubleFlapping", false);
            butterflyAnim.SetBool("TurnLeft", false);
            butterflyAnim.SetBool("TurnRight", false);
            butterflyAnim.SetBool("GoForward", false);
            butterflyAnim.SetBool("IsSlowFlapping", true);
            butterflyAnim.SetBool("IsTouched", false);

            this.rb.MovePosition(rb.position + downSpeed * Time.deltaTime);

            Debug.Log("Down");
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isBtnDown = true;
        butterflyAnim.SetBool("GoForward", false);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isBtnDown = false;

        butterflyAnim.SetBool("IsDoubleFlapping", false);
        butterflyAnim.SetBool("TurnLeft", false);
        butterflyAnim.SetBool("TurnRight", false);
        butterflyAnim.SetBool("GoForward", true);
        butterflyAnim.SetBool("IsSlowFlapping", false);
        butterflyAnim.SetBool("IsTouched", false);
    }
}