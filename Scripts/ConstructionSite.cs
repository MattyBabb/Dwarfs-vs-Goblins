using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class ConstructionSite : Resources
{
    public GameObject Building;
    public string function;
    public string name;
    public int[] costs;
    public resource[] types;

    //void Awake()
    //{
    //    Dictionary<resource, int> dict = new Dictionary<resource, int>();
    //    for (int i = 0; i < costs.Length; i++)
    //    {
    //        dict.Add(types[i], costs[i]);
    //    }
    //    foreach(KeyValuePair<resource,int> pair in dict)
    //    {
    //        GameManager.instance.ReduceResources(pair);
    //    }

    //}

    public override void ReduceResources(float amount)
    {
        resourcesRemaining -= amount;
        if (resourcesRemaining <= 0)
        {
            gameObject.SetActive(false);
            GameObject instance = Instantiate(Building, transform.position, Quaternion.identity) as GameObject;
            for (int i = workerSlots.Count - 1; i >= 0; i--)
            {
                workerSlots[i].Cancel();
            }
            if (GameManager.instance.currentResource == this)
                GameManager.instance.RemoveHighlightedResource();
            Destroy(gameObject);
        }
    }
}
