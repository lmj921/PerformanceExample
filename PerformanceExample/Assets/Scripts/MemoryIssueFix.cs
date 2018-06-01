using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MemoryIssueFix : MonoBehaviour {
    public int CubeCount=10;
    private Object _cubePrefab;

    private List<GameObject> _cubeList;
	// Use this for initialization
	void Start () {
        _cubePrefab = Resources.Load("PlainCube");
        _cubeList = new List<GameObject>();

        for (int i = 0; i < CubeCount; i++)
        {
            GameObject cubeGo = GameObject.Instantiate(_cubePrefab) as GameObject;
            cubeGo.SetActive(false);
            _cubeList.Add(cubeGo);
        }
	}
	
	// Update is called once per frame
	void Update () {
        if (_cubeList.Count>0)
        {
            for (int i = 0,max=_cubeList.Count; i < max; i++)
            {
                _cubeList[i].SetActive(false);
            }
        }

        for (int i = 0, max=_cubeList.Count; i < max; i++)
        {            
            GameObject cubeGo = _cubeList[i];
            cubeGo.SetActive(true);
            cubeGo.transform.position = new Vector3(Random.Range(1, 10), 0, Random.Range(1, 10));
        }
	}
}
