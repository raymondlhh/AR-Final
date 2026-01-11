using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HouseBlow : MonoBehaviour
{
    public Animator houseAnim;

    
    // Start is called before the first frame update
    void Start()
    {
        houseAnim = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void StartBlowHouse()
    {
        houseAnim.SetBool("StartBlow", true);
    }

    public void StopBlowHouse()
    {
        houseAnim.SetBool("StartBlow", false);
    }
}
