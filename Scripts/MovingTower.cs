using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class MovingTower : Tower
{

    public float moveSpeed = .5f;
    [HideInInspector]
    public SpriteRenderer sprite;
    [HideInInspector]
    public Animator anim;
    private float baseMoveSpeed = .5f;
    [HideInInspector]
    public BoxCollider2D boxCollider;
    Rigidbody2D rb2D;

    // Use this for initialization
    void Awake ()
    {
        anim = GetComponent<Animator>();
        sprite = GetComponent<SpriteRenderer>();
        boxCollider = GetComponent<BoxCollider2D>();
        rb2D = GetComponent<Rigidbody2D>();
        anim.SetBool("Moving", false);
        path = new List<Vector2>();
    }

    public virtual void IsMovingAnimation(bool moving)
    {
        if (moving)
        {
            anim.SetBool("Moving", false);
        }
        else
            anim.SetBool("Moving", true);
    }

    protected virtual void OnPathFound(List<Vector2> newPath, bool pathSuccessful, float distance)
    {
        if (pathSuccessful && this != null && this.isActiveAndEnabled)
        {
            path = newPath;
            transform.position = path[0];
            StopAllCoroutines();
            if (path.Count > 0)
            {
                StartCoroutine(SmoothMovement(path, false));
                IsMovingAnimation(true);
            }
            else
            {
                SimplePool.Despawn(this.gameObject);
            }
        }
    }

    public void MoveToObject(GameObject target)
    {
        StopAllCoroutines();
        PathRequestManager.RequestPath(target.transform.position, new Vector2[] { transform.position }, 0, false, OnPathFound);
    }

    public void MoveToClosestObject(GameObject[] targets)
    {
        StopAllCoroutines();
        Vector2[] locations = Array.ConvertAll(targets, item => new Vector2(item.transform.position.x, item.transform.position.y));
        PathRequestManager.RequestPath(transform.position, locations, 0, true, OnPathFound);
    }

    public void UpdateMoveSpeed(float multiplier)
    {
        moveSpeed = multiplier * baseMoveSpeed;
    }

    protected IEnumerator SmoothMovement(List<Vector2> ends, bool reversed)
    {
        IsMovingAnimation(true);
        float xDir = 0, yDir = 0;
        if (reversed)
        {
            for (int i = ends.Count - 1; i >= 0; i--)
            {
                float squRemainingDistance = (transform.position - new Vector3(ends[i].x, ends[i].y, 0)).sqrMagnitude;
                //float totalDistance = squRemainingDistance;

                while (squRemainingDistance > float.Epsilon)
                {
                    xDir = ends[i].x - transform.position.x;
                    yDir = ends[i].y - transform.position.y;

                    //flip sprite if moving right 
                    if (xDir > 0)
                        sprite.flipX = true;
                    else
                        sprite.flipX = false;

                    Vector2 newPosition = Vector2.MoveTowards(transform.position, ends[i], moveSpeed * Time.deltaTime);
                    rb2D.MovePosition(newPosition);
                    squRemainingDistance = (transform.position - new Vector3(ends[i].x, ends[i].y, 0)).sqrMagnitude;

                    yield return null;
                }
            }
        }
        else
        {
            foreach (Vector2 end in ends)
            {
                float squRemainingDistance = (transform.position - new Vector3(end.x, end.y, 0)).sqrMagnitude;
                //float totalDistance = squRemainingDistance;

                while (squRemainingDistance > float.Epsilon)
                {
                    xDir = end.x - transform.position.x;
                    yDir = end.y - transform.position.y;

                    //flip sprite if moving right 
                    if (xDir > 0)
                        sprite.flipX = true;
                    else
                        sprite.flipX = false;

                    Vector2 newPosition = Vector2.MoveTowards(transform.position, end, moveSpeed * Time.deltaTime);
                    rb2D.MovePosition(newPosition);
                    squRemainingDistance = (transform.position - new Vector3(end.x, end.y, 0)).sqrMagnitude;

                    yield return null;
                }
            }
        }
    }

    void Update()
    {
        if (checkTimer <= 0 && currentTarget == null)
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
                }
            }
        }
        else if (currentTarget == null)
        {
            checkTimer -= Time.deltaTime;
        }
    }

}
