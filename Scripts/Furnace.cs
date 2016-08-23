using UnityEngine;
using System.Collections;

public class Furnace : Resources {

    public override void ReduceResources(float amount)
    {
        
        if(GameManager.instance.oreAmount >= 6)
        {
            GameManager.instance.oreAmount -= (int)amount;
            GameManager.instance.metalAmount += (int)(amount / 3);
        }
        else
        {
            GameManager.instance.oreAmount = 0;
            GameManager.instance.metalAmount += 1;
            workerSlots[0].Cancel();
        }
    }
}
