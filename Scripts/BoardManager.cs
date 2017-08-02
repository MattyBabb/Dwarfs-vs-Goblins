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

        public Count(int min, int max)
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
    public GameObject[] gold;
    public GameObject[] Castle;

    private Transform boardHolder;
    public List<Vector2> gridPositions = new List<Vector2>();
    public Vector2 marker;
    [HideInInspector]
    public GameObject homeBase;


    void initialiseList()
    {
        gridPositions.Clear();
        for (int x = 0; x < columns + 1; x++)
        {
            for (int y = 0; y < rows + 1; y++)
            {
                gridPositions.Add(new Vector2(x, y));
            }
        }
    }

    void BoardSetup()
    {
        boardHolder = new GameObject("Board").transform;
        for (int x = 0; x < (columns + 1)*2; x++)
        {
            for (int y = 0; y < rows + 1; y++)
            {
                GameObject toInstantiate = grassTiles[Random.Range(0, grassTiles.Length)];
                GameObject instance = Instantiate(toInstantiate, new Vector2(x, y), Quaternion.identity) as GameObject;
                instance.transform.SetParent(boardHolder);
            }
        }
    }

    Vector2 RandomPosition()
    {
        int randomIndex = Random.Range(0, gridPositions.Count);
        Vector2 randomPosition = gridPositions[randomIndex];
        gridPositions.RemoveAt(randomIndex);
        return randomPosition;
    }

    void LayoutObjectAtRandomLeftBorder(GameObject[] tile)
    {
        int randomIndex = Random.Range(0, gridPositions.Count);
        Vector2 randomPosition = gridPositions[randomIndex];
        while (randomPosition.x != 0f)
        {
            randomIndex = Random.Range(0, gridPositions.Count);
            randomPosition = gridPositions[randomIndex];
        }
        gridPositions.RemoveAt(randomIndex);
        Instantiate(tile[0], randomPosition, Quaternion.identity);
        MirrorObject(randomPosition, tile[0]);
    }

    void layoutObjectAtRandom(GameObject[] tileArray, int min, int max)
    {
        int objectCount = Random.Range(min, max + 1);
        for (int y = 0; y < objectCount; y++)
        {
            Vector2 randomPostion = RandomPosition();
            GameObject tileChoice = tileArray[Random.Range(0, tileArray.Length)];
            Instantiate(tileChoice, randomPostion, Quaternion.identity);
            MirrorObject(randomPostion, tileChoice);
        }
    }

    void MirrorObject(Vector2 position, GameObject tile)
    {
        position.x = (columns * 2) + 1 - position.x;
        Instantiate(tile, position, Quaternion.identity);
    }


    public void SetupScene(int level)
    {
        BoardSetup();
        initialiseList();
        LayoutObjectAtRandomLeftBorder(Castle);
        homeBase = Castle[0];
        layoutObjectAtRandom(waterTiles, waterCount.minimum, waterCount.maximum);
        layoutObjectAtRandom(stoneTiles, stoneCount.minimum, stoneCount.maximum);
        layoutObjectAtRandom(treeTiles, treeCount.minimum, treeCount.maximum);
        layoutObjectAtRandom(mountainTiles, mountainCount.minimum, mountainCount.maximum);
        layoutObjectAtRandom(gold, 1, 1);
    }
}
