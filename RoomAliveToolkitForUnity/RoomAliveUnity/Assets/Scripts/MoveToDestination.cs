using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;



public class MoveToDestination : MonoBehaviour
{

    private GameObject butterfly;
    private GameObject player;
    private Animator butterflyAnim;
    public Rigidbody rb;
    public Transform destinationObject;
    //Vector3 targetPosition = new Vector3(0, 0, 0);
    public float speed = 1.0f;

    void Start()
    {
        butterfly = GameObject.FindGameObjectWithTag("butterfly");
        butterflyAnim = butterfly.GetComponent<Animator>();
    }


    void Update()
    {

        float distanceToOrigin = (butterfly.transform.position - destinationObject.position).sqrMagnitude;

        if (Input.GetKey(KeyCode.Return))
        {
            StopAllCoroutines();
            StartCoroutine(MoveTo(butterfly.transform.position, destinationObject.position, speed));

            butterflyAnim.SetBool("IsDoubleFlapping", true);
            butterflyAnim.SetBool("TurnLeft", false);
            butterflyAnim.SetBool("TurnRight", false);
            butterflyAnim.SetBool("GoForward", false);
            butterflyAnim.SetBool("IsSlowFlapping", false);
            butterflyAnim.SetBool("IsTouched", false);
            butterflyAnim.SetBool("IsReturning", true);
        }

        if (distanceToOrigin < 0.01f)
        {
            Vector3 eulerAngles = transform.eulerAngles;
            transform.eulerAngles = new Vector3(0, eulerAngles.y, 0);

            butterflyAnim.SetBool("IsDoubleFlapping", false);
            butterflyAnim.SetBool("TurnLeft", false);
            butterflyAnim.SetBool("TurnRight", false);
            butterflyAnim.SetBool("GoForward", false);
            butterflyAnim.SetBool("IsSlowFlapping", false);
            butterflyAnim.SetBool("IsTouched", true);
            butterflyAnim.SetBool("IsReturning", true);
        }

    }


    IEnumerator MoveTo(Vector3 start, Vector3 destination, float speed)
    {
        float distanceToTarget = (butterfly.transform.position - destination).sqrMagnitude;

        while (distanceToTarget >= 0.01f)
        {
            transform.LookAt(destination);
            transform.position = Vector3.MoveTowards(transform.position, destination, speed * Time.deltaTime);
            yield return null;
            break;
        }
    }

   
}
