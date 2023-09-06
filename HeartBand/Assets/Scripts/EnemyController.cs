using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [SerializeField] private float movementSpeed   = 1;
    [SerializeField] private float attackFrequency = 1;
    [SerializeField] private int   attackDamage    = 1;
    [SerializeField] private float attackDistance  = 0.5f;
    
    private float attackTimer = 0;
    private TreeController tree;
    private WaveManager    waveManager;
    
    void Start()
    {
        tree        = FindObjectOfType<TreeController>();
        waveManager = FindObjectOfType<WaveManager>();
        
        // Spawn randomly around the screen.
        Camera  cam       = FindObjectOfType<Camera>();
        float   orthoSize = cam.orthographicSize;
        Vector2 camExtent = new Vector2(orthoSize, orthoSize * Screen.width / Screen.height);
        float   randAngle = Random.Range(0f, Mathf.PI*2);
        transform.position = new Vector3(Mathf.Cos(randAngle), Mathf.Sin(randAngle), 0) * (camExtent.x + 4);
    }

    void Update()
    {
        Vector2 enemyToTree = tree.transform.position - transform.position;
        if (enemyToTree.magnitude > attackDistance)
        {
            transform.position += (Vector3)(enemyToTree.normalized * (movementSpeed * Time.deltaTime));
        }
        else
        {
            attackTimer += Time.deltaTime;
            if (attackTimer >= attackFrequency)
            {
                attackTimer = 0;
                tree.OnDamage(attackDamage);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Sword"))
        {
            waveManager.DestroyEnemy(this);
        }
    }
}
