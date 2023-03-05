using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using SegmentExit = Segment.SegmentExit;
using Segment;
using StraightSegment = Segment.StraightSegment;
using SegmentType = Segment.SegmentType;
using GlobalDirection = Direction.GlobalDirection;
using DirectionConversion = Direction.DirectionConversion;
using UnityEngine;
using JoinException = Dunegon.JoinException;
using LevelMap = level.LevelMap;
using TMPro;

using Debug = UnityEngine.Debug;
using RuntimeHelpers = System.Runtime.CompilerServices.RuntimeHelpers;

namespace Dunegon {
    public class GenerateDunegon : MonoBehaviour {
        public GameObject floor;
        public GameObject mark;
        public GameObject mapper;
        public GameObject exit;
        public int noOfSegments;
        public int defaultMapSize;
        public int scale;

        public GameObject playerObj;

        public GameObject cameraHolderObj;

        public int restartAfterBackWhenWSIsBelow = 2; // This amount or less forks in the workingset and we restart fork after backout
        private int currentSegment = 0;
        private DunegonHelper dHelper = new DunegonHelper();

        private EnvironmentMgr environmentMgr;

        private List<(SegmentExit, Segment.Segment)> workingSet = new List<(SegmentExit, Segment.Segment)>();
        private List<Segment.Segment> segmentList = new List<Segment.Segment>();
        private LevelMap levelMap;
        private List<GameObject> marks = new List<GameObject>();
        private List<GameObject> exitList = new List<GameObject>();

        private GameObject mapDisplay;
        private bool showMap = false;

        //Logger logger = new Logger("./Logs/dunegon.log");

        // Start is called before the first frame update
        void Start() {
            levelMap = new LevelMap(defaultMapSize);
            mapDisplay = new GameObject(); 
            environmentMgr = FindObjectOfType<EnvironmentMgr>();
            workingSet.Add((new SegmentExit(0, 0, Direction.GlobalDirection.North, 0, 0, Direction.LocalDirection.Straight), null));
        }

        void Update() {
            if (Input.GetKeyDown(KeyCode.Space)) {
                if (currentSegment < noOfSegments && workingSet.Count > 0) {
                    StartCoroutine(AddWorkingSet());
                } else {
                    Debug.Log("CurrentSegment: " + currentSegment + " workingSet.Count: " + workingSet.Count);
                }
            }
            if (Input.GetKeyDown(KeyCode.M)) {
                StartCoroutine(ShowMap());
            }
            if (Input.GetKeyDown(KeyCode.B)) {
                StartCoroutine(BackupWorkingSet());
            }
            /*
            if (Input.GetKeyDown(KeyCode.P)) {
                StartCoroutine(PrintSegments());
            }
            */
            if (Input.GetKeyDown(KeyCode.P)) {
                StartCoroutine(SpawnPlayer());
            }
        }

        IEnumerator SpawnPlayer() {
            GameObject player = Instantiate(playerObj, new Vector3(3, 20, 0), Quaternion.identity) as GameObject;
            GameObject cameraHolder = Instantiate(cameraHolderObj, new Vector3(3, 20, 0), Quaternion.identity) as GameObject;
            var cameraPos = GameObject.Find("CameraPos").transform;
            cameraHolder.GetComponent<MoveCamera>().cameraPosition = cameraPos;
            var orientation = GameObject.Find("Orientation").transform;
            var playerCam = GameObject.Find("PlayerCam").transform;
            playerCam.GetComponent<PlayerCamScript>().orientation = orientation;
            player.GetComponent<PlayerMovement>().orientation = orientation;
            
            yield return null;
        }

        IEnumerator PrintSegments() {
            foreach((SegmentExit, Segment.Segment) wsEntry in workingSet) {
                var segment = wsEntry.Item2;
                var tilesStr = DebugUtil.printTupleList(segment.GetTiles());
                var globalSpaceNeeded = DirectionConversion.GetGlobalCoordinatesFromLocal(segment.NeededSpace(), segment.X, segment.Z, segment.GlobalDirection);
                var neededSpace = DebugUtil.printTupleList(globalSpaceNeeded);
                Debug.Log(segment.Type + " X: " + segment.X + " Z: " + segment.Z + " gDirection: " + segment.GlobalDirection + " Tiles: " + tilesStr + " NeededSpace: " + neededSpace);
                yield return null;
            }
        }

