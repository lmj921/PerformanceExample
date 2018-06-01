using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MemoryIssue : MonoBehaviour {
    public int CubeCount=10;
    private Object _cubePrefab;

    private List<GameObject> _cubeList;
	// Use this for initialization
	void Start () {
        _cubePrefab = Resources.Load("PlainCube");
        _cubeList = new List<GameObject>();
	}
	
	// Update is called once per frame
	void Update () {
        if (_cubeList.Count>0)
        {
            for (int i = 0,max=_cubeList.Count; i < max; i++)
            {
                GameObject.Destroy(_cubeList[i]);
            }
        }

        for (int i = 0; i < CubeCount; i++)
        {
            GameObject cubeGo = GameObject.Instantiate(_cubePrefab) as GameObject;
            cubeGo.transform.position = new Vector3(Random.Range(1, 10), 0, Random.Range(1, 10));
            _cubeList.Add(cubeGo);
        }
	}
}
