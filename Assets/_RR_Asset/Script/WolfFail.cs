using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WolfFail : MonoBehaviour
{
    public Animator wolfAnim;
    public GameObject oldWolf;
    public GameObject wolfDialogue;
    public GameObject windparticleEffect;
    public GameObject House;
    public TapTapTap gamestatus;
    public HouseBlow houseBlow;

    public GameObject instructionText;
    public GameObject gameUi;
    public GameObject owhnoText;
    Coroutine windCoroutine;
    public AudioSource blowsound;


    public bool activateOnce = false;
    void Start()
    {
        wolfAnim = GetComponent<Animator>();
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
        blowsound.Play();
        houseBlow.StartBlowHouse();
        windparticleEffect.SetActive(true);
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
        houseBlow.StopBlowHouse();  
        wolfDialogue.SetActive(true);
        windparticleEffect.SetActive(false);
        
    }


    IEnumerator StrongWindLoop()
    {
        yield return new WaitForSeconds(1.2f);
        owhnoText.SetActive(true);

        yield return new WaitForSeconds(6.2f);
        wolfAnim.SetBool("StrongAttack", false);
        EndBlowing();
    }
}