        IEnumerator BackupWorkingSet() {
            RemoveOldMarksAndExits();
            levelMap.ClearContent(8);
            var segmentsToBack = new List<Segment.Segment>();
            foreach ((SegmentExit, Segment.Segment) wsEntry in workingSet) { 
                /*
                    Make list with each unique segment segmentsToBack
                    Make list with each segments parent + the exit corresponing to entry (exit) (nextWorkingSet)
                    remove segments tiles from LevelMap
                    remove segments from segmentList
                    clear and set workingset
                */
                if (segmentsToBack.Contains(wsEntry.Item2) == false) {
                    segmentsToBack.Add(wsEntry.Item2);
                }
            }
            var parentsAndExits = new List<(SegmentExit, Segment.Segment)>();
            foreach(Segment.Segment segmentToBack in segmentsToBack) {
                var parent = segmentToBack.Parent;
                var exit = parent.Exits.Single(exit => exit.X == segmentToBack.X && exit.Z == segmentToBack.Z);
                parentsAndExits.Add((exit, parent));
                ShowMarks(parent);
                ClearSegment(segmentToBack);
            }
            levelMap.ClearContent(8);
            workingSet.Clear();
            workingSet.AddRange(parentsAndExits);
            yield return null;
        }

        IEnumerator AddWorkingSet() {
            RemoveOldMarksAndExits();

            List<(SegmentExit, Segment.Segment)> nextWorkingSet = new List<(SegmentExit, Segment.Segment)>();
            //foreach ((SegmentExit, Segment.Segment) wsEntry in workingSet) {
            var workingSetCount = workingSet.Count;
            for (int i = workingSetCount -1; i >= 0; i--) {
                var wsEntry = workingSet[i];
                var segmentStart = wsEntry.Item1;
                var parentSegment = wsEntry.Item2;
                Segment.Segment segment = dHelper.DecideNextSegment(
                    segmentStart.X,
                    segmentStart.Z,
                    segmentStart.Direction,
                    levelMap.GetValueAtCoordinate,
                    workingSet.Count,
                    parentSegment
                );
                if (!(segment is StopSegment)) {
                    if (segment is JoinSegment) {
                        try {
                            Debug.Log("JoinSegment x: " + segment.X + " z: " + segment.Z + " direction: " + segment.GlobalDirection);
                            new Join(
                                (JoinSegment)segment, 
                                AddSegment, 
                                ClearSegment,
                                ReplaceSegmentWithNewSegmentInWorkingSet,
                                levelMap.GetValueAtCoordinate,
                                GetSegmentList, 
                                GetChildrenOfSegment,
                                ChangeParentOfChildren
                            );
                        } catch (JoinException ex) {
                            Debug.Log("JoinException - backing out - message: " + ex.Message);
                            var backout = new Backout(
                                dHelper,
                                ClearSegment, 
                                SetSegmentColor,
                                AddSegment,
                                GetSegmentList, 
                                GetChildrenOfSegment, 
                                ChangeParentOfChildren,
                                ReplaceSegmentWithNewSegmentInWorkingSet,
                                IsBackableSegment,
                                restartAfterBackWhenWSIsBelow
                            );
                            backout.BackoutDeadEnd(segment, 0, 0, workingSet.Count);
                        }
                    } else {
                        Debug.Log("Main loop, adding segment type: " + segment.Type + " at (" + segment.X + ", " + segment.Z + ") ref: " + RuntimeHelpers.GetHashCode(segment) + " value at coord in levelMap: " + levelMap.GetValueAtCoordinate((segment.X, segment.Z)));
                        AddSegment(segment);
                        var addOnSegments = segment.GetAddOnSegments();
                        if (addOnSegments.Count > 0) {
                            foreach(Segment.Segment addSegment in addOnSegments) {
                                AddSegment(addSegment);    
                                AddExitsToNextWorkingSet(nextWorkingSet, addSegment);
                            }
                        } else {
                            AddExitsToNextWorkingSet(nextWorkingSet, segment);
                        }
                    }
                    currentSegment++;
                } else {
                    Debug.Log("Starting BackoutDeadEnd segment: (" + segment.X + ", " + segment.Z + ") type: " + segment.Type + " ------------------------------------------");
                    var backout = new Backout(
                        dHelper,
                        ClearSegment, 
                        SetSegmentColor,
                        AddSegment,
                        GetSegmentList, 
                        GetChildrenOfSegment, 
                        ChangeParentOfChildren,
                        ReplaceSegmentWithNewSegmentInWorkingSet,
                        IsBackableSegment,
                        restartAfterBackWhenWSIsBelow
                    );
                    var backedOutSegment = backout.BackoutDeadEnd(segment, 0, 0, workingSet.Count);
                    Debug.Log("##### StopSegment - Backing out of dead end! backedOutSegment: (" + backedOutSegment.X + ", " + backedOutSegment.Z + ") " + backedOutSegment.Type + " ----------------------------------------");
                    //Debug.Log(" workingSet.Count: " + workingSet.Count + " nextWorkingSet.Count: " + nextWorkingSet.Count);
                    if (workingSet.Count < restartAfterBackWhenWSIsBelow) {
                        nextWorkingSet.Add((backedOutSegment.Exits[0], backedOutSegment));
                    }
                }
            }
            if (nextWorkingSet.Count < 1) {
                var newStartCoord = GetNewStartCoord();
                Debug.Log("out of segments -> nextWorkingSet.Add newStartCoord: " + newStartCoord);
                nextWorkingSet.Add((new SegmentExit(newStartCoord.Item1, newStartCoord.Item2, newStartCoord.Item3, 0, 0, Direction.LocalDirection.Straight), null));
            }
            levelMap.ClearContent(8);
            workingSet.Clear();
            workingSet.AddRange(nextWorkingSet);
            yield return null;
        }

        private List<Segment.Segment> GetSegmentList() {
            return segmentList;
        }
        private void AddSegment(Segment.Segment segment) {
            AddSegment(segment, true);
        }
        private void AddSegment(Segment.Segment segment, bool scan, string strColor = "white")
        {
            var tiles = segment.GetTiles();
            var globalSpaceNeeded = DirectionConversion.GetGlobalCoordinatesFromLocal(segment.NeededSpace(), segment.X, segment.Z, segment.GlobalDirection);

            levelMap.AddCooridnates(tiles, 1);
            if (scan) {
                InstantiateExits(segment);
                ShowMarks(segment); // Debug show neededspace
            }

            Color color = GetColorByStr(strColor);
            SetInstantiatedTiles(segment, tiles, color);
            segmentList.Add(segment);
        }

        private static Color GetColorByStr(string strColor) {
            switch (strColor) {
                case "red": {
                    return Color.red;
                }
                case "green": {
                    return Color.green;
                }
                case "blue": {
                    return Color.blue;
                }
                case "grey": {
                    return Color.grey;
                }
                case "cyan": {
                    return Color.cyan;
                }
                case "magenta": {
                    return Color.magenta;
                }
                case "yellow": {
                    return Color.yellow;
                }
                default: {
                    return Color.white;
                }
            }
        }

        private void SetSegmentColor(Segment.Segment segment, string cStr) {
            Color color = GetColorByStr(cStr);
            List<GameObject> iGSegments = segment.Instantiated;
            foreach(GameObject iGSegment in iGSegments) {
                iGSegment.GetComponent<Renderer>().material.SetColor("_Color", color);
            }
            levelMap.RemoveCoordinates(segment.GetTiles());
            segmentList.Remove(segment);
        }

        private void RemoveOldMarksAndExits() {
            foreach (GameObject mark in marks) {
                Destroy(mark);
            }
            foreach (GameObject exit in exitList) {
                Destroy(exit);
            }
            marks.Clear();
            exitList.Clear();
        }

        private void InstantiateExits(Segment.Segment segment) {
            foreach(SegmentExit segmentExit in segment.Exits) {
                var iExit = Instantiate(exit, new Vector3(segmentExit.X * scale, 0, segmentExit.Z * scale), Quaternion.identity) as GameObject;
                iExit.transform.localScale = new Vector3(scale, 0.5f, scale);
                exitList.Add(iExit);
            }
        }

        private void SetInstantiatedTiles(Segment.Segment segment, List<(int, int)> tiles) {
            SetInstantiatedTiles(segment, tiles, Color.white);
        }
        private void SetInstantiatedTiles(Segment.Segment segment, List<(int, int)> tiles, Color color) {
            var gSegments = segment.GetGSegments(environmentMgr);
            if (gSegments.Count == 0) {
                var instantiatedTiles = new List<GameObject>();
                foreach ((int x, int z) in tiles) {
                    GameObject tileFloor = Instantiate(floor, new Vector3(x * scale, 0, z * scale), Quaternion.identity) as GameObject;
                    tileFloor.transform.localScale = new Vector3(scale, 0.1f, scale);
                    tileFloor.transform.SetParent(environmentMgr.transform);
                    instantiatedTiles.Add(tileFloor);
                }
                segment.Instantiated = instantiatedTiles;
            } else {
                var instantiatedGSegments = new List<GameObject>();
                foreach((int, int, GlobalDirection, float, GameObject) gSegment in gSegments) {
                    GameObject iGSegment = Instantiate(gSegment.Item5, new Vector3(gSegment.Item1 * scale, 0, gSegment.Item2 * scale), Quaternion.identity) as GameObject;
                    iGSegment.transform.Rotate(0.0f, gSegment.Item4, 0.0f, Space.Self);
                    iGSegment.transform.SetParent(environmentMgr.transform);
                    if (color != null) {
                        iGSegment.GetComponent<Renderer>().material.SetColor("_Color", color);
                    } 
                    TMP_Text debugText = Instantiate(environmentMgr.debugText, new Vector3(gSegment.Item1 * scale, -0.98f, gSegment.Item2 * scale), Quaternion.identity);
                    var debugTextComp = debugText.GetComponent<TextMeshPro>();
                    debugTextComp.SetText("(" + gSegment.Item1 + "," + gSegment.Item2 + ")");
                    debugTextComp.color = new Color32(255, 99, 71, 255);
                    debugText.transform.Rotate(90f, 90f, 0f, Space.Self);
                    debugText.transform.SetParent(iGSegment.transform);

                    TMP_Text debugRefText = Instantiate(environmentMgr.debugText, new Vector3((gSegment.Item1 * scale) - 0.5f, -0.98f, gSegment.Item2 * scale), Quaternion.identity);
                    var debugRefTextComp = debugRefText.GetComponent<TextMeshPro>();
                    debugRefTextComp.fontSize = 12;
                    debugRefTextComp.SetText("" + RuntimeHelpers.GetHashCode(segment));
                    debugRefTextComp.color = new Color32(20, 20, 20, 255);
                    debugRefText.transform.Rotate(90f, 90f, 0f, Space.Self);
                    debugRefText.transform.SetParent(iGSegment.transform);

                    instantiatedGSegments.Add(iGSegment);
                }
                segment.Instantiated = instantiatedGSegments;
            }
        }

        private static void AddExitsToNextWorkingSet(List<(SegmentExit, Segment.Segment)> nextWorkingSet, Segment.Segment segment)
        {
            var segmentsWithExits = new List<(SegmentExit, Segment.Segment)>();
            foreach (SegmentExit segmentExit in segment.Exits) {
                segmentsWithExits.Add((segmentExit, segment));
            }

            nextWorkingSet.AddRange(segmentsWithExits);
        }

        private void ShowMarks(Segment.Segment segment)
        {
            var localSpaceNeeded = segment.NeededSpace();
            var globalNeededSpace = Direction.DirectionConversion.GetGlobalCoordinatesFromLocal(localSpaceNeeded, segment.X, segment.Z, segment.GlobalDirection);
            foreach ((int, int) mark in globalNeededSpace) {
                GameObject krockScan = Instantiate(this.mark, new Vector3(mark.Item1 * scale, 0, mark.Item2 * scale), Quaternion.identity) as GameObject;
                krockScan.transform.localScale = new Vector3(scale, 0.1f, scale);
                krockScan.transform.SetParent(environmentMgr.transform);
                marks.Add(krockScan);
            }
        }

        private void ClearSegment(Segment.Segment segment) {
            var instantiatedTiles = segment.Instantiated;
            foreach (GameObject tile in instantiatedTiles) {
                Destroy(tile);
            }
            levelMap.RemoveCoordinates(segment.GetTiles());
            segmentList.Remove(segment);
        }

        IEnumerator ShowMap() {
            if (showMap) {
                showMap = false;
                for (int i = 0; i < mapDisplay.transform.childCount; i++) {
                    Destroy(mapDisplay.transform.GetChild(i).gameObject);
                }
            } else {
                showMap = true;
                var map = levelMap.Map;
                for (int x = 0; x < map.GetLength(0); x++) {
                    for (int z = 0; z < map.GetLength(1); z++) {
                        if (map[x, z] == 1) {
                            var xPos = (x - map.GetLength(0)/2) * scale;
                            var zPos = (z - map.GetLength(1)/2) * scale;
                            GameObject mapObj = Instantiate(
                                mapper, 
                                new Vector3(xPos, 0, zPos), 
                                Quaternion.identity
                            ) as GameObject;

                            mapObj.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
                            mapObj.transform.SetParent(mapDisplay.transform);
                        }
                    }
                }
            }
            yield return null;
        }
        public void ReplaceSegmentWithNewSegmentInWorkingSet(Segment.Segment oldSegment, Segment.Segment newSegment) {
            for (int i = 0; i < workingSet.Count; i++) {
                var wsSegment = workingSet[i].Item2;
                if (wsSegment != null && wsSegment.X == oldSegment.X && wsSegment.Z == oldSegment.Z) {
                    workingSet[i] = (workingSet[i].Item1, newSegment);
                }
            }
        }

        public List<Segment.Segment> GetChildrenOfSegment(Segment.Segment parentSegment) {
            var resultList = new List<Segment.Segment>();
            foreach(Segment.Segment segment in segmentList) {
                if (System.Object.ReferenceEquals(segment.Parent, parentSegment)) {
                    resultList.Add(segment);
                }
            }
            return resultList;
        }

        private void ChangeParentOfChildren(Segment.Segment oldParentSegment, Segment.Segment newParentSegment) {
            var children = GetChildrenOfSegment(oldParentSegment);
            foreach(Segment.Segment segment in children) {
                segment.Parent = newParentSegment;
            }
        }

        private bool IsBackableSegment(Segment.Segment segment) {
            List<Segment.Segment> segmentsAtCoordInList = segmentList.FindAll(seg => (seg.X == segment.X) && (seg.Z == segment.Z));
            //Debug.Log("isBackableSegment - segment with coord (" + segment.X + "," + segment.Z + "). Type: " + segment.Type + " ref: " + RuntimeHelpers.GetHashCode(segment));
            if (segmentsAtCoordInList.Count > 1) {
                throw new Exception("WOOAAHHH isBackableSegment, there are " + segmentsAtCoordInList.Count + " segments with coord (" + segment.X + "," + segment.Z + ") in segmentList...");
            } else if (segment.Type != SegmentType.Stop && segmentsAtCoordInList.Count > 0) {
                var listSegment = segmentsAtCoordInList[0]; 
                var listSegmentRef = RuntimeHelpers.GetHashCode(listSegment); 
                var segmentRef = RuntimeHelpers.GetHashCode(segment);
            }
            var wsEntriesWithSegment = 0;
            foreach((SegmentExit, Segment.Segment) wsEntry in workingSet) {
                if (wsEntry.Item2 != null && wsEntry.Item2.X == segment.X && wsEntry.Item2.Z == segment.Z) {
                    wsEntriesWithSegment ++;
                }
            }
            if (segment.Type == SegmentType.Stop) return true;
            if (wsEntriesWithSegment > 1) return false;
            if (segment is Room) return false;
            if (segment.Exits.Count <= 1) return true;
            if (segment.Join) return false;
            if (GetChildrenOfSegment(segment).Count < 1) return true;
            return false;
        }

        private (int, int, GlobalDirection) GetNewStartCoord() {
            (int maxX, int maxZ, int minX, int minZ) minMax = levelMap.GetMinMaxPopulated();
            Debug.Log("maxX: " + minMax.maxX + " maxZ: " + minMax.maxZ + " minX: " + minMax.minX + " minZ: " + minMax.minZ);
            Dictionary<string, int> sides = new Dictionary<string, int>();
            sides.Add("zMin", levelMap.MapSize/2 - Math.Abs(minMax.minZ));
            sides.Add("xMin", levelMap.MapSize/2 - Math.Abs(minMax.minX));
            sides.Add("xMax", levelMap.MapSize/2 - Math.Abs(minMax.maxX));
            sides.Add("zMax", levelMap.MapSize/2 - Math.Abs(minMax.maxZ));

            var side = sides.OrderByDescending(x => x.Value).First();
            string maxKey = side.Key;
            int maxValue = side.Value;
            Debug.Log("maxKey: " + maxKey + " maxValue:  " + maxValue);
            var distance = 10;
            if (maxValue < 20) {
                distance = maxValue/2;
            }

            var offSide = getStartRandomSideIx();

            Debug.Log("distance: " + distance);
            switch(maxKey) {
                case "zMin": {
                    return (offSide, minMax.minZ - distance, GlobalDirection.North);
                }
                case "xMin": {
                    return (minMax.minX - distance, offSide, GlobalDirection.East);
                }
                case "zMax": {
                    return (offSide, minMax.maxZ + distance, GlobalDirection.South);
                }
                case "xMax": {
                    return (minMax.maxX + distance, offSide, GlobalDirection.West);
                }
                default: {
                    throw new Exception("shoudn't happend..");
                }
            }
        }
        private int getStartRandomSideIx() {
            var side = levelMap.MapSize/2;
            var mutedSide = (side/3) * 2;
            return UnityEngine.Random.Range(mutedSide * -1, mutedSide); 
        }
    }
}