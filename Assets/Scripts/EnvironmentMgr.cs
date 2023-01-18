using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnvironmentMgr : MonoBehaviour {
    public GameObject cornerCurved;
    public GameObject cornerSquare;
    public GameObject cross3;
    public GameObject cross4;
    public GameObject deadEnd;
    public GameObject straight;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public List<GameObject> DunegonSegments { get; }
    public GameObject Straight { get; }
}
