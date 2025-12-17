using UnityEngine;

public class Devil : Enemy
{
    void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Enemy")) 
        {
            // ters yone git collide attıldığında find new path with ters yön vektörü
        }
    }

}

    // Start is called once before the first execution of Update after the MonoBehaviour is created
