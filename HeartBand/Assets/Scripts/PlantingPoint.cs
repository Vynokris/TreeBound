using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlantingPoint : MonoBehaviour
{
    [SerializeField] private GameObject     plantingSlatePrefab;
    [SerializeField] private float          healingRange        = 5;
    [SerializeField] private float          healingAnimDuration = 5;
    [SerializeField] private AnimationCurve healingAnimCurve;
    
    private List<PlantingSlate> slates = new();
    private SpriteMask spriteMask;
    private bool  used;
    private float healingAnimTimer;

    private void Start()
    {
        spriteMask = transform.GetChild(0).GetComponent<SpriteMask>();
    }

    private void Update()
    {
        if (!WasUsed() || healingAnimTimer > healingAnimDuration) return;
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
        float slateUnitAngle = (Mathf.PI*2) / playerCount;
        float slateAngleOffset = 0;
        switch (playerCount)
        {
            case 1: slateAngleOffset = -Mathf.PI*0.50f; break;
            case 3: slateAngleOffset = -Mathf.PI*0.33f; break;
            case 4: slateAngleOffset = -Mathf.PI*0.25f; break;
        }
        for (int i = 0; i < playerCount; i++)
        {
            GameObject slate = Instantiate(plantingSlatePrefab, transform);
            float slatePosAngle = slateUnitAngle * i + slateAngleOffset;
            float slatePosX = Mathf.Cos(slatePosAngle);
            float slatePosY = Mathf.Sin(slatePosAngle);
            slate.transform.position = transform.position + new Vector3(slatePosX, slatePosY, 0) * 2;
            slates.Add(slate.GetComponent<PlantingSlate>());
        }
    }
    
    public bool WasUsed() { return used; }
    public void SetUsed()
    {
        used = true;
        slates.ForEach(slate => slate.SetUsed());
    }

    public bool IsActivated()
    {
        if (WasUsed()) return false;
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
