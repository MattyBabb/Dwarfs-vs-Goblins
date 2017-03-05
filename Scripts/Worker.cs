using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public enum state
{
    movingToResource, gathering, inBattle, cancel, movingHome, pathEnded 
}

public class Worker : MonoBehaviour
{
    [HideInInspector]
    public Resources targetResource;
    bool gathering, movingToResource, movingHome, pathEnded;
    state currentState; 
    [HideInInspector] public bool cancel, destroy;
    [HideInInspector] public float heldResourceAmount = 0;
    public float moveSpeed = .5f;
    [HideInInspector]
    public SpriteRenderer sprite;
    [HideInInspector]
    public Animator anim;
    private float baseMoveSpeed = .5f;
    [HideInInspector]
    public BoxCollider2D boxCollider;
    [HideInInspector]
    public Rigidbody2D rb2D;
    float gatherTimer = 0;
    int gatherCounter = 0;
    protected List<Vector2> path;
    protected Vector2[] locations;
    private resource heldResourceType;
    GameObject engagedEnemy;

    void Awake()
    {
        anim = GetComponent<Animator>();
        sprite = GetComponent<SpriteRenderer>();
        boxCollider = GetComponent<BoxCollider2D>();
        rb2D = GetComponent<Rigidbody2D>();
        anim.SetBool("WorkerGather", false);
        path = new List<Vector2>();
        UpdateMoveSpeed(GameManager.instance.speedMult);
        gathering = false;
        cancel = false;
        destroy = false;
        movingToResource = false;
    }

    public void Init(Resources aResource)
    {
        gathering = false;
        cancel = false;
        destroy = false;
        movingToResource = true;
        movingHome = false;
        pathEnded = false;
        //path.Clear();
        targetResource = aResource;
        if (targetResource != null)
        {
            StopAllCoroutines();
            path = targetResource.path;
            transform.position = path[0];
            StartCoroutine(SmoothMovement(path, false));
            IsMovingAnimation(true);
        }
        //GetComponent<Entity>().CreateEntity();
    }

    public void Uncancel()
    {
        gathering = false;
        cancel = false;
        destroy = false;
        movingToResource = true;
        movingHome = false;
        targetResource = GameManager.instance.currentResource;
        if (targetResource != null)
        {
            StopAllCoroutines();
            PathRequestManager.RequestPath(targetResource.transform.position, new Vector2[] { transform.position }, 0, false, OnPathFound);
        }
    }

    protected void OnPathFound(List<Vector2> newPath, bool pathSuccessful, float distance)
    {
        if (pathSuccessful && this != null && this.isActiveAndEnabled)
        {
            path = newPath;
            transform.position = path[0];
            StopAllCoroutines();
            if (path.Count > 0)
            {
                StartCoroutine(SmoothMovement(path, false));
                IsMovingAnimation(true);
            }
            else
            {
                DestroyObject(this);
            }
        }
    }

    public void EnterCombat()
    {
        StopAllCoroutines();
    }

    public void UpdateMoveSpeed( float multiplier)
    {
        moveSpeed = multiplier * baseMoveSpeed;
    }

    protected IEnumerator SmoothMovement (List<Vector2> ends, bool reversed)
    {
        IsMovingAnimation(true);
        float xDir = 0, yDir = 0;
        if (reversed)
        {
            for (int i = ends.Count-1; i >= 0; i--)
            {
                float squRemainingDistance = (transform.position - new Vector3(ends[i].x, ends[i].y, 0)).sqrMagnitude;
                //float totalDistance = squRemainingDistance;

                while (squRemainingDistance > float.Epsilon)
                {
                    xDir = ends[i].x - transform.position.x;
                    yDir = ends[i].y - transform.position.y;

                    //flip sprite if moving right 
                    if (xDir > 0)
                        sprite.flipX = true;
                    else
                        sprite.flipX = false;

                    Vector2 newPosition = Vector2.MoveTowards(transform.position, ends[i], moveSpeed * Time.deltaTime);
                    rb2D.MovePosition(newPosition);
                    squRemainingDistance = (transform.position - new Vector3(ends[i].x, ends[i].y, 0)).sqrMagnitude;

                    yield return null;
                }
            }
        }
        else
        {
            foreach (Vector2 end in ends)
            {
                float squRemainingDistance = (transform.position - new Vector3(end.x, end.y, 0)).sqrMagnitude;
                //float totalDistance = squRemainingDistance;

                while (squRemainingDistance > float.Epsilon)
                {
                    xDir = end.x - transform.position.x;
                    yDir = end.y - transform.position.y;

                    //flip sprite if moving right 
                    if (xDir > 0)
                        sprite.flipX = true;
                    else
                        sprite.flipX = false;

                    Vector2 newPosition = Vector2.MoveTowards(transform.position, end, moveSpeed * Time.deltaTime);
                    rb2D.MovePosition(newPosition);
                    squRemainingDistance = (transform.position - new Vector3(end.x, end.y, 0)).sqrMagnitude;

                    yield return null;
                }
            }
        }
        pathEnded = true;
    }

