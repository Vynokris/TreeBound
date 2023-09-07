using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyDeadController : MonoBehaviour
{
    [SerializeField] private Material dissolveMat;
    
    private float timer = 1;
    private Animator animator;
    
    void Start()
    {
        animator = GetComponent<Animator>();
        dissolveMat.SetFloat("_Fade", timer);
    }

    void Update()
    {
        if (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1) return;
        timer -= Time.deltaTime;
        dissolveMat.SetFloat("_Fade", timer);
        if (timer < 0)
            Destroy(gameObject);
    }
}
