using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class ConstructionSite : Resources
{
    public GameObject Building;
    public string function;
    public new string name;
    public int[] costs;
    public resource[] types;
    List<GameObject> resources;

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
            //GameManager.instance.RemoveHighlightedResource();
            gameObject.SetActive(false);
            GameObject instance =  Instantiate(Building, transform.position, Quaternion.identity) as GameObject;
            resources = new List<GameObject>(GameObject.FindGameObjectsWithTag("Resource"));
            resources.Sort((v1, v2) => (v1.transform.position - transform.position).sqrMagnitude.CompareTo((v2.transform.position - transform.position).sqrMagnitude));

            Resources tempResource;
            if (name == "House")
            {
                foreach(GameObject location in resources)
                {
                    tempResource = location.GetComponent<Resources>();
                    if(tempResource.path.Count > 0)
                    {
                        tempResource.RequestPath();
                    }
                }
                foreach(Worker worker in workerSlots)
                {
                    worker.destroy = true;
                }
            }
            else
            {
                foreach (GameObject location in resources)
                {
                    tempResource = location.GetComponent<Resources>();
                    if (tempResource.path.Count > 0)
                    {
                        tempResource.ValidatePath();
                    }
                }
            }
            if (GameManager.instance.currentResource == this)
            {
                GameManager.instance.RemoveHighlightedResource();
                if (instance.tag == "BuildingResource")
                {
                    RaycastHit2D hit = Physics2D.Raycast(instance.transform.position, Vector2.zero);
                    GameManager.instance.HighlightResource(hit);
                }
                    
            }
                
            Destroy(gameObject);
        }
    }
}
