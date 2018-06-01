using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorCubeMat : MonoBehaviour {    

    private float _randomTime;
    private float _time;
    private Material _greenMat;
    private Material _redMat;
    private Renderer _renderer;

    private Color _colorFlag;
    // Use this for initialization
    void Start () {   
        _greenMat = Resources.Load("Standard Green") as Material;
        _redMat = Resources.Load("Standard Red") as Material;
        _randomTime = Random.Range(1f, 5f);
        _renderer = GetComponent<Renderer>();
    }

    // Update is called once per frame
    void Update () {
        _time += Time.deltaTime;
        if (_time>_randomTime)
        {
            if (_colorFlag == Color.red)
            {
                _colorFlag = Color.green;
                _renderer.material = _greenMat;
            }
            else
            {
                _colorFlag = Color.red;
                _renderer.material = _redMat;
            }


            _time = 0;
        }
    }
}
