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

        public DunegonHelper() {
            randomGenerator = new DefaultRandom();
        }
        public DunegonHelper(RandomGenerator _randomGenerator) {
            this.randomGenerator = _randomGenerator;
        }

        public Segment.Segment DecideNextSegment(int x, int z, GlobalDirection gDirection, LevelMap levelMap, int forks, Segment.Segment parent) {
            var possibleSegments = new List<(SegmentType, int)>();
            int totalWeight = 0;
            List<(int, int)> krockCoords = new List<(int, int)>();

            foreach (SegmentType segmentType in Enum.GetValues(typeof(SegmentType))) {
                Segment.Segment segment = segmentType.GetSegmentByType(x, z, gDirection, forks, null);
                var localSpaceNeeded = segment.NeededSpace();
                var globalSpaceNeeded = DirectionConversion.GetGlobalCoordinatesFromLocal(localSpaceNeeded, x, z, gDirection);
                (bool unJoinableKrock, List<(int, int)> globalJoinableKrockCoord) = checkIfSpaceIsAvailiable(globalSpaceNeeded, levelMap, segmentType);
                if (!unJoinableKrock) {
                    if (globalJoinableKrockCoord.Count > 0) {
                        krockCoords.AddRange(globalJoinableKrockCoord);
                        int segmentWeight = 100;
                        totalWeight += segmentWeight;
                        possibleSegments.Add(item: (SegmentType.Join, segmentWeight));
                    } else {
                        int straigthParentChain = GetStraightParentChain(parent);
                        int segmentWeight = segmentType.GetSegmentTypeWeight(forks, straigthParentChain);
                        totalWeight += segmentWeight;
                        possibleSegments.Add(item: (segmentType, segmentWeight));
                    }
                } 
            }

            int ran = randomGenerator.Generate(totalWeight);
            int collectWeight = 0;
            //logger.WriteLine("DecideOnNextSegment possibleSegments Count: " + possibleSegments.Count + " possibleSegments: " + logger.PrintPossibleSegments(possibleSegments) + " ran: " + ran + " totalWeight: " + totalWeight);

            foreach ((SegmentType segmentType, int weight) in possibleSegments) {
                collectWeight += weight;
                if (collectWeight >= ran) {
                    var segment = segmentType.GetSegmentByType(x, z, gDirection, forks, parent, true);
                    if (segment is JoinSegment) {
                        Debug.Log("JoinSegment krock: ");
                        ((JoinSegment)segment).KrockCoords = krockCoords;
                        return segment;
                    }
                    return segment; 
                }
            }
            Debug.Log("STOPSEGMENT!!! -> (" + x + ", " + z + ") GlobalDirection: " + gDirection + " parent: " + parent.Type);
            return new StopSegment(x, z, gDirection, parent);
        }

        public int GetStraightParentChain(Segment.Segment segment, int ix = 0) {
            if (segment == null) return 0;
            var straightParents = new SegmentType[] {SegmentType.Straight, SegmentType.Left, SegmentType.Right, SegmentType.Join};
            if (!straightParents.Contains(segment.Type)) return ix;
            if (segment.Parent == null) return ix;
            return GetStraightParentChain(segment.Parent, ix++);
        }

        public (bool, List<(int, int)>) checkIfSpaceIsAvailiable(List<(int, int)> globalSpaceNeeded, LevelMap levelMap, SegmentType segmentType) {
            var globalJoinableKrockCoord = new List<(int, int)>();
            foreach((int, int) space in globalSpaceNeeded) {
                if (levelMap.GetValueAtCoordinate(space) != 0) {
                    if (segmentType == SegmentType.Straight  && levelMap.GetValueAtCoordinate(space) == 1) {
                        globalJoinableKrockCoord.Add(space);
                    } else {
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

        public List<Segment.Segment> GetChildrenOfSegment(Segment.Segment parentSegment, List<Segment.Segment> segmentList) {
            var resultList = new List<Segment.Segment>();
            foreach(Segment.Segment segment in segmentList) {
                if (System.Object.ReferenceEquals(segment.Parent, parentSegment)) {
                    resultList.Add(segment);
                }
            }
            return resultList;
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
    }
}
