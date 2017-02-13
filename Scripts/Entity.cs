using UnityEngine;
using System.Collections;

public class Entity : MonoBehaviour {

    public int hitPoints;

    public void LoseLife(int amount)
    {
        hitPoints -= amount;
        if(hitPoints <= 0)
        {
            this.gameObject.SetActive(false);
            Destroy(this.gameObject);
        }
    }


}
