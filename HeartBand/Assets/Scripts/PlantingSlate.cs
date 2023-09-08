using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlantingSlate : MonoBehaviour
{
    [SerializeField] private Sprite[] slateSprites;
    
    private GameObject interactingPlayer = null;
    private new SpriteRenderer renderer;
    private bool used = false;
    
    void Start()
    {
        renderer = transform.GetChild(0).gameObject.GetComponent<SpriteRenderer>();
    }
    
    public bool WasUsed() { return used; }
    public void SetUsed() { used = true; }
    
    public bool IsActivated() { return !used && interactingPlayer; }

    public void Activate(GameObject player)
    {
        if (used || interactingPlayer) return;
        interactingPlayer = player;
        renderer.sprite = slateSprites[interactingPlayer.GetComponent<PlayerController>().GetPlayerIndex()+1];
    }

    public void Deactivate()
    {
        renderer.sprite = slateSprites[0];
        interactingPlayer = null;
    }
}
