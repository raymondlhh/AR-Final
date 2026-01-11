using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitGroundDestroy : MonoBehaviour
{
    public LayerMask groundLayer;

    private void OnCollisionEnter(Collision collision)
    {
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            Destroy(gameObject);
        }
    }
}
