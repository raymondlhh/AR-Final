using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Testing : MonoBehaviour
{
    private Vector3 offset;
    private float zCoord;
    private bool dragging = false;

    public GameObject dragPrefab;
    private GameObject currentDragObj;

    public GameObject[] hayOnTable;

    void OnMouseDown()
    {
        zCoord = Camera.main.WorldToScreenPoint(transform.position).z;
        offset = transform.position - GetTouchWorldPos();
        dragging = true;

        // Spawn drag prefab at touch position
        currentDragObj = Instantiate(dragPrefab, transform.position, Quaternion.identity);
    }

    void OnMouseDrag()
    {
        if (!dragging || currentDragObj == null) return;

        currentDragObj.transform.position = GetTouchWorldPos() + offset;
    }

    void OnMouseUp()
    {
        dragging = false;

        // Activate next hay on table
        ActivateNextHay();

        // Destroy dragged prefab
        if (currentDragObj != null)
            Destroy(currentDragObj);
    }

    void ActivateNextHay()
    {
        for (int i = 0; i < hayOnTable.Length; i++)
        {
            if (!hayOnTable[i].activeSelf)
            {
                hayOnTable[i].SetActive(true);
                break;
            }
        }
    }

    Vector3 GetTouchWorldPos()
    {
        Vector3 mousePoint = Input.mousePosition;
        mousePoint.z = zCoord;
        return Camera.main.ScreenToWorldPoint(mousePoint);
    }

}
