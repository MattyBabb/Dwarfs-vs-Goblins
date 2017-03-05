using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class Enemy : Worker
{
    public int attackDamgage;
    public GameObject target;
    public bool isAttacking;
    GameObject nearestWorker;
    private float oneSecondTimer = 0f;
    bool movingToTarget;

    void Init()
    {
        movingToTarget = true;
        locations = Array.ConvertAll(GameObject.FindGameObjectsWithTag("Building"), item => new Vector2(item.transform.position.x, item.transform.position.y));
        PathRequestManager.RequestPath(transform.position, locations, 0, true, OnPathFound);
    }

    GameObject GetClosestWorker()
    {
        Vector3 currentPos = transform.position;
        GameObject[] workers = GameObject.FindGameObjectsWithTag("Worker");
        GameObject tMin = null;
        float minDist = Mathf.Infinity;
        foreach (GameObject worker in workers)
        {
            float dist = (worker.transform.position - currentPos).sqrMagnitude;
            if (dist < minDist)
            {
                tMin = worker;

                minDist = dist;
            }
        }
        return tMin;
    }

    void Attack()
    {
        StopAllCoroutines();
        anim.SetBool("EnemyGather", true);
        isAttacking = true;
    }

    // Update is called once per frame
    void Update ()
    {
        if (isActiveAndEnabled)
        {
            oneSecondTimer += Time.deltaTime;

            if (oneSecondTimer >= 1.0f)
            {
                oneSecondTimer = 0f;
                nearestWorker = GetClosestWorker();
                if (nearestWorker != null)
                {
                    float distNearestWorker = (nearestWorker.transform.position - transform.position).sqrMagnitude;
                    RaycastHit2D hit = Physics2D.Linecast(transform.position, nearestWorker.transform.position);
                    if (distNearestWorker < 2 && hit.collider == null)
                    {
                        //attack worker
                    }
                }


                if (target != null)
                {
                    if (isAttacking)
                    {
                        target.GetComponent<Entity>().LoseLife(attackDamgage);
                    }
                    else if ((transform.position - target.transform.position).sqrMagnitude < 1)
                    {
                        Attack();
                    }
                }
            }
        }
	}


}
