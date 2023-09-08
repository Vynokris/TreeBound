using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum TreeState
{
    Moving,
    Planted,
    Waiting,
}

[Serializable] public struct NamedAudioClip
{
    public string name;
    public AudioClip sound;
}

public class TreeController : MonoBehaviour
{
    private readonly List<PlayerController> players = new();
    
    [SerializeField] private float maxHealth      = 10;
    [SerializeField] private float decaySpeed     = 0.1f;
    [SerializeField] private float pullSpeed      = 0.1f;
    [SerializeField] private float maxPlayerDist  = 4;
    [SerializeField] private float healedAreaSize = 5;
    [SerializeField] private AnimationCurve   damageFeedbackCurve;
    [SerializeField] private Slider           healthBar;
    [SerializeField] private Material         defaultMaterial;
    [SerializeField] private Material         transitionMaterial;
    [SerializeField] private List<float>      evolveDurations;
    [SerializeField] private List<Sprite>     stageSprites;
    [SerializeField] private List<Color>      playerColors;
    [SerializeField] private List<GameObject> playerSpritePrefabs;
    [SerializeField] private List<GameObject> swordPrefabs;
    [SerializeField] private List<GameObject> shieldPrefabs;
    [SerializeField] private List<NamedAudioClip> sounds;
    
    private new SpriteRenderer renderer;
    private     SpriteRenderer transitionRenderer;
    private new Rigidbody2D    rigidbody;
    private     SpriteMask     spriteMask;
    private     ParticleSystem plantingParticles;
    private     PlantingPoint  plantingPoint = null;
    private     WaveManager    waveManager;
    private     AudioSource    audioSource;
    private Dictionary<string, AudioClip> soundsDict;
    private TreeState state           = TreeState.Waiting;
    private int   growingStage        = 0;
    private float health              = -1;
    private float damageFeedbackTimer = -1;
    private float evolveTimer         = -1;
    private float transitionTimer     = -1;

    void Start()
    {
        renderer           = GetComponent<SpriteRenderer>();
        transitionRenderer = transform.GetChild(1).gameObject.GetComponent<SpriteRenderer>();
        rigidbody          = GetComponent<Rigidbody2D>();
        spriteMask         = transform.GetChild(0).gameObject.GetComponent<SpriteMask>();
        plantingParticles  = transform.GetChild(2).GetChild(0).gameObject.GetComponent<ParticleSystem>();
        waveManager        = FindObjectOfType<WaveManager>();
        audioSource        = GetComponent<AudioSource>();
        health             = maxHealth;
        
        transitionRenderer.enabled = false;
        plantingParticles.Stop();
        
        soundsDict = new(sounds.Count);
        foreach (NamedAudioClip clip in sounds) {
            soundsDict.Add(clip.name, clip.sound);
        }
    }

    void Update()
    {
        UpdateHealingMask();
        UpdateTransition();
        UpdateDamageFeedback();
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
            CheckHealth(growingStage > 0 && growingStage < 4);
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
        if (plantingPoint && !plantingPoint.WasUsed())
        {
            Vector2 treeToPoint = plantingPoint.transform.position - transform.position;
            pullDir = treeToPoint * 2;
        }
        rigidbody.velocity = pullDir * pullSpeed;
    }

    private void UpdateHealingMask()
    {
        float targetMaskSize = growingStage * healedAreaSize; if (growingStage > 0) targetMaskSize += 5;
        Vector3 curScale = spriteMask.transform.localScale;
        spriteMask.transform.localScale = Vector3.Lerp(curScale, new Vector3(targetMaskSize, targetMaskSize, targetMaskSize), 0.5f * Time.deltaTime);
    }

    private void UpdateTransition()
    {
        if (transitionTimer <= 0) return;
        transitionTimer -= Time.deltaTime;
        renderer          .material.SetFloat("_Fade", 1-transitionTimer);
        transitionRenderer.material.SetFloat("_Fade", transitionTimer);
        if (transitionTimer <= 0) {
            renderer.material = defaultMaterial;
            transitionRenderer.enabled = false;
        }
    }

    private void UpdateDamageFeedback()
    {
        if (damageFeedbackTimer <= 0) return;
        damageFeedbackTimer -= Time.deltaTime;
        renderer.material.SetFloat("_Fade", damageFeedbackCurve.Evaluate(1-damageFeedbackTimer));
        if (damageFeedbackTimer <= 0) {
            renderer.material = defaultMaterial;
        }
    }

    private void CheckHealth(bool decay = true)
    {
        // Loose health from decay damage and check for game over.
        if (decay) {
            health -= decaySpeed * Time.deltaTime;
            if (healthBar) healthBar.value = health / maxHealth;
        }
        if (health < 0) {
            Debug.Log("Tree destroyed!");
            enabled = false;
            SceneManager.LoadScene("GameOver");
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
            audioSource.Stop();
            audioSource.clip = soundsDict["Tree_Rip"];
            audioSource.Play();
            break;
        case TreeState.Planted:
            rigidbody.velocity = Vector2.zero;
            waveManager.EndWave();
            waveManager.StartWave(WaveType.Enemies);
            plantingParticles.Play();
            evolveTimer = evolveDurations[growingStage];
            audioSource.Stop();
            audioSource.clip = soundsDict["Tree_Drop"];
            audioSource.Play();
            break;
        case TreeState.Waiting:
            rigidbody.velocity = Vector2.zero;
            plantingPoint.DeactivateSlates();
            waveManager.EndWave();
            growingStage++;
            renderer          .sprite  = stageSprites[growingStage];
            transitionRenderer.sprite  = stageSprites[growingStage-1];
            renderer.material          = Instantiate(transitionMaterial);
            renderer.material.SetFloat("_Fade", 0);
            transitionRenderer.enabled = true;
            evolveTimer = -1;
            transitionTimer = 1;
            health = maxHealth;
            audioSource.Stop();
            audioSource.clip = soundsDict["Tree_Grow"];
            audioSource.Play();
            break;
        }
    }

    public TreeState GetState() { return state; }
    public int       GetGrowingStage() { return growingStage; }

    public void OnHeal  (float value) { health += value; if (healthBar) healthBar.value = health / maxHealth; }
    public void OnDamage(float value)
    {
        health -= value;
        if (healthBar) healthBar.value = health / maxHealth; 
        damageFeedbackTimer = 1;
        renderer.material   = transitionMaterial;
        renderer.material.SetFloat("_Fade", 0);
        if (!audioSource.isPlaying || audioSource.clip == soundsDict["Tree_Hit"])
        {
            audioSource.Stop();
            audioSource.clip = soundsDict["Tree_Hit"];
            audioSource.Play();
        }
    }

    void OnPlayerJoined(PlayerInput playerInput)
    {
        // Spawn player with the right color, shield and sword.
        players.Add(playerInput.gameObject.GetComponent<PlayerController>());
        int              playerIdx = players.Count-1;
        PlayerController newPlayer = players.Last();
        newPlayer.transform.position = transform.position;
        newPlayer.SetPlayerIndex(playerIdx);
        newPlayer.SetColor(playerColors[playerIdx]);
        newPlayer.SetSprite(Instantiate(playerSpritePrefabs[playerIdx], newPlayer.transform.GetChild(0)));
        newPlayer.SetShield(Instantiate(shieldPrefabs      [playerIdx], newPlayer.transform));
        newPlayer.SetSword (Instantiate(swordPrefabs       [playerIdx], newPlayer.transform));
        
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
