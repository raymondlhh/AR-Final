using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class WolfAttack : MonoBehaviour
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

    public GameObject instructionText;
    public GameObject gameUi;
    public GameObject owhnoText;
    public GameObject pangbai;
    Coroutine windCoroutine;

    public AudioSource blowsound;
    public AudioSource breaksound;
    public AudioSource runsound;

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
        blowsound.Play();
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
        blowsound.Stop();
        owhnoText.SetActive(false);
        House.SetActive(false); 
        smokeParticleEffect.SetActive(false);
        windparticleEffect.SetActive(false);
        BrokenObject.SetActive(true);
        
        secondPig.SetActive(true);
        StartCoroutine(Playrunsound());
        pangbai.SetActive(true);
        
    }

    IEnumerator Playrunsound()
    {
        yield return new WaitForSeconds(1.0f);
        runsound.Play();
    }


    IEnumerator StrongWindLoop()
    {
        yield return new WaitForSeconds(1.2f);  
        owhnoText.SetActive(true);
        
        yield return new WaitForSeconds(2.2f);
        breaksound.Play();
        wolfAnim.SetBool("StrongAttack", false);

        EndBlowing();
    }
}
