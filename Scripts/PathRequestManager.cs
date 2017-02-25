using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class PathRequestManager : MonoBehaviour {

    Queue<PathRequest> pathRequestQueue = new Queue<PathRequest>();
    PathRequest currentPathRequest;

    static PathRequestManager instance;

    bool isProcessingPath;

    void Awake()
    {
        instance = this;
    }

    public static void RequestPath( Vector2 pathStart, Vector2[] pathEnds, int dps, bool toBuilding, Action<List<Vector2>, bool, float> callback)
    {
        PathRequest newRequest = new PathRequest(pathStart, pathEnds, dps, toBuilding, callback);
        instance.pathRequestQueue.Enqueue(newRequest);
        instance.TryProcessNext();
    }

    void TryProcessNext()
    {
        if(!isProcessingPath && pathRequestQueue.Count > 0)
        {
            currentPathRequest = pathRequestQueue.Dequeue();
            isProcessingPath = true;
            GameManager.instance.StartFindPath(currentPathRequest.pathStart, currentPathRequest.pathEnds, currentPathRequest.dps, currentPathRequest.toBuilding);
        }
    }

    public void FinishedProcessingPath(List<Vector2> path, float distance, bool success)
    {
        currentPathRequest.callback(path, success, distance);
        isProcessingPath = false;
        TryProcessNext();
    }

    struct PathRequest
    {
        public Vector2 pathStart;
        public Vector2[] pathEnds;
        public int dps;
        public bool toBuilding;
        public Action<List<Vector2>, bool, float> callback;

        public PathRequest(Vector2 _start, Vector2[] _ends, int _dps, bool _toBuilding,  Action<List<Vector2>, bool, float> _callback)
        {
            toBuilding = _toBuilding;
            pathStart = _start;
            pathEnds = _ends;
            dps = _dps;
            callback = _callback;
        }
    }
}
