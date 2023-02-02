using System;
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
                        int segmentWeight = segmentType.GetSegmentTypeWeight(forks);
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
            Debug.Log("STOPSEGMENT!!!");
            return new StopSegment(x, z, gDirection, parent);
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

        private string printTupleList(List<(int, int)> tupleList) {
            var result = "";
            foreach ((int, int) tuple in tupleList) {
                result += ", (" + tuple.Item1 + ", " + tuple.Item2 + ")";
            }
            return result;
        }
    }
}
