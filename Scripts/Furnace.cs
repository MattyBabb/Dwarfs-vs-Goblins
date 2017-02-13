using UnityEngine;
using System.Collections;

public class Furnace : Resources {

    public override void ReduceResources(float amount)
    {
        
        if(GameManager.resources[resource.ore] >= 6)
        {
            GameManager.resources[resource.ore] -= (int)amount;
            GameManager.resources[resource.metal] += (int)(amount / 3);
        }
        else
        {
            GameManager.resources[resource.ore] = 0;
            GameManager.resources[resource.metal] += 1;
            workerSlots[0].Cancel();
        }
    }
}
