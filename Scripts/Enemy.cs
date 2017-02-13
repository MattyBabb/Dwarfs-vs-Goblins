using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Enemy : Worker
{

    public int attackDamgage;
    public GameObject target;
    public bool isAttacking;
    GameObject nearestWorker;
    private float oneSecondTimer = 0f;
	
	// Update is called once per frame
	void Update ()
    {
        oneSecondTimer += Time.deltaTime;

        if(oneSecondTimer >= 1.0f)
        {
            oneSecondTimer = 0f;
            nearestWorker = GetClosestWorker();
            if(nearestWorker != null)
            {
                float distNearestWorker = (nearestWorker.transform.position - transform.position).sqrMagnitude;
                if (distNearestWorker < 2 && (GameManager.instance.boardScript.homeBase.transform.position - transform.position).sqrMagnitude > 2)
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
                else if((transform.position - target.transform.position).sqrMagnitude < 1)
                {
                    Attack();
                }
            }
        }
	}

    public new void Move(List<Vector3> destinations)
    {
        
        RaycastHit2D hit;
        if (destinations.Count > 0)
        {
            IsMovingAnimation(true);
            foreach (Vector3 destination in destinations)
            {
                hit = Physics2D.Raycast(destination, -Vector2.up);
                if (hit.collider != null && target == null)
                {
                    StartCoroutine(SmoothMovement(destination));
                    //move up to object, attack it, destroy it and then continue moving along path
                    target = hit.collider.gameObject;
                }
                else
                    StartCoroutine(SmoothMovement(destination));
            }
        }
        else
        {
            DestroyObject(this);
        }
    }

    GameObject GetClosestWorker()
    {
        Vector3 currentPos = transform.position;
        GameObject[]  workers = GameObject.FindGameObjectsWithTag("Worker");
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
}
