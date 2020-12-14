using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class TurnLeft : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private bool isBtnDown = false;
    private GameObject butterfly;
    private Animator butterflyAnim;
    public Rigidbody rb;
    private float forwardSpeed = 20.0f;

    void Start()
    {
        butterfly = GameObject.FindGameObjectWithTag("butterfly");
        butterflyAnim = butterfly.GetComponent<Animator>();
    }

    void FixedUpdate()
    {
        ExecuteTurnRight();
    }

    public void ExecuteTurnRight()
    {
        if (isBtnDown || Input.GetKey(KeyCode.A))
        {
            this.rb.isKinematic = false;

            butterflyAnim.SetBool("IsDoubleFlapping", false);
            butterflyAnim.SetBool("TurnLeft", false);
            butterflyAnim.SetBool("TurnRight", true);
            butterflyAnim.SetBool("GoForward", true);
            butterflyAnim.SetBool("IsSlowFlapping", false);
            butterflyAnim.SetBool("IsTouched", false);

            //rotation
            butterfly.transform.Rotate(0, -3.0f, 0);

            //move
            this.rb.AddRelativeForce(Vector3.forward * forwardSpeed);

            Debug.Log("Right");
        }

        else
            butterflyAnim.SetBool("TurnRight", false);
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