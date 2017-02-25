using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Random = UnityEngine.Random;
using UnityEngine.EventSystems;
using System;
using System.Diagnostics;

public enum resource
{
    wood, food, stone, gold, ore, metal, building
}

public class GameManager : MonoBehaviour
{
    public BoardManager boardScript;
    public static GameManager instance = null;
    public GameObject worker;
    public GameObject stealer;
    public GameObject enemy;
    public LayerMask BlockingLayer;
    public LayerMask CameraRaycastLayerMask;
    public float gatherMult = 1f;
    public float speedMult = 1f;
    public GameObject house;
    public GameObject furnace;
    public GameObject farm;
    public GameObject wall;
    public GameObject tower;
    public Sprite filledWorker;
    public Sprite EmptyWorker;
    public GameObject highlightBox;
    public GameObject tradeRouteGO;
    public float startingCameraSize;
    public float currentCameraSize;

    private float eatCycle = 10;
    private float eatTimer = 0.0f;
    public List<Worker> workers;
    private int totalWorkers = 8;
    private int workerCount;
    private float oneSecondTimer = 0f;
    private float halfSecondTimer = 0f;
    private int foodEatenPerWorker = 1;
    private Text resourceText;
    private Text workerText;
    private Text highlightedText;
    private Text gameTimeText;
    private Text buildingInfoText;
    private Slider foodSlider;
    private Text sliderText;
    public GameObject[] buildings;
    private bool constructingBuilding;
    public Resources currentResource;
    private GameObject currentBuildingtoBuild;
    private ConstructionSite buildingDetails;
    private GameObject highlightedBox;
    private int gameTime;
    private IEnumerator coroutine;
    private int columns;
    private int rows;
    List<Vector2> gridPositions;
    List<Vector2> path;
    private Heap<Node> openNodes;
    private List<Node> closedNodes = new List<Node>();
    float minFov = 2f;
    float maxFov = 15f;
    float sensitivity = 10f;
    float scrollSensitivity = 1f;
    float mouseScroll;
    float pathDistance;
    Vector2 dragStartPosition;
    List<GameObject> previewWallGameObjects;
    int numberofBuildingsPurchaseable;  
    PathRequestManager requestManager;
    public static Dictionary<resource, int> resources = new Dictionary<resource, int>();
    Vector2[] tradeRouteDestinations;
    List<int[]> tradeRouteCostAmounts;
    List<resource[]> tradeRouteCostTypes;
    List<int[]> tradeRouteDeliverAmounts;
    List<resource[]> tradeRouteDeliverTypes;


    void Awake()
    {
        if (instance == null)
            instance = this;
        else if (instance != this)
            Destroy(gameObject);
        previewWallGameObjects = new List<GameObject>();
        DontDestroyOnLoad(gameObject);
        boardScript = GetComponent<BoardManager>();
        InitGame();
        constructingBuilding = false;
        workers = new List<Worker>();
        Instantiate(highlightBox, new Vector2(-10, -10), Quaternion.identity);
        highlightedBox = Instantiate(highlightBox, new Vector2(-10, -10), Quaternion.identity) as GameObject;
        //print(highlightBox.GetComponent<SpriteRenderer>().isVisible);
        startingCameraSize = Camera.main.orthographicSize;
        currentCameraSize = startingCameraSize;
    }

    void InitGame()
    {
        boardScript.SetupScene();
        columns = boardScript.columns;
        rows = boardScript.rows;
        gridPositions = boardScript.gridPositions;
        buildings = GameObject.FindGameObjectsWithTag("Building");
        resourceText = GameObject.Find("ResourceText").GetComponent<Text>();
        workerText = GameObject.Find("WorkerText").GetComponent<Text>();
        highlightedText = GameObject.Find("HighlightedText").GetComponent<Text>();
        foodSlider = GameObject.Find("FoodSlider").GetComponent<Slider>();
        sliderText = GameObject.Find("SliderText").GetComponent<Text>();
        gameTimeText = GameObject.Find("GameTimeText").GetComponent<Text>();
        buildingInfoText = GameObject.Find("BuildingInfoText").GetComponent<Text>();
        resources[resource.food] = 1000;
        resources[resource.wood] = 1000;
        resources[resource.gold] = 0;
        resources[resource.metal] = 0;
        resources[resource.stone] = 0;
        resources[resource.ore] = 0;
        resources[resource.building] = 0;
        PrintResources();
        PrintWorkers();
        gameTime = 0;
        requestManager = GetComponent<PathRequestManager>();

        //FindPath(boardScript.marker, GetClosestBuilding(buildings, boardScript.marker).transform.position);
        SimplePool.Preload(worker, 100);
        SimplePool.Preload(wall, 100);

        //this should come from the board manager, not hard coded like it is here
        tradeRouteCostAmounts = new List<int[]>();
        tradeRouteCostTypes = new List<resource[]>();
        tradeRouteDeliverAmounts = new List<int[]>();
        tradeRouteDeliverTypes = new List<resource[]>();

        tradeRouteCostAmounts.Add(new int[] { 50,20});
        tradeRouteCostTypes.Add(new resource[] { resource.food, resource.wood });
        tradeRouteDeliverTypes.Add(new resource[] { resource.gold, resource.metal });
        tradeRouteDeliverAmounts.Add(new int[] { 50, 30 });
        tradeRouteDestinations = new Vector2[] { new Vector2(1,1) };
    }

