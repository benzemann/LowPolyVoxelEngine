using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour {

    private void Awake()
    {
        VoxelEngine.Instance.playerPos = this.transform.position;
    }

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        VoxelEngine.Instance.playerPos = this.transform.position;
	}
}
