using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MoveUp : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private bool isBtnDown = false;
    private GameObject butterfly;
    private Animator butterflyAnim;
    public Rigidbody rb;
    private Vector3 upSpeed = new Vector3(0, 1.0f, 0);

    void Start()
    {
        butterfly = GameObject.FindGameObjectWithTag("butterfly");
        butterflyAnim = butterfly.GetComponent<Animator>();
    }


    void FixedUpdate()
    {
        ExecuteMoveUp();

    }

    public void ExecuteMoveUp()
    {
        if (isBtnDown || Input.GetKey(KeyCode.W))
        {
            this.rb.isKinematic = false;

            butterflyAnim.SetBool("IsDoubleFlapping", true);
            butterflyAnim.SetBool("TurnLeft", false);
            butterflyAnim.SetBool("TurnRight", false);
            butterflyAnim.SetBool("GoForward", false);
            butterflyAnim.SetBool("IsSlowFlapping", false);
            butterflyAnim.SetBool("IsTouched", false);
            butterflyAnim.SetBool("IsReturning", false);

            this.rb.MovePosition(rb.position + upSpeed * Time.deltaTime);

            Debug.Log("Up");
        }

    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isBtnDown = true;
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
