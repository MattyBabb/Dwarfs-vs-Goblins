using UnityEngine;
using System.Collections;

public class Entity : MonoBehaviour {

    public int maxHP;

    int currentHP;
    float healthBarLength, healthBarHeight, cameraScale;
    Texture2D texture;
    Color[] colourArray;
    Color fillColour = new Color(1.0f, 0, 0);
    Vector2 currentPos;
    

    void Awake()
    {
        currentHP = maxHP;
        healthBarLength = gameObject.GetComponent<SpriteRenderer>().sprite.rect.width;
        healthBarHeight = 9;
    }

    void OnGUI()
    {
        float height = GameManager.instance.startingCameraSize / (GameManager.instance.currentCameraSize*2);
        float width = (height * (Screen.width / Screen.height))/2;

        texture = new Texture2D((int)(healthBarLength * width), (int)(healthBarHeight * height));
        colourArray = texture.GetPixels();
        currentPos = gameObject.transform.position;

        currentPos.y += 0.5f;

        currentPos = Camera.main.WorldToScreenPoint(currentPos);
        currentPos.y = Screen.height - currentPos.y;

        currentPos.x -= texture.width / 2 ;
        currentPos.y -= texture.height / 2;

        //texture.SetPixels(new Color(1.0f, 0, 0), 0);
        for (int i = 0; i < colourArray.Length; i++)
        {
            colourArray[i] = fillColour;
        }

        //cameraScale = GameManager.instance.startingCameraSize / GameManager.instance.currentCameraSize;

        texture.SetPixels(colourArray);
        texture.Apply();
         GUI.Box(new Rect(currentPos, new Vector2(healthBarLength, healthBarHeight)), texture, GUIStyle.none);

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
