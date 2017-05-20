using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GradientTest : MonoBehaviour {

    Vector3 gradient;
    public test t;
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        if (t == null)
            return;
        gradient = -t.GetGradient(transform.position);
        Debug.DrawLine(this.transform.position, this.transform.position + gradient);
        // Debug.Log(-gradient);
    }
    
    private void OnDrawGizmos()
    {
        
        Gizmos.DrawLine(transform.position, transform.position + gradient * 0.5f);
    }
}
