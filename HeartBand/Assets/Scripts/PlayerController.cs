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
    [SerializeField] private float equipmentLerp  = 3;

    private Color   color;
    private int     health       = -1;
    private float   attackTimer  = 0;
    private float   respawnTimer = 0;
    private Vector2 moveDir;
    private Vector2 lookDir;
    
    private new Rigidbody2D   rigidbody;
    private GameObject        sprite;
    private GameObject        shield;
    private GameObject        sword;
    private CapsuleCollider2D swordCollider;
    private Animator          swordAnimator;
    private List<GameObject>  swordTrails;
    private TreeController    tree;
    private PlantingSlate     slate = null;
    
    void Start()
    {
        rigidbody = GetComponent<Rigidbody2D>();
        sprite    = transform.GetChild(0).gameObject;
        tree      = FindObjectOfType<TreeController>();
        health    = maxHealth;
        
        shield.SetActive(false);
        sword .SetActive(false);
    }

    private void FixedUpdate()
    {
        // Move if not dead.
        if (health <= 0) return;
        Vector2 movement = moveDir * movementSpeed;
        rigidbody.velocity = new Vector2(movement.x, movement.y);
    }

    void Update()
    {
        sword.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
        if (health > 0)
        {
            // Smoothly move the shield/sword where the player is looking.
            if (tree.GetState() != TreeState.Waiting)
            {
                GameObject activeEquipment = null;
                switch (tree.GetState())
                {
                    case TreeState.Moving:  activeEquipment = shield; break;
                    case TreeState.Planted: activeEquipment = sword;  break;
                }
                if (activeEquipment)
                {
                    // Rotate the equipment around the player in the direction where they are looking.
                    Vector3 targetPos = lookDir * equipmentDist;
                    float   targetRot = Vector2.SignedAngle(Vector2.right, lookDir);
                    activeEquipment.transform.localPosition    = Vector3.Slerp(activeEquipment.transform.localPosition, targetPos, Time.deltaTime * equipmentLerp);
                    activeEquipment.transform.localEulerAngles = new Vector3(0, 0, Mathf.LerpAngle(activeEquipment.transform.localEulerAngles.z, targetRot, Time.deltaTime * equipmentLerp));
                    
                    // Flip the equipment when it is on the left side of the player.
                    Vector3 curScale = activeEquipment.transform.localScale;
                    if (Vector2.Angle(Vector2.right, lookDir) > 90) {
                        activeEquipment.transform.localScale = new Vector3(curScale.x, -Mathf.Abs(curScale.y), curScale.z);
                    }
                    else {
                        activeEquipment.transform.localScale = new Vector3(curScale.x, Mathf.Abs(curScale.y), curScale.z);
                    }
                }
            }
        }
        else
        {
            // If dead this frame, start timer.
            if (respawnTimer <= 0)
            {
                respawnTimer = respawnTime;
                // TODO: Hide the player.
            }
            else
            {
                // Update timer and respawn once done.
                respawnTimer -= Time.deltaTime;
                if (respawnTimer <= 0)
                {
                    respawnTimer = 0;
                    health = maxHealth;
                    // TODO: Show the player.
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
                swordAnimator.Rebind();
                swordAnimator.Update(0f);
                swordAnimator.enabled = false;
                swordCollider.enabled = false;
                swordTrails.ForEach(trail => trail.SetActive(false));
                attackTimer = 0;
            }
        }
    }

    public void SetShield(GameObject newShield) { shield = newShield; shield.SetActive(false); }
    public void SetSword (GameObject newSword)
    {
        sword = newSword; 
        GameObject swordChild = sword.transform.GetChild(0).gameObject;
        swordCollider = swordChild.GetComponent<CapsuleCollider2D>(); 
        swordCollider.enabled = false;
        swordAnimator = swordChild.GetComponent<Animator>();
        swordAnimator.Rebind();
        swordAnimator.Update(0f);
        swordAnimator.enabled = false;
        swordTrails = new List<GameObject>(swordChild.transform.childCount);
        for (int i = 0; i < swordChild.transform.childCount; i++) {
            swordTrails.Add(swordChild.transform.GetChild(i).gameObject);
        }
        swordTrails.ForEach(trail => trail.SetActive(false));
        sword.SetActive(false);
    }
    public void  SetColor(Color newColor) { color = newColor; }
    public Color GetColor() { return color; }

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
        if (!sprite || moveDir.sqrMagnitude < 1e-3) return;
        
        // Face forwards.
        Vector3 curScale = sprite.transform.localScale;
        if (Vector2.Angle(Vector2.right, moveDir) > 90) {
            sprite.transform.localScale = new Vector3(-Mathf.Abs(curScale.x), curScale.y, curScale.z);
        }
        else {
            sprite.transform.localScale = new Vector3(Mathf.Abs(curScale.x), curScale.y, curScale.z);
        }
    }
    
    public void OnLook(InputValue input)
    {
        Vector2 inputVal = input.Get<Vector2>().normalized;
        if (inputVal.sqrMagnitude <= 1e-3f) return;
        lookDir = inputVal;
    }
    
    public void OnAttack(InputValue input)
    {
        if (!sword.activeSelf || attackTimer > 0) return;
        swordCollider.enabled = true;
        swordAnimator.enabled = true;
        swordTrails.ForEach(trail => trail.SetActive(true));
        attackTimer = swordAnimator.GetCurrentAnimatorStateInfo(0).length;
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
