using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PigShock : MonoBehaviour
{
    public Animator animator;
    //public GameObject RunSmoke;

    [Header("Rotation")]
    public float rotationSpeed = 5f;

    [Header("Run")]
    public float runSpeed = 3f;
    public float runDuration = 2f;

    private bool hasShocked = false;

    void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        StartCoroutine(Sequence());
    }

    IEnumerator Sequence()
    {
        yield return new WaitForSeconds(0.5f); // initial delay
        yield return StartCoroutine(RotateByAngle(90f));


        if (!hasShocked)
        {
            animator.SetBool("Talking", true);
            hasShocked = true;
            yield return new WaitForSeconds(0.8f); // shock animation duration
            animator.SetBool("Talking", false);
        }


        yield return StartCoroutine(RotateByAngle(180f));

        animator.SetBool("Run", true);
        //RunSmoke.SetActive(true);

        float timer = 0f;
        while (timer < runDuration)
        {
            transform.Translate(Vector3.forward * runSpeed * Time.deltaTime);
            timer += Time.deltaTime;
            yield return null;
        }

        animator.SetBool("Run", false);
        gameObject.SetActive(false);
    }

    IEnumerator RotateByAngle(float angle)
    {
        Quaternion startRot = transform.rotation;
        Quaternion targetRot = startRot * Quaternion.Euler(0, angle, 0);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * rotationSpeed;
            transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }
    }
}
