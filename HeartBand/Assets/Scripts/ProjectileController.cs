using System;
using System.Collections;
using System.Collections.Generic;
using System.Timers;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable] public class ProjectileStats
{
    public float activationTime   = 3;
    public float accelerationTime = 1;
    public float maxSpeed         = 3;
    public int   damage           = 1;
}

public class ProjectileController : MonoBehaviour
{
    [SerializeField] private ProjectileStats stats;
    [SerializeField] private float attackDistance   = 0.5f;
    [SerializeField] private AnimationCurve accelerationCurve;
    
    private TreeController tree;
    private WaveManager    waveManager;
    private float activationTimer   = 0;
    private float accelerationTimer = 0;
    
    void Start()
    {
        tree        = FindObjectOfType<TreeController>();
        waveManager = FindObjectOfType<WaveManager>();
        
        // Spawn randomly around the screen.
        Camera  cam       = FindObjectOfType<Camera>();
        float   orthoSize = cam.orthographicSize;
        Vector2 camExtent = new Vector2(orthoSize, orthoSize * Screen.width / Screen.height);
        float   randAngle = Random.Range(0f, Mathf.PI*2);
        transform.position = new Vector3(Mathf.Cos(randAngle) * camExtent.x - 2, Mathf.Sin(randAngle) * camExtent.y - 2, 0) + new Vector3(cam.transform.position.x, cam.transform.position.y, 0);
        
        // Rotate towards the tree.
        Vector2 treeDir = ((Vector2)(tree.transform.position - transform.position)).normalized;
        transform.localEulerAngles = new Vector3(0, 0, Vector2.SignedAngle(Vector2.down, treeDir));
    }

    void Update()
    {
        float movementSpeed = 0;
        if (activationTimer < stats.activationTime) {
            activationTimer += Time.deltaTime;
        }
        else if (accelerationTimer < stats.accelerationTime) {
            accelerationTimer += Time.deltaTime;
            movementSpeed = accelerationCurve.Evaluate(accelerationTimer) * stats.maxSpeed;
        }
        else {
            movementSpeed = stats.maxSpeed;
        }

        if (movementSpeed > 0)
        {
            // Explode the projectile if close enough to tree, make it move towards the tree in other cases.
            Vector2 projectileToTree = tree.transform.position - transform.position;
            if (projectileToTree.magnitude < attackDistance)
            {
                tree.OnDamage(stats.damage);
                waveManager.DestroyProjectile(this);
            }
            transform.position += (Vector3)(projectileToTree.normalized * (movementSpeed * Time.deltaTime));
        }
    }
    
    public void SetStats(ProjectileStats newStats)
    {
        stats = newStats;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Shield"))
        {
            waveManager.DestroyProjectile(this);
        }
    }
}