    void Update()
    {

        //workerTimer += Time.deltaTime;
        oneSecondTimer += Time.deltaTime;
        halfSecondTimer += Time.deltaTime;
        //stealerTimer += Time.deltaTime;

        if (workerCount == 0)
            eatTimer = 0;
        else
            eatTimer += Time.deltaTime;

        foodSlider.value = eatTimer;


        if (Input.GetMouseButtonUp(0) && constructingBuilding && currentBuildingtoBuild == wall && !EventSystem.current.IsPointerOverGameObject())
        {
            constructingBuilding = false;
            previewWallGameObjects.Clear();
        }

        

        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
           
            RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
            Vector2 clickPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            if (hit.collider != null)
            {
                string hitTag = hit.collider.gameObject.tag;
                if(hitTag == "Resource" || hitTag == "BuildingResource")
                {
                    HighlightResource(hit);
                    constructingBuilding = false;
                }
            }
            else if (constructingBuilding && currentBuildingtoBuild == wall && hit.collider == null)
            {
                dragStartPosition = clickPosition;
                
            }
            else if (hit.collider == null && constructingBuilding)
            {
                if (clickPosition.y >= -0.5 && clickPosition.y <= rows + 0.5 && clickPosition.x >= -0.5 && clickPosition.x <= columns + 0.5)
                    CreateConstructionSite(clickPosition);
            }            
            else
            {
                RemoveHighlightedResource();
            }
        }

        //dragging wall to be created
        if(Input.GetMouseButton(0) && constructingBuilding && currentBuildingtoBuild == wall && !EventSystem.current.IsPointerOverGameObject())
        {
            RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
            Vector2 clickPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 position = new Vector2();

            numberofBuildingsPurchaseable = NumberOfBuildingsPurchaseable(wall);

            while (previewWallGameObjects.Count > 0)
            {
                GameObject go = previewWallGameObjects[0];
                previewWallGameObjects.RemoveAt(0);
                SimplePool.Despawn(go);
            }

            int startY = (int)Mathf.Round(dragStartPosition.y);
            int endY = (int)Mathf.Round(clickPosition.y);
            int startX = (int)Mathf.Round(dragStartPosition.x);
            int endX = (int)Mathf.Round(clickPosition.x);

            if(startY > endY)
            {
                for (int i = startY; i > endY; i--)
                {
                    position = new Vector2(Mathf.Round(dragStartPosition.x), Mathf.Round(i));
                    hit = Physics2D.Raycast(position, Vector2.zero);

                    if (hit.collider == null && previewWallGameObjects.Count <= numberofBuildingsPurchaseable)
                    {
                        GameObject go = SimplePool.Spawn(currentBuildingtoBuild, position, Quaternion.identity);
                        previewWallGameObjects.Add(go);
                    }
                }
            }
            else
            {
                for (int i = startY; i <= endY; i++)
                {
                    position = new Vector2(Mathf.Round(dragStartPosition.x), Mathf.Round(i));
                    hit = Physics2D.Raycast(position, Vector2.zero);

                    if (hit.collider == null && previewWallGameObjects.Count <= numberofBuildingsPurchaseable)
                    {
                        GameObject go = SimplePool.Spawn(currentBuildingtoBuild, position, Quaternion.identity);
                        previewWallGameObjects.Add(go);
                    }
                }
            }


            if(startX > endX)
            {
                for (int i = startX; i > endX; i--)
                {
                    position = new Vector2(Mathf.Round(i), Mathf.Round(clickPosition.y));
                    hit = Physics2D.Raycast(position, Vector2.zero);
                    if (hit.collider == null && previewWallGameObjects.Count <= numberofBuildingsPurchaseable)
                    {
                        GameObject go = SimplePool.Spawn(currentBuildingtoBuild, position, Quaternion.identity);
                        previewWallGameObjects.Add(go);
                    }
                }
            }
            else
            {
                for (int i = startX; i <= endX; i++)
                {
                    position = new Vector2(Mathf.Round(i), Mathf.Round(clickPosition.y));
                    hit = Physics2D.Raycast(position, Vector2.zero);
                    if (hit.collider == null && previewWallGameObjects.Count <= numberofBuildingsPurchaseable)
                    {
                        GameObject go = SimplePool.Spawn(currentBuildingtoBuild, position, Quaternion.identity);
                        previewWallGameObjects.Add(go);
                    }
                }
            }

        }


