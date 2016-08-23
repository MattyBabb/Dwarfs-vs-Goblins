using UnityEngine;
using System.Collections.Generic;

public class Resources : MonoBehaviour
{
    public float resourcesRemaining;
    public List<Worker> workerSlots;
    public string resourceType;
    public float gatherTime;
    public int gatherAmount;
    public int numberOfSlots;

    void Awake()
    {
    }

    public virtual void ReduceResources(float amount)
    {
            resourcesRemaining -= amount;
            if (resourcesRemaining <= 0)
                gameObject.SetActive(false);
    }

    public bool AreSlotsAvailable(int amount)
    {
        if (workerSlots.Count + amount <= numberOfSlots)
            return true;  
        else
            return false;
    }

    public void AddWorkerToSlot(Worker worker)
    {
        if(workerSlots.Count < numberOfSlots)
        {
            workerSlots.Add(worker);
        }
    }

    public void RemoveWorkerFromSlot(Worker worker)
    {
        if (workerSlots.Count > 0)
            workerSlots.RemoveAt(workerSlots.Count-1);
    }

}
