using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlantingPoint : MonoBehaviour
{
    [SerializeField] private GameObject     plantingSlatePrefab;
    [SerializeField] private float          healingRange        = 5;
    [SerializeField] private float          healingAnimDuration = 5;
    [SerializeField] private AnimationCurve healingAnimCurve;
    [SerializeField] private float          slateDistance = 8;
    [SerializeField] private bool           isFinalPoint = false;
    
    private List<PlantingSlate> slates = new();
    private SpriteMask spriteMask;
    private bool  used;
    private float healingAnimTimer;
    private TreeController tree;

    private void Start()
    {
        spriteMask = transform.GetChild(0).GetComponent<SpriteMask>();
        tree = FindObjectOfType<TreeController>();
    }

    private void Update()
    {
        if (!used || !spriteMask || healingAnimTimer > healingAnimDuration) return;
        healingAnimTimer += Time.deltaTime;
        float maskSize = healingAnimCurve.Evaluate(healingAnimTimer / healingAnimDuration) * healingRange;
        spriteMask.transform.localScale = new Vector3(maskSize, maskSize, maskSize);
    }

    public void UpdateSlateCount(int playerCount)
    {
        if (slates.Count == playerCount) return;
        
        // Delete the previous slates.
        slates.ForEach(slate => Destroy(slate.gameObject));
        slates.Clear();
        slates.Capacity = playerCount;
        
        // Create new equally spaces slates.
        List<Vector3> slateDirs = new(playerCount);
        switch (playerCount)
        {
            case 1: 
                slateDirs.Add(Vector3.down);
                break;
            case 2:
                slateDirs.Add((Vector3.down + Vector3.left  * 0.5f).normalized);
                slateDirs.Add((Vector3.down + Vector3.right * 0.5f).normalized);
                break;
            case 3:
                slateDirs.Add(Vector3.down);
                slateDirs.Add((Vector3.down + Vector3.left ).normalized * 1.14f);
                slateDirs.Add((Vector3.down + Vector3.right).normalized * 1.15f);
                break;
        }
        for (int i = 0; i < playerCount; i++)
        {
            GameObject slate = Instantiate(plantingSlatePrefab, transform);
            slate.transform.position = transform.position + slateDirs[i] * slateDistance;
            slates.Add(slate.GetComponent<PlantingSlate>());
        }
    }
    
    public bool WasUsed() { return (isFinalPoint && tree.GetGrowingStage() < 3) || used; }
    public void SetUsed(bool shouldHeal = true)
    {
        used = true;
        if (!shouldHeal) spriteMask = null;
        slates.ForEach(slate => slate.SetUsed());
        if (isFinalPoint) SceneManager.LoadScene("Victory");
    }

    public bool IsActivated()
    {
        if (WasUsed() || slates.Count <= 0) return false;
        foreach (PlantingSlate slate in slates) {
            if (!slate.IsActivated()) {
                return false;
            }
        }
        return true;
    }

    public void DeactivateSlates()
    {
        slates.ForEach(slate => slate.Deactivate());
    }
}
