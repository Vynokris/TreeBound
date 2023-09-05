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
    
    private List<EnemyController>      enemies;
    private List<ProjectileController> projectiles;
    
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
        if (currentWaveIdx >= curWaveList[0].waves.Count)
        {
            switch (currentWaveType)
            {
                case WaveType.Enemies:
                    return;
                case WaveType.Projectiles:
                    currentWaveIdx = 0;
                    break;
            }
        }
        WaveData curWaveData = curWaveList[0].waves[currentWaveIdx];
        
        // Spawn enemies/projectiles during each wave.
        if (currentBatchIdx <= curWaveData.totalSpawnBatchCount)
        {
            waveTimer += Time.deltaTime;
            if (waveTimer >= curWaveData.spawningFrequency)
            {
                // TODO: Spawn enemies/projectiles.
                Debug.Log("Spawned enemies.");
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

    public void EndWave()
    {
        switch (currentWaveType)
        {
        case WaveType.Enemies:
            enemies.ForEach(Destroy);
            enemies.Clear();
            enemyWaves.RemoveAt(0);
            break;
        case WaveType.Projectiles:
            projectiles.ForEach(Destroy);
            projectiles.Clear();
            projectileWaves.RemoveAt(0);
            break;
        }
        currentWaveType = WaveType.None;
    }
}
