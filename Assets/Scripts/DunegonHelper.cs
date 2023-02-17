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
            Debug.Log("STOPSEGMENT!!!");
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

        private string printTupleList(List<(int, int)> tupleList) {
            var result = "";
            foreach ((int, int) tuple in tupleList) {
                result += ", (" + tuple.Item1 + ", " + tuple.Item2 + ")";
            }
            return result;
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
        public Segment.Segment RedoSegmentWithOneLessExit(Segment.Segment redoSegment, (int x, int z) exit) {
            void LogProblem(Segment.Segment redoSegment, int leX, int leZ) {
                Debug.Log("RedoSegmentWithOneLessExit problem---------------------------------------\n" + 
                "redoSegment: {" + redoSegment.X + ", " + redoSegment.Z + "} gDirection: " + redoSegment.GlobalDirection + "\n" +
                "Remove exit on " + redoSegment.Type + " exitToRemove: {" + leX + ", " + leZ + "}\n" + 
                "----------------------------------------------------------------------------");
            }
            (int leX, int leZ) = GetLocalCooridnatesForSegment(redoSegment, (exit.x, exit.z));
            Debug.Log("RedoSegmentWithOneLessExit type: " + redoSegment.Type + " localCoord: (" + leX + ", " + leZ + ")" );
            switch (redoSegment.Type) {
                case SegmentType.LeftRight: {
                    if (leX == 0 && leZ == -1) {
                        Debug.Log("LeftRight - left");
                        return SegmentType.Right.GetSegmentByType(redoSegment.X, redoSegment.Z, redoSegment.GlobalDirection, 0, redoSegment.Parent);
                    } 
                    if (leX == 0 && leZ == 1) {
                        Debug.Log("LeftRight - right");
                        return SegmentType.Left.GetSegmentByType(redoSegment.X, redoSegment.Z, redoSegment.GlobalDirection, 0, redoSegment.Parent);
                    }
                    break;
                }
                case SegmentType.StraightRight: {
                    if (leX == 1 && leZ == 0) {
                        Debug.Log("StraightRight - right");
                        return SegmentType.Right.GetSegmentByType(redoSegment.X, redoSegment.Z, redoSegment.GlobalDirection, 0, redoSegment.Parent);
                    } 
                    if (leX == 0 && leZ == 1) {
                        Debug.Log("StraightRight - straight");
                        return SegmentType.Straight.GetSegmentByType(redoSegment.X, redoSegment.Z, redoSegment.GlobalDirection, 0, redoSegment.Parent);
                    }
                    break;
                }
                case SegmentType.StraightLeft: {
                    if (leX == 1 && leZ == 0) {
                        Debug.Log("StraightLeft - left");
                        return SegmentType.Left.GetSegmentByType(redoSegment.X, redoSegment.Z, redoSegment.GlobalDirection, 0, redoSegment.Parent);
                    } 
                    if (leX == 0 && leZ == -1) {
                        Debug.Log("StraightLeft - straight");
                        return SegmentType.Straight.GetSegmentByType(redoSegment.X, redoSegment.Z, redoSegment.GlobalDirection, 0, redoSegment.Parent);
                    }
                    break;
                }
                case SegmentType.LeftStraightRight: {
                    if (leX == 1 && leZ == 0) {
                        Debug.Log("LeftStraightRight - leftRight");
                        return SegmentType.LeftRight.GetSegmentByType(redoSegment.X, redoSegment.Z, redoSegment.GlobalDirection, 0, redoSegment.Parent);
                    } 
                    if (leX == 0 && leZ == 1) {
                        Debug.Log("LeftStraightRight - straightRight");
                        return SegmentType.StraightRight.GetSegmentByType(redoSegment.X, redoSegment.Z, redoSegment.GlobalDirection, 0, redoSegment.Parent);
                    }
                    if (leX == 0 && leZ == -1) {
                        Debug.Log("LeftStraightRight - straightLeft");
                        return SegmentType.StraightLeft.GetSegmentByType(redoSegment.X, redoSegment.Z, redoSegment.GlobalDirection, 0, redoSegment.Parent);
                    }
                    break;
                }
                default: {
                    if (redoSegment is Room) {
                        SegmentExit sExit = null;
                        try {
                            (int geX, int geZ) = DirectionConversion.GetGlobalCoordinatesFromLocal(new List<(int, int)>() {(leX, leZ)}, redoSegment.X, redoSegment.Z, redoSegment.GlobalDirection)[0];
                            sExit = redoSegment.GetExitByCoord(geX, geZ);
                        } catch (RedoSegmentException) {
                            string redoSegExits = "";
                            foreach (SegmentExit ex in redoSegment.Exits) {
                                redoSegExits += "   (" + ex.X + "," + ex.Z + ") direction: " + ex.Direction + "\n";
                            }
                            Debug.Log("RedoSegmentWithOneLessExit problem---------------------------------------\n" + 
                            "redoSegment: {" + redoSegment.X + ", " + redoSegment.Z + "} gDirection: " + redoSegment.GlobalDirection + "\n" +
                            "Remove exit on " + redoSegment.Type + " exitToRemove: {" + leX + ", " + leZ + "}\n" + 
                            "exits on redoSegment: \n" + 
                            redoSegExits +
                            "----------------------------------------------------------------------------");
                        }
                        if (redoSegment is Room3x3Segment) {
                            return new Room3x3Segment((Room3x3Segment)redoSegment, sExit);
                        } else if (redoSegment is Room3x4Segment) {
                            return new Room3x4Segment((Room3x4Segment)redoSegment, sExit);
                        } else if (redoSegment is RoomVariableSegment) {
                            return new RoomVariableSegment((RoomVariableSegment)redoSegment, sExit);
                        }
                    }
                    break;
                }
            }
            LogProblem(redoSegment, leX, leZ);
            throw new RedoSegmentException("RedoSegmentWithOneLessExit segment not found...");
        }
    }

    public class RedoSegmentException : Exception {
        public RedoSegmentException(string message) : base(message) {}
    }
}
