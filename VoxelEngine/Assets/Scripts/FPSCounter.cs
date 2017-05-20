using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class FPSCounter : MonoBehaviour {

    int frameCount = 0;
    float dt = 0.0f;
    float fps = 0.0f;
    public int updateRate;
    public Text fpsText;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
        frameCount++;
        dt += Time.deltaTime;
        if(dt > 1f / updateRate)
        {
            fps = frameCount / dt;
            frameCount = 0;
            dt -= 1f / updateRate;
            int fpsInt = (int)fps;
            fpsText.text = "FPS: " + fpsInt.ToString();
        }
	}
}
