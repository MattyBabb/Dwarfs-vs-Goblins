using UnityEngine;
using System.Collections;

public class ConstructionSite : Resources
{
    public GameObject Building;

    public override void ReduceResources(float amount)
    {
        resourcesRemaining -= amount;
        if (resourcesRemaining <= 0)
        {
            gameObject.SetActive(false);
            GameObject instance = Instantiate(Building, transform.position, Quaternion.identity) as GameObject;
            foreach(Worker worker in workerSlots)
            {
                worker.Cancel();
            }
            Destroy(gameObject);
        }
    }
}
