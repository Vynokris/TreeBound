using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlantingSlate : MonoBehaviour
{
    [SerializeField] private Sprite[] slateSprites;
    
    private GameObject interactingPlayer = null;
    private new SpriteRenderer renderer;
    private     SpriteRenderer buttonRenderer;
    private bool isFinalPoint = false;
    private bool used = false;
    private TreeController tree;
    
    void Start()
    {
        renderer       = transform.GetChild(0).gameObject.GetComponent<SpriteRenderer>();
        buttonRenderer = transform.GetChild(1).gameObject.GetComponent<SpriteRenderer>();
        tree           = FindObjectOfType<TreeController>();
        
        isFinalPoint = transform.GetComponentInParent<PlantingPoint>().IsFinalPoint();
        if (isFinalPoint) buttonRenderer.enabled = false;
    }

    void Update()
    {
        if (!isFinalPoint || (tree.GetGrowingStage() < 3 && !buttonRenderer.enabled)) return;
        buttonRenderer.enabled = true;
    }
    
    public bool WasUsed() { return used; }
    public void SetUsed() { used = true; buttonRenderer.enabled = false; }
    
    public bool IsActivated() { return !used && interactingPlayer; }

    public void Activate(GameObject player)
    {
        if (isFinalPoint && tree.GetGrowingStage() < 3) return;
        if (used || interactingPlayer) return;
        interactingPlayer = player;
        renderer.sprite = slateSprites[interactingPlayer.GetComponent<PlayerController>().GetPlayerIndex()+1];
        buttonRenderer.enabled = false;
    }

    public void Deactivate()
    {
        renderer.sprite = slateSprites[0];
        interactingPlayer = null;
        if (!used)
            buttonRenderer.enabled = true;
    }
}
