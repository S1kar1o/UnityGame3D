using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Barrack : MonoBehaviour
{
    public int hp = 300;
    public int inProcecing;
    // Start is called before the first frame update
    void Start()
    {
        
    }
    void Destroying()
    {
        if(hp<=0)
        {
            Destroy(gameObject); // Видалення об'єкта 
        }
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
