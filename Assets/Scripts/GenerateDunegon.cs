using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
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
        public int defaultMapLevels;
        public int scale;

        public GameObject playerObj;

        public GameObject cameraHolderObj;

        public int restartAfterBackWhenWSIsBelow = 2; // This amount or less forks in the workingset and we restart fork after backout
        private int currentSegment = 0;
        private DunegonHelper dHelper = new DunegonHelper();

        private EnvironmentMgr environmentMgr;

        private List<(SegmentExit, Segment.Segment)> workingSet = new List<(SegmentExit, Segment.Segment)>();
        private ConcurrentDictionary<(int, int, int), Segment.Segment> segmentDict = new ConcurrentDictionary<(int, int, int), Segment.Segment>();
        private LevelMap levelMap;
        private List<GameObject> marks = new List<GameObject>();
        private List<GameObject> exitList = new List<GameObject>();

        private GameObject mapDisplay;
        private bool showMap = false;

        void Start() {
            levelMap = new LevelMap(defaultMapSize, defaultMapLevels);
            mapDisplay = new GameObject(); 
            Debug.Log("defaultMapLevels: " + defaultMapLevels + " defaultMapLevels/2: " + (int)defaultMapLevels/2);
            environmentMgr = FindObjectOfType<EnvironmentMgr>();
            workingSet.Add((new SegmentExit(0, 0, defaultMapLevels/2, Direction.GlobalDirection.North, 0, 0, 0, Direction.LocalDirection.Straight), null));
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
                var globalSpaceNeeded = DirectionConversion.GetGlobalCoordinatesFromLocal(segment.NeededSpace(), segment.X, segment.Z, segment.Y, segment.GlobalDirection);
                var neededSpace = DebugUtil.printTupleList(globalSpaceNeeded);
                Debug.Log(segment.Type + " X: " + segment.X + " Z: " + segment.Z + " gDirection: " + segment.GlobalDirection + " Tiles: " + tilesStr + " NeededSpace: " + neededSpace);
                yield return null;
            }
        }

        IEnumerator AddWorkingSet() {
            RemoveOldMarksAndExits();

            List<(SegmentExit, Segment.Segment)> nextWorkingSet = new List<(SegmentExit, Segment.Segment)>();
            List<(int, int, int)> removeFromNextWorkingSetAfterLoop = new List<(int, int, int)>();
            //foreach ((SegmentExit, Segment.Segment) wsEntry in workingSet) {
            var workingSetCount = workingSet.Count;
            for (int i = workingSetCount -1; i >= 0; i--) {
                Debug.Log("new main loop iteration i = " + i + " workingSet.Count: " + workingSet.Count);
                printWorkingSet();
                var wsEntry = workingSet[i];
                var segmentStart = wsEntry.Item1;
                var parentSegment = wsEntry.Item2;
                Debug.Log("segmentStart: (" + segmentStart.X + ", " + segmentStart.Z + ", " + segmentStart.Y + ")");
                Segment.Segment segment = dHelper.DecideNextSegment(
                    segmentStart.X,
                    segmentStart.Z,
                    segmentStart.Y,
                    segmentStart.Direction,
                    levelMap.GetValueAtCoordinate,
                    GetSegmentWithTile,
                    workingSet.Count,
                    parentSegment
                );
                Debug.Log("MainLoop adding segment (" + segment.X + ", " + segment.Z + ", " + segment.Y + ") Type: " + segment.Type + " ref: " + RuntimeHelpers.GetHashCode(segment));
                if (!(segment is StopSegment)) {
                    if (segment is JoinSegment) {
                        try {
                            Debug.Log("JoinSegment x: " + segment.X + " z: " + segment.Z + " direction: " + segment.GlobalDirection);
                            new Join(
                                (JoinSegment)segment, 
                                AddSegment, 
                                UpdateSegment,
                                RemoveSegment,
                                ReplaceSegmentWithNewSegmentInWorkingSet,
                                levelMap.GetValueAtCoordinate,
                                GetSegmentWithTile,
                                GetChildrenOfSegment,
                                ChangeParentOfChildren, 
                                removeFromNextWorkingSetAfterLoop
                            );
                        } catch (JoinException ex) {
                            Debug.Log("JoinException - backing out - message: " + ex.Message);
                            Backout(segment);
                        }
                    } else {
                        Debug.Log("Main loop, adding segment type: " + segment.Type + " at (" + segment.X + ", " + segment.Z + ", " + segment.Y + ") ref: " + RuntimeHelpers.GetHashCode(segment) + " value at coord in levelMap: " + levelMap.GetValueAtCoordinate((segment.X, segment.Z, segment.Y)));
                        
                        if (removeFromNextWorkingSetAfterLoop.Find(coord => (coord.Item1 == segment.X && coord.Item2 == segment.Z)) == default) {
                            bool addSuccess = AddSegment(segment);
                            if (!addSuccess) {
                                Debug.Log("Fail to Add segment (" + segment.X + ", " + segment.Z + ", " + segment.Y + ") in mainloop...");
                                Backout(segment);
                            } else {
                                var addOnSegments = segment.GetAddOnSegments();
                                if (addOnSegments.Count > 0) {
                                    foreach(Segment.Segment addSegment in addOnSegments) {
                                        bool addAddSuccess = AddSegment(addSegment);    
                                        if (addAddSuccess) {
                                            AddExitsToNextWorkingSet(nextWorkingSet, addSegment);
                                            Debug.Log("Added addSegment (" + addSegment.X + ", " + addSegment.Z + ", " + addSegment.Y + ") to nextWorkingSet");
                                        } else {
                                            Backout(addSegment.Parent);
                                        }
                                    }
                                } else {
                                    AddExitsToNextWorkingSet(nextWorkingSet, segment);
                                    Debug.Log("Added Segment (" + segment.X + ", " + segment.Z + ", " + segment.Y + ") to nextWorkingSet");
                                }
                            }
                        }
                    }
                    currentSegment++;
                } else {
                    Debug.Log("Starting BackoutDeadEnd segment: (" + segment.X + ", " + segment.Z + ", " + segment.Y + ") type: " + segment.Type + " ------------------------------------------");
                    Backout(segment);
                    Debug.Log("##### StopSegment - Backing out of dead end! ----------------------------------------");
                }
            }
            // Remove entry from nextws in case of a join to an active thread
            Debug.Log("removeFromNextWorkingSetAfterLoop.Count: " + removeFromNextWorkingSetAfterLoop.Count);
            foreach((int x, int z, int y) coord in removeFromNextWorkingSetAfterLoop) {
                var ix = nextWorkingSet.FindIndex(wsEntry => (wsEntry.Item1.X == coord.x && wsEntry.Item1.Z == coord.z && wsEntry.Item1.Y == coord.y));
                if (ix > -1) {
                    Debug.Log("nextWorkingSet - removing entry with coord (" + coord.x + ", " + coord.z + ")");
                    nextWorkingSet.RemoveAt(ix);
                }
            }
            if (nextWorkingSet.Count < 1) {
                try {
                    var newStartCoord = GetNewStartCoord(defaultMapLevels/2);
                    Debug.Log("out of segments -> nextWorkingSet.Add newStartCoord: " + newStartCoord);
                    nextWorkingSet.Add((new SegmentExit(newStartCoord.Item1, newStartCoord.Item2, newStartCoord.Item3, newStartCoord.Item4, 0, 0, 0, Direction.LocalDirection.Straight), null));
                } catch (GenerateDunegonException ex) {
                    Debug.Log(ex.Message);
                }
            }
            levelMap.ClearContent(8);
            workingSet.Clear();
            workingSet.AddRange(nextWorkingSet);
            yield return null;
        }

        private void Backout(Segment.Segment segment) {
            var backout = new Backout(
                dHelper,
                RemoveSegment, 
                SetSegmentColor,
                AddSegment,
                UpdateSegment,
                GetChildrenOfSegment, 
                ChangeParentOfChildren,
                ReplaceSegmentWithNewSegmentInWorkingSet,
                IsBackableSegment
            );
            var backedOutSegment = backout.BackoutDeadEnd(segment, 0, 0, 0, workingSet.Count);
        }

        private void RemoveSegment(Segment.Segment segment) {
            ClearSegment(segment);
            RemoveSegmentFromDict(segment);
        }

        private void ClearSegment(Segment.Segment segment) {
            var instantiatedTiles = segment.Instantiated;
            foreach (GameObject tile in instantiatedTiles) {
                Destroy(tile);
            }
            levelMap.RemoveCoordinates(segment.GetTiles());
        }

        private bool AddSegment(Segment.Segment segment) {
            Debug.Log("AddSegment (" + segment.X + ", " + segment.Z + ", " + segment.Y + ")");
            return AddSegment(segment, true);
        }
        private bool AddSegment(Segment.Segment segment, bool scan, string strColor = "white") {
            if (AddSegmentToDict(segment)) {
                FurbishSegment(segment, scan, strColor);
                return true;
            } 
            return false;
        }

        private void UpdateSegment(Segment.Segment newSegment, Segment.Segment oldSegment, string strColor) {
            ClearSegment(oldSegment);
            FurbishSegment(newSegment, false, strColor);
            var key = (newSegment.X, newSegment.Z, newSegment.Y);
            if (!segmentDict.TryUpdate(key, newSegment, oldSegment)) {
                Segment.Segment ailingSegment;
                if (!segmentDict.TryRemove(key, out ailingSegment)) {
                    Debug.Log("UpdateSegment - remove Failed");
                }
                Debug.Log("UpdateSegment (" + newSegment.X + ", " + newSegment.Z + ") not successfull - incorrect segment found?! oldSegment type: " + ailingSegment.Type);
                ClearSegment(ailingSegment);
                var newSeg = segmentDict.AddOrUpdate(key, newSegment, (key, oldSeg) => newSegment);
                FurbishSegment(newSeg, false, "green");
                Debug.Log("UpdateSegment addOrUpdate, newSeg Type: " + newSeg.Type);
            }
        }

        private void FurbishSegment(Segment.Segment segment, bool scan, string strColor = "white") {
            var tiles = segment.GetTiles();
            var globalSpaceNeeded = DirectionConversion.GetGlobalCoordinatesFromLocal(segment.NeededSpace(), segment.X, segment.Z, segment.Y, segment.GlobalDirection);

            levelMap.AddCooridnates(tiles, 1);
            if (scan) {
                InstantiateExits(segment);
                ShowMarks(segment); // Debug show neededspace
            }

            Color color = GetColorByStr(strColor);
            SetInstantiatedTiles(segment, tiles, color);
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
            RemoveSegmentFromDict(segment);
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
                var iExit = Instantiate(exit, new Vector3(segmentExit.X * scale, segmentExit.Y * scale, segmentExit.Z * scale), Quaternion.identity) as GameObject;
                iExit.transform.localScale = new Vector3(scale, 0.5f, scale);
                exitList.Add(iExit);
            }
        }

        private void SetInstantiatedTiles(Segment.Segment segment, List<(int, int, int)> tiles) {
            SetInstantiatedTiles(segment, tiles, Color.white);
        }
        private void SetInstantiatedTiles(Segment.Segment segment, List<(int, int, int)> tiles, Color color) {
            var gSegments = segment.GetGSegments(environmentMgr);
            if (gSegments.Count == 0) {
                var instantiatedTiles = new List<GameObject>();
                foreach ((int x, int z, int y) in tiles) {
                    GameObject tileFloor = Instantiate(floor, new Vector3(x * scale, y * scale, z * scale), Quaternion.identity) as GameObject;
                    tileFloor.transform.localScale = new Vector3(scale, 0.1f, scale);
                    tileFloor.transform.SetParent(environmentMgr.transform);
                    instantiatedTiles.Add(tileFloor);
                }
                segment.Instantiated = instantiatedTiles;
            } else {
                var instantiatedGSegments = new List<GameObject>();
                foreach((int, int, int, GlobalDirection, float, GameObject) gSegment in gSegments) {
                    GameObject iGSegment = Instantiate(gSegment.Item6, new Vector3(gSegment.Item1 * scale, gSegment.Item3 * scale, gSegment.Item2 * scale), Quaternion.identity) as GameObject;
                    iGSegment.transform.Rotate(0.0f, gSegment.Item5, 0.0f, Space.Self);
                    iGSegment.transform.SetParent(environmentMgr.transform);
                    if (color != null) {
                        iGSegment.GetComponent<Renderer>().material.SetColor("_Color", color);
                    } 
                    TMP_Text debugText = Instantiate(environmentMgr.debugText, new Vector3(gSegment.Item1 * scale, (gSegment.Item3 * scale) - 0.98f, gSegment.Item2 * scale), Quaternion.identity);
                    var debugTextComp = debugText.GetComponent<TextMeshPro>();
                    debugTextComp.SetText("(" + gSegment.Item1 + "," + gSegment.Item2 + ", " + gSegment.Item3 + ")");
                    debugTextComp.color = new Color32(255, 99, 71, 255);
                    debugText.transform.Rotate(90f, 90f, 0f, Space.Self);
                    debugText.transform.SetParent(iGSegment.transform);

                    TMP_Text debugRefText = Instantiate(environmentMgr.debugText, new Vector3((gSegment.Item1 * scale) - 0.5f, (gSegment.Item3 * scale) - 0.98f, gSegment.Item2 * scale), Quaternion.identity);
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
            var globalNeededSpace = Direction.DirectionConversion.GetGlobalCoordinatesFromLocal(localSpaceNeeded, segment.X, segment.Z, segment.Y, segment.GlobalDirection);
            foreach ((int, int, int) mark in globalNeededSpace) {
                GameObject krockScan = Instantiate(this.mark, new Vector3(mark.Item1 * scale, mark.Item3 * scale, mark.Item2 * scale), Quaternion.identity) as GameObject;
                krockScan.transform.localScale = new Vector3(scale, 0.1f, scale);
                krockScan.transform.SetParent(environmentMgr.transform);
                marks.Add(krockScan);
            }
        }

        private void RemoveSegmentFromDict(Segment.Segment segment) {
            if (segmentDict.TryRemove((segment.X, segment.Z, segment.Y), out var removedSeg)) {
                Debug.Log("Removed segment type: " + removedSeg.Type + " at (" + segment.X + ", " + segment.Z + ", " + segment.Y + ")");
            } else {
                Debug.Log("Remove failed!!! Segment already removed segment type: " + segment.Type + " at (" + segment.X + ", " + segment.Z + ", " + segment.Y + ")");
            }
        }
        private bool AddSegmentToDict(Segment.Segment segment) {
            if (segmentDict.TryAdd((segment.X, segment.Z, segment.Y), segment)) {
                Debug.Log("Added segment type: " + segment.Type + " at (" + segment.X + ", " + segment.Z + ", " + segment.Y + ")");
                return true;
            } else {
                Debug.Log("Add segment failed!!! segment type: " + segment.Type + " at (" + segment.X + ", " + segment.Z + ", " + segment.Y + ")");
                return false;
            }
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
                for (int y = 0; y < map.GetLength(2); y++) {
                    for (int x = 0; x < map.GetLength(0); x++) {
                        for (int z = 0; z < map.GetLength(1); z++) {
                            if (map[x, z, y] == 1) {
                                var xPos = (x - map.GetLength(0)/2) * scale;
                                var zPos = (z - map.GetLength(1)/2) * scale;
                                var yPos = (y - map.GetLength(2)) * scale;
                                GameObject mapObj = Instantiate(
                                    mapper, 
                                    new Vector3(xPos, yPos, zPos), 
                                    Quaternion.identity
                                ) as GameObject;

                                mapObj.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
                                mapObj.transform.SetParent(mapDisplay.transform);
                            }
                        }
                    }
                }
            }
            yield return null;
        }
        public void ReplaceSegmentWithNewSegmentInWorkingSet(Segment.Segment oldSegment, Segment.Segment newSegment) {
            Debug.Log("ReplaceSegmentWithNewSegmentInWorkingSet start oldSeg Type: " + oldSegment.Type + " (" + oldSegment.X + ", " + oldSegment.Z + ")  newSegment Type: " + newSegment.Type + " (" + newSegment.X + ", " + newSegment.Z + ")");
            printWorkingSet();
            for (int i = 0; i < workingSet.Count; i++) {
                var wsSegment = workingSet[i].Item2;
                if (wsSegment != null && wsSegment.X == oldSegment.X && wsSegment.Z == oldSegment.Z) {
                    workingSet[i] = (workingSet[i].Item1, newSegment);
                }
            }
            Debug.Log("ReplaceSegmentWithNewSegmentInWorkingSet end");
            printWorkingSet();
        }

        public List<Segment.Segment> GetChildrenOfSegment(Segment.Segment parentSegment) {
            var resultList = new List<Segment.Segment>();
            foreach(Segment.Segment segment in segmentDict.Values) {
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

        private (bool, Segment.Segment) IsBackableSegment(Segment.Segment segment) {

            Segment.Segment actualSegment;
            if (!segmentDict.TryGetValue((segment.X, segment.Z, segment.Y), out actualSegment)) {
                Debug.Log("IsBackableSegment - Fail to get actualSegment at (" + segment.X + ", " + segment.Z + ", " + segment.Y + ")");
                actualSegment = segment;
            }

            var wsEntriesWithSegment = 0;
            foreach((SegmentExit, Segment.Segment) wsEntry in workingSet) {
                if (wsEntry.Item2 != null && wsEntry.Item2.X == segment.X && wsEntry.Item2.Z == segment.Z && wsEntry.Item2.Y == segment.Y) {
                    wsEntriesWithSegment ++;
                }
            }
            if (actualSegment.Type == SegmentType.Stop) return (true, actualSegment);
            if (wsEntriesWithSegment > 1) return (false, actualSegment);
            if (actualSegment is Room) return (false, actualSegment);
            if (actualSegment.Join) return (false, actualSegment);
            if (actualSegment.Exits.Count <= 1) return (true, actualSegment);
            if (GetChildrenOfSegment(actualSegment).Count < 1) return (true, actualSegment);
            return (false, actualSegment);
        }

        private (int, int, int, GlobalDirection) GetNewStartCoord(int mapLevel) {
            (int maxX, int maxZ, int minX, int minZ) minMax = levelMap.GetMinMaxPopulated(mapLevel);
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
            if (maxValue < 6) {
                throw new GenerateDunegonException("Too little space left for new starting point...");
            } else if (maxValue < 20) {
                distance = maxValue/2;
            }

            var offSide = getStartRandomSideIx();

            Debug.Log("distance: " + distance);
            switch(maxKey) {
                case "zMin": {
                    return (offSide, minMax.minZ - distance, mapLevel, GlobalDirection.North);
                }
                case "xMin": {
                    return (minMax.minX - distance, offSide, mapLevel, GlobalDirection.East);
                }
                case "zMax": {
                    return (offSide, minMax.maxZ + distance, mapLevel, GlobalDirection.South);
                }
                case "xMax": {
                    return (minMax.maxX + distance, offSide, mapLevel, GlobalDirection.West);
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

        private Segment.Segment GetSegmentWithTile((int, int, int) tileCoord) {
            foreach(Segment.Segment segment in segmentDict.Values) {
                var tiles = segment.GetTiles();
                if (tiles.Exists(tile => tile.Item1 == tileCoord.Item1 && tile.Item2 == tileCoord.Item2 && tile.Item3 == tileCoord.Item3)) {
                    if (!segmentDict.TryGetValue((segment.X, segment.Z, segment.Y), out var segmentWithTile)) {
                        throw new Exception("GetSegmentWithTile (" + segment.X + ", " + segment.Z + ") - Segment do no longer exist in dic??");
                    } else {
                        return segmentWithTile;
                    }
                }
            }
            throw new Exception("GetSegmentWithTile segment with tile (" + tileCoord.Item1 + ", " + tileCoord.Item2 + ") do not exist?!");
        }

        private void printWorkingSet() {
            var printStr = "workingSet: \n";
            foreach((SegmentExit exit, Segment.Segment seg) work in workingSet) {
                var parentStr = work.seg == null ? " parent == null " : " parent Type: " + work.seg.Type + " at (" + work.seg.X + ", " + work.seg.Z + ", " + work.seg.Y + ")";
                printStr += "  exit (" + work.exit.X + ", " + work.exit.Z + ", " + work.exit.Y + ") " + parentStr + "\n";
            }
            Debug.Log(printStr);
        }
    }

    public class GenerateDunegonException : Exception {
        public GenerateDunegonException(string message) : base(message) {
        }
    }
}