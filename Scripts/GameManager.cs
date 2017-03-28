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
    wood, food, stone, gold, ore, metal, building, none
}

enum buildable
{
    house, furnace, tower, wall, farm
}

enum actionMenu
{
    nothing, building, upgrading, trading, buildBuilding, startTradeRoute, finaliseUpgrade, upgradingFood, upgradingStone, upgradingGold, upgradingWood, upgradingOre, tower, towerMove
}

public class GameManager : MonoBehaviour
{
    public BoardManager boardScript;
    public static GameManager instance = null;
    public GameObject worker;
    public GameObject stealer;
    public GameObject enemy;
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
    public static Dictionary<resource, float> resourceUpgrades = new Dictionary<resource, float>();
    public List<GameObject> enemyTypes;
    float[] enemySpawnDetails;
    int enemySpawnInt;
    Vector2[] spawnAreas;
    Vector2[] tradeRouteDestinations;
    List<int[]> costAmounts;
    List<resource[]> costTypes;
    List<int[]> deliverAmounts;
    List<resource[]> deliverTypes;
    buildable[] buildingsBuildable;
    actionMenu currentMenu;
    int currentTradeRoute;
    resource currentUpgrade;
    resource[] upgradeOrder;

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
        enemySpawnInt = 0;
        resources[resource.food] = 1000;
        resources[resource.wood] = 1000;
        resources[resource.gold] = 0;
        resources[resource.metal] = 0;
        resources[resource.stone] = 500;
        resources[resource.ore] = 0;
        resources[resource.building] = 0;
        resourceUpgrades[resource.food] = 1;
        resourceUpgrades[resource.wood] = 1;
        resourceUpgrades[resource.gold] = 1;
        resourceUpgrades[resource.metal] = 1;
        resourceUpgrades[resource.stone] = 1;
        resourceUpgrades[resource.ore] = 1;
        resourceUpgrades[resource.building] = 1;
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

        PrintResources();
        PrintWorkers();
        gameTime = 0;
        requestManager = GetComponent<PathRequestManager>();

        //FindPath(boardScript.marker, GetClosestBuilding(buildings, boardScript.marker).transform.position);
        SimplePool.Preload(worker, 100);
        SimplePool.Preload(wall, 100);
        SimplePool.Preload(tradeRouteGO, 10);
        SimplePool.Preload(enemy, 100);

        //this should come from the board manager, not hard coded like it is here
        costAmounts = new List<int[]>();
        costTypes = new List<resource[]>();
        deliverAmounts = new List<int[]>();
        deliverTypes = new List<resource[]>();

        costAmounts.Add(new int[] { 50,20});
        costTypes.Add(new resource[] { resource.food, resource.wood });
        deliverTypes.Add(new resource[] { resource.gold, resource.metal });
        deliverAmounts.Add(new int[] { 50, 30 });

        costAmounts.Add(new int[] { 50, 20 });
        costTypes.Add(new resource[] { resource.gold, resource.metal });
        deliverTypes.Add(new resource[] { resource.food, resource.wood });
        deliverAmounts.Add(new int[] { 50, 30 });
        //tradeRouteDestinations[1] = new Vector2(boardScript.rows, boardScript.columns);
        tradeRouteDestinations = new Vector2[] { new Vector2(1, 1), new Vector2(boardScript.rows, boardScript.columns) };

        upgradeOrder = new resource[] { resource.building ,resource.food, resource.wood, resource.stone, resource.ore, resource.none};

        //building upgrade requirements
        costTypes.Add(new resource[] { resource.stone, resource.metal });
        costAmounts.Add(new int[] { 50, 20 });

        //food upgrade requirements
        costTypes.Add(new resource[] { resource.wood, resource.metal});
        costAmounts.Add(new int[] { 100, 20 });

        //wood upgrade requirements
        costTypes.Add(new resource[] { resource.wood, resource.metal });
        costAmounts.Add(new int[] { 100, 20 });

        //stone upgrade requirements
        costTypes.Add(new resource[] { resource.wood, resource.metal });
        costAmounts.Add(new int[] { 150, 35 });

        //ore upgrade requirements
        costTypes.Add(new resource[] { resource.wood, resource.metal });
        costAmounts.Add(new int[] { 100, 15 });

        //worker speed upgrade requirements
        costTypes.Add(new resource[] { resource.food, resource.wood });
        costAmounts.Add(new int[] { 100, 100 });

        //this should also be passed in from the board script - which buildings are buildable for the level
        buildingsBuildable = new buildable[] { buildable.farm, buildable.furnace, buildable.house, buildable.tower, buildable.wall};
        currentMenu = actionMenu.nothing;

