using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Every [spawningFrequency] seconds, a batch of [spawnBatchSize] enemies is spawned.
// In total, a waves spawns [totalSpawnBatchCount] spawn batches.
// Once all the batches have been spawned, the next waves starts after [nextWaveCooldown] seconds.
// The total duration of a wave is: [spawningFrequency * totalSpawnBatchCount + nextWaveCooldown] seconds.
// The total number of enemies spawned in a wave is: [totalSpawnBatchCount * spawnBatchSize] enemies.
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
    [SerializeField] private List<WaveList> enemyWaves      = new(4);
    [SerializeField] private List<WaveList> projectileWaves = new(3);
    
    private List<EnemyController>      enemies     = new();
    private List<ProjectileController> projectiles = new();
    
    private WaveType currentWaveType = WaveType.None;
    private int      currentWaveIdx  = 0;
    private int      currentBatchIdx = 0;
    private float    waveTimer       = 0;

    void Update()
    {
        // Get the current wave data, making sure the overall wave list isn't finished.
        if (currentWaveType == WaveType.None) return;
        List<WaveList> curWaveList = currentWaveType == WaveType.Enemies ? enemyWaves : projectileWaves;
        if (curWaveList.Count <= 0) return;
        if (currentWaveIdx >= curWaveList[0].waves.Count) {
            currentWaveIdx = 0;
        }
        WaveData curWaveData = curWaveList[0].waves[currentWaveIdx];
        
        // Spawn enemies/projectiles during each wave.
        if (currentBatchIdx < curWaveData.totalSpawnBatchCount)
        {
            waveTimer += Time.deltaTime;
            if (waveTimer >= curWaveData.spawningFrequency)
            {
                for (int i = 0; i < curWaveData.spawnBatchSize; i++) 
                {
                    GameObject instantiated = Instantiate(currentWaveType == WaveType.Enemies ? enemyPrefab : projectilePrefab);
                    if (currentWaveType == WaveType.Enemies) {
                        enemies.Add(instantiated.GetComponent<EnemyController>());
                    }
                    else {
                        projectiles.Add(instantiated.GetComponent<ProjectileController>());
                    }
                }
                Debug.Log("Spawned " + curWaveData.spawnBatchSize + (currentWaveType == WaveType.Enemies ? " enemies." : " projectiles."));
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
                Debug.Log("Next wave.");
                currentWaveIdx++;
                currentBatchIdx = 0;
                waveTimer = 0;
            }
        }
    }

    public void StartWave(WaveType waveType)
    {
        currentWaveType = waveType;
    }

    public void DestroyEnemy(EnemyController enemy)
    {
        int idx = enemies.FindIndex(enemyController => enemyController == enemy);
        Destroy(enemy.gameObject);
        enemies.RemoveAt(idx);
    }

    public void DestroyProjectile(ProjectileController projectile)
    {
        int idx = projectiles.FindIndex(projectileController => projectileController == projectile);
        Destroy(projectile.gameObject);
        projectiles.RemoveAt(idx);
    }

    public void EndWave()
    {
        switch (currentWaveType)
        {
        case WaveType.Enemies:
            enemies.ForEach(enemy => Destroy(enemy.gameObject));
            enemies.Clear();
            enemyWaves.RemoveAt(0);
            break;
        case WaveType.Projectiles:
            projectiles.ForEach(projectile => Destroy(projectile.gameObject));
            projectiles.Clear();
            projectileWaves.RemoveAt(0);
            break;
        }
        currentWaveType = WaveType.None;
    }
}
