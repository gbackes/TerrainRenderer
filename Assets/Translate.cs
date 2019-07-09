using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Translate : MonoBehaviour {
	public float speed = 500;
	// Use this for initialization
	bool translate = false;
	void Start () {
		
	}

	// Update is called once per frame
	void Update()
	{
		if (Input.GetKeyUp(KeyCode.Space))
		{
			translate = !translate;
		}
	}

	private void FixedUpdate()
	{
		if(translate)
			transform.position += transform.forward * speed * Time.deltaTime;
	}
}
