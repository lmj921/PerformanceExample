using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DynamicBatching : MonoBehaviour {
    public int prefabType;
    private int _row=10;
    private int _col=10;
	// Use this for initialization
	void Start () {
        Object cubePrefab;
        if (prefabType == 0)
        {
            cubePrefab = Resources.Load("ColorCube");
        }
        else if (prefabType == 1)
        {
            cubePrefab = Resources.Load("ColorCubeMatProp");

        }
        else
        {
            cubePrefab = Resources.Load("ColorCubeMat");
        }

        for (int i = 0; i < _row; i++)
        {
            for (int j = 0; j < _col; j++) {
                GameObject go = GameObject.Instantiate(cubePrefab) as GameObject;
                go.transform.position = new Vector3(j * 3, 0, i * 3);

            }
        }
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
