using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;



public class MoveToDestination : MonoBehaviour
{

    public GameObject butterfly;
    private Animator butterflyAnim;
    //Vector3 targetPosition = new Vector3(0, 0, 0);

    void Start()
    {
        butterflyAnim = butterfly.GetComponent<Animator>();
    }


    void FixedUpdate()
    {

        Animation_Go();


    }

    void Animation_Go()
    {
        butterflyAnim.SetBool("IsDoubleFlapping", true);
        butterflyAnim.SetBool("TurnLeft", false);
        butterflyAnim.SetBool("TurnRight", false);
        butterflyAnim.SetBool("GoForward", false);
        butterflyAnim.SetBool("IsSlowFlapping", false);
        butterflyAnim.SetBool("IsTouched", false);
        butterflyAnim.SetBool("IsReturning", true);

    }
    void Animation_Stop()
    {
        butterflyAnim.SetBool("IsDoubleFlapping", false);
        butterflyAnim.SetBool("TurnLeft", false);
        butterflyAnim.SetBool("TurnRight", false);
        butterflyAnim.SetBool("GoForward", false);
        butterflyAnim.SetBool("IsSlowFlapping", false);
        butterflyAnim.SetBool("IsTouched", true);
        butterflyAnim.SetBool("IsReturning", true);
    }
}
