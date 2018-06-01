using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorCube : MonoBehaviour {

    private float _randomTime;
    private float _time;
    private Material _mat;
    // Use this for initialization
    private Color _colorFlag;
    void Start () {
        _mat = GetComponent<Renderer>().material;
        _randomTime = Random.Range(1f, 5f);
    }

    // Update is called once per frame
    void Update () {
        _time += Time.deltaTime;
        if (_time>_randomTime)
        {
            if (_colorFlag == Color.red)
            {
                _colorFlag = Color.green;
            }
            else
            {
                _colorFlag = Color.red;
            }

            _mat.SetColor("_Color", _colorFlag);  
            _time = 0;
        }
    }
}
