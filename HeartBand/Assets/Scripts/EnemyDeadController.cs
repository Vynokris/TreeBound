using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyDeadController : MonoBehaviour
{
    [SerializeField] private Material dissolveMat;
    
    private float timer = 1;
    private Animator animator;
    private AudioSource audioSource;
    
    void Start()
    {
        animator = transform.GetChild(0).gameObject.GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        dissolveMat = Instantiate(dissolveMat);
        dissolveMat.SetFloat("_Fade", timer);
        DeepCopyMaterial(transform);
        
        audioSource.Play();
    }

    void Update()
    {
        if (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1) return;
        timer -= Time.deltaTime;
        dissolveMat.SetFloat("_Fade", timer);
        if (timer < 0)
            Destroy(transform.parent.gameObject);
    }

    private void DeepCopyMaterial(Transform objTransform)
    {
        // Set material to dissolveMat copy.
        SpriteRenderer spriteRenderer = objTransform.gameObject.GetComponent<SpriteRenderer>();
        if (spriteRenderer) {
            spriteRenderer.material = dissolveMat;
        }
        
        // Deep copy children material.
        int children = objTransform.childCount;
        for (int i = 0; i < children; i++) {
            DeepCopyMaterial(objTransform.GetChild(i));
        }
    }
}
