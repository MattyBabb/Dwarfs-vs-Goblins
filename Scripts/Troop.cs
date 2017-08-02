using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Troop : Worker
{

    public bool flying;
    GameObject nearestWorker;
    private float oneSecondTimer = 0f;
    GameObject nearestTradeRoute;

    public void Init()
    {
        currentState = state.movingToTarget;
        //locations = Array.ConvertAll(GameObject.FindGameObjectsWithTag("Building"), item => new Vector2(item.transform.position.x, item.transform.position.y));
        //PathRequestManager.RequestPath(transform.position, locations, 0, true, OnPathFound);
        //thisMovingObject.MoveToClosestObject(GameObject.FindGameObjectsWithTag("Building"));


        //todo: move to enemy building
    }



    void Awake()
    {
        anim = GetComponent<Animator>();
        thisMovingObject = GetComponent<MovingObject>();
        //Init();
    }

    GameObject GetClosestObjectWithTag(string tag)
    {
        Vector3 currentPos = transform.position;
        GameObject[] workers = GameObject.FindGameObjectsWithTag(tag);
        GameObject tMin = null;
        float minDist = Mathf.Infinity;
        foreach (GameObject worker in workers)
        {
            float dist = (worker.transform.position - currentPos).sqrMagnitude;
            if (dist < minDist)
            {
                if (tag == "Worker")
                {
                    if (worker.GetComponent<Worker>().currentState != state.inBuilding)
                    {
                        tMin = worker;
                        minDist = dist;
                    }
                }
                else
                {
                    tMin = worker;
                    minDist = dist;
                }

            }
        }
        return tMin;
    }

    void EngageTarget(Worker worker)
    {
        target = worker.gameObject;
        thisMovingObject.MoveToObject(worker.gameObject);
        worker.EnterCombat(this);
    }

    void Attack()
    {
        StopAllCoroutines();
        anim.SetBool("Move", false);
    }

    // Update is called once per frame
    void Update()
    {
        if (isActiveAndEnabled)
        {
            oneSecondTimer += Time.deltaTime;

            if (oneSecondTimer >= 1.0f)
            {
                oneSecondTimer = 0f;
                nearestWorker = GetClosestObjectWithTag("Worker");
                nearestTradeRoute = GetClosestObjectWithTag("TradeRoute");
                //todo: find nearest opposing unit
                if (currentState != state.inBattle)
                {
                    if (target == null)
                    {
                        if (thisMovingObject.path.Count > 0)
                        {
                            RaycastHit2D ray = Physics2D.Raycast(thisMovingObject.path[thisMovingObject.path.Count - 1], Vector2.zero);
                            if (ray.collider != null)
                            {
                                target = ray.collider.gameObject;
                            }
                        }
                    }
                    else if (target == nearestTradeRoute)
                    {
                        if ((nearestTradeRoute.transform.position - transform.position).sqrMagnitude < 1)
                        {
                            //catch TradeRoute
                            nearestTradeRoute.GetComponent<TradeRoute>().Loot();
                            target = null;
                            Init();
                        }
                        else
                        {
                            //PathRequestManager.RequestPath(transform.position, new Vector2[] { nearestTradeRoute.transform.position }, 0, true, OnPathFound);
                            thisMovingObject.MoveToObject(nearestTradeRoute);
                        }

                    }
                    else if (nearestTradeRoute != null)
                    {
                        float distNearestTradeRoute = (nearestTradeRoute.transform.position - transform.position).sqrMagnitude;
                        //RaycastHit2D hit = Physics2D.Linecast(transform.position, nearestWorker.transform.position);
                        if (distNearestTradeRoute < 6 && nearestTradeRoute.GetComponent<TradeRoute>().AddAttackers(this.GetComponent<Entity>()))
                        {
                            StopAllCoroutines();
                            currentState = state.movingToTarget;
                            target = nearestTradeRoute;
                            //MoveToObject(nearestTradeRoute);
                            thisMovingObject.MoveToObject(nearestTradeRoute);
                        }
                    }
                    else if (nearestWorker != null && nearestWorker.GetComponent<Worker>().currentState != state.inBuilding)
                    {
                        float distNearestWorker = (nearestWorker.transform.position - transform.position).sqrMagnitude;
                        //RaycastHit2D hit = Physics2D.Linecast(transform.position, nearestWorker.transform.position);
                        if (distNearestWorker < 2)
                        {
                            EngageTarget(nearestWorker.GetComponent<Worker>());
                        }
                    }
                }


                if (target != null)
                {
                    if (currentState == state.inBattle)
                    {
                        this.GetComponent<Entity>().Attack(target.GetComponent<Entity>());
                        if (target.GetComponent<Entity>().currentHP <= 0)
                        {
                            target = null;
                            Init();
                        }
                    }
                }
                else
                {
                    Init();
                }
            }

            if (currentState == state.movingToTarget && target != null)
            {
                if ((target.transform.position - transform.position).sqrMagnitude < attackRange)
                {
                    currentState = state.inBattle;
                }
            }
            else if (currentState == state.inBattle)
            {
                Attack();
            }
        }
    }
}