    private void Gather()
    {
        if(targetResource.resourceType != resource.building)
        {

            heldResourceAmount = (targetResource.gatherAmount / (targetResource.gatherTime / gatherCounter));
            targetResource.ReduceResources((targetResource.gatherAmount / (targetResource.gatherTime)));
            if (gatherTimer >= targetResource.gatherTime)
            {
                heldResourceAmount = targetResource.gatherAmount;
                gathering = false;
                movingHome = true;
                pathEnded = false;
            }
            if (heldResourceAmount > 0)
                heldResourceType = targetResource.resourceType;
            
        }
        else if (targetResource.resourceType == resource.building)
        {
            targetResource.ReduceResources(targetResource.gatherAmount);
            gatherTimer = 0;
        }
    }

    public void IsMovingAnimation(bool moving)
    {
        if (moving)
        {
            anim.SetBool("WorkerGather", false);
            gatherTimer = 0;
            gatherCounter = 1;
        }
        else
            anim.SetBool("WorkerGather", true);
    }

    void DepositResources()
    {
        
        GameManager.instance.AddResources(new KeyValuePair<resource, int>(heldResourceType, (int)heldResourceAmount));
        heldResourceAmount = 0;
    }

    public void Cancel()
    {
        StopAllCoroutines();
        locations = Array.ConvertAll(GameObject.FindGameObjectsWithTag("Building"), item => new Vector2(item.transform.position.x, item.transform.position.y));
        PathRequestManager.RequestPath(transform.position, locations, 0, true, OnPathFound);
        gathering = false;
        movingToResource = false;
        movingHome = false;
        cancel = true;
        if(targetResource != null)
        { 
            targetResource.workerSlots.Remove(this);
        }
    }
	
	void Update ()
    {
        if (isActiveAndEnabled)
        {
            if (movingHome)
            {
                //move to target building
                StartCoroutine(SmoothMovement(targetResource.path, true));
                IsMovingAnimation(true);
                movingHome = false;
            }
            else if(movingToResource)
            {
                //move to target resource
                StartCoroutine(SmoothMovement(targetResource.path, false));
                IsMovingAnimation(true);
                movingToResource = false;
            }
            else if (!gathering && targetResource != null && (targetResource.transform.position - transform.position).sqrMagnitude < float.Epsilon && heldResourceAmount < targetResource.gatherAmount)
            {
                gathering = true;
                movingToResource = false;
                IsMovingAnimation(false);
                StopAllCoroutines();
                //path.Clear();
            }
            else if (gathering)
            {
                if (targetResource.resourcesRemaining <= 0)
                {
                    Cancel();
                }
                else
                {
                    gatherTimer += Time.deltaTime;
                    if (gatherTimer >= gatherCounter && targetResource.resourcesRemaining > 0)
                    {
                        gatherCounter++;
                        Gather();
                    }
                }
            }
            else if (pathEnded)
            {
                pathEnded = false;
                StopAllCoroutines();
                if (heldResourceAmount > 0)
                {
                    DepositResources();
                }
                if (cancel)
                {
                    destroy = true;
                    GameManager.instance.ProcessWorkers();
                }
                else if (targetResource.resourcesRemaining <= 0)
                {
                    destroy = true;
                    GameManager.instance.ProcessWorkers();
                }
                else //if ((new Vector3(path[path.Count - 1].x, path[path.Count - 1].y, 0) - transform.position).sqrMagnitude < float.Epsilon)
                {
                    movingToResource = true;
                    //path.Clear();
                }
            }
        }
    }
}