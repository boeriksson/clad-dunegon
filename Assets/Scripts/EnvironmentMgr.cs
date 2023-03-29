using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

public class EnvironmentMgr : MonoBehaviour {
    public GameObject cornerCurved;
    public GameObject celing;
    public GameObject celingCorner;
    public GameObject celingCornerLeftExit;
    public GameObject celingCornerRightExit;
    public GameObject celingCornerLeftRightExit;
    public GameObject celingExit;
    public GameObject celingWall;
    public GameObject corner;
    public GameObject cornerLeftExit;
    public GameObject cornerRightExit;
    public GameObject cornerLeftRightExit;
    public GameObject exit;
    public GameObject wall;
    public GameObject cornerSquare;
    public GameObject cross3;
    public GameObject cross4;
    public GameObject deadEnd;
    public GameObject floor;
    public GameObject floorCeling;
    public GameObject floorCelingWall;
    public GameObject floorCelingCorner;
    public GameObject floorCelingCornerRightExit;
    public GameObject floorCelingCornerLeftExit;
    public GameObject floorCelingCornerLeftRightExit;
    public GameObject floorCelingExit;
    public GameObject floorCorner;
    public GameObject floorCornerLeftExit;
    public GameObject floorCornerRightExit;
    public GameObject floorCornerLeftRightExit;
    public GameObject floorExit;
    public GameObject floorWall;
    public GameObject start;
    public GameObject straight;
    public GameObject straightStairs;

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
