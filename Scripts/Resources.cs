using UnityEngine;
using System.Collections.Generic;
using System;

public class Resources : MonoBehaviour
{
    public float resourcesRemaining;
    public List<Worker> workerSlots;
    public LayerMask BuildingLayerMask;
    public resource resourceType;
    public float gatherTime;
    public int gatherAmount;
    public int numberOfSlots;
    [HideInInspector]
    public int tempWorkers;
    [HideInInspector]
    public List<Vector2> path;
    [HideInInspector]
    public float pathDistance;
    protected Vector2[] locations;

    void Awake()
    {
        AdjustGatherAmount(GameManager.resourceUpgrades[resourceType]);
        path = new List<Vector2>();
    }

    void OnPathFound(List<Vector2> newPath, bool pathSuccessful, float distance)
    {
        if (pathSuccessful)
        {
            path = newPath;
            pathDistance = distance;
        }
    }

    public void RequestPath()
    {
        locations = Array.ConvertAll(GameObject.FindGameObjectsWithTag("Building"), item => new Vector2(item.transform.position.x, item.transform.position.y));
        PathRequestManager.RequestPath(transform.position, locations, 0, false, OnPathFound);
    }

    public void ValidatePath()
    {
        RaycastHit2D hit;
        for (int i = 0; i < path.Count-1; i++)
        {
            hit = Physics2D.Linecast(path[i], path[i+1], BuildingLayerMask);
            if(hit.collider != null)
            {
                RequestPath();
            }
        }

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

    public void AdjustGatherAmount(float mult)
    {
        gatherAmount = (int)Mathf.Round(gatherAmount * mult);
    }

    public bool AreSlotsAvailable(int amount)
    {
        if (workerSlots.Count + amount <= numberOfSlots)
            return true;  
        else
            return false;
    }

    public virtual void AddWorkerToSlot(Worker worker)
    {
        if(workerSlots.Count < numberOfSlots)
        {
            workerSlots.Add(worker);
        }
    }

    public virtual void RemoveWorkerFromSlot(Worker worker)
    {
        if (workerSlots.Count > 0)
        {
            workerSlots.RemoveAt(workerSlots.Count - 1);
        }
    }

}
