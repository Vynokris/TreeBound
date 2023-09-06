using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlantingSlate : MonoBehaviour
{
    private GameObject interactingPlayer = null;
    private new SpriteRenderer renderer;
    private bool used = false;
    
    void Start()
    {
        renderer = GetComponent<SpriteRenderer>();
    }
    
    public bool WasUsed() { return used; }
    public void SetUsed() { used = true; }
    
    public bool IsActivated() { return !used && interactingPlayer; }

    public void Activate(GameObject player)
    {
        if (used || interactingPlayer) return;
        interactingPlayer = player;
        renderer.color = interactingPlayer.GetComponent<PlayerController>().GetColor();
    }

    public void Deactivate()
    {
        renderer.color = Color.grey;
        interactingPlayer = null;
    }
}
