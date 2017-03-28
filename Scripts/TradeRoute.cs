using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TradeRoute : Worker
{
    
    [HideInInspector]
    public bool active;

    int[] costAmounts;
    resource[] costTypes;
    int[] deliverAmounts;
    resource[] deliverTypes;
    Vector2 destination, start;
    bool delivering, returning;
    List<Vector2> path;
    float oneSecTimer = 0f;
    float waitCounter;
    int waitTime;
    List<Entity> attackers;
    int maxAttackers;
    SpriteRenderer sprite;

    // Use this for initialization
    void Awake ()
    {
        anim = GetComponent<Animator>();
        sprite = GetComponent<SpriteRenderer>();
        thisMovingObject = GetComponent<MovingObject>();
        //boxCollider = GetComponent<BoxCollider2D>();
        //rb2D = GetComponent<Rigidbody2D>();
        path = new List<Vector2>();
        start = GameManager.instance.boardScript.homeBase.transform.position;
        attackers = new List<Entity>();
        maxAttackers = 1;
    }

    public void Init(Vector2 dest, int[] _costAmounts, resource[] _costTypes, int[] _deliverAmounts, resource[] _deliverTypes, int wait)
    {
        //PathRequestManager.RequestPath(transform.position, new Vector2[] { dest}, 0, true, OnPathFound);
        thisMovingObject.MoveToLocation(dest);
        destination = dest;
        costAmounts = _costAmounts;
        costTypes = _costTypes;
        deliverAmounts = _deliverAmounts;
        deliverTypes = _deliverTypes;
        active = true;
        waitTime = wait;
    }

    void ReduceResources()
    {
        for (int i = 0; i < costTypes.Length; i++)
        {
            GameManager.instance.ReduceResources(new KeyValuePair<resource, int>(costTypes[i], costAmounts[i]));
        }
    }

    void IncreaseResources()
    {
        for (int i = 0; i < deliverTypes.Length; i++)
        {
            GameManager.instance.AddResources(new KeyValuePair<resource, int>(deliverTypes[i], deliverAmounts[i]));
        }
    }

    bool AreResourcesAvailable()
    {
        bool retVal = true;
        for (int i = 0; i < costTypes.Length; i++)
        {
            if(!GameManager.instance.IsPurchaseable(new KeyValuePair<resource, int>(costTypes[i], costAmounts[i])))
            {
                retVal = false;
            }
        }
        return retVal;
    }

    public bool AddAttackers(Entity attacker)
    {
        if(attackers.Count < maxAttackers)
        {
            attackers.Add(attacker);
            return true;
        }
        return false;
        
    }

    //the trade route has been looted by enemies
    public void Loot()
    {
        active = false;
        sprite.enabled = false;
        SimplePool.Despawn(this.gameObject);

    }
	
	// Update is called once per frame
	void Update ()
    {
        if (active)
        {
            oneSecTimer += Time.deltaTime;
            if (oneSecTimer >= 1)
            {
                //request path
            }
            //start!
            if (!delivering && !returning && AreResourcesAvailable())
            {
                if (((Vector2)transform.position - start).sqrMagnitude < Mathf.Epsilon)
                {
                    waitCounter = 0f;
                    delivering = true;
                    thisMovingObject.MoveToLocation(start);
                    //StartCoroutine(SmoothMovement(path, false));
                    ReduceResources();
                }
            }
            else if (delivering)
            {
                if (((Vector2)transform.position - destination).sqrMagnitude < Mathf.Epsilon)
                {
                    //destination reached!
                    sprite.enabled = false;
                    waitCounter += Time.deltaTime;
                    if (waitCounter >= waitTime)
                    {
                        sprite.enabled = true;
                        delivering = false;
                        returning = true;
                        //StartCoroutine(SmoothMovement(path, true));
                        thisMovingObject.MoveToLocation(destination);
                    }
                }
            }
            else if (returning)
            {
                if (((Vector2)transform.position - start).sqrMagnitude < Mathf.Epsilon)
                {
                    //trade route complete! 
                    //deliver resources!!
                    IncreaseResources();
                    Loot();
                }
            }
        }
	}
}
