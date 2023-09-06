using System;
using System.Collections;
using System.Collections.Generic;
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
    
    private new SpriteRenderer renderer;
    private TreeState state    = TreeState.Waiting;
    private int   growingStage = 0;
    private float health       = -1;
    private float evolveTimer  = -1;
    private PlantingPoint plantingPoint = null;
    private WaveManager   waveManager;
    
    void Start()
    {
        renderer    = GetComponent<SpriteRenderer>();
        waveManager = FindObjectOfType<WaveManager>();
        health      = maxHealth;
    }

    void Update()
    {
        switch (state)
        {
        case TreeState.Moving:
            CheckHealth();
            MovingBehavior();
            break;
        case TreeState.Planted:
            PlantedBehavior();
            break;
        case TreeState.Waiting:
            CheckHealth();
            WaitingBehavior();
            break;
        }
    }

    private void CheckHealth(bool decay = true)
    {
        // Loose health from decay damage and check for game over.
        if (decay && growingStage > 0) health -= decaySpeed * Time.deltaTime;
        if (health < 0) {
            Debug.Log("Tree destroyed!");
            Destroy(gameObject);
        }
    }

    private void MovingBehavior()
    {
        // Plant the tree when all players interact with the planting slates.
        if (plantingPoint && plantingPoint.IsActivated())
        {
            state = TreeState.Planted;
            players.ForEach(player => player.UpdateState(state));
            waveManager.EndWave();
            waveManager.StartWave(WaveType.Enemies);
            evolveTimer = evolveTime;
            renderer.color = Color.magenta;
        }

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
        transform.position += (Vector3)(pullDir * (pullSpeed * Time.deltaTime));
    }

    private void PlantedBehavior()
    {
        // Update timer and check if evolution is done.
        evolveTimer -= Time.deltaTime;
        if (evolveTimer <= 0)
        {
            growingStage++;
            state = TreeState.Waiting;
            players.ForEach(player => player.UpdateState(state));
            plantingPoint.DeactivateSlates();
            waveManager.EndWave();
            evolveTimer = -1;
            health = maxHealth;
            renderer.color = Color.red;
        }
    }

    private void WaitingBehavior()
    {
        // Start moving once all the players interact with the planting slates.
        if (plantingPoint && plantingPoint.IsActivated()) {
            state = TreeState.Moving;
            players.ForEach(player => player.UpdateState(state));
            waveManager.StartWave(WaveType.Projectiles);
            plantingPoint.SetUsed();
            plantingPoint = null;
            renderer.color = Color.green;
        }
    }

    public TreeState GetState() { return state; }
    public void OnDamage(float value) { health -= value; }
    public void OnHeal  (float value) { health += value; }

    void OnPlayerJoined(PlayerInput playerInput)
    {
        players.Add(playerInput.gameObject.GetComponent<PlayerController>());
        
        List<PlantingPoint> plantingPoints = FindObjectsOfType<PlantingPoint>().ToList();
        plantingPoints.ForEach(point => point.UpdateSlateCount(players.Count));

        // TODO: Finalize this.
        switch (players.Count)
        {
        case 1:
            players[0].GetComponent<SpriteRenderer>().color = Color.red;
            break;
        case 2:
            players[1].GetComponent<SpriteRenderer>().color = Color.blue;
            break;
        case 3:
            players[2].GetComponent<SpriteRenderer>().color = Color.green;
            break;
        case 4:
            players[3].GetComponent<SpriteRenderer>().color = Color.magenta;
            break;
        }
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
