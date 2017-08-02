using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public enum state
{
    movingToTarget, gathering, inBattle, cancel, movingHome, pathEnded, inBuilding, stunned, frozen, nothing 
}

public class Worker : MonoBehaviour
{
    [HideInInspector]
    public Animator anim;
    public float attackRange;
    [HideInInspector]
    public Resources targetResource;
    [HideInInspector]
    public GameObject target;
    [HideInInspector] public state currentState; 
    [HideInInspector] public bool cancel, destroy;
    [HideInInspector] public float heldResourceAmount = 0;
    float timer = 0;
    int gatherCounter;
    protected Vector2[] locations;
    private resource heldResourceType;
    Entity thisEntity;
    protected MovingObject thisMovingObject;
    protected SpriteRenderer sprite;

    void Awake()
    {
        sprite = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        currentState = state.nothing;
        cancel = false;
        destroy = false;
        thisEntity = GetComponent<Entity>();
        thisMovingObject = GetComponent<MovingObject>();
        gatherCounter = 1;
    }

    public void Init(Resources aResource)
    {
        thisMovingObject.UpdateMoveSpeed(GameManager.instance.speedMult);
        cancel = false;
        destroy = false;
        currentState = state.movingToTarget;
        //path.Clear();
        targetResource = aResource;
        timer = 0;
        gatherCounter = 1;
        if (targetResource != null)
        {
            thisMovingObject.StopAllCoroutines();
            //path = targetResource.path;
            transform.position = targetResource.path[0];
            thisMovingObject.MoveOnPath(targetResource.path, false);
            //IsMovingAnimation(true);
        }
        thisEntity.currentHP = thisEntity.maxHP;
        //GetComponent<Entity>().CreateEntity();
    }

    public void Uncancel()
    {
        cancel = false;
        destroy = false;
        currentState = state.movingToTarget;
        targetResource = GameManager.instance.currentResource;
        if (targetResource != null)
        {
            thisMovingObject.MoveToObject(targetResource.gameObject);
        }
    }

    public void EnterCombat(Enemy enemy)
    {
        thisMovingObject.StopAllCoroutines();
        currentState = state.inBattle;
        timer = 0f;
        target = enemy.gameObject;
        anim.SetBool("Move", false);
    }

    public void SpriteEnabled(bool enable)
    {
        sprite.enabled = enable;
    }

    public void BecomeFrozen(float duration)
    {
        //play frozen animation
        currentState = state.frozen;
        Invoke("ResetState", duration);
    }

    public void ResetState()
    {
        currentState = state.nothing;
    }

    private void Gather()
    {
        if(targetResource.resourceType != resource.building)
        {
            
            heldResourceAmount = (targetResource.gatherAmount / (targetResource.gatherTime / gatherCounter));
            targetResource.ReduceResources((targetResource.gatherAmount / (targetResource.gatherTime)));
            if (timer >= targetResource.gatherTime)
            {
                heldResourceAmount = targetResource.gatherAmount;
            }
            if (heldResourceAmount > 0)
                heldResourceType = targetResource.resourceType;
            
        }
        else if (targetResource.resourceType == resource.building)
        {

            targetResource.ReduceResources(targetResource.gatherAmount);
        }
    }

    void DepositResources()
    {
        
        GameManager.instance.AddResources(new KeyValuePair<resource, int>(heldResourceType, (int)heldResourceAmount));
        heldResourceAmount = 0;
    }

    GameObject GetClosestResourceDistance(string resourceName, out float distance)
    {
        GameObject[] gos = GameObject.FindGameObjectsWithTag("Resource");
        List<GameObject> egos = new List<GameObject>();
        GameObject closest = null;
        distance = Mathf.Infinity;
        Vector3 position = transform.position;

        foreach (GameObject ex in gos)
        {
            if (ex.GetComponent<ConstructionSite>() != null)
            {
                if(ex.GetComponent<ConstructionSite>().name == resourceName)
                    egos.Add(ex);
            }
        }

        foreach (GameObject go in egos)
        {
            Vector3 diff = go.transform.position - position;
            float curDistance = diff.sqrMagnitude;
            if (curDistance < distance && distance > 0)
            {
                closest = go;
                distance = curDistance;
            }
        }

        return closest;
    }

