using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

// Every [spawningFrequency] seconds, a batch of [spawnBatchSize] enemies is spawned.
// In total, a waves spawns [totalSpawnBatchCount] spawn batches.
// Once all the batches have been spawned, the next waves starts after [nextWaveCooldown] seconds.
[Serializable] public class WaveData
{
    [Tooltip("The total number of enemy batches spawned in this wave.")] public int   totalSpawnBatchCount;
    [Tooltip("The number of seconds between each spawn batch.")]         public float spawningFrequency;
    [Tooltip("The number of enemies to spawn at once for every batch.")] public int   spawnBatchSize;
    [Tooltip("The time between this wave and the next.")]                public float nextWaveCooldown;
}

[Serializable] public class WaveList
{
    public List<WaveData> waves;
}

[Serializable] public class WaveDataRandom
{
    public int   spawnCountMin;
    public int   spawnCountMax;
    public float spawnFrequencyMin;
    public float spawnFrequencyMax;
}

public enum WaveType
{
    None,
    Enemies,
    Projectiles,
}

public class WaveManager : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private List<EnemyStats>      enemyStats      = new(4);
    [SerializeField] private List<ProjectileStats> projectileStats = new(3);
    [SerializeField] private List<WaveList>        enemyWaves      = new(4);
    [SerializeField] private List<WaveDataRandom>  projectileWaves = new(3);
    
    private List<EnemyController>      enemies     = new();
    private List<ProjectileController> projectiles = new();
    
    private WaveType currentWaveType = WaveType.None;
    private int      currentWaveIdx  = 0;
    private int      currentBatchIdx = 0;
    private float    waveTimer       = 0;

    void Update()
    {
        switch (currentWaveType)
        {
            case WaveType.Enemies:
                EnemyWave();
                break;
            case WaveType.Projectiles:
                ProjectileWave();
                break;
        }
    }

    public void EnemyWave()
    {
        // Get the current wave data, making sure the overall wave list isn't finished.
        if (currentWaveType != WaveType.Enemies) return;
        if (enemyWaves.Count <= 0) return;
        if (currentWaveIdx >= enemyWaves[0].waves.Count) {
            currentWaveIdx = 0;
        }
        WaveData curWaveData = enemyWaves[0].waves[currentWaveIdx];
        
        // Spawn enemies/projectiles during each wave.
        if (currentBatchIdx < curWaveData.totalSpawnBatchCount)
        {
            waveTimer += Time.deltaTime;
            if (waveTimer >= curWaveData.spawningFrequency)
            {
                for (int i = 0; i < curWaveData.spawnBatchSize; i++) 
                {
                    GameObject instantiated = Instantiate(enemyPrefab);
                    EnemyController enemy = instantiated.GetComponent<EnemyController>();
                    enemy.SetStats(enemyStats[0]);
                    enemies.Add(enemy);
                }
                // Debug.Log("Spawned " + curWaveData.spawnBatchSize + " enemies.");
                currentBatchIdx++;
                waveTimer = 0;
            }
        }
        
        // Wait for the wave cooldown to end before starting the next wave.
        else
        {
            waveTimer += Time.deltaTime;
            if (waveTimer >= curWaveData.nextWaveCooldown)
            {
                // Debug.Log("Next wave.");
                currentWaveIdx++;
                currentBatchIdx = 0;
                waveTimer = 0;
            }
        }
    }

    private void ProjectileWave()
    {
        if (currentWaveType != WaveType.Projectiles) return;
        if (projectileWaves.Count <= 0) return;
        WaveDataRandom curWaveData = projectileWaves[0];

        if (waveTimer <= 0)
        {
            waveTimer      = Random.Range(curWaveData.spawnFrequencyMin, curWaveData.spawnFrequencyMax);
            int spawnCount = Random.Range(curWaveData.spawnCountMin,     curWaveData.spawnCountMax);
            for (int i = 0; i < spawnCount; i++)
            {
                GameObject instantiated = Instantiate(projectilePrefab);
                ProjectileController projectile = instantiated.GetComponent<ProjectileController>();
                projectile.SetStats(projectileStats[0]);
                projectiles.Add(projectile);
            }
        }
        waveTimer -= Time.deltaTime;
    }

    public void StartWave(WaveType waveType)
    {
        currentWaveType = waveType;
    }

    public void DestroyEnemy(EnemyController enemy)
    {
        int idx = enemies.FindIndex(enemyController => enemyController == enemy);
        Destroy(enemy.gameObject);
        if (idx < enemies.Count)
            enemies.RemoveAt(idx);
    }

    public void DestroyProjectile(ProjectileController projectile)
    {
        int idx = projectiles.FindIndex(projectileController => projectileController == projectile);
        Destroy(projectile.gameObject);
        if (idx < projectiles.Count)
            projectiles.RemoveAt(idx);
    }

    public void EndWave()
    {
        switch (currentWaveType)
        {
        case WaveType.Enemies:
            enemies.ForEach(enemy => Destroy(enemy.gameObject));
            enemies.Clear();
            if (enemyWaves.Count > 0 && enemyStats.Count > 0)
            {
                enemyWaves.RemoveAt(0);
                enemyStats.RemoveAt(0);
            }

            break;
        case WaveType.Projectiles:
            projectiles.ForEach(projectile => Destroy(projectile.gameObject));
            projectiles.Clear();
            if (projectileWaves.Count > 0 && projectileStats.Count > 0)
            {
                projectileWaves.RemoveAt(0);
                projectileStats.RemoveAt(0);
            }

            break;
        }
        currentWaveType = WaveType.None;
    }
}
