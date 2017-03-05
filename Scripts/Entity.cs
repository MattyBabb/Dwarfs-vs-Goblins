using UnityEngine;
using System.Collections;

public class Entity : MonoBehaviour {

    public int maxHP;

    int currentHP;
    float healthBarLength, healthBarHeight;
    Texture2D texture, texture2;
    Color[] colourArray;
    Color fillColourRed = new Color(1.0f, 0, 0);
    Color fillColourGreen = new Color(0, 1.0f, 0);
    Vector2 currentPos;
    
    void Awake()
    {
        currentHP = maxHP;
        healthBarLength = GetComponent<SpriteRenderer>().sprite.rect.width;

        if (healthBarLength < 50)
            healthBarLength = 50;

        healthBarHeight = 9;
    }

    void OnGUI()
    {
        if(currentHP != maxHP)
        {
            float height = GameManager.instance.startingCameraSize / (GameManager.instance.currentCameraSize * 2);
            float width = (height * (Screen.width / Screen.height)) / 3.1f;

            texture = new Texture2D((int)(healthBarLength * width), (int)(healthBarHeight * height));
            colourArray = texture.GetPixels();
            currentPos = gameObject.transform.position;

            currentPos.y += 0.5f;

            currentPos = Camera.main.WorldToScreenPoint(currentPos);
            currentPos.y = Screen.height - currentPos.y;

            currentPos.x -= texture.width / 2f;
            currentPos.y -= texture.height / 2;

            for (int i = 0; i < colourArray.Length; i++)
            {
                colourArray[i] = fillColourRed;
            }

            texture.SetPixels(colourArray);
            texture.Apply();
            GUI.Box(new Rect(currentPos, new Vector2(texture.width, texture.height)), texture, GUIStyle.none);

            texture2 = new Texture2D((int)((healthBarLength * width * currentHP) / maxHP), (int)(healthBarHeight * height));

            for (int i = 0; i < colourArray.Length; i++)
            {
                colourArray[i] = fillColourGreen;
            }
            texture2.SetPixels(colourArray);
            texture2.Apply();
            GUI.Box(new Rect(currentPos, new Vector2(texture2.width, texture2.height)), texture2, GUIStyle.none);
        }

    }

    public void LoseLife(int amount)
    {
        currentHP -= amount;
        if(currentHP <= 0)
        {
            this.gameObject.SetActive(false);
            Destroy(this.gameObject);
        }
    }


}
