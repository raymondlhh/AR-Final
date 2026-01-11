using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WolfAttack1 : MonoBehaviour
{
    public Animator wolfAnim;
    public GameObject oldWolf;
    public GameObject windparticleEffect;
    public GameObject smokeParticleEffect;
    public GameObject House;
    public TapTapTap gamestatus;
    public HouseBlow houseBlow;
    public GameObject BrokenObject;
    public GameObject secondPig;
    public GameObject secondPig1;

    public GameObject instructionText;
    public GameObject gameUi;
    public GameObject owhnoText;
    public GameObject pangbai;
    Coroutine windCoroutine;

    public bool activateOnce = false;
    void Start()
    {
        wolfAnim = GetComponent<Animator>();
        //windparticleEffect.SetActive(false);
        BrokenObject.SetActive(false);
    }

    private void Update()
    {
        if (gamestatus.Successful && !activateOnce)
        {
            activateOnce = true;
            instructionText.SetActive(false);
            gameUi.SetActive(false);
            wolfAnim.SetBool("Blowing", false);
            wolfAnim.SetBool("StrongAttack", true);
            StartCoroutine(StrongWindLoop());
        }
    }

    private void OnEnable()
    {
        if (oldWolf != null)
        {
            oldWolf.SetActive(false);
        }


        wolfAnim.SetBool("Blowing", true);
        houseBlow.StartBlowHouse();
        //smokeParticleEffect.SetActive(true);
        windparticleEffect.SetActive(true);

        //StartCoroutine(OldHouseDisappear());
        StartCoroutine(ShowInstruction());
    }

    IEnumerator ShowInstruction()
    {
        yield return new WaitForSeconds(0.5f);
        instructionText.SetActive(true);
        gameUi.SetActive(true);
    }


    private void EndBlowing()
    {
        owhnoText.SetActive(false);
        House.SetActive(false);
        smokeParticleEffect.SetActive(false);
        windparticleEffect.SetActive(false);
        BrokenObject.SetActive(true);
        secondPig.SetActive(true);
        secondPig1.SetActive(true); 
        pangbai.SetActive(true);

    }


    IEnumerator StrongWindLoop()
    {
        yield return new WaitForSeconds(1.2f);
        owhnoText.SetActive(true);

        yield return new WaitForSeconds(4.2f);
        wolfAnim.SetBool("StrongAttack", false);
        EndBlowing();
    }
}
