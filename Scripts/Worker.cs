using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Worker : Entity
{
    public Resources targetResource;
    public bool gathering, movingToResource, movingHome, cancel;
    [HideInInspector] public float heldResourceAmount = 0;
    public float moveSpeed = .5f;
    public bool destroy;

    public SpriteRenderer sprite;
    public Animator anim;
    private float baseMoveSpeed = .5f;
    public BoxCollider2D boxCollider;
    public Rigidbody2D rb2D;
    private float gatherTimer = 0;
    private int gatherCounter = 0;
    //float pathDistance;
    public List<Vector2> path;
    GameObject[] buildings;
    GameObject closestBuilding;
    int targetIndex;


    private resource heldResourceType;

    // Use this for initialization
    void Awake()
    {
        anim = GetComponent<Animator>();
        sprite = GetComponent<SpriteRenderer>();
        boxCollider = GetComponent<BoxCollider2D>();
        rb2D = GetComponent<Rigidbody2D>();
        anim.SetBool("WorkerGather", false);
        path = new List<Vector2>();
        UpdateMoveSpeed(GameManager.instance.speedMult);
    }

    public void Init(List<Vector2> passedPath)
    {
        gathering = false;
        cancel = false;
        destroy = false;
        path.Clear();
        MoveToTargetResource(passedPath);
        //PathRequestManager.RequestPath(transform.position, targetResource.transform.position, OnPathFound);
    }

    //public void OnPathFound(List<Vector2> newPath, bool pathSuccessful, float distance)
    //{
    //    if (pathSuccessful)
    //    {
    //        path = newPath;
    //        Move(path);
    //        IsMovingAnimation(true);
    //    }
    //    else
    //    {
    //        DestroyObject(this);
    //    }
    //}

    public void Move(List<Vector2> destinations)
    {
        if(destinations.Count > 0)
        {
            IsMovingAnimation(true);
            foreach (Vector2 destination in destinations)
            {
                StartCoroutine(SmoothMovement(destination));
            }
        }
        else
        {
            DestroyObject(this);
        }
    }

    public void UpdateMoveSpeed( float multiplier)
    {
        moveSpeed = multiplier * baseMoveSpeed;
    }

    protected IEnumerator SmoothMovement (Vector3 end )
    {
        float xDir = 0, yDir = 0;
        

        float squRemainingDistance = (transform.position - end).sqrMagnitude;
        float totalDistance = squRemainingDistance;

        while(squRemainingDistance > float.Epsilon)
        {
            xDir = end.x - transform.position.x;
            yDir = end.y - transform.position.y;

            //flip sprite if moving right
            if (xDir > 0)
                sprite.flipX = true;
            else
                sprite.flipX = false;

            Vector3 newPosition = Vector3.MoveTowards(rb2D.position, end, moveSpeed * Time.deltaTime);
            rb2D.MovePosition(newPosition);
            squRemainingDistance = (transform.position - end).sqrMagnitude;                

            yield return null;
        }
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

    public void MoveToTargetResource()
    {
        gathering = false;
        StopAllCoroutines();
        path.Clear();
        buildings = GameObject.FindGameObjectsWithTag("Building");
        closestBuilding = GameManager.instance.GetClosestBuilding(buildings, targetResource.transform.position, false, out path);
        if (path != null)
        {
            Move(path);
            IsMovingAnimation(true);
        }
        else
        {
            Cancel();
        }
    }

    public void MoveToTargetResource(List<Vector2> passedPath)
    {
        gathering = false;
        StopAllCoroutines();
        path = passedPath;
        if (path != null)
        {
            Move(path);
            IsMovingAnimation(true);
        }
    }

    public void MoveToNearestBuilding()
    {
        gathering = false;
        StopAllCoroutines();
        path.Clear();
        buildings = GameObject.FindGameObjectsWithTag("Building");
        closestBuilding = GameManager.instance.GetClosestBuilding(buildings, transform.position, true, out path);
        if (path != null)
        {
            Move(path);
            IsMovingAnimation(true);
        }
		else
			Cancel();
    }

    void DepositResources()
    {
        
        GameManager.instance.AddResources(new KeyValuePair<resource, int>(heldResourceType, (int)heldResourceAmount));
        heldResourceAmount = 0;
    }

    public void Cancel()
    {
        StopAllCoroutines();
        path.Clear();
        gathering = false;
        cancel = true;
        if(targetResource != null)
        { 
            targetResource.workerSlots.Remove(this);
        }
    }
	
	void Update ()
    {

        if(!gathering && path.Count <= 0)
        {
            if(movingHome || cancel)
            {
                MoveToNearestBuilding();
            }
            else
            {
                MoveToTargetResource();
            }
        }
        else if (movingToResource && (targetResource.transform.position - transform.position).sqrMagnitude < float.Epsilon && !gathering)
        {
            gathering = true;
			movingToResource = false;
			IsMovingAnimation(false);
			path.Clear();
        }
        else if (gathering && heldResourceAmount != targetResource.gatherAmount)
        {
            if(targetResource.resourcesRemaining <= 0)
            {
                Cancel();
            }
            else
            {
                gatherTimer += Time.deltaTime;
				if(gatherTimer >= gatherCounter)
				{
					gatherCounter++;
					Gather();
				}
            }
        }

        if (movingHome && (closestBuilding.transform.position - transform.position).sqrMagnitude < float.Epsilon)
        {
            if(heldResourceAmount > 0)
            {
                DepositResources();
            }
            if (cancel)
                destroy = true;
            else if (targetResource.resourcesRemaining <= 0)
            {
                Cancel();
                destroy = true;
            }
            else if ((closestBuilding.transform.position - transform.position).sqrMagnitude < float.Epsilon)
            {
                MoveToTargetResource();
            }
        }
    }
}
