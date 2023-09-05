using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float movementSpeed = 2;
    [SerializeField] private int   maxHealth     = 3;
    [SerializeField] private float respawnTime   = 5;
    [SerializeField] private float shieldDist    = 1.2f;

    private int     health       = -1;
    private float   respawnTimer = -1;
    private Vector2 moveDir;
    private Vector2 lookDir;
    
    private new SpriteRenderer renderer;
    private GameObject     shield;
    private TreeController tree;
    private PlantingSlate  slate = null;
    
    void Start()
    {
        renderer = GetComponent<SpriteRenderer>();
        shield   = transform.GetChild(0).gameObject;
        tree     = FindObjectOfType<TreeController>();
        health   = maxHealth;
    }

    void Update()
    {
        if (health > 0)
        {
            // Move if not dead.
            Vector2 movement = moveDir * (movementSpeed * Time.deltaTime);
            transform.position += new Vector3(movement.x, movement.y, 0);

            if (tree.GetState() == TreeState.Moving)
            {
                // Move shield in front of the player.
                shield.transform.localPosition = lookDir * shieldDist;
                shield.transform.localEulerAngles = new Vector3(0, 0, Vector2.SignedAngle(Vector2.down, lookDir));
            }
        }
        else
        {
            // If dead this frame, start timer.
            if (respawnTimer <= 0)
            {
                respawnTimer = respawnTime;
                renderer.enabled = false;
            }
            else
            {
                // Update timer and respawn once done.
                respawnTimer -= Time.deltaTime;
                if (respawnTimer <= 0)
                {
                    respawnTimer = -1;
                    health = maxHealth;
                    renderer.enabled = true;
                    transform.position = tree.transform.position;
                }
            }
        }
    }

    public Color GetColor() { return renderer.color; }

    public void UpdateState(TreeState treeState)
    {
        switch (treeState)
        {
        case TreeState.Moving:
            shield.gameObject.SetActive(true);
            break;
        case TreeState.Planted:
        case TreeState.Waiting:
            shield.gameObject.SetActive(false);
            break;
        }
    }
    
    public void OnMove(InputValue input)
    {
        moveDir = input.Get<Vector2>().normalized;
    }
    
    public void OnLook(InputValue input)
    {
        Vector2 inputVal = input.Get<Vector2>().normalized;
        if (inputVal.sqrMagnitude <= 1e-3f) return;
        lookDir = inputVal;
    }
    
    public void OnAttack(InputValue input)
    {
        bool temp = input.Get<float>() != 0;
    }

    public void OnInteract(InputValue input)
    {
        bool interacting = input.Get<float>() != 0;
        if (!slate) return;
        if (interacting) {
            slate.Activate(gameObject);
        }
        else if (tree.GetState() != TreeState.Planted) {
            slate.Deactivate();
        }
    }

    public void OnDamage(int value) { health -= value; }
    public void OnHeal  (int value) { health += value; }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.gameObject.CompareTag("InteractPoint")) return;
        slate = other.gameObject.GetComponent<PlantingSlate>();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.gameObject.CompareTag("InteractPoint")) return;
        if (slate && slate.gameObject == other.gameObject)
            slate = null;
    }
}
