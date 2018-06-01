using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorCubeMatProp : MonoBehaviour {
    MaterialPropertyBlock _prop = null;

    private float _randomTime;
    private float _time;
    private Material _mat;
    private Renderer _renderer;

    private Color _colorFlag;
    // Use this for initialization
    void Start () {        
//        _mat = GetComponent<Renderer>().material;
        _randomTime = Random.Range(1f, 5f);
        _renderer = GetComponent<Renderer>();
        _prop = new MaterialPropertyBlock();
    }

    // Update is called once per frame
    void Update () {
        _time += Time.deltaTime;
        if (_time>_randomTime)
        {
            _renderer.GetPropertyBlock(_prop);
            if (_colorFlag == Color.red)
            {
                _colorFlag = Color.green;
            }
            else
            {
                _colorFlag = Color.red;
            }
            _prop.SetColor("_Color", _colorFlag);
            _renderer.SetPropertyBlock(_prop);  

            _time = 0;
        }
    }
}
