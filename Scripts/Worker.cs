using UnityEngine;
using System.Collections;

public class Worker : MonoBehaviour
{
    public Resources targetResource;
    public bool gathering;
    [HideInInspector] public float heldResourceAmount = 0;
    public bool cancel;

    private SpriteRenderer sprite;
    private Animator anim;
    private float moveSpeed = .5f;
    private BoxCollider2D boxCollider;
    private Rigidbody2D rb2D;
    private float gatherTimer = 0;
    private int gatherCounter = 0;
    
    //private string heldResourceType;

    // Use this for initialization
    void Awake()
    {
        anim = GetComponent<Animator>();
        sprite = GetComponent<SpriteRenderer>();
        boxCollider = GetComponent<BoxCollider2D>();
        rb2D = GetComponent<Rigidbody2D>();
        anim.SetBool("WorkerGather", false);
        targetResource = null;
        gathering = false;
        cancel = false;
    }

    public void Move(Transform destination)
    {
        IsMovingAnimation(true);
        float xDir = 0, yDir = 0;
        xDir = destination.position.x - transform.position.x;
        yDir = destination.position.y - transform.position.y;    

        //flip sprite if moving right
        if (xDir > 0)
            sprite.flipX = true;
        else
            sprite.flipX = false;
        
        StartCoroutine(SmoothMovement(destination.position));

    }

    protected IEnumerator SmoothMovement (Vector3 end )
    {
        float squRemainingDistance = (transform.position - end).sqrMagnitude;
        float totalDistance = squRemainingDistance;
        while(squRemainingDistance > float.Epsilon)
        {
            Vector3 newPosition = Vector3.MoveTowards(rb2D.position, end, moveSpeed * Time.deltaTime);
            rb2D.MovePosition(newPosition);
            squRemainingDistance = (transform.position - end).sqrMagnitude;                

            yield return null;
        }
    }

    private void Gathering()
    {
        if(targetResource.resourceType != "Building")
        {
            if (gatherTimer >= gatherCounter)
            {
                heldResourceAmount = (targetResource.gatherAmount / (targetResource.gatherTime / gatherCounter));
                targetResource.ReduceResources((targetResource.gatherAmount / (targetResource.gatherTime)));
                gatherCounter++;
                if (gatherTimer >= targetResource.gatherTime)
                {
                    Move(FindClosestBuilding().transform);
                    IsMovingAnimation(true);
                    heldResourceAmount = targetResource.gatherAmount;
                    //targetResource.ReduceResources(targetResource.gatherAmount);
                }
            }
            else if (targetResource.resourcesRemaining <= 0)
            {
                Move(FindClosestBuilding().transform);
                IsMovingAnimation(true);
            }
        }
        else if (targetResource.resourceType == "Building")
        {
            if(gatherTimer >= targetResource.gatherTime)
            {
                targetResource.ReduceResources(targetResource.gatherAmount);
                gatherTimer = 0;
            }
            else if (targetResource.resourcesRemaining <= 0)
            {
                Move(FindClosestBuilding().transform);
                IsMovingAnimation(true);
            }
        }

    }

    private void IsMovingAnimation(bool moving)
    {
        if (moving)
        {
            anim.SetBool("WorkerGather", false);
            gathering = false;
            gatherTimer = 0;
            gatherCounter = 1;
        }
        else
            anim.SetBool("WorkerGather", true);
    }

    private GameObject FindClosestBuilding()
    {
        GameObject[] buildings;
        GameObject closestBuilding = null;

        buildings = GameObject.FindGameObjectsWithTag("Building");

        foreach (GameObject building in buildings)
        {
            if (closestBuilding == null)
                closestBuilding = building;
            else if ((transform.position - building.transform.position).sqrMagnitude < (transform.position - closestBuilding.transform.position).sqrMagnitude)
                closestBuilding = building;
        }
        return closestBuilding;
    }

    public void Cancel()
    {
        if (gameObject.activeSelf)
        {
            cancel = true;
            StopAllCoroutines();
            Move(FindClosestBuilding().transform);
            IsMovingAnimation(true);
        }
    }
	
	void Update ()
    {

	    if(targetResource != null && !cancel)
        {
            if (gathering)
            {
                gatherTimer += Time.deltaTime;
                Gathering();

            }
            else if ((transform.position - targetResource.transform.position).sqrMagnitude < float.Epsilon && heldResourceAmount < targetResource.gatherAmount)
            {

                gathering = true;
                IsMovingAnimation(false);
            }
        }
	}
}
