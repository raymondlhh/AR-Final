using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WolfComing : MonoBehaviour
{
    public Transform finalTarget;
    public float moveSpeed = 1f;
    public Animator WolfAnim;
    public GameObject WolfDialogue;

    public float stopDistance = 0.05f; // how close is "arrived"

    void Start()
    {
        WolfAnim = GetComponent<Animator>();
        WolfDialogue.SetActive(false);
    }

    void Update()
    {
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
            // Reached destination
            WolfAnim.SetBool("Reach", true);
            WolfDialogue.SetActive(true);
        }
    }
}
