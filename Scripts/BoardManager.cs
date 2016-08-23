using UnityEngine;
using System;
using Random = UnityEngine.Random;
using System.Collections.Generic;

public class BoardManager : MonoBehaviour
{
    [Serializable]
    public class Count
    {
        public int minimum, maximum;

        public Count (int min, int max)
        {
            minimum = min;
            maximum = max;
        }
    }

    public int columns;
    public int rows;
    public Count mountainCount = new Count(6, 10);
    public Count waterCount = new Count(10, 18);
    public Count treeCount = new Count(6, 10);
    public Count stoneCount = new Count(3, 6);
    public GameObject[] grassTiles;
    public GameObject[] waterTiles;
    public GameObject[] treeTiles;
    public GameObject[] mountainTiles;
    public GameObject[] playerBase;
    public GameObject[] stoneTiles;
    public GameObject gold;

    private Transform boardHolder;
    private List<Vector3> gridPositions = new List<Vector3>();

    void initialiseList()
    {
        gridPositions.Clear();
        for (int x = 0; x < columns + 1; x++)
        {
            for (int y = 0; y < rows + 1; y++)
            {
                gridPositions.Add(new Vector3(x, y, 0f));
            }
        }
    }

    void BoardSetup()
    {
        boardHolder = new GameObject("Board").transform;
        for(int x = 0; x < columns +1; x++)
        {
            for (int y = 0; y < rows + 1; y++)
            {
                GameObject toInstantiate = grassTiles[Random.Range(0, grassTiles.Length)];
                GameObject instance = Instantiate(toInstantiate, new Vector3(x, y, 0f), Quaternion.identity) as GameObject;
                instance.transform.SetParent(boardHolder);
            }
        }
    }

    Vector3 RandomPosition()
    {
        int randomIndex = Random.Range(0, gridPositions.Count);
        Vector3 randomPosition = gridPositions[randomIndex];
        gridPositions.RemoveAt(randomIndex);
        return randomPosition;
    }

    void layoutObjectAtRandom(GameObject[] tileArray, int min, int max)
    {
        int objectCount = Random.Range(min, max + 1);
        for (int y = 0; y < objectCount; y++)
        {
            Vector3 randomPostion = RandomPosition();
            GameObject tileChoice = tileArray[Random.Range(0, tileArray.Length)];
            Instantiate(tileChoice, randomPostion, Quaternion.identity);
        }
    }

    void PlaceGoldInOppositeCorner(Transform trans, GameObject gold)
    {
        int gridPos = (int)(trans.position.x * trans.position.y);
        int randomIndex;
        Vector3 randomPos;
        bool acceptable = false;
        int i = 0;
        int distance = 2;
        do
        {
            randomIndex = Random.Range(0, gridPositions.Count);
            randomPos = gridPositions[randomIndex];
            if ((Math.Abs(randomPos.x - trans.position.x) + Math.Abs(randomPos.y - trans.position.y)) > ((columns + rows) / distance))
            {
                acceptable = true;
            }
            i++;
            if(i > 100)
            {
                distance++;
            }
        }
        while (!acceptable);
        gridPositions.RemoveAt(randomIndex);
        Instantiate(gold, randomPos, Quaternion.identity);
    }

    Transform GetTransform (string tag)
    {
        GameObject house = GameObject.FindGameObjectWithTag(tag);
        return house.transform;
    }

	public void SetupScene ()
    {
        BoardSetup();
        initialiseList();
        layoutObjectAtRandom(waterTiles, waterCount.minimum, waterCount.maximum);
        layoutObjectAtRandom(stoneTiles, stoneCount.minimum, stoneCount.maximum);
        layoutObjectAtRandom(treeTiles, treeCount.minimum, treeCount.maximum);
        layoutObjectAtRandom(mountainTiles, mountainCount.minimum, mountainCount.maximum);
        layoutObjectAtRandom(playerBase, 1, 1);
        Transform trans = GetTransform("Building");
        PlaceGoldInOppositeCorner(trans, gold);
    }
}
