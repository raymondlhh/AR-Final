using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class SpawnMilletFromUI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public GameObject milletPrefab;
    public Transform spawnParent;

    private GameObject spawnedMillet;
    private DragObject drag;

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("POINTER DOWN FIRED");

        spawnedMillet = Instantiate(milletPrefab, spawnParent);
        drag = spawnedMillet.GetComponent<DragObject>();
        if (drag != null)
        {
            drag.StartDragging();
            drag.SendMessage("Drag", eventData.position,
                SendMessageOptions.DontRequireReceiver);
        } 
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (drag != null) drag.StopDragging();
        drag = null;
        spawnedMillet = null;
    }

}