        spawnAreas = new Vector2[] { new Vector2(rows, 1) };

        //enemy spawn details: time, position.x, position.y, enemyTypeNo
        enemySpawnDetails = new float[] { 500.5f, columns, 1, 1, 500.25f, 1, rows, 1 };
        
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

        if(enemySpawnInt < enemySpawnDetails.Length && enemySpawnDetails[enemySpawnInt] <= gameTime)
        {
            Vector2 spawnPos = new Vector2(enemySpawnDetails[enemySpawnInt + 1], enemySpawnDetails[enemySpawnInt + 2]); 
            //SpawnEnemy(spawnPos, enemySpawnDetails[enemySpawnInt + 3]) //last int is enemy type

            GameObject instance = SimplePool.Spawn(enemyTypes[(int)enemySpawnDetails[enemySpawnInt + 3]], spawnPos, Quaternion.identity) as GameObject;
            enemySpawnInt += 4;
            Enemy enemy = instance.GetComponent(typeof(Enemy)) as Enemy;
            Stealer stealer = instance.GetComponent(typeof(Stealer)) as Stealer;

            if(enemy != null)
            {
                enemy.Init();
            }else if(stealer != null)
            {
                stealer.Init();
            }
        }

        if (Input.GetMouseButtonUp(0) && currentMenu == actionMenu.buildBuilding && currentBuildingtoBuild == wall && !EventSystem.current.IsPointerOverGameObject())
        {
            currentMenu = actionMenu.nothing;
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
                    RemoveHighlightedResource();
                    HighlightResource(hit);
                }
            }
            else if (currentMenu == actionMenu.buildBuilding && currentBuildingtoBuild == wall && hit.collider == null)
            {
                dragStartPosition = clickPosition;
                
            }
            else if (hit.collider == null && currentMenu == actionMenu.buildBuilding)
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
        if(Input.GetMouseButton(0) && currentMenu == actionMenu.buildBuilding && currentBuildingtoBuild == wall && !EventSystem.current.IsPointerOverGameObject())
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

            if (workers.Count > totalWorkers)
            {
                //cancels workers if one too many has been created
                ControlWorkerCounts();
            }

            if (currentResource != null)
            {

                if (currentResource.workerSlots.Count >0 && currentResource.tempWorkers < currentResource.workerSlots.Count)
                {
                    //cancel worker
                    currentResource.workerSlots[currentResource.workerSlots.Count - 1].Cancel();
                }
                    
                else if (currentResource.tempWorkers > currentResource.workerSlots.Count)
                {
                    //create worker

                    Worker closestCancelledWorker = GetClosestCancelledWorker(currentResource);
                    if (closestCancelledWorker != null && closestCancelledWorker.heldResourceAmount == 0)
                    {
                        float workerDistance;

                        workerDistance = GetDistance(closestCancelledWorker.transform.position, currentResource.transform.position)/10;

                        if (workerDistance < currentResource.pathDistance)
                        {
                            closestCancelledWorker.StopAllCoroutines();
                            closestCancelledWorker.targetResource = currentResource;
                            closestCancelledWorker.Uncancel();
                            currentResource.AddWorkerToSlot(closestCancelledWorker);
                        }
                    }
                    else
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

    public void OnGUI()
    {
        if(currentMenu == actionMenu.tower)
        {
            Tower tower = currentResource.GetComponent<Tower>();
            if (tower.canMove)
            {
                buildingInfoText.text = "";
                if (GUI.Button(new Rect(Screen.width - 130, 150, 130, 80), "Move Tower"))
                {
                    currentMenu = actionMenu.towerMove;
                }
            }
            else
            {
                currentMenu = actionMenu.nothing;
            }
        }
        else if(currentMenu == actionMenu.towerMove)
        {
            //display move speed in buildinginfotext, cancel button and information like the tower can't fire while moving
        }
        else if (currentMenu == actionMenu.nothing)
        {
            buildingInfoText.text = "";
            if (GUI.Button(new Rect(Screen.width - 130, 150, 130, 80), "Build Buildings"))
            {
                currentMenu = actionMenu.building;
            }

            if (GUI.Button(new Rect(Screen.width - 130, 230, 130, 80), "Upgrades"))
            {
                currentMenu = actionMenu.upgrading;
            }
            if (GUI.Button(new Rect(Screen.width - 130, 310, 130, 80), "Trade Routes"))
            {
                currentMenu = actionMenu.trading;
            }
        } else if (currentMenu == actionMenu.building)
        {
            if (GUI.Button(new Rect(Screen.width - 130, 150, 130, 80), "Build House"))
            {
                if (BuildingPurchaseable(house))
                {
                    currentMenu = actionMenu.buildBuilding;
                    currentBuildingtoBuild = house;
                    PrintResources();
                }
                else
                {
                    StartCoroutine(FlashText(resourceText, 0.5f, "\nInsufficient Resources"));
                }

            }

            if (GUI.Button(new Rect(Screen.width - 130, 230, 130, 80), "Build Furnace"))
            {
                //resources[resource.wood] -= 50;
                //resources[resource.stone] -= 30;
                if (BuildingPurchaseable(furnace))
                {
                    currentBuildingtoBuild = furnace;
                    currentMenu = actionMenu.buildBuilding;
                    PrintResources();
                }
                else
                {
                    StartCoroutine(FlashText(resourceText, 0.5f, "\nInsufficient Resources"));
                }
            }



            if (GUI.Button(new Rect(Screen.width - 130, 310, 130, 80), "Build Farm"))
            {
                if (BuildingPurchaseable(farm))
                {
                    currentBuildingtoBuild = farm;
                    currentMenu = actionMenu.buildBuilding;
                    PrintResources();
                }

            }

            if (GUI.Button(new Rect(Screen.width - 130, 390, 130, 80), "Build Wall"))
            {

                if (BuildingPurchaseable(wall))
                {
                    currentBuildingtoBuild = wall;
                    currentMenu = actionMenu.buildBuilding;
                    PrintResources();
                }
                else
                {
                    StartCoroutine(FlashText(resourceText, 0.5f, "\nInsufficient Resources"));
                }
            }

            if (GUI.Button(new Rect(Screen.width - 130, 470, 130, 80), "Build Tower"))
            {

                if (BuildingPurchaseable(tower))
                {
                    currentBuildingtoBuild = tower;
                    currentMenu = actionMenu.buildBuilding;
                    PrintResources();
                }
                else
                {
                    StartCoroutine(FlashText(resourceText, 0.5f, "\nInsufficient Resources"));
                }
            }
        }
        else if (currentMenu == actionMenu.trading)
        {
            
            for (int i = 0; i < tradeRouteDestinations.Length; i++)
            {
                if (GUI.Button(new Rect(Screen.width - 130, 150 + (80 * i), 130 , 80), "Start Trade Route " + (i+1)))
                {
                    currentMenu = actionMenu.startTradeRoute;
                    currentTradeRoute = i;
                }

            }
        }
        else if (currentMenu == actionMenu.upgrading)
        {
            
            if (GUI.Button(new Rect(Screen.width - 130, 150, 130, 65), "Build Speed"))
            {
                currentUpgrade = resource.building;
                currentMenu = actionMenu.finaliseUpgrade;
            }

            if (GUI.Button(new Rect(Screen.width - 130, 215, 130, 65), "Food Gather Rate"))
            {
                currentUpgrade = resource.food;
                currentMenu = actionMenu.finaliseUpgrade;
            }
            if (GUI.Button(new Rect(Screen.width - 130, 280, 130, 65), "Wood Gather Rate"))
            {
                currentUpgrade = resource.wood;
                currentMenu = actionMenu.finaliseUpgrade;
            }
            if (GUI.Button(new Rect(Screen.width - 130, 345, 130, 65), "Stone Gather Rate"))
            {
                currentUpgrade = resource.stone;
                currentMenu = actionMenu.finaliseUpgrade;
            }
            if (GUI.Button(new Rect(Screen.width - 130, 410, 130, 65), "Ore Gather Rate"))
            {
                currentUpgrade = resource.ore;
                currentMenu = actionMenu.finaliseUpgrade;
            }
            if (GUI.Button(new Rect(Screen.width - 130, 475, 130, 65), "Woker Speed"))
            {
                currentUpgrade = resource.none;
                currentMenu = actionMenu.finaliseUpgrade;
            }

        }
        else if(currentMenu == actionMenu.buildBuilding)
        {
            buildingDetails = currentBuildingtoBuild.GetComponent<ConstructionSite>();
            string costText = "";
            for (int i = 0; i < buildingDetails.costs.Length; i++)
            {
                costText += buildingDetails.costs[i] + " ";
                costText += buildingDetails.types[i] + "\n";
            }

            buildingInfoText.text = "Building: " + buildingDetails.name + "\n\n" + "Cost:\n" + costText + "\nFunction:\n" + buildingDetails.function + "\n\nClick Empty Space to Place";

            if (GUI.Button(new Rect(Screen.width - 130, 450, 130, 80), "Cancel"))
            {

                currentMenu = actionMenu.nothing;
            }
        }
        else if(currentMenu == actionMenu.startTradeRoute)
        {
            buildingInfoText.text = "\nCosts: \n";
            for (int i = 0; i < costAmounts[currentTradeRoute].Length; i++)
            {
                buildingInfoText.text += costTypes[currentTradeRoute][i].ToString() + ": " + costAmounts[currentTradeRoute][i] + "\n";
            }
            buildingInfoText.text += "\nBenefits: \n";
            for (int i = 0; i < deliverAmounts[currentTradeRoute].Length; i++)
            {
                buildingInfoText.text += deliverTypes[currentTradeRoute][i].ToString() + ": " + deliverAmounts[currentTradeRoute][i] + "\n";
            }
            buildingInfoText.text += "\nDestination: " + tradeRouteDestinations[currentTradeRoute].ToString();

            if (GUI.Button(new Rect(Screen.width - 130, 450, 65, 80), "Start"))
            {
                bool valid = true;
                for (int j = 0; j < costAmounts[currentTradeRoute].Length; j++)
                {
                    if (!IsPurchaseable(new KeyValuePair<resource, int>(costTypes[currentTradeRoute][j], costAmounts[currentTradeRoute][j])))
                    {
                        valid = false;
                    }
                }

                if (valid)
                {
                    //GameObject instance = Instantiate(tradeRouteGO, boardScript.homeBase.transform.position, Quaternion.identity) as GameObject;
                    GameObject instance = SimplePool.Spawn(tradeRouteGO, boardScript.homeBase.transform.position, Quaternion.identity);
                    TradeRoute theTradeRoute = instance.GetComponent<TradeRoute>();
                    theTradeRoute.Init(tradeRouteDestinations[currentTradeRoute], costAmounts[currentTradeRoute], costTypes[currentTradeRoute],
                        deliverAmounts[currentTradeRoute], deliverTypes[currentTradeRoute], 5);
                }
                else
                {
                    StartCoroutine(FlashText(resourceText, 1.0f, "\n\nNot Enough\nResources"));
                }
                currentMenu = actionMenu.nothing;
            }
            if(GUI.Button(new Rect(Screen.width - 65, 450, 65, 80), "Cancel"))
            {
                
                currentMenu = actionMenu.nothing;
            }

        }
        else if(currentMenu == actionMenu.finaliseUpgrade)
        {
            int upgradeNo = -1;
            for (int i = 0; i < upgradeOrder.Length; i++)
            {
                if(upgradeOrder[i] == currentUpgrade)
                {
                    upgradeNo = i;
                    break;
                }
            } 

            buildingInfoText.text = "Upgrade " + currentUpgrade.ToString() + " Gather Rate ";

            buildingInfoText.text += "to collect " + currentUpgrade.ToString() + " 20% faster \n\n";
            buildingInfoText.text += "Costs:\n";
            for (int i = 0; i <  costAmounts[tradeRouteDestinations.Length + upgradeNo].Length; i++)
            {
                buildingInfoText.text += costTypes[tradeRouteDestinations.Length + upgradeNo][i].ToString() + ": " + costAmounts[tradeRouteDestinations.Length + upgradeNo][i] + "\n";
            }
            
            if (GUI.Button(new Rect(Screen.width - 130, 450, 65, 80), "Upgrade"))
            {
                UpdateResourceGatherAmount(currentUpgrade, 1.2f);
                currentMenu = actionMenu.nothing;
            }
            if (GUI.Button(new Rect(Screen.width - 65, 450, 65, 80), "Cancel"))
            {

                currentMenu = actionMenu.nothing;
            }
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

    //needs fixing
    void ControlWorkerCounts()
    {
        GameObject[] resources = GameObject.FindGameObjectsWithTag("Resource");
        foreach (GameObject resource in resources)
        {
            Resources theResource = resource.GetComponent<Resources>();
            if (theResource.workerSlots.Count > 1 && theResource != currentResource)
            {
                for (int i = 0; i < theResource.workerSlots.Count; i++)
                {
                    theResource.workerSlots[i].Cancel();
                    theResource.tempWorkers--;
                }
            }
            if (workers.Count <= totalWorkers)
            {
                continue;
            }
        }
    }

    //SpawnEnemy(spawnPos, enemySpawnDetails[enemySpawnInt + 3]) //last int is enemy type
    void SpawnEnemy(Vector2 position, int enemyType)
    {
        GameObject enemy = Instantiate(enemyTypes[enemyType], position, Quaternion.identity) as GameObject;

        //create generic enemy class which inherits from worker but is a parent of all enemy types
        //overwrite init() from worker;  
    } 

    public bool IsPurchaseable(KeyValuePair<resource, int> pair)
    {
        if (resources[pair.Key] < pair.Value)
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
                if (!IsPurchaseable(new KeyValuePair<resource, int>(site.types[i], site.costs[i] * (returnVal + 3))))
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
        currentMenu = actionMenu.nothing;
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
        currentMenu = actionMenu.nothing;
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

    private void UpdateResourceGatherAmount( resource upgradeType, float mult)
    {
        if(upgradeType == resource.none)
        {
            speedMult *= mult;
            UpdateWorkerMoveSpeed();
            return;
        }
        GameObject[] resources = GameObject.FindGameObjectsWithTag("Resource");
        resourceUpgrades[upgradeType] *= mult;
        foreach (GameObject resource in resources)
        {
            Resources upgradeResource = resource.GetComponent<Resources>();
            if(upgradeResource.resourceType == upgradeType)
                resource.GetComponent<Resources>().AdjustGatherAmount(resourceUpgrades[upgradeType]);
        }
        //resources = GameObject.FindGameObjectsWithTag("BuildingResource");

        //foreach (GameObject resource in resources)
        //{
        //    resource.GetComponent<Resources>().AdjustGatherAmount();
        //}
        currentMenu = actionMenu.nothing;
    }

    private void UpdateWorkerMoveSpeed()
    {
        foreach (Worker worker in workers)
        {
            worker.GetComponent<MovingObject>().UpdateMoveSpeed(speedMult);
        }
        currentMenu = actionMenu.nothing;
    }

    void DestroyWorker(Worker worker, int position)
    {
        //worker.gameObject.SetActive(false);
        
        //if (worker.targetResource == currentResource)  
        //    currentResource.tempWorkers--;
        if(worker.targetResource != null)
        {
            worker.targetResource.RemoveWorkerFromSlot(worker);
            worker.targetResource.tempWorkers--;
        }
            
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
                if(worker.GetComponent<Entity>().currentHP <= 0)
                {
                    totalWorkers--;
                }
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

    public Vector2 GetCloseBuilding(Vector2 resource)
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

    void UpdateHighlightedText(Resources highlightedResource)
    {
        string resourceType = "";
        if (highlightedResource != null)
        {
            if (highlightedResource.resourceType == resource.building)
            {
                resourceType = highlightedResource.GetComponent<ConstructionSite>().name;
            }
            else
            {
                resourceType = highlightedResource.resourceType.ToString();
            }

            //typical resource
            if(highlightedResource.gameObject.tag == "Resource")
            {
                highlightedText.text = "Resource\nType: " + resourceType + "\nRemaining: " +
                    (int)Mathf.Round(highlightedResource.resourcesRemaining) + "\nWorkers: " + highlightedResource.workerSlots.Count +
                    "\nGather Rate: " + highlightedResource.gatherAmount + "\nGather Time: " + highlightedResource.gatherTime;
            }
            else if (highlightedResource.gatherAmount > 0)
            {
                highlightedText.text = "Resource\nType: " + resourceType + "\nWorkers: " + highlightedResource.workerSlots.Count +
                    "\nGather Rate: " + highlightedResource.gatherAmount + "\nGather Time: " + highlightedResource.gatherTime;
            }
            else
            {
                highlightedText.text = "Resource\nWorkers: " + highlightedResource.workerSlots.Count +
                    "\nDamage: " + highlightedResource.GetComponent<Entity>().minDmg + " - " + highlightedResource.GetComponent<Entity>().maxDmg + "\nAttack Spd: " + highlightedResource.gatherTime
                    + "\nHitPoints: " + highlightedResource.GetComponent<Entity>().currentHP;
            }
        }
        else
            highlightedText.text = "Resource\nType: \nRemaining: \nWorkers: \nGather Rate: \nGather Time: ";

    }

    public void HighlightResource(RaycastHit2D clickPosition)
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
        if(currentResource.GetComponent<Tower>() != null)
        {
            currentMenu = actionMenu.tower;
        }
        //}
    }

    void CreateWorkers(Resources resource, int amount)
    {
        
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

    //todo: round the start and end vectors! this should fix the worker path finding after a building is built
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
        hit = Physics2D.Linecast(temp, end);
        if(hit.collider != null)
            path.Add(endNode.position);
        
        path.Add(temp);
        distance += GetDistance(temp, endNode.position);
        //Instantiate(boardScript.gold, temp, Quaternion.identity);

        do
        {
            tempParent = currentNode.parent.position;
            hit = Physics2D.Linecast(temp, tempParent);
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
                        if ((hitTag == "Building" || hitTag == "BuildingResource" || hitTag == "Fort") && dps > 0)
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
