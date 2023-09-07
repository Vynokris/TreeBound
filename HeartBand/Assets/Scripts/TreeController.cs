using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public enum TreeState
{
    Moving,
    Planted,
    Waiting,
}

public class TreeController : MonoBehaviour
{
    private readonly List<PlayerController> players = new();
    
    [SerializeField] private float maxHealth     = 10;
    [SerializeField] private float decaySpeed    = 0.1f;
    [SerializeField] private float pullSpeed     = 0.1f;
    [SerializeField] private float maxPlayerDist = 4;
    [SerializeField] private float evolveTime    = 30;
    [SerializeField] private List<Color>      playerColors;
    [SerializeField] private List<GameObject> swordPrefabs;
    [SerializeField] private List<GameObject> shieldPrefabs;
    
    private new SpriteRenderer renderer;
    private new Rigidbody2D    rigidbody;
    [SerializeField] private TreeState state    = TreeState.Waiting;
    private int   growingStage = 0;
    private float health       = -1;
    private float evolveTimer  = -1;
    private PlantingPoint plantingPoint = null;
    private WaveManager   waveManager;
    
    void Start()
    {
        renderer    = GetComponent<SpriteRenderer>();
        rigidbody   = GetComponent<Rigidbody2D>();
        waveManager = FindObjectOfType<WaveManager>();
        health      = maxHealth;
    }

    void Update()
    {
        switch (state)
        {
        case TreeState.Moving:
            // Plant the tree when all players interact with the planting slates.
            if (plantingPoint && plantingPoint.IsActivated()) {
                SetState(TreeState.Planted);
            }
            CheckHealth();
            break;
        
        case TreeState.Planted:
            // Update timer and check if evolution is done.
            evolveTimer -= Time.deltaTime;
            if (evolveTimer <= 0) {
                SetState(TreeState.Waiting);
            }
            CheckHealth(false);
            break;
        
        case TreeState.Waiting:
            // Start moving once all the players interact with the planting slates.
            if (plantingPoint && plantingPoint.IsActivated()) {
                SetState(TreeState.Moving);
            }
            CheckHealth(growingStage > 0);
            break;
        }
    }

    private void FixedUpdate()
    {
        if (state != TreeState.Moving) return;
        
        // Let the players pull the tree.
        Vector2 pullDir = new();
        foreach (PlayerController player in players)
        {
            Vector2 playerToTree = player.transform.position - transform.position;
            if (playerToTree.magnitude > maxPlayerDist)
            {
                playerToTree = playerToTree.normalized * maxPlayerDist;
                player.transform.position = transform.position + (Vector3)playerToTree;
            }
            pullDir += playerToTree;
        }
        // Move towards the planting point.
        if (plantingPoint)
        {
            Vector2 treeToPoint = plantingPoint.transform.position - transform.position;
            pullDir += treeToPoint * 2;
        }
        rigidbody.velocity = pullDir * pullSpeed;
    }

    private void CheckHealth(bool decay = true)
    {
        // Loose health from decay damage and check for game over.
        if (decay) health -= decaySpeed * Time.deltaTime;
        if (health < 0) {
            Debug.Log("Tree destroyed!");
            this.enabled = false;
        }
    }

    private void SetState(TreeState newState)
    {
        state = newState;
        players.ForEach(player => player.UpdateState(state));
        switch (state)
        {
        case TreeState.Moving:
            waveManager.StartWave(WaveType.Projectiles);
            plantingPoint.SetUsed(growingStage > 0);
            plantingPoint = null;
            renderer.color = Color.green;
            break;
        case TreeState.Planted:
            rigidbody.velocity = Vector2.zero;
            waveManager.EndWave();
            waveManager.StartWave(WaveType.Enemies);
            evolveTimer = evolveTime;
            renderer.color = Color.magenta;
            break;
        case TreeState.Waiting:
            rigidbody.velocity = Vector2.zero;
            plantingPoint.DeactivateSlates();
            waveManager.EndWave();
            growingStage++;
            evolveTimer = -1;
            health = maxHealth;
            renderer.color = Color.red;
            break;
        }
    }

    public TreeState GetState() { return state; }
    public void OnDamage(float value) { health -= value; }
    public void OnHeal  (float value) { health += value; }

    void OnPlayerJoined(PlayerInput playerInput)
    {
        // Spawn player with the right color, shield and sword.
        players.Add(playerInput.gameObject.GetComponent<PlayerController>());
        int              playerIdx = players.Count-1;
        PlayerController newPlayer = players.Last();
        newPlayer.SetColor(playerColors[playerIdx]);
        newPlayer.SetShield(Instantiate(shieldPrefabs[playerIdx], newPlayer.transform));
        newPlayer.SetSword (Instantiate(swordPrefabs [playerIdx], newPlayer.transform));
        
        List<PlantingPoint> plantingPoints = FindObjectsOfType<PlantingPoint>().ToList();
        plantingPoints.ForEach(point => point.UpdateSlateCount(players.Count));
    }

    void OnPlayerLeft(PlayerInput playerInput)
    {
        players.Remove(playerInput.gameObject.GetComponent<PlayerController>());
        
        List<PlantingPoint> plantingPoints = FindObjectsOfType<PlantingPoint>().ToList();
        plantingPoints.ForEach(point => point.UpdateSlateCount(players.Count));
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.gameObject.CompareTag("Checkpoint")) return;
        plantingPoint = other.gameObject.GetComponent<PlantingPoint>();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!plantingPoint || !other.gameObject.CompareTag("Checkpoint")) return;
        if (plantingPoint.gameObject == other.gameObject)
            plantingPoint = null;
    }
}