    public void Cancel()
    {
        //StopAllCoroutines();
        //locations = Array.ConvertAll(GameObject.FindGameObjectsWithTag("Building"), item => new Vector2(item.transform.position.x, item.transform.position.y));
        //PathRequestManager.RequestPath(transform.position, locations, 0, true, OnPathFound);
        //MoveToClosestObject(GameObject[] targets)
        thisMovingObject.MoveToClosestObject(GameObject.FindGameObjectsWithTag("Building"));
        cancel = true;
        currentState = state.movingHome;
        anim.SetBool("Move", true);
        if (targetResource != null)
        {
            //if(targetResource.name == "House")
            //{
            //    destroy = true;
            //}    
            targetResource.RemoveWorkerFromSlot(this);
            
        }
    }
	
	void Update ()
    {
        if (isActiveAndEnabled)
        {
            if (currentState == state.nothing)
            {
                thisMovingObject.StopAllCoroutines();
                //IsMovingAnimation(false);
                anim.SetBool("Move", true);
            }
            else if (currentState == state.inBattle)
            {
                //thisMovingObject.StopAllCoroutines();
                //anim.SetBool("Move", false);
                timer += Time.deltaTime;
                if (thisEntity != null && thisEntity.attackFrequency < timer)
                {
                    thisEntity.Attack(target.GetComponent<Entity>());
                    timer = 0;

                }
            }
            else if (currentState == state.movingToTarget && targetResource != null
                && (targetResource.transform.position - transform.position).sqrMagnitude < float.Epsilon)
            {
                //IsMovingAnimation(false);
                anim.SetBool("Move", false);
                thisMovingObject.StopAllCoroutines();
                if (targetResource.tag == "Resource")
                    currentState = state.gathering;
                else
                {
                    currentState = state.inBuilding;
                    timer = 1;
                }

            }
            else if (currentState == state.gathering)
            {
                if (targetResource.resourcesRemaining <= 0)
                {
                    timer = 0;
                    gatherCounter = 1;
                    if (targetResource.resourceType == resource.building)
                    {
                        float distance;
                        GameObject closest = GetClosestResourceDistance("Wall", out distance);
                        if (distance < 2)
                        {
                            Resources newTarget = closest.GetComponent<Resources>();
                            newTarget.path = new List<Vector2>();
                            newTarget.path.Add(this.transform.position);
                            newTarget.path.Add(newTarget.transform.position);
                            Init(closest.GetComponent<Resources>());
                            newTarget.workerSlots.Add(this);
                            newTarget.tempWorkers++;
                        }
                        else
                        {
                            Cancel();
                        }
                    }
                    else
                    {
                        Cancel();
                    }
                }
                else
                {
                    timer += Time.deltaTime;
                    if (timer >= gatherCounter && targetResource.resourcesRemaining > 0)
                    {
                        Gather();
                        gatherCounter++;
                    }
                    if (heldResourceAmount == targetResource.gatherAmount)
                    {
                        thisMovingObject.MoveOnPath(targetResource.path, true);
                        //StartCoroutine(SmoothMovement(targetResource.path, true));
                        //IsMovingAnimation(true);
                        timer = 0;
                        gatherCounter = 1;
                        currentState = state.movingHome;
                    }
                }
            }
            else if (currentState == state.inBuilding)
            {
                if (timer >= 1)
                {
                    targetResource.ReduceResources(targetResource.gatherAmount);
                    timer = 0;
                }
                else
                {
                    timer += Time.deltaTime;
                }
            }
            else if (currentState == state.movingHome && (targetResource.path[0] - (Vector2)transform.position).sqrMagnitude < float.Epsilon)
            {
                //pathEnded = false;
                thisMovingObject.StopAllCoroutines();
                if (heldResourceAmount > 0)
                {
                    DepositResources();
                }
                if (cancel)
                {
                    destroy = true;
                    GameManager.instance.ProcessWorkers();
                }
                //else if (targetResource.resourcesRemaining <= 0)
                //{
                //    destroy = true;
                //    GameManager.instance.ProcessWorkers();
                //}
                else //if ((new Vector3(path[path.Count - 1].x, path[path.Count - 1].y, 0) - transform.position).sqrMagnitude < float.Epsilon)
                {
                    thisMovingObject.MoveOnPath(targetResource.path, false);
                    //StartCoroutine(SmoothMovement(targetResource.path, false));
                    //IsMovingAnimation(true);
                    currentState = state.movingToTarget;
                    //path.Clear();
                }
            }
            else if (cancel && destroy)
            {
                GameManager.instance.ProcessWorkers();
            }
            //else if (targetResource == null)
            //{
            //    Cancel();
            //    destroy = true;
            //}
        }
    }
}