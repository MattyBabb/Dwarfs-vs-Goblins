using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingObject : MonoBehaviour
{

    
    [HideInInspector]
    public Resources targetResource;
    [HideInInspector]
    public GameObject target;
    //bool pathEnded;
    [HideInInspector]
    public state currentState;
    [HideInInspector]
    public bool cancel, destroy;
    [HideInInspector]
    public float heldResourceAmount = 0;
    public float moveSpeed;
    [HideInInspector]
    public SpriteRenderer sprite;
    [HideInInspector]
    public Animator anim;
    float currentMoveSpeed;
    [HideInInspector]
    public BoxCollider2D boxCollider;
    [HideInInspector]
    public Rigidbody2D rb2D;
    [HideInInspector]
    public List<Vector2> path;

    // Use this for initialization
    void Awake ()
    {
        anim = GetComponent<Animator>();
        sprite = GetComponent<SpriteRenderer>();
        boxCollider = GetComponent<BoxCollider2D>();
        rb2D = GetComponent<Rigidbody2D>();
        anim.SetBool("Move", true);
        currentMoveSpeed = moveSpeed;
        path = new List<Vector2>();
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

    public void MoveOnPath(List<Vector2> path, bool reversed)
    {
        StopAllCoroutines();
        IsMovingAnimation(true);
        StartCoroutine(SmoothMovement(path, reversed));
    }

    public void MoveToObject(GameObject target)
    {
        StopAllCoroutines();
        IsMovingAnimation(true);
        PathRequestManager.RequestPath(target.transform.position, new Vector2[] { transform.position }, 0, false, OnPathFound);
    }

    public void MoveToLocation(Vector2 location)
    {
        StopAllCoroutines();
        IsMovingAnimation(true);
        PathRequestManager.RequestPath(location, new Vector2[] { transform.position }, 0, false, OnPathFound);
    }

    public void MoveToClosestObject(GameObject[] targets)
    {
        StopAllCoroutines();
        IsMovingAnimation(true);
        Vector2[] locations = Array.ConvertAll(targets, item => new Vector2(item.transform.position.x, item.transform.position.y));
        PathRequestManager.RequestPath(transform.position, locations, 0, true, OnPathFound);
    }

    public void UpdateMoveSpeed(float multiplier)
    {
        moveSpeed = multiplier * currentMoveSpeed;
    }

    public IEnumerator SmoothMovement(List<Vector2> ends, bool reversed)
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
        //pathEnded = true;
    }

    public void IsMovingAnimation(bool moving)
    {
        if (moving)
        {
            anim.SetBool("Move", true);
        }
        else
            anim.SetBool("Move", false);
    }
}
