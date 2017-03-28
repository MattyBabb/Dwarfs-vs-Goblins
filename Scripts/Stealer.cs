using UnityEngine;
using System.Collections;

public class Stealer : Worker {

	
	// Update is called once per frame
	void Update () {
	
	}

    public void Init()
    {

    }

    void Awake()
    {
        anim = GetComponent<Animator>();
        //sprite = GetComponent<SpriteRenderer>();
        //boxCollider = GetComponent<BoxCollider2D>();
        //rb2D = GetComponent<Rigidbody2D>();
        //anim.SetBool("WorkerGather", false);
        //UpdateMoveSpeed(GameManager.instance.speedMult);
    }
}
