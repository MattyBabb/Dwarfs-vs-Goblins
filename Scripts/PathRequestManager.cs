using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class PathRequestManager : MonoBehaviour {

    Queue<PathRequest> pathRequestQueue = new Queue<PathRequest>();
    PathRequest currentPathRequest;

    static PathRequestManager instance;

    bool isProcessingPath;
    GameManager gameManager;

    void Awake()
    {
        instance = this;
        gameManager = GetComponent<GameManager>();
    }

    public static void RequestPath( Vector2 pathStart, Vector2 pathEnd, int dps, Action<List<Vector2>, bool, float> callback)
    {
        PathRequest newRequest = new PathRequest(pathStart, pathEnd, dps, callback);
        instance.pathRequestQueue.Enqueue(newRequest);
        instance.TryProcessNext();
    }

    void TryProcessNext()
    {
        if(!isProcessingPath && pathRequestQueue.Count > 0)
        {
            currentPathRequest = pathRequestQueue.Dequeue();
            isProcessingPath = true;
            gameManager.StartFindPath(currentPathRequest.pathStart, currentPathRequest.pathEnd);
        }
    }

    public void FinishedProcessingPath(List<Vector2> path, bool success, float distance)
    {
        currentPathRequest.callback(path, success, distance);
        isProcessingPath = false;
        TryProcessNext();
    }

    struct PathRequest
    {
        public Vector2 pathStart;
        public Vector2 pathEnd;
        public int dps;
        public Action<List<Vector2>, bool, float> callback;

        public PathRequest(Vector2 _start, Vector2 _end, int _dps, Action<List<Vector2>, bool, float> _callback)
        {
            pathStart = _start;
            pathEnd = _end;
            dps = _dps;
            callback = _callback;
        }
    }
}
