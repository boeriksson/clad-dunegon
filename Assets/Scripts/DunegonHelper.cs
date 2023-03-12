using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;
using Debug = UnityEngine.Debug;

using Segment;
using SegmentType = Segment.SegmentType;
using DirectionConversion = Direction.DirectionConversion;
using GlobalDirection = Direction.GlobalDirection;
using LevelMap = level.LevelMap;
using RandomGenerator = util.RandomGenerator;
using DefaultRandom = util.DefaultRandom;

namespace Dunegon {

    public class DunegonHelper {
        private RandomGenerator randomGenerator;
        private Logger logger = new Logger("Logs/debug");

        public DunegonHelper() {
            randomGenerator = new DefaultRandom();

        }
        public DunegonHelper(RandomGenerator _randomGenerator) {
            this.randomGenerator = _randomGenerator;
        }

        public Segment.Segment DecideNextSegment(
            int x, 
            int z, 
            GlobalDirection gDirection, 
            Func<(int, int), int> GetLevelMapValueAtCoordinate,
            Func<(int, int), Segment.Segment> GetSegmentWithTile, 
            int forks, 
            Segment.Segment parent
        ) {
            var possibleSegments = new List<(SegmentType, int)>();
            int totalWeight = 0;
            List<(int, int)> krockCoords = new List<(int, int)>();
            var debugStr = "";
            (int, int) joinCoord = (999, 999);

            if (parent == null) {
                return SegmentType.Start.GetSegmentByType(x, z, gDirection, 0, null, true);
            }

            foreach (SegmentType segmentType in Enum.GetValues(typeof(SegmentType))) {
                Segment.Segment segment = segmentType.GetSegmentByType(x, z, gDirection, forks, null);
                var localSpaceNeeded = segment.NeededSpace();
                var globalSpaceNeeded = DirectionConversion.GetGlobalCoordinatesFromLocal(localSpaceNeeded, x, z, gDirection);
                (bool unJoinableKrock, List<(int, int)> globalJoinableKrockCoord) = checkIfSpaceIsAvailiable(globalSpaceNeeded, GetLevelMapValueAtCoordinate, segmentType);
                debugStr += "\nDecideNextSegment Type: " + segment.Type + " segment direction " + segment.GlobalDirection + " unJoinableKrock: " + unJoinableKrock + " globalJoinableKrockCoord.Count: " + globalJoinableKrockCoord.Count;
                if (!unJoinableKrock) {
                    if (globalJoinableKrockCoord.Count > 0) {
                        krockCoords.AddRange(globalJoinableKrockCoord);
                    } else {
                        int straightParentChain = GetStraightParentChain(parent);
                        int segmentWeight = segmentType.GetSegmentTypeWeight(forks, straightParentChain);
                        totalWeight += segmentWeight;
                        possibleSegments.Add(item: (segmentType, segmentWeight));
                    }
                } 
            }
            if (krockCoords.Count > 0) {
                joinCoord = GetJoinCoord(x, z, gDirection, krockCoords, GetLevelMapValueAtCoordinate);
                logger.WriteLine("Should be called once.. ");
                //if (!TooTightLoop(parent, joinCoord, GetSegmentWithTile)) {
                    int segmentWeight = 100;
                    totalWeight += segmentWeight;
                    possibleSegments.Add(item: (SegmentType.Join, segmentWeight));
                //}
            }
            debugStr += "\npossibleSegments.Count: " + possibleSegments.Count;
            int ran = randomGenerator.Generate(totalWeight);
            int collectWeight = 0;

            foreach ((SegmentType segmentType, int weight) in possibleSegments) {
                collectWeight += weight;
                if (collectWeight >= ran) {
                    var segment = segmentType.GetSegmentByType(x, z, gDirection, forks, parent, true);
                    if (segment is JoinSegment) {
                        ((JoinSegment)segment).JoinCoord = joinCoord;
                        return segment;
                    }
                    return segment; 
                }
            }
            Debug.Log("STOPSEGMENT!!! -> (" + x + ", " + z + ") GlobalDirection: " + gDirection + " parent: " + (parent != null ? parent.Type : "no parent!"));
            Debug.Log(debugStr);
            return new StopSegment(x, z, gDirection, parent);
        }

