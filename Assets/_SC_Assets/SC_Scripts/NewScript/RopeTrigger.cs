using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RopeTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Table"))
        {
            Debug.Log("Done");
        }
    }
}
