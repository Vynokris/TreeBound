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
    
    private float attackTimer = 0;
    private TreeController tree;
    private WaveManager    waveManager;
    
    void Start()
    {
        tree        = FindObjectOfType<TreeController>();
        waveManager = FindObjectOfType<WaveManager>();
        
        // Spawn randomly around the screen.
        Camera  cam        = FindObjectOfType<Camera>();
        float   orthoSize  = cam.orthographicSize;
        Vector2 camExtent  = new Vector2(orthoSize, orthoSize * Screen.width / Screen.height);
        float   randAngle  = Random.Range(0f, Mathf.PI*2);
        if (Mathf.PI / 3 < randAngle && randAngle < 2 * Mathf.PI / 3) {
            randAngle += Mathf.PI;
        }
        transform.position = new Vector3(Mathf.Cos(randAngle), Mathf.Sin(randAngle), 0) * (camExtent.x + 4) + new Vector3(cam.transform.position.x, cam.transform.position.y, 0);
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
            waveManager.DestroyEnemy(this);
        }
    }
}
