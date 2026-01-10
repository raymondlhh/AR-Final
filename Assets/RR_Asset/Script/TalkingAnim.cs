using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TalkingAnim : MonoBehaviour
{
    public GameObject targetObject;
    public Animator animator;       
    public string boolParameterName = "Talking"; // the bool in Animator

    void Update()
    {

        // Set the animator bool based on object active state
        if (targetObject.activeSelf)
        {
            animator.SetBool(boolParameterName, true);
        }
        else
        {
            animator.SetBool(boolParameterName, false);
        }
    }
}
