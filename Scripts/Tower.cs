using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tower : Resources
{
    
    public float firingDistance;
    public string enemyTag;
    public bool canMove;
  
    protected float fireRateTimer;
    protected GameObject[] enemies;
    protected GameObject currentTarget;
    protected int safetyBuffer = 2;
    protected float checkTimer = 0.5f;
    static protected float restTimer = 0.5f;
    MovingObject thisMovingObject;

    void Awake()
    {
        path = new List<Vector2>();
    }

    protected GameObject GetNearestEnemy()
    {
        enemies = GameObject.FindGameObjectsWithTag(enemyTag);
        GameObject closestEnemy = null;
        float smallestDist = Mathf.Infinity;
        float currentDist;
        foreach(GameObject enemy in enemies)
        {
            currentDist = (transform.position - enemy.transform.position).sqrMagnitude;
            if (currentDist < smallestDist)
            {
                smallestDist = currentDist;
                closestEnemy = enemy;
            }
        }
        return closestEnemy;
    }

    public void Move(Vector2 location)
    {
        //todo: move to target location if workers > 0
    }

    protected float GetDistance(GameObject otherObject)
    {
        return (transform.position - otherObject.transform.position).sqrMagnitude;
    }

    protected void Fire()
    {
        if (currentTarget != null)
            this.GetComponent<Entity>().Attack(currentTarget.GetComponent<Entity>());

        // play fire animation
    }

    //this is just a placeholder for the workers
    public override void ReduceResources(float amount)
    {
    }

    void Update ()
    {
        if(currentTarget != null)
        {
            fireRateTimer += Time.deltaTime;
            if(workerSlots.Count > 0 && fireRateTimer >= gatherTime / workerSlots.Count)
            {
                Fire();
                fireRateTimer = 0;
            }
        }
        else if(checkTimer <= 0 && currentTarget == null)
        {
            GameObject thisEnemy = GetNearestEnemy();
            if(thisEnemy != null)
            {
                float distance = GetDistance(thisEnemy);
                if (distance > firingDistance + safetyBuffer)
                {
                    checkTimer += restTimer;
                }
                else if (distance <= firingDistance)
                {
                    currentTarget = thisEnemy;
                }
            }
            else
            {
                //reset checker
                checkTimer += restTimer;
            }
        }
        else if(currentTarget == null)
        {
            checkTimer -= Time.deltaTime;
        }
	}
}
