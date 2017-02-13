using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node : IHeapItem<Node>
{
    public int movementPenalty;
    public Vector2 position;
    public int gCost, hCost;
    public Node parent;
    int heapIndex;

    public int fCost
    {
        get
        {
            return gCost + hCost;
        }
    }

    public Node(int xVal, int yVal, int _penalty)
    {
        position = new Vector2(xVal, yVal);
        movementPenalty = _penalty;
    }

    public int HeapIndex
    {
        get
        {
            return heapIndex;
        }
        set
        {
            heapIndex = value;
        }
    }

    public int CompareTo(Node nodeToCompare)
    {
        int compare = fCost.CompareTo(nodeToCompare.fCost);
        if(compare == 0)
        {
            compare = hCost.CompareTo(nodeToCompare.hCost);
        }
        return -compare;
    }
}
