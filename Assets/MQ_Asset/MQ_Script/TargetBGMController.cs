using Ricimi;
using UnityEngine;
using Vuforia;

public class TargetBGMController : MonoBehaviour
{
    public BackgroundMusic bgm;

    ObserverBehaviour observer;
    Status lastStatus = Status.NO_POSE;

    void Awake()
    {
        observer = GetComponent<ObserverBehaviour>();
        observer.OnTargetStatusChanged += OnStatusChanged;
    }

    void OnDestroy()
    {
        observer.OnTargetStatusChanged -= OnStatusChanged;
    }

    void OnStatusChanged(ObserverBehaviour ob, TargetStatus status)
    {
        // Only react when status actually changes
        if (status.Status == lastStatus)
            return;

        lastStatus = status.Status;

        if (status.Status == Status.TRACKED)
        {
            Debug.Log("[Target] TRACKED → Play BGM");
            bgm.PlayBGM();
        }
        else if (status.Status == Status.EXTENDED_TRACKED)
        {
            Debug.Log("[Target] EXTENDED_TRACKED → Stop BGM");
            bgm.StopBGM();
        }
        else if (status.Status == Status.NO_POSE)
        {
            Debug.Log("[Target] NO_POSE → Stop BGM");
            bgm.StopBGM();
        }
    }
}
