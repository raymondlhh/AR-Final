using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeHarvest : MonoBehaviour
{
    private bool isHarvesting = false;

    public void StartHarvest()
    {
        if (isHarvesting) return;

        isHarvesting = true;

    }

    public void DestroyTree()
    {
        Destroy(gameObject);
        Debug.Log("tree destroy");
    }
}
