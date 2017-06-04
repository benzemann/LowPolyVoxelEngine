using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

public class Player : MonoBehaviour {

    Rigidbody rigidBody;
    FirstPersonController fps;
    private void Awake()
    {
        VoxelEngine.Instance.playerPos = this.transform.position;
        rigidBody = GetComponent<Rigidbody>();
        fps = GetComponent<FirstPersonController>();
    }

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        VoxelEngine.Instance.playerPos = this.transform.position;
        if (rigidBody.useGravity == true && !VoxelEngine.Instance.IsInLoadedChunk(this.transform.position))
        {
            rigidBody.useGravity = false;
            fps.enabled = false;
        } else if (VoxelEngine.Instance.IsInLoadedChunk(this.transform.position))
        {
            fps.enabled = true;
            rigidBody.useGravity = true;
        }
        
    }
}
