using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tower : MonoBehaviour
{
    
    public float firingDistance;
    public string enemyTag;
    public int damage;
    public float fireRate;
    float fireRateTimer;
    GameObject[] enemies;
    GameObject currentTarget;

    GameObject GetNearestEnemy()
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

    float GetDistance(GameObject otherObject)
    {
        return (transform.position - otherObject.transform.position).sqrMagnitude;
    }

    void Fire()
    {
        if(currentTarget != null)
            currentTarget.GetComponent<Entity>().LoseLife(damage);

        // play fire animation
    }
	
	// Update is called once per frame
	void Update () {
        fireRateTimer += Time.deltaTime;
        if(fireRateTimer >= fireRate)
        {
            fireRateTimer = 0;
            if(currentTarget != null && GetDistance(currentTarget) <= firingDistance)
            {
                Fire();
            }
            else
            {
                currentTarget = GetNearestEnemy();
                if(currentTarget != null && GetDistance(currentTarget) <= firingDistance)
                {
                    Fire();
                }
            }
        }
	}
}
