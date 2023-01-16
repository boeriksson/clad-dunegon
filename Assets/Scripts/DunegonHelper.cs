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

        private bool goOnWithStraightDespiteKrock;

        public DunegonHelper() {
            randomGenerator = new DefaultRandom();
        }
        public DunegonHelper(RandomGenerator _randomGenerator) {
            this.randomGenerator = _randomGenerator;
        }

        public Segment.Segment DecideNextSegment(int x, int z, GlobalDirection gDirection, LevelMap levelMap, Logger logger, int forks, Segment.Segment parent) {
            var possibleSegments = new List<(SegmentType, int)>();
            int totalWeight = 0;

            foreach (SegmentType segmentType in Enum.GetValues(typeof(SegmentType))) {
                Segment.Segment segment = segmentType.GetSegmentByType(x, z, gDirection, forks, null);
                var localSpaceNeeded = segment.NeededSpace();
                var globalSpaceNeeded = DirectionConversion.GetGlobalCoordinatesFromLocal(localSpaceNeeded, x, z, gDirection);
                if (checkIfSpaceIsAvailiable(globalSpaceNeeded, levelMap, logger, segmentType)) {
                    if (goOnWithStraightDespiteKrock) {
                        possibleSegments.Add((SegmentType.DoubleStraight, 100));
                        Debug.Log("XXXXX DoubleStraight!!!");
                        goOnWithStraightDespiteKrock = false;
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
                    //levelMap.AddCooridnates(segment.NeededSpace(), 8);
                    Debug.Log("Returning: " + segment.Type + " parent: " + segment.Parent?.Type);
                    return segment; 
                }
            }
            Debug.Log("STOPSEGMENT!!!");
            return new StopSegment(x, z, gDirection, parent);
        }

        public Boolean checkIfSpaceIsAvailiable(List<(int, int)> globalSpaceNeeded, LevelMap levelMap, Logger logger, SegmentType segmentType) {
            foreach((int, int) space in globalSpaceNeeded) {
                if (levelMap.GetValueAtCoordinate(space) != 0) {
                    if (
                        segmentType == SegmentType.Straight 
                        && levelMap.GetValueAtCoordinate(space) == 1 
                        && randomGenerator.Generate(100) > 85
                        ) {
                        //logger.WriteLine("############################### Going on with straightsegment despite KROCK!!!");
                        goOnWithStraightDespiteKrock = true;
                        return true;
                    }
                    return false;
                }
            }
            return true;
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