        if (oneSecondTimer >= 1)
        {
            gameTime++;
            gameTimeText.text = "GameTime:\n" + Mathf.Abs(gameTime / 60) + ":" + (gameTime % 60).ToString("00");
            oneSecondTimer = 0;
            if (currentResource != null)
            {
                UpdateHighlightedText(currentResource);
                PrintWorkers();
            }
        }

        mouseScroll = Input.GetAxis("Mouse ScrollWheel");
        if (mouseScroll != 0f)
        {
            float fov = Camera.main.orthographicSize;
            fov -= Input.GetAxis("Mouse ScrollWheel") * sensitivity;
            fov = Mathf.Clamp(fov, minFov, maxFov);
            currentCameraSize = fov;
            Camera.main.orthographicSize = fov;
            scrollSensitivity = fov/10;
        }

        if (Input.GetMouseButton(1))
        {
            Camera.main.transform.position -= Camera.main.transform.right * Input.GetAxis("Mouse X") * scrollSensitivity;
            Camera.main.transform.position -= Camera.main.transform.up * Input.GetAxis("Mouse Y") * scrollSensitivity;
        }


        //if (stealerTimer >= 5)
        //{
        //    stealerTimer = 0;
        //    //CreateAndMoveEnemy(boardScript.marker);
        //}

        if (halfSecondTimer >= 0.5)
        {
            //ProcessWorkers();
            halfSecondTimer = 0;

            //cancels worker if one too many has been created
            if(workers.Count > totalWorkers)
            {
                currentResource.tempWorkers-= workers.Count - totalWorkers;
            }

            if (currentResource != null)
            {
                if (currentResource.tempWorkers < currentResource.workerSlots.Count)
                {

                    currentResource.workerSlots[currentResource.workerSlots.Count - 1].Cancel();
                }
                    
                else if (currentResource.tempWorkers > currentResource.workerSlots.Count)
                {
                    //GameObject closestBuilding = GetClosestBuilding(buildings, currentResource.transform.position, false, out path);
                    Vector2 closestBuilding = GetCloseBuilding(currentResource.transform.position);
                    Worker closestCancelledWorker = GetClosestCancelledWorker(currentResource);
                    if (closestCancelledWorker != null && closestCancelledWorker.heldResourceAmount == 0)
                    {
                        float workerDistance, buildingDistance;

                        workerDistance = GetDistance(closestCancelledWorker.transform.position, currentResource.transform.position)/10;
                        buildingDistance = GetBuildingDistance(buildings, currentResource.transform.position);

                        if (workerDistance < buildingDistance)
                        {
                            closestCancelledWorker.StopAllCoroutines();
                            closestCancelledWorker.targetResource = currentResource;
                            closestCancelledWorker.Uncancel();
                            currentResource.AddWorkerToSlot(closestCancelledWorker);
                        }

                    }
                    else if (closestBuilding != new Vector2(0,0))
                        CreateWorkers(currentResource, currentResource.workerSlots.Count + 1);
                }
            }
        }

        for (int touchNumber = 0; touchNumber < Input.touchCount; touchNumber++)
        {
            UnityEngine.Touch touch = Input.GetTouch(touchNumber);
        }

        if (eatTimer >= eatCycle)
            EatFood();

    }

    public bool IsPurchaseable(KeyValuePair<resource,int> pair)
    {
        if(resources[pair.Key] < pair.Value)
        {
            return false;
        }
        return true;
    }

    public void ReduceResources(KeyValuePair<resource, int> pair)
    {
        resources[pair.Key] -= pair.Value;
        PrintResources();
    }

    public void AddResources(KeyValuePair<resource, int> pair)
    {
        resources[pair.Key] += pair.Value;
        PrintResources();
    }

    int NumberOfBuildingsPurchaseable(GameObject building)
    {
        ConstructionSite site = building.GetComponent<ConstructionSite>();
        bool loop = true;
        int returnVal = -1;

        while (loop)
        {
            for (int i = 0; i < site.costs.Length; i++)
            {
                if (!IsPurchaseable(new KeyValuePair<resource, int>(site.types[i], site.costs[i]*(returnVal + 3))))
                {
                    loop = false;
                }
            }
            returnVal++;
        }

        return returnVal;
    }

    bool BuildingPurchaseable(GameObject building)
    {
        ConstructionSite site = building.GetComponent<ConstructionSite>();
        for (int i = 0; i < site.costs.Length; i++)
        {
            if (!IsPurchaseable(new KeyValuePair<resource, int>(site.types[i], site.costs[i])))
            {
                return false;
            }
        }

        return true;
    }

    public void OnGUI()
    {
        if (!constructingBuilding)
        {
            buildingInfoText.text = "";
            if (GUI.Button(new Rect(Screen.width - 130, 150, 130, 30), "Build House"))
            {
                if(BuildingPurchaseable(house))
                {
                    constructingBuilding = true;
                    currentBuildingtoBuild = house;
                    PrintResources();
                }
                else
                {
                    StartCoroutine(FlashText(resourceText, 0.5f, "\nInsufficient Resources"));
                }
                
            }

            if (GUI.Button(new Rect(Screen.width - 130, 190, 130, 30), "Build Furnace"))
            {
                //resources[resource.wood] -= 50;
                //resources[resource.stone] -= 30;
                if(BuildingPurchaseable(furnace))
                {
                    constructingBuilding = true;
                    currentBuildingtoBuild = furnace;
                    PrintResources();
                }
                else
                {
                    StartCoroutine(FlashText(resourceText, 0.5f, "\nInsufficient Resources"));
                }
            }

           

            if (GUI.Button(new Rect(Screen.width - 130, 230, 130, 30), "Build Farm"))
            {
                if (BuildingPurchaseable(farm))
                {
                    constructingBuilding = true;
                    currentBuildingtoBuild = farm;
                    PrintResources();
                }

            }

            if (GUI.Button(new Rect(Screen.width - 130, 270, 130, 30), "Build Wall"))
            {

                if (BuildingPurchaseable(wall))
                {
                    constructingBuilding = true;
                    currentBuildingtoBuild = wall;
                    PrintResources();
                }
                else
                {
                    StartCoroutine(FlashText(resourceText, 0.5f, "\nInsufficient Resources"));
                }
            }

            if (GUI.Button(new Rect(Screen.width - 130, 310, 130, 30), "Build Tower"))
            {

                if (BuildingPurchaseable(tower))
                {
                    constructingBuilding = true;
                    currentBuildingtoBuild = tower;
                    PrintResources();
                }
                else
                {
                    StartCoroutine(FlashText(resourceText, 0.5f, "\nInsufficient Resources"));
                }
            }
            if (GUI.Button(new Rect(Screen.width - 130, 350, 130, 30), "Start Trade Route"))
            {
                for (int i = 0; i < tradeRouteDestinations.Length; i++)
                {
                    bool valid = true;
                    for (int j = 0; j < tradeRouteCostAmounts[i].Length; j++)
                    {
                        if (!IsPurchaseable(new KeyValuePair<resource, int>(tradeRouteCostTypes[i][j], tradeRouteCostAmounts[i][j])))
                        {
                            valid = false;
                        }
                    }
                    if (valid)
                    {
                        GameObject instance = Instantiate(tradeRouteGO, boardScript.homeBase.transform.position, Quaternion.identity) as GameObject;
                        TradeRoute theTradeRoute = instance.GetComponent<TradeRoute>();
                        theTradeRoute.Init(tradeRouteDestinations[i], tradeRouteCostAmounts[i], tradeRouteCostTypes[i], tradeRouteDeliverAmounts[i], tradeRouteDeliverTypes[i], 5);
                    }
                        
                }
                //instantiate Trade Route here!!!
                
            }

            if (GUI.Button(new Rect(Screen.width - 130, 410, 130, 30), "Inc. Gather Amount") && resources[resource.food] >= 0 && resources[resource.gold] >= 0)
            {
                resources[resource.food] -= 0;
                resources[resource.gold] -= 0;
                if (gatherMult < 1.2f)
                    gatherMult = 1.2f;
                UpdateResourceGatherAmount();
                PrintResources();
            }

            if (GUI.Button(new Rect(Screen.width - 130, 450, 130, 30), "Inc. Move Speed") && resources[resource.food] >= 0 && resources[resource.gold] >= 0)
            {
                resources[resource.food] -= 0;
                resources[resource.gold] -= 0;
                speedMult += .2f;
                UpdateWorkerMoveSpeed();
                PrintResources();
            }

        }
        else
        {
            buildingDetails = currentBuildingtoBuild.GetComponent<ConstructionSite>();

            buildingInfoText.text = "Building: " + buildingDetails.name + "\n" + "Cost: " + /*buildingDetails.cost +*/ "\n" + "Function: " + buildingDetails.function;

        }


        if (currentResource != null)
        {
            int currentWorkers;
            if (currentResource.tempWorkers == currentResource.workerSlots.Count)
                currentWorkers = currentResource.workerSlots.Count;
            else
                currentWorkers = currentResource.tempWorkers;
            for (int workerCount = 1; workerCount <= currentResource.numberOfSlots; workerCount++)
            {
                if (currentWorkers >= workerCount)
                {
                    if (GUI.Button(new Rect((Screen.width / 2) - ((currentResource.numberOfSlots/2 * 58) + 58) + (workerCount * 58), 0, 58, 58), filledWorker.texture, GUIStyle.none))
                    {
                        currentResource.tempWorkers = workerCount - 1;
                        for (int i = currentResource.workerSlots.Count; i >= workerCount; i--)
                        {
                            currentResource.workerSlots[i - 1].Cancel();
                        }
                    }
                }
                else
                {

                    if (GUI.Button(new Rect((Screen.width / 2) - ((currentResource.numberOfSlots / 2 * 58) + 58) + (workerCount * 58), 0, 58, 58), EmptyWorker.texture, GUIStyle.none))
                    {
                        int noCancelledWorkers = NumberOfCancelledWorkers();
                        if (workers.Count + workerCount - currentResource.workerSlots.Count <= totalWorkers + noCancelledWorkers)
                        {
                            if(workers.Count + workerCount - currentResource.workerSlots.Count > totalWorkers + noCancelledWorkers)
                            {
                                while (workers.Count + workerCount > totalWorkers + noCancelledWorkers)
                                {
                                    workerCount--;
                                }
                            }
                            else
                            currentResource.tempWorkers = workerCount;
                        }
                        else
                            StartCoroutine(FlashText(workerText, 0.5f, ""));
                    }
                }
            }
        }
    }

    Worker GetClosestCancelledWorker(Resources resource)
    {

        Worker closestWorker = null;
        float closestDist = 0.0f;
        float workerDist = 0.0f;

        foreach(Worker worker in workers)
        {
            if (worker.cancel)
            {
                workerDist = (worker.transform.position - resource.transform.position).sqrMagnitude;
                if (closestWorker == null)
                {
                    closestWorker = worker;
                    closestDist = workerDist;
                }
                else if (workerDist < closestDist)
                {
                    closestWorker = worker;
                }
            }
        }
        return closestWorker;
    }

    int NumberOfCancelledWorkers()
    {
        int returnValue = 0;
        foreach(Worker worker in workers)
        {
            if (worker.cancel && worker.heldResourceAmount == 0)
            {
                returnValue++;
            }
                
        }
        return returnValue;
    }

    private void EatFood()
    {
        eatTimer = 0;

        int random = 0;
        if ((workers.Count * foodEatenPerWorker) > resources[resource.food])
        {
            for (int i = workers.Count * foodEatenPerWorker; i > resources[resource.food]; i -= foodEatenPerWorker)
            {
                random = Random.Range(0, workers.Count - 1);

                DestroyWorker(workers[random], random);

            }
        }

        resources[resource.food] -= workers.Count * foodEatenPerWorker;
        //sliderText.text = "Food Timer\n-" + workers.Count * foodEatenPerWorker + " food";
        StartCoroutine(FlashText(sliderText, 1.0f, "\n-" + workers.Count * foodEatenPerWorker + " food"));
        PrintResources();
        PrintWorkers();
    }

    public void RemoveHighlightedResource()
    {
        if (currentResource != null)
            if (currentResource.tempWorkers > currentResource.workerSlots.Count)
            {
                CreateWorkers(currentResource, currentResource.tempWorkers);
            }
        highlightedBox.transform.position = new Vector2(-10, -10);
        currentResource = null;
        UpdateHighlightedText(null);
    }

    private void CreateConstructionSite(Vector2 clickPosition)
    {
        Vector2 position = new Vector2(Mathf.Round(clickPosition.x), Mathf.Round(clickPosition.y));
        Instantiate(currentBuildingtoBuild, position, Quaternion.identity);
        ConstructionSite site = currentBuildingtoBuild.GetComponent<ConstructionSite>();
        for (int i = 0; i < site.costs.Length; i++)
        {
            ReduceResources(new KeyValuePair<resource, int>(site.types[i], site.costs[i]));
        }
        constructingBuilding = false;
    }

    protected IEnumerator FlashText(Text redText, float delay, string addText)
    {
        redText.color = new Color(1.0f, 0, 0);
        string temp = redText.text;
        redText.text += addText;
        yield return new WaitForSeconds(delay);
        redText.color = new Color(1.0f, 1.0f, 1.0f);
        redText.text = temp;
    }

    float GetBuildingDistance(GameObject[] buildings, Vector2 worker)
    {
        buildings = GameObject.FindGameObjectsWithTag("Building");
        GameObject tMin = null;
        float minDist = Mathf.Infinity;
        Vector2 currentPos = worker;
        foreach (GameObject building in buildings)
        {
            float dist = (new Vector2(building.transform.position.x, building.transform.position.y) - currentPos).sqrMagnitude;
            if (dist < minDist)
            {
                tMin = building;
                minDist = dist;
            }
        }
        return (new Vector2(tMin.transform.position.x, tMin.transform.position.y) - worker).sqrMagnitude;
    }

    private void UpdateResourceGatherAmount()
    {
        GameObject[] resources = GameObject.FindGameObjectsWithTag("Resource");
        
        foreach (GameObject resource in resources)
        {
            resource.GetComponent<Resources>().AdjustGatherAmount();
        }
        resources = GameObject.FindGameObjectsWithTag("BuildingResource");
        foreach (GameObject resource in resources)
        {
            resource.GetComponent<Resources>().AdjustGatherAmount();
        }
    }

    private void UpdateWorkerMoveSpeed()
    {
        foreach (Worker worker in workers)
        {
            worker.UpdateMoveSpeed(speedMult);
        }
    }

    void DestroyWorker(Worker worker, int position)
    {
        //worker.gameObject.SetActive(false);
        
        //if (worker.targetResource == currentResource)  
        //    currentResource.tempWorkers--;
        //worker.targetResource.RemoveWorkerFromSlot(worker);
        workers.RemoveAt(position);
        SimplePool.Despawn(worker.gameObject);
        PrintWorkers();
    }

    public void PrintResources()
    {
        resourceText.text = "Food: " + resources[resource.food] + " \nOre: " + resources[resource.ore] + " \nWood: " + resources[resource.wood] + "\nStone: " + resources[resource.stone] + "\nMetal: " + resources[resource.metal] + "\nGold: " + resources[resource.gold];
    }

    void PrintWorkers()
    {
        if (workers != null)
        {
            totalWorkers = (8 * GameObject.FindGameObjectsWithTag("Building").Length);
            workerCount = workers.Count;
            workerText.text = "Workers: " + workerCount + " / " + totalWorkers;

        }
    }

    public void ProcessWorkers()
    {
        Worker worker;

        for (int i = 0; i < workers.Count; i++)
        {
            worker = workers[i];
            if (worker.destroy)
            {
                DestroyWorker(worker, i);
            }

        }
    }

    public IEnumerator GetFastestPath(Vector2[] ends, Vector2 position, int dps, bool toBuilding)
    { 
        float minDistance = Mathf.Infinity;
        List<Vector2> testPath = new List<Vector2>();
        path = null;

        foreach (Vector2 end in ends)
        {
            if (toBuilding)
                path = FindPath(end, position, dps, out pathDistance);
            else
                path = FindPath(position, end, dps, out pathDistance);
            if (path != null)
            {
                if (pathDistance < minDistance)
                {
                    testPath = path;
                    minDistance = pathDistance;
                }
            }
            
        }
        yield return null;
        if (minDistance != Mathf.Infinity)
            requestManager.FinishedProcessingPath(testPath, minDistance, true);
    }

    Vector2 GetCloseBuilding(Vector2 resource)
    {
        buildings = GameObject.FindGameObjectsWithTag("Building");
        GameObject tMin = null;
        float minDist = Mathf.Infinity;
        Vector3 currentPos = resource;
        foreach (GameObject building in buildings)
        {
            float dist = (building.transform.position - currentPos).sqrMagnitude;
            if (dist < minDist)
            {
                tMin = building;
                minDist = dist;
            }
        }
        return tMin.transform.position;
    }

    protected IEnumerator CreateAndMoveWorkers(Resources resource, int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            //instantiate new instance of worker in the house and make them move to the touch position. 
            GameObject newWorker = SimplePool.Spawn(worker, GetCloseBuilding(resource.transform.position), Quaternion.identity) as GameObject;
            Worker aWorker = newWorker.GetComponent<Worker>();
            aWorker.targetResource = resource;
            aWorker.Init(resource);
            workers.Add(aWorker);
            resource.AddWorkerToSlot(aWorker);
            PrintWorkers();
            yield return new WaitForSeconds(0.5f);
        }
        yield return null;
    }

    //private void CreateAndMoveStealer(Vector2 location)
    //{
    //    List<Vector2> path;
    //    GameObject closestBuilding = GetClosestBuilding(buildings, location, false, out path);
    //    if (path != null)
    //    {
    //        //instantiate new instance of worker in the house and make them move to the touch position. 
    //        GameObject newStealer = Instantiate(stealer, location, Quaternion.identity) as GameObject;
    //        //yield return null;
    //        Stealer aStealer = newStealer.GetComponent<Stealer>();
    //        aStealer.Move(path);
    //        //print stealers
    //    }
    //}

    //private void CreateAndMoveEnemy(Vector2 location)
    //{
    //    instantiate new instance of worker in the house and make them move to the touch position. 
    //    GameObject newEnemy = Instantiate(enemy, location, Quaternion.identity) as GameObject;
    //    yield return null;
    //    Enemy aEnemy = newEnemy.GetComponent<Enemy>();
    //    List<Vector2> path = FindPath(location, boardScript.homeBase.transform.position, aEnemy.attackDamgage, out pathDistance);
    //    aEnemy.Move(path);
    //    print enemies
    //}

    void UpdateHighlightedText(Resources highlightedResource)
    {
        if (highlightedResource != null)
            highlightedText.text = "Resource\nType: " + highlightedResource.resourceType + "\nRemaining: " +
                (int)Mathf.Round(highlightedResource.resourcesRemaining) + "\nWorkers: " + highlightedResource.workerSlots.Count;
        else
            highlightedText.text = "Resource\nType: \nRemaining: \nWorkers: ";

    }

    void HighlightResource(RaycastHit2D clickPosition)
    {
        
        if (currentResource != null)
            if (currentResource.tempWorkers > currentResource.workerSlots.Count)
            {
               // List<Vector2> path;
                //GameObject closestBuilding = GetClosestBuilding(buildings, currentResource.transform.position, true, out path);
                //if(path != null)
                    CreateWorkers(currentResource, currentResource.tempWorkers);
            }
        Resources resource = clickPosition.transform.gameObject.GetComponent<Resources>();
        currentResource = resource;
        if(currentResource.path.Count <= 0)
        {
            currentResource.RequestPath();
        }
        //tempWorkers = resource.workerSlots.Count;
        UpdateHighlightedText(resource);
        highlightedBox.transform.position = new Vector2(currentResource.transform.position.x, currentResource.transform.position.y);
        //}
    }

    void CreateWorkers(Resources resource, int amount)
    {
        
        //GameObject closestBuilding = GetClosestBuilding(buildings, resource.transform.position);
        //List<Vector2> path = FindPath(closestBuilding.transform.position, resource.transform.position, 0, out pathDistance);
        //if(path == null)
        //{
        //    currentResource.tempWorkers = 0;
        //}
        //else
        //{
        if (workers.Count < totalWorkers && resource != null)
        {
            amount = amount - resource.workerSlots.Count;
            if (resource.AreSlotsAvailable(amount))
            {
                if (amount + workers.Count <= totalWorkers)
                {
                    coroutine = CreateAndMoveWorkers(resource, amount);
                    StartCoroutine(coroutine);
                    PrintWorkers();
                }
            }
        }
        //}
    }

    int GetDistance(Vector2 start, Vector2 end)
    {
        int dstX = (int)Mathf.Abs(start.x - end.x);
        int dstY = (int)Mathf.Abs(start.y - end.y);

        if (dstX > dstY)
            return 14 * dstY + 10 * (dstX - dstY);
        return 14 * dstX + 10 * (dstY - dstX);
    }

    public void StartFindPath(Vector2 startPos, Vector2[] targetPos, int dps, bool toBuilding) 
    {
        StartCoroutine(GetFastestPath(targetPos, startPos, dps, toBuilding));
    }

    private List<Vector2> FindPath(Vector2 start, Vector2 end, int dps ,out float distance)
    {
        bool pathSuccess = false, easyPath = false;
        Stopwatch sw = new Stopwatch();
        sw.Start();
        openNodes = new Heap<Node>((int)(rows * columns));

        List<Vector2> waypoints = new List<Vector2>(); 

        RaycastHit2D ray1 = Physics2D.Raycast(start, Vector2.zero);
        RaycastHit2D ray2 = Physics2D.Raycast(end, Vector2.zero);

        if (ray1.collider != null)
        {
            ray1.collider.gameObject.GetComponent<BoxCollider2D>().enabled = false;
        }
        if (ray2.collider != null)
        {
            ray2.collider.gameObject.GetComponent<BoxCollider2D>().enabled = false;
        }
        RaycastHit2D hit = Physics2D.Linecast(start, end);
        Node startNode = new Node((int)start.x, (int)start.y, 0);
        Node endNode = new Node((int)end.x, (int)end.y, 0);
        Node currentNode = startNode;
        if (ray1.collider != null)
        {
            ray1.collider.gameObject.GetComponent<BoxCollider2D>().enabled = true;
        }
        if (ray2.collider != null)
        {
            ray2.collider.gameObject.GetComponent<BoxCollider2D>().enabled = true;
        }

        currentNode.gCost = 0;
        currentNode.hCost = GetDistance(startNode.position, endNode.position);
        openNodes.Add(currentNode);


        while (openNodes.Count > 0)
        {
            if (GetDistance(startNode.position, endNode.position) <= 14 || hit.collider == null || new Vector2(hit.collider.transform.position.x, hit.collider.transform.position.y) == end)
            {
                //Debug.DrawRay(start, end, Color.red, 1000, false);

                easyPath = true;
                break;
            }

            currentNode = openNodes.RemoveFirst();

            if (GetDistance(currentNode.position, endNode.position) <= 14)
            {
                pathSuccess = true;
                break;
            }

                //openNodes.Remove(currentNode);
                closedNodes.Add(currentNode);

            foreach (Node neighbour in GetSurroundingCells(currentNode, dps))
            {
                int newCostToNeighbour = currentNode.gCost + GetDistance(currentNode.position, neighbour.position) + neighbour.movementPenalty;

                if (neighbour.fCost > 0)
                {
                    //compare fCosts
                    if (neighbour.gCost > newCostToNeighbour)
                    {
                        neighbour.gCost = newCostToNeighbour;
                        neighbour.parent = currentNode;
                    }
                }
                else
                {
                    neighbour.gCost = newCostToNeighbour;
                    neighbour.hCost = GetDistance(neighbour.position, endNode.position);
                    neighbour.parent = currentNode;
                    //openNodes.Add(neighbour);
                }
                if (!openNodes.Contains(neighbour))
                    openNodes.Add(neighbour);
                else
                    openNodes.UpdateItem(neighbour);
            }
        }
          
        closedNodes.Clear();
         sw.Stop();
        if (pathSuccess && !easyPath)
        {
            print("path finding ended: " + sw.ElapsedMilliseconds + " ms ");
            waypoints = ReconstructPath(startNode, currentNode, end, out distance);
            return waypoints;
            //requestManager.FinishedProcessingPath(waypoints, true, distance);
        }
        else if (easyPath)
        {
            waypoints.Add(end);
            waypoints.Add(start);
            distance = GetDistance(start, end) / 10;
            return waypoints;
            //requestManager.FinishedProcessingPath(waypoints, true, distance);
        }
        else
        {
            distance = Mathf.Infinity;
            print("path finding ended: no path found" + sw.ElapsedMilliseconds + " ms ");
            return null;
            //requestManager.FinishedProcessingPath(null, false, 0f);
        }
    }

    List<Vector2> ReconstructPath(Node startNode, Node endNode, Vector2 end, out float distance)
    {
        List<Vector2> path = new List<Vector2>();
        distance = 0;
        Node currentNode = endNode;
        
        distance += GetDistance(end, endNode.position);
        Vector2 temp = new Vector2();
        Vector2 tempParent = new Vector2();
        RaycastHit2D hit;
        temp = currentNode.position;
        path.Add(end);
        hit = Physics2D.Linecast(temp, end, BlockingLayer);
        if(hit.collider != null)
            path.Add(endNode.position);
        
        path.Add(temp);
        distance += GetDistance(temp, endNode.position);
        //Instantiate(boardScript.gold, temp, Quaternion.identity);

        do
        {
            tempParent = currentNode.parent.position;
            hit = Physics2D.Linecast(temp, tempParent, BlockingLayer);
            if (hit.collider != null)
            {
                path.Add(currentNode.position);
                distance += GetDistance(currentNode.position, temp);
                //Instantiate(boardScript.gold, new Vector2(currentNode.x, currentNode.y), Quaternion.identity);
                temp = currentNode.position;
            }
            currentNode = currentNode.parent;

        }
        while (currentNode.parent != null);
        path.Add(currentNode.position);
        distance += GetDistance(temp, currentNode.position);
        distance = distance / 10;
        closedNodes.Clear();
        return path;
    }

    List<Node> GetSurroundingCells(Node node, int dps)
    {
        List<Node> neighbours = new List<Node>();
        for (int x = (int)node.position.x - 1; x <= node.position.x + 1; x++)
        {
            for (int y = (int)node.position.y - 1; y <= node.position.y + 1; y++)
            {
                if ((x == node.position.x && y == node.position.y) || x < 0 || y < 0 || x > columns || y > rows)
                {
                    //do nothing
                }
                // check if node is in grid positions
                else if (gridPositions.Contains(new Vector2(x, y)) && closedNodes.Find(p => p.position.x == x && p.position.y == y) == null)
                {
                    RaycastHit2D ray = Physics2D.Raycast(new Vector2(x, y), Vector2.zero);
                    if (ray.collider != null)
                    {
                        string hitTag = ray.collider.gameObject.tag;
                        if ((hitTag == "Building" || hitTag == "BuildingResource" || hitTag == "Forts") && dps > 0)
                        {
                            Entity entity = ray.collider.gameObject.GetComponent<Entity>();
                            neighbours.Add(new Node(x, y, entity.maxHP / dps));
                        }
                        else if (hitTag == "Resource")
                        {
                            // construction site, ok to move over
                            neighbours.Add(new Node(x, y, 0));
                        }
                    }
                    else
                        neighbours.Add(new Node(x, y, 0));
                }
            }
        }
        return neighbours;
    }
}
