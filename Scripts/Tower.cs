using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tower : Resources
{
    
    public float firingDistance;
    public string enemyTag;
    public bool canMove;
    public string movementText;
    public int damage;
    public bool aoe;
    public float explosionRadius;
    public int level;


    protected float fireRateTimer;
    protected GameObject[] enemies;
    protected GameObject currentTarget;
    protected int safetyBuffer = 2;
    protected float checkTimer = 0.5f;
    protected Vector2 previousPos;
    static protected float restTimer = 0.5f;
    MovingObject thisMovingObject;
    Animator anim;
    int activeWorkers;
    bool moving;

    void Awake()
    {
        path = new List<Vector2>();
        thisMovingObject = GetComponent<MovingObject>();
        //thisMovingObject.IsMovingAnimation(false);
        anim = GetComponent<Animator>();
        moving = false;
        anim.SetBool("Move", false);
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
        anim.enabled = true;
        //todo: move to target location if workers > 0
        if(workerSlots.Count > 1)
        {
            thisMovingObject.MoveToLocation(location);
            thisMovingObject.IsMovingAnimation(true);
            foreach(Worker worker in workerSlots)
            {
                worker.transform.position = location;
                worker.SpriteEnabled(false);
            }
            moving = true;
            gameObject.tag = "Untagged";
        }
    }

    protected float GetDistance(GameObject otherObject)
    {
        return (transform.position - otherObject.transform.position).sqrMagnitude;
    }

    protected void Fire()
    {
        if (currentTarget != null)
        {
            if (GetComponent<ProjectileLauncher>() != null)
            {
                ProjectileLauncher thisProjectile = GetComponent<ProjectileLauncher>();
                Vector2 fireDirection = thisProjectile.FireProjectile(1, previousPos, gameObject, currentTarget.GetComponent<MovingObject>(), GameManager.instance.gravity);
                GameObject instance = SimplePool.Spawn(thisProjectile.projectile, transform.position, Quaternion.identity) as GameObject;
                //GameObject aProjectile = (GameObject)Instantiate(thisProjectile.projectile, transform.position, Quaternion.identity);
                instance.GetComponent<Projectile>().Init(fireDirection.magnitude, fireDirection / fireDirection.magnitude, GameManager.instance.gravity, level, explosionRadius, damage, currentTarget.GetComponent<Entity>());
            }

        }
    }

    //this is just a placeholder for the workers
    public override void ReduceResources(float amount)
    { 
    }

    void Update ()
    {
        if (!moving)
        {
            if (currentTarget != null)
            {
                fireRateTimer += Time.deltaTime;
                checkTimer -= Time.deltaTime;
                

                if (checkTimer <= 0)
                {
                    checkTimer += restTimer;
                    activeWorkers = 0;
                    foreach (Worker worker in workerSlots)
                    {
                        if (worker.currentState == state.inBuilding)
                            activeWorkers++;
                    }
                }
                if(currentTarget.GetComponent<Entity>().currentHP <= 0)
                {
                    currentTarget = null;
                }
                if (activeWorkers > 0 && fireRateTimer >= gatherTime / activeWorkers && previousPos != (Vector2)currentTarget.transform.position)
                {
                    Fire();
                    fireRateTimer = 0;
                    float distance = GetDistance(currentTarget);
                    if (distance > firingDistance)
                    {
                        currentTarget = null;
                    }
                    
                }
                
            }
            else if (checkTimer <= 0 && currentTarget == null)
            {
                GameObject thisEnemy = GetNearestEnemy();
                if (thisEnemy != null)
                {
                    float distance = GetDistance(thisEnemy);
                    if (distance > firingDistance + safetyBuffer)
                    {
                        checkTimer += restTimer;
                    }
                    else if (distance <= firingDistance)
                    {
                        currentTarget = thisEnemy;
                        checkTimer += restTimer;
                    }
                }
                else
                {
                    //reset checker
                    checkTimer += restTimer;
                }
            }
            else if (currentTarget == null)
            {  
                checkTimer -= Time.deltaTime;
            }
            if (currentTarget != null)
                previousPos = currentTarget.transform.position;
        }
        else if (thisMovingObject.path.Count > 0 && (thisMovingObject.path[thisMovingObject.path.Count-1] - (Vector2)transform.position).sqrMagnitude < Mathf.Epsilon)
        {
            //position arrived at!
            thisMovingObject.path = new List<Vector2>();
            moving = false;
            thisMovingObject.IsMovingAnimation(false);
            gameObject.tag = "BuildingResource";
            foreach (Worker worker in workerSlots)
            {
                worker.SpriteEnabled(true);
            }

        }
       

    }
}
