using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable] public class EnemyStats
{
    public float movementSpeed   = 1;
    public float attackFrequency = 1;
    public int   attackDamage    = 1;
}

public class EnemyController : MonoBehaviour
{
    [SerializeField] private EnemyStats stats;
    [SerializeField] private float attackDistance  = 0.5f;
    [SerializeField] private GameObject deathPrefab;
    
    private float attackTimer = 0;
    private Animator       animator;
    private TreeController tree;
    private WaveManager    waveManager;
    
    void Start()
    {
        animator    = transform.GetChild(0).gameObject.GetComponent<Animator>();
        tree        = FindObjectOfType<TreeController>();
        waveManager = FindObjectOfType<WaveManager>();
        
        // Spawn randomly around the screen but not above the tree.
        Camera  cam        = FindObjectOfType<Camera>();
        float   orthoSize  = cam.orthographicSize;
        Vector2 camExtent  = new Vector2(orthoSize, orthoSize * Screen.width / Screen.height);
        float   randAngle  = Random.Range(0f, Mathf.PI*2);
        if (Mathf.PI / 3 < randAngle && randAngle < 2 * Mathf.PI / 3) {
            randAngle += Mathf.PI;
        }
        transform.position = new Vector3(Mathf.Cos(randAngle), Mathf.Sin(randAngle), 0) * (camExtent.x + 6) + new Vector3(cam.transform.position.x, cam.transform.position.y, 0);
    }

    void Update()
    {
        Vector2 enemyToTree = tree.transform.position - transform.position;
        if (enemyToTree.magnitude > attackDistance)
        {
            transform.position += (Vector3)(enemyToTree.normalized * (stats.movementSpeed * Time.deltaTime));
        }
        else
        {
            attackTimer += Time.deltaTime;
            if (attackTimer >= stats.attackFrequency)
            {
                attackTimer = 0;
                animator.SetTrigger("Attacking");
                tree.OnDamage(stats.attackDamage);
            }
        }
    }

    public void SetStats(EnemyStats newStats)
    {
        stats = newStats;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Sword"))
        {
            GameObject deadEnemy = Instantiate(deathPrefab);
            deadEnemy.transform.position = transform.position;
            // Destroy(deadEnemy, 2);
            waveManager.DestroyEnemy(this);
        }
    }
}
