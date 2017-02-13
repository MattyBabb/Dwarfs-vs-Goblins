using UnityEngine;
using System.Collections.Generic;

public class Resources : MonoBehaviour
{
    public float resourcesRemaining;
    public List<Worker> workerSlots;
    public resource resourceType;
    public float gatherTime;
    public int gatherAmount;
    public int numberOfSlots;
    [HideInInspector]
    public int tempWorkers;

    //float halfSecondTimer = 0f;

    //void Update()
    //{
    //    halfSecondTimer += Time.deltaTime;

    //    if(halfSecondTimer > 0.5f)
    //    {

    //    }
    //}

    void Awake()
    {
        AdjustGatherAmount();
    }

    public virtual void ReduceResources(float amount)
    {
        resourcesRemaining -= amount;
        if (resourcesRemaining <= 0)
        {
            gameObject.SetActive(false);
            if (GameManager.instance.currentResource == this)
                GameManager.instance.RemoveHighlightedResource();
        }
    }

    public void AdjustGatherAmount()
    {
        gatherAmount = (int)Mathf.Round(gatherAmount * GameManager.instance.gatherMult);
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
