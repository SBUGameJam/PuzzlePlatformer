using UnityEngine;

public class Enemy : MonoBehaviour, IKillable
{
    public void Kill()
    {
        Destroy(gameObject);
    }
}
