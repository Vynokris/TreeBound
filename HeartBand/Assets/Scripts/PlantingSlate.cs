using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlantingSlate : MonoBehaviour
{
    private GameObject interactingPlayer = null;
    private new SpriteRenderer renderer;
    
    void Start()
    {
        renderer = GetComponent<SpriteRenderer>();
    }

    public void Activate(GameObject player)
    {
        if (interactingPlayer) return;
        interactingPlayer = player;
        renderer.color = interactingPlayer.GetComponent<PlayerController>().GetColor();
    }

    public void Deactivate(GameObject player)
    {
        if (interactingPlayer != player) return;
        renderer.color = Color.grey;
        interactingPlayer = null;
    }
    
    public bool IsActivated() { return interactingPlayer; }
}
