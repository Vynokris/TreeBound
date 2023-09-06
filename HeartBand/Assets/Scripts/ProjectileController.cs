using System;
using System.Collections;
using System.Collections.Generic;
using System.Timers;
using UnityEngine;
using Random = UnityEngine.Random;

public class ProjectileController : MonoBehaviour
{
    [SerializeField] private float activationTime   = 3;
    [SerializeField] private float maxSpeed         = 3;
    [SerializeField] private int   damage           = 1;
    [SerializeField] private float attackDistance   = 0.5f;
    [SerializeField] private float accelerationTime = 1;
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
        transform.position = new Vector3(Mathf.Cos(randAngle) * camExtent.x - 2, Mathf.Sin(randAngle) * camExtent.y - 2, 0);
    }

    void Update()
    {
        float movementSpeed = 0;
        if (activationTimer < activationTime) {
            activationTimer += Time.deltaTime;
        }
        else if (accelerationTimer < accelerationTime) {
            accelerationTimer += Time.deltaTime;
            movementSpeed = accelerationCurve.Evaluate(accelerationTimer) * maxSpeed;
        }
        else {
            movementSpeed = maxSpeed;
        }

        if (movementSpeed > 0)
        {
            Vector2 projectileToTree = tree.transform.position - transform.position;
            if (projectileToTree.magnitude < attackDistance)
            {
                tree.OnDamage(damage);
                waveManager.DestroyProjectile(this);
            }
            transform.position += (Vector3)(projectileToTree.normalized * (movementSpeed * Time.deltaTime));
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Shield"))
        {
            waveManager.DestroyProjectile(this);
        }
    }
}
