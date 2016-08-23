using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviour
{
    public BoardManager boardScript;
    public static GameManager instance = null;
    public GameObject worker;
    public LayerMask BlockingLayer;
    public int oreAmount;
    public int foodAmount = 20;
    public int woodAmount =0;
    public int stoneAmount =0;
    public int metalAmount =0;
    public int goldAmount =0;
    public GameObject house;
    public GameObject furnace;
    public GameObject farm;
    public Sprite filledWorker;
    public Sprite EmptyWorker;
    public GameObject highlightBox;

    private float eatCycle = 10;
    private float eatTimer = 0.0f;
    private List<Worker> workers;
    private int totalWorkers = 8;
    private float workerTimer = .5f;
    private float workerTick = .5f;
    private int workerCount;
    private float oneSecondTimer = 0f;
    private int foodEatenPerWorker = 3;
    private Text resourceText;
    private Text workerText;
    private Text highlightedText;
    private Slider foodSlider;
    private GameObject[] buildings;
    private bool constructingBuilding;
    private Resources currentResource;
    private GameObject currentBuildingtoBuild;
    private Rect highlightResource;

    void Awake ()
    {
        if (instance == null)
            instance = this;
        else if (instance != this)
            Destroy(gameObject);

        DontDestroyOnLoad(gameObject);
        boardScript = GetComponent<BoardManager>();
        InitGame();
        constructingBuilding = false;
        workers = new List<Worker>();
        Instantiate(highlightBox, new Vector2(-10,-10), Quaternion.identity);
        //highlightBox.GetComponent<SpriteRenderer>().enabled = false;
        print(highlightBox.GetComponent<SpriteRenderer>().isVisible);
    }

    void InitGame()
    {
        boardScript.SetupScene();
        buildings = GameObject.FindGameObjectsWithTag("Building");
        resourceText = GameObject.Find("ResourceText").GetComponent<Text>();
        workerText = GameObject.Find("WorkerText").GetComponent<Text>();
        highlightedText = GameObject.Find("HighlightedText").GetComponent<Text>();
        foodSlider = GameObject.Find("FoodSlider").GetComponent<Slider>();
        PrintResources();
        PrintWorkers();
    } 

	void Update ()
    {
        
        workerTimer += Time.deltaTime;
        oneSecondTimer += Time.deltaTime;

        if (workerCount == 0)
            eatTimer = 0;
        else
            eatTimer += Time.deltaTime;

        foodSlider.value = eatTimer;

        if (Input.GetButton("Fire1"))
        {
            RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
            Vector2 clickPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            if (hit.collider != null && !constructingBuilding)
            {
                HighlightResource(clickPosition);
                constructingBuilding = false;
            }
            else if (hit.collider == null && constructingBuilding)
            {
                CreateConstructionSite(clickPosition);
            }
            //else if (hit.collider == null && !constructingBuilding)
            //{
            //    currentResource = null;
            //}
        }
        if(oneSecondTimer >= 1)
        {
            
            oneSecondTimer = 0;
            if (currentResource != null)
            {
                UpdateHighlightedText(currentResource);
                PrintWorkers();
            }
                
        }

        for(int touchNumber = 0; touchNumber < Input.touchCount; touchNumber++)
        {
            UnityEngine.Touch touch = Input.GetTouch(touchNumber);
        }

        //turn workers back to their resources if they are delivering them
        ProcessWorkers();

        if (eatTimer >= eatCycle)
            EatFood();

    }

    public void OnGUI()
    {
        if (GUI.Button(new Rect(Screen.width - 130, 150, 130, 30), "Build House") && woodAmount >= 50)
        {
            woodAmount -= 50;
            constructingBuilding = true;
            currentBuildingtoBuild = house;
            PrintResources();
        }

        if (GUI.Button(new Rect(Screen.width - 130, 190, 130, 30), "Build Furnace") && woodAmount >= 50 && stoneAmount >= 30)
        {
            woodAmount -= 50;
            stoneAmount -= 30;
            constructingBuilding = true;
            currentBuildingtoBuild = furnace;
            PrintResources();
        }

        if (GUI.Button(new Rect(Screen.width - 130, 230, 130, 30), "Build Farm") && woodAmount >= 50)
        {
            woodAmount -= 50;
            constructingBuilding = true;
            currentBuildingtoBuild = farm;
            PrintResources();
        }

        if (currentResource != null)
        {
            int currentWorkers = currentResource.workerSlots.Count;
            for (int workerCount = 1; workerCount <= currentResource.numberOfSlots; workerCount++)
            {
                if (currentWorkers >= workerCount)
                {
                    if (GUI.Button(new Rect((Screen.width / 2) - 174 + (workerCount * 58), 0, 58, 58), filledWorker.texture, GUIStyle.none))
                        for(int i = currentWorkers; i >= workerCount; i--)
                        {
                            currentResource.workerSlots[i - 1].Cancel();
                        }
                }
                else
                {
                    if (GUI.Button(new Rect((Screen.width / 2) - 174 + (workerCount * 58), 0, 58, 58), EmptyWorker.texture, GUIStyle.none))
                        CreateWorkers(currentResource, workerCount);
                }
            }

            
            //currentResource.GetComponent<SpriteRenderer>().enabled = false;
        }
    }

    private void EatFood()
    {
        eatTimer = 0;
        
        int random = 0;
        if((workers.Count * foodEatenPerWorker) > foodAmount)
        {
            for(int i = workers.Count * foodEatenPerWorker; i > foodAmount; i -= foodEatenPerWorker)
            {
                random = Random.Range(0, workers.Count-1);

                DestroyWorker(workers[random], random);

            }
        }

        foodAmount -= workers.Count * foodEatenPerWorker;
        PrintResources();
        PrintWorkers();
    }

    private void CreateConstructionSite(Vector2 clickPosition)
    {
        GameObject instance = Instantiate(currentBuildingtoBuild, clickPosition, Quaternion.identity) as GameObject;
        constructingBuilding = false;
    }

    private void ProcessWorkers()
    {
        Worker worker;
        GameObject closestBuilding;
        float closestBuildingDistance;

        for (int i = 0; i < workers.Count; i++)
        {
            worker = workers[i];
            closestBuilding = GetClosestBuilding(buildings, worker.transform.position);
            closestBuildingDistance = (closestBuilding.transform.position - worker.transform.position).sqrMagnitude;
            if (worker.heldResourceAmount > 1 && closestBuildingDistance <= 0 && worker != null)
            {
                switch (worker.targetResource.resourceType)
                {
                    case "Ore":
                        oreAmount += (int)Mathf.Round(worker.heldResourceAmount);
                        break;
                    case "Food":
                        foodAmount += (int)Mathf.Round(worker.heldResourceAmount);
                        break;
                    case "Wood":
                        woodAmount += (int)Mathf.Round(worker.heldResourceAmount);
                        break;
                    case "Stone":
                        stoneAmount += (int)Mathf.Round(worker.heldResourceAmount);
                        break;
                    case "Gold":
                        goldAmount += (int)Mathf.Round(worker.heldResourceAmount);
                        break;
                }
                PrintResources();
                worker.heldResourceAmount = 0;
                if (worker.targetResource.resourcesRemaining > 0 && !worker.cancel)
                    worker.Move(worker.targetResource.transform);
                else
                {
                    DestroyWorker(worker, i);
                }
            }else if((worker.targetResource.resourcesRemaining <= 0 && closestBuildingDistance <= 0) || (worker.cancel && closestBuildingDistance <= 0))
            {
                DestroyWorker(worker, i);
            }
        }
    }

    void DestroyWorker(Worker worker, int position)
    {
        worker.gameObject.SetActive(false);
        worker.targetResource.RemoveWorkerFromSlot(worker);
        workers.RemoveAt(position);
        PrintWorkers();
    }

    void PrintResources()
    {
        resourceText.text = "Food: " + foodAmount+ " \nOre: " + oreAmount + " \nWood: " + woodAmount + "\nStone: " + stoneAmount + "\nMetal: " + metalAmount + "\nGold: " + goldAmount;
    }

    void PrintWorkers()
    {
        if(workers != null)
        {
            totalWorkers = (8 * GameObject.FindGameObjectsWithTag("Building").Length);
            workerCount = workers.Count;
            workerText.text = "Workers: " + workerCount + " / " + totalWorkers;

        }
            
    }

    GameObject GetClosestBuilding(GameObject[] buildings, Vector2 worker)
    {
        buildings = GameObject.FindGameObjectsWithTag("Building");
        GameObject tMin = null;
        float minDist = Mathf.Infinity;
        Vector3 currentPos = worker;
        foreach (GameObject building in buildings)
        {
            float dist = (building.transform.position - currentPos).sqrMagnitude;
            if (dist < minDist)
            {
                tMin = building;
                minDist = dist;
            }
        }
        return tMin;
    }

    protected IEnumerator CreateAndMoveWorkers(Resources resource, int amount)
    {
        GameObject closestBuilding = GetClosestBuilding(buildings, resource.transform.position);
        for (int i = 0; i < amount; i++)
        {
            //instantiate new instance of worker in the house and make them move to the touch position. 
            GameObject newWorker = Instantiate(worker, closestBuilding.transform.position, Quaternion.identity) as GameObject;
            //yield return null;
            Worker aWorker = newWorker.GetComponent<Worker>();
            aWorker.targetResource = resource;
            workers.Add(aWorker);
            workers[workers.Count - 1].Move(resource.transform);
            resource.AddWorkerToSlot(aWorker);
            PrintWorkers();
            yield return new WaitForSeconds(0.5f);
        }
        
        yield return null;
    }

    void UpdateHighlightedText (Resources highlightedResource)
    {
        highlightedText.text = "Resource\nType: " + highlightedResource.resourceType +"\nRemaining: " +
            (int)Mathf.Round(highlightedResource.resourcesRemaining) + "\nWorkers: " + highlightedResource.workerSlots.Count;
        
    }

    void HighlightResource(Vector2 clickPosition)
    {
        GameObject closestBuilding = GetClosestBuilding(buildings, clickPosition);

        RaycastHit2D hit = Physics2D.Linecast(closestBuilding.transform.position, clickPosition, BlockingLayer);
        if (hit.transform != null && hit.collider.tag == "Resource")
        {
            Resources resource = hit.transform.gameObject.GetComponent<Resources>();
            currentResource = resource;
            UpdateHighlightedText(resource);
            highlightBox.transform.position = new Vector3(currentResource.transform.position.x, currentResource.transform.position.y, 0);
            highlightBox.GetComponent<Renderer>().enabled = true;
        }
    }

    void CreateWorkers(Resources resource, int amount)
    {
        GameObject closestBuilding = GetClosestBuilding(buildings, resource.transform.position);
        if ( workers.Count < totalWorkers && resource != null)
        {
            amount = amount - resource.workerSlots.Count;
            if (resource.AreSlotsAvailable(amount))
            {
                if(amount + workers.Count <= totalWorkers)
                {
                    StartCoroutine(CreateAndMoveWorkers(resource, amount));
                    PrintWorkers();
                }
            }
        }
    }
}

