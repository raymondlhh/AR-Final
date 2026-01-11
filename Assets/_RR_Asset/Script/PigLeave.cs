using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PigLeave : MonoBehaviour
{
    [Header("Movement Settings")]
    public Transform intermediateTarget; 
    public Transform finalTarget;        
    public float moveSpeed = 1f;         
    public float rotateSpeed = 5f;       
    public GameObject targetObject;      
    public bool appearOnce = false;

    private Transform currentTarget;
    private bool shouldRotate = false;
    private bool shouldMove = false;

    [Header("Animator Settings")]
    public Animator animator;
    public string boolParameterName = "Walking";

    void Start()
    {
        animator = GetComponent<Animator>();
        currentTarget = intermediateTarget; 
    }

    void Update()
    {
        if (targetObject.activeSelf && !appearOnce)
        {
            appearOnce = true;
        }

        
        if (!targetObject.activeSelf && appearOnce)
        {
            shouldRotate = true;
        }

        if (shouldRotate && currentTarget != null)
        {
            
            Vector3 direction = (currentTarget.position - transform.position).normalized;
            if (direction != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, rotateSpeed * Time.deltaTime);

                
                if (Quaternion.Angle(transform.rotation, lookRotation) < 1f)
                {
                    shouldRotate = false; 
                    shouldMove = true;   
                    animator.SetBool(boolParameterName, true);
                }
            }
        }

        if (shouldMove && currentTarget != null)
        {
            
            transform.position = Vector3.MoveTowards(transform.position, currentTarget.position, moveSpeed * Time.deltaTime);

            
            if (Vector3.Distance(transform.position, currentTarget.position) < 0.01f)
            {
                
                if (currentTarget == intermediateTarget)
                {
                    currentTarget = finalTarget;
                    shouldRotate = true; 
                    shouldMove = false;
                }

                else if (currentTarget == finalTarget)
                {
                    shouldMove = false;
                    animator.SetBool(boolParameterName, false); 
                    gameObject.SetActive(false);               
                }
            }
        }
    }

}
