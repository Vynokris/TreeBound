using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float movementSpeed  = 2;
    [SerializeField] private int   maxHealth      = 3;
    [SerializeField] private float attackDuration = 0.5f;
    [SerializeField] private float respawnTime    = 5;
    [SerializeField] private float equipmentDist  = 1.2f;
    [SerializeField] private AnimationCurve swordMovementCurve;

    private int     health       = -1;
    private float   attackTimer  = 0;
    private float   respawnTimer = 0;
    private Vector2 moveDir;
    private Vector2 lookDir;
    
    private new SpriteRenderer renderer;
    private GameObject         shield;
    private GameObject         sword;
    private PolygonCollider2D  swordCollider;
    private TreeController     tree;
    private PlantingSlate      slate = null;
    
    void Start()
    {
        renderer      = GetComponent<SpriteRenderer>();
        shield        = transform.GetChild(0).gameObject;
        sword         = transform.GetChild(1).gameObject;
        swordCollider = sword.GetComponent<PolygonCollider2D>();
        tree          = FindObjectOfType<TreeController>();
        health        = maxHealth;
        
        shield.SetActive(false);
        sword .SetActive(false);
        swordCollider.enabled = false;
    }

    void Update()
    {
        if (health > 0)
        {
            // Move if not dead.
            Vector2 movement = moveDir * (movementSpeed * Time.deltaTime);
            transform.position += new Vector3(movement.x, movement.y, 0);

            Vector3 targetPos;
            float   targetRot;
            switch (tree.GetState())
            {
            case TreeState.Moving:
                // Move shield in front of the player.
                targetPos = lookDir * equipmentDist;
                targetRot = Vector2.SignedAngle(Vector2.down, lookDir);
                shield.transform.localPosition    = Vector3.Slerp(shield.transform.localPosition, targetPos, Time.deltaTime*3);
                shield.transform.localEulerAngles = new Vector3(0, 0, Mathf.LerpAngle(shield.transform.localEulerAngles.z, targetRot, Time.deltaTime*3));
                break;
            case TreeState.Planted:
                // Move sword in front of the player.
                targetPos = lookDir * (equipmentDist + swordMovementCurve.Evaluate(attackTimer / attackDuration));
                targetRot = Vector2.SignedAngle(Vector2.up, lookDir);
                sword.transform.localPosition    = Vector3.Slerp(sword.transform.localPosition, targetPos, Time.deltaTime*3);
                sword.transform.localEulerAngles = new Vector3(0, 0, Mathf.LerpAngle(sword.transform.localEulerAngles.z, targetRot, Time.deltaTime*3));
                break;
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
                    respawnTimer = 0;
                    health = maxHealth;
                    renderer.enabled = true;
                    transform.position = tree.transform.position;
                }
            }
        }
        
        // Update the attack timer.
        if (attackTimer > 0)
        {
            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0)
            {
                swordCollider.enabled = false;
                attackTimer = 0;
            }
        }
    }

    public Color GetColor() { return renderer.color; }

    public void UpdateState(TreeState treeState)
    {
        switch (treeState)
        {
        case TreeState.Moving:
            shield.SetActive(true);
            sword .SetActive(false);
            break;
        case TreeState.Planted:
            shield.SetActive(false);
            sword .SetActive(true);
            break;
        case TreeState.Waiting:
            shield.SetActive(false);
            sword .SetActive(false);
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
        if (!sword.activeSelf) return;
        attackTimer = attackDuration;
        swordCollider.enabled = true;
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
