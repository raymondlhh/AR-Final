using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting;
using UnityEngine;

public class WolfLeaving : MonoBehaviour
{
    public Transform finalTarget;
    public GameObject previousWolf;
    public float moveSpeed = 1f;
    public float rotateSpeed = 360f; // degrees per second

    public Animator WolfAnim;

    public float stopDistance = 0.05f;

    bool hasRotated = false;
    bool appear = false;
    public bool activate = false;

    Quaternion targetRotation;

    void Start()
    {
        WolfAnim = GetComponent<Animator>();

        // Rotate 180 degrees from current direction
        targetRotation = Quaternion.Euler(
            transform.eulerAngles.x,
            transform.eulerAngles.y + 180f,
            transform.eulerAngles.z
        );
    }

    public void OnEnable()
    {
        activate = true;
        previousWolf.SetActive(false);
    }

    void Update()
    {
        if (activate)
        {

            if (!hasRotated)
            {
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    rotateSpeed * Time.deltaTime
                );


                if (Quaternion.Angle(transform.rotation, targetRotation) < 0.5f)
                {
                    hasRotated = true;
                }

                return;
            }

            float distance = Vector3.Distance(transform.position, finalTarget.position);

            if (distance > stopDistance)
            {
                WolfAnim.SetBool("Reach", false);

                transform.position = Vector3.MoveTowards(
                    transform.position,
                    finalTarget.position,
                    moveSpeed * Time.deltaTime
                );
            }
            else
            {

                WolfAnim.SetBool("Reach", true);

                if (!appear)
                {
                    appear = true;
                    gameObject.SetActive(false);
                }
            }
        }
    }
}
