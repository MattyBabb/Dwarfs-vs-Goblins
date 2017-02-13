using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wall : MonoBehaviour
{

    public Texture2D[] tiles;

    private Texture2D EvaluateSurroundingsForWall()
    {
        Texture2D combined;
        RaycastHit2D ray;
        Vector2 position = transform.position;
        string hitTag;

        bool up = false, down = false, left = false, right = false;

        for (int i = (int)position.y - 1; i <= position.y + 1; i++)
        {
            ray = Physics2D.Raycast(new Vector2(position.x, i), Vector2.zero);
            if (ray.collider != null)
            {
                hitTag = ray.collider.gameObject.tag;
                if (hitTag == "Forts")
                {
                    if (i == position.y - 1)
                    {
                        down = true;
                    }
                        

                    if (i == position.y + 1)
                    {
                        up = true;
                    }
                        
                }
            }
        }

        for (int i = (int)position.x - 1; i <= position.x + 1; i++)
        {
            ray = Physics2D.Raycast(new Vector2(i, position.y), Vector2.zero);
            if (ray.collider != null)
            {
                hitTag = ray.collider.gameObject.tag;
                if (hitTag == "Forts")
                {
                    if (i == position.x - 1)
                    {
                        left = true;
                    }
                        
                    if (i == position.x + 1)
                    {
                        right = true;
                    }
                        
                }
            }
        }


        if (!up && !down)
        {
            combined = tiles[2].AlphaBlend(tiles[3]);
            return combined;
        }
        else if(!left && !right)
        {
            combined = tiles[0].AlphaBlend(tiles[1]);
            return combined;
        }
        else if (up)
        {
            combined = tiles[0];
        }else if (down)
        {
            combined = tiles[1];
        }else if (right)
        {
            combined = tiles[2];
        }else
        {
            combined = tiles[3];
        }

        if (right)
            combined = combined.AlphaBlend(tiles[2]);
        if (left)
            combined = combined.AlphaBlend(tiles[3]);
        if (up)
            combined = combined.AlphaBlend(tiles[0]);
        if(down)
            combined = combined.AlphaBlend(tiles[1]);
        

        return combined;
    }

    void Awake()
    {
        EvaluateSprite();
        FixSurroundingWalls();
    }

    private void FixSurroundingWalls()
    {
        RaycastHit2D ray;
        int loopCount = 0;
        while(loopCount <= 3)
        {
            if(loopCount == 0)
                ray = Physics2D.Raycast(new Vector2(transform.position.x, transform.position.y + 1f), Vector2.zero);
            else if(loopCount == 1)
                ray = Physics2D.Raycast(new Vector2(transform.position.x, transform.position.y - 1f), Vector2.zero);
            else if (loopCount == 2)
                ray = Physics2D.Raycast(new Vector2(transform.position.x + 1f, transform.position.y), Vector2.zero);
            else
                ray = Physics2D.Raycast(new Vector2(transform.position.x - 1f, transform.position.y), Vector2.zero);

            if(ray.collider != null)
            {
                if(ray.collider.gameObject.tag == "Forts")
                {
                    ray.collider.gameObject.GetComponent<Wall>().EvaluateSprite();
                }
            }
            loopCount++;
        }
          
    }

    public void EvaluateSprite()
    {
        Vector2 position = transform.position;
        Texture2D texture = EvaluateSurroundingsForWall();
        GetComponent<SpriteRenderer>().sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 128f);
        transform.position = position;
    }

}

 
 public static class ImageHelpers
{
    public static Texture2D AlphaBlend(this Texture2D aBottom, Texture2D aTop)
    {
        if (aBottom.width != aTop.width || aBottom.height != aTop.height)
            throw new System.InvalidOperationException("AlphaBlend only works with two equal sized images");
        var bData = aBottom.GetPixels();
        var tData = aTop.GetPixels();
        int count = bData.Length;
        var rData = new Color[count];
        for (int i = 0; i < count; i++)
        {
            Color B = bData[i];
            Color T = tData[i];
            float srcF = T.a;
            float destF = 1f - T.a;
            float alpha = srcF + destF * B.a;
            Color R = (T * srcF + B * B.a * destF) / alpha;
            R.a = alpha;
            rData[i] = R;
        }
        var res = new Texture2D(aTop.width, aTop.height);
        res.SetPixels(rData);
        res.Apply();
        return res;
    }
}
