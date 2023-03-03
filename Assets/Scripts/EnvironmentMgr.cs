using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

public class EnvironmentMgr : MonoBehaviour {
    public GameObject cornerCurved;
    public GameObject cornerSquare;
    public GameObject cross3;
    public GameObject cross4;
    public GameObject deadEnd;
    public GameObject straight;
    public GameObject floorCeling;
    public GameObject floorCelingWall;
    public GameObject floorCelingCorner;
    public GameObject floorCelingCornerRightExit;
    public GameObject floorCelingCornerLeftExit;
    public GameObject floorCelingCornerLeftRightExit;
    public GameObject floorCelingExit;
    public GameObject start;

    public TMP_Text debugText;


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
