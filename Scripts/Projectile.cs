using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    float verticalVelocity;
    Vector2 direction;
    float initialVelocity;
    float gravity;
    int level;
    float explosionRadius;
    int damage;
    Entity target;

    public void Init(float verticalVelocitya, Vector2 directiona, float gravitya, int levela, float explosionRadiusa, int damagea, Entity targeta)
    {
        verticalVelocity = verticalVelocitya;
        initialVelocity = verticalVelocity;
        direction = directiona;
        gravity = gravitya;
        level = levela;
        explosionRadius = explosionRadiusa;
        damage = damagea;
        target = targeta;
    }

    List<GameObject> GetObjectsWithinRadius(string tag)
    {
        Vector3 currentPos = transform.position;
        GameObject[] workers = GameObject.FindGameObjectsWithTag(tag);
        List<GameObject> objectsWithinRadius = new List<GameObject>();

        foreach (GameObject worker in workers)
        {
            float dist = (worker.transform.position - currentPos).sqrMagnitude;
            if (dist < explosionRadius)
            {
                if (tag == "Enemy")
                {
                    objectsWithinRadius.Add(worker);
                }
            }
        }
        return objectsWithinRadius;
    }

    void Explode()
    {
        //todo:find all enemies within radius and deal damage
        List<GameObject> objects = GetObjectsWithinRadius("Enemy");
        foreach (GameObject anObject in objects)
        {
            anObject.GetComponent<Entity>().LoseLife(damage);
        }
    }

    void Update()
    {
        //enlarge object based on vertical velocity
        verticalVelocity -= (gravity * Time.deltaTime);
        //transform.localScale = transform.position * ((verticalVelocity / 100) + 1);
        transform.Translate(direction * Time.deltaTime * initialVelocity);
        transform.Translate(new Vector2(0, verticalVelocity * Time.deltaTime));

        if (-verticalVelocity >= initialVelocity)
        {
            if(explosionRadius > 0)
            {
                Explode();
            }
            else if (target != null)
            {
                target.LoseLife(damage);
            }
            SimplePool.Despawn(this.gameObject);
        }
    }
}