        private bool TooTightLoop(Segment.Segment startSeg, (int, int) coord, Func<(int, int), Segment.Segment> GetSegmentWithTile, int ix = 0, List<(int, int)> visited = null) {
            if (visited == null) {
                visited = new List<(int, int)>();
                logger.WriteLine("visited == null, creating a new List");
            }
            if (visited.Exists(lcoord => lcoord.Item1 == coord.Item1 && lcoord.Item2 == coord.Item2)) {
                logger.WriteLine("visited exists - returning false..");
                return false;
            } else {
                visited.Add(coord);
            }
            Segment.Segment segment;
            try {
                segment = GetSegmentWithTile(coord);
            } catch (Exception ex) {
                logger.WriteLine("segment not found ex, returning false..");
                return false;
            }
            logger.WriteLine("TooTightLoop startSeg at (" + startSeg.X + ", " + startSeg.Z + ") Type: " + startSeg.Type + " | segment at (" + segment.X + ", " + segment.Z  + ") Type: " + segment.Type);
            if (segment.X == startSeg.X && segment.Z == startSeg.Z) {
                logger.WriteLine("startSeg found - returning true");
                return true;
            }
            if (ix > 3) {
                logger.WriteLine("ix > 9 returning false");
                return false;

            }           
            var segParent = segment.Parent;
            if (segParent == null) {
                logger.WriteLine("segParent == null");
            } else {
                if (TooTightLoop(startSeg, (segParent.X, segParent.Z), GetSegmentWithTile, ix++, visited)) {
                    logger.WriteLine("segParent true..");
                    return true;
                }
            }
            foreach(SegmentExit exit in segment.Exits) {
                if (TooTightLoop(startSeg, (exit.X, exit.Z), GetSegmentWithTile, ix++, visited)) {
                    logger.WriteLine("segExit true...");
                    return true;
                }
            }
            logger.WriteLine("TightLoop end returning false for segment at (" + segment.X + ", " + segment.Z  + ")");
            return false;
        }

        private (int, int) GetJoinCoord(int x, int z, GlobalDirection gDirection, List<(int, int)> krockCoordList, Func<(int, int), int> GetLevelMapValueAtCoordinate) {
            Debug.Log("GetJoinCoord (" + x + ", " + z + ")");
            if (GetLevelMapValueAtCoordinate((x, z)) == 1) {
                krockCoordList.Add((x, z));
            }
            var xplus1Coord = DirectionConversion.GetGlobalCoordinateFromLocal((1, 0), x, z, gDirection);
            if (GetLevelMapValueAtCoordinate(xplus1Coord) == 1) {
                Debug.Log("GetJoinCoord adding joinSegment + 1 coordinates to krocklist: " + xplus1Coord);
                krockCoordList.Add(xplus1Coord);
            }
            var orderedKrockCoordList = krockCoordList.OrderBy(coord => GetDistanceCompBetweenCoords(coord, (x, z))).ToList();
            return orderedKrockCoordList[0];
        }

        private float GetDistanceCompBetweenCoords((int, int) coord1, (int, int) coord2) {
            (int x1, int z1) = coord1;
            (int x2, int z2) = coord2;
            return ((x1 - x2) * (x1 - x2) + (z1 - z2) * (z1 - z2)); 
        }

        public int GetStraightParentChain(Segment.Segment segment, int ix = 0) {
            if (segment == null) return 0;
            var straightParents = new SegmentType[] {SegmentType.Straight, SegmentType.Left, SegmentType.Right, SegmentType.Join};
            if (!straightParents.Contains(segment.Type)) return ix;
            if (segment.Parent == null) return ix;
            return GetStraightParentChain(segment.Parent, ix++);
        }

        public (bool, List<(int, int)>) checkIfSpaceIsAvailiable(
            List<(int, int)> globalSpaceNeeded, 
            Func<(int, int), int> GetLevelMapValueAtCoordinate, 
            SegmentType segmentType
        ) {
            var globalJoinableKrockCoord = new List<(int, int)>();
            foreach((int, int) space in globalSpaceNeeded) {
                if (GetLevelMapValueAtCoordinate(space) != 0) {
                    if (segmentType == SegmentType.Straight  && GetLevelMapValueAtCoordinate(space) == 1) {
                        globalJoinableKrockCoord.Add(space);
                    } else {
                        //Debug.Log("checkIfSpaceIsAvailiable " + segmentType + " coord: (" + space.Item1 + ", " + space.Item2 + ")  valueAtCoord: " + GetLevelMapValueAtCoordinate(space));
                        return (true, new List<(int, int)>());
                    }
                }
            }
            return (false, globalJoinableKrockCoord);
        }

        public (int, int) GetLocalCooridnatesForSegment(Segment.Segment segment, (int, int) gCoord) { 
            switch(segment.GlobalDirection) {
                case GlobalDirection.North: {
                    return (gCoord.Item1 - segment.X, gCoord.Item2 - segment.Z);
                }
                case GlobalDirection.East: {
                    return (gCoord.Item2 - segment.Z, segment.X - gCoord.Item1);
                }
                case GlobalDirection.South: {
                    return (segment.X - gCoord.Item1, segment.Z - gCoord.Item2);
                }
                case GlobalDirection.West: {
                    return (segment.Z - gCoord.Item2, gCoord.Item1 - segment.X);
                }
            }
            throw new Exception("Segment.Globaldirection not reqognized..");
        }

        public void AddNewParentToChildren(Segment.Segment parent, List<Segment.Segment> segmentChildren) {
            foreach(Segment.Segment segment in segmentChildren) {
                segment.Parent = parent;
            }
        }

        private string printTupleList(List<(int, int)> tupleList) {
            var result = "";
            foreach ((int, int) tuple in tupleList) {
                result += ", (" + tuple.Item1 + ", " + tuple.Item2 + ")";
            }
            return result;
        }

        public void RemoveDanglingWorkingTreads(List<(SegmentExit, Segment.Segment)> workingSet, Segment.Segment segment) {
            workingSet.RemoveAll(workItem => workItem.Item2 == segment);
        }

    }
}
