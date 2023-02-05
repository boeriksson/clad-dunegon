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
using LevelMap = level.LevelMap;

using Debug = UnityEngine.Debug;

namespace Dunegon {
    public class GenerateDunegon : MonoBehaviour {
        public GameObject floor;
        public GameObject mark;
        public GameObject mapper;
        public GameObject exit;
        public int noOfSegments;
        public int defaultMapSize;
        public int scale;

        public int restartAfterBackWhenWSIsBelow = 2; // This amount or less forks in the workingset and we restart fork after backout
        private int currentSegment = 0;
        private DunegonHelper dHelper = new DunegonHelper();

        private Join join = new Join();

        private EnvironmentMgr environmentMgr;

        private List<(SegmentExit, Segment.Segment)> workingSet = new List<(SegmentExit, Segment.Segment)>();
        private List<Segment.Segment> segmentList = new List<Segment.Segment>();
        private LevelMap levelMap = new LevelMap();
        private List<GameObject> marks = new List<GameObject>();
        private List<GameObject> exitList = new List<GameObject>();
        //Logger logger = new Logger("./Logs/dunegon.log");

        // Start is called before the first frame update
        void Start() {
            environmentMgr = FindObjectOfType<EnvironmentMgr>();
            workingSet.Add((new SegmentExit(0, 0, Direction.GlobalDirection.North, 0, 0, Direction.LocalDirection.Straight), null));
        }

        void Update() {
            if (Input.GetKeyDown(KeyCode.Space)) {
                if (currentSegment < noOfSegments && workingSet.Count > 0) {
                    StartCoroutine(AddWorkingSet());
                }
            }
            if (Input.GetKeyDown(KeyCode.M)) {
                StartCoroutine(ShowMap());
            }
            if (Input.GetKeyDown(KeyCode.B)) {
                StartCoroutine(BackupWorkingSet());
            }
            if (Input.GetKeyDown(KeyCode.P)) {
                StartCoroutine(PrintSegments());
            }
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
            foreach ((SegmentExit, Segment.Segment) wsEntry in workingSet) {
                var segmentStart = wsEntry.Item1;
                var parentSegment = wsEntry.Item2;
                Segment.Segment segment = dHelper.DecideNextSegment(
                    segmentStart.X,
                    segmentStart.Z,
                    segmentStart.Direction,
                    levelMap,
                    workingSet.Count,
                    parentSegment
                );
                if (!(segment is StopSegment)) {
                    AddSegment(segment);
                    if (segment is JoinSegment) {
                        Debug.Log("JoinSegment x: " + segment.X + " z: " + segment.Z + " direction: " + segment.GlobalDirection);
                        join.doJoin((JoinSegment)segment, AddSegment, ClearSegment, segmentList, levelMap);
                    } else {
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
                    var workingSetSize = workingSet.Count;
                    var backedOutSegment = BackoutDeadEnd(segment, 0, 0, workingSetSize);
                    Debug.Log("##### StopSegment - Backing out of dead end! backedOutSegment: " + backedOutSegment.Type);
                    if (workingSetSize < restartAfterBackWhenWSIsBelow) {
                        nextWorkingSet.Add((backedOutSegment.Exits[0], backedOutSegment));
                    }
                }
            }
            levelMap.ClearContent(8);
            workingSet.Clear();
            workingSet.AddRange(nextWorkingSet);
            yield return null;
        }

        private void AddSegment(Segment.Segment segment, bool scan = true)
        {
            var tiles = segment.GetTiles();
            var globalSpaceNeeded = DirectionConversion.GetGlobalCoordinatesFromLocal(segment.NeededSpace(), segment.X, segment.Z, segment.GlobalDirection);
            levelMap.AddCooridnates(globalSpaceNeeded, 8);
            levelMap.AddCooridnates(tiles, 1);
            if (scan) {
                InstantiateExits(segment);
                ShowMarks(segment); // Debug show neededspace
            }
            SetInstantiatedTiles(segment, tiles);
            segmentList.Add(segment);
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
                    instantiatedGSegments.Add(iGSegment);
                }
                segment.Instantiated = instantiatedGSegments;
            }
        }

/*
        private void RotateSegment((int, int, GlobalDirection, float, GameObject) gSegment, GameObject iGSegment, Segment.Segment segment) {
            var segmentRotation = gSegment.Item4;
            var rotation = 0.0f;
            Debug.Log("Segment: " + segment.Type + " Rotation: " + rotation + " GlobalDirection: " + gSegment.Item3);
            iGSegment.transform.Rotate(0.0f, rotation, 0.0f, Space.Self);
        }
*/
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
            //Debug.Log("Segment: " + segment.Type + " x: " + segment.X + " z: " + segment.Z + " gDirection: " + segment.GlobalDirection + " localSpaceNeeded: " + DebugUtil.printTupleList(localSpaceNeeded));
            var globalNeededSpace = Direction.DirectionConversion.GetGlobalCoordinatesFromLocal(localSpaceNeeded, segment.X, segment.Z, segment.GlobalDirection);
            //Debug.Log("Segment: " + segment.Type + " globalSpaceNeeded: " + DebugUtil.printTupleList(globalNeededSpace));
            foreach ((int, int) mark in globalNeededSpace) {
                GameObject krockScan = Instantiate(this.mark, new Vector3(mark.Item1 * scale, 0, mark.Item2 * scale), Quaternion.identity) as GameObject;
                krockScan.transform.localScale = new Vector3(scale, 0.1f, scale);
                krockScan.transform.SetParent(environmentMgr.transform);
                marks.Add(krockScan);
            }
        }

        private Segment.Segment BackoutDeadEnd(Segment.Segment segment, int exitX, int exitZ, int workingSetSize) {
            var backedOutSegment = segment;
            var backableSegmentsArray = new SegmentType[] {SegmentType.Straight, SegmentType.Left, SegmentType.Right, SegmentType.Stop, SegmentType.LeftRight, SegmentType.LeftStraightRight, SegmentType.StraightNoCheck};
            if (segment.Exits.Count <= 1 && backableSegmentsArray.Contains(segment.Type)) {
                ClearSegment(segment);
                backedOutSegment = BackoutDeadEnd(segment.Parent, segment.X, segment.Z, workingSetSize);
            } else { // We're gonna remove the exit in segment where we roll back to
                if (workingSetSize >= restartAfterBackWhenWSIsBelow) {
                    var segmentExits = segment.Exits;
                    var segmentExit = segmentExits.Single(exit => exit.X == exitX && exit.Z == exitZ);
                    var stopSegment = SegmentType.Stop.GetSegmentByType(segmentExit.X, segmentExit.Z, segmentExit.Direction, workingSetSize, backedOutSegment, true);
                    AddSegment(stopSegment);
                    //segmentExits.Remove(segmentExitToRemove);
                }
            }
            return backedOutSegment;
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
            var map = levelMap.Map;
            for (int x = 0; x < map.GetLength(0); x++) {
                for (int z = 0; z < map.GetLength(1); z++) {
                    if (map[x, z] != 0) {
                        GameObject mapObj = Instantiate(mapper, new Vector3((x - (map.GetLength(0)/2)) * scale, 0, (z - (map.GetLength(1)/2))) * scale, Quaternion.identity) as GameObject;
                        mapObj.transform.localScale = new Vector3(scale, 0.3f, scale);
                        mapObj.transform.SetParent(environmentMgr.transform);
                    }
                }
            }
            yield return null;
        }
    }
}