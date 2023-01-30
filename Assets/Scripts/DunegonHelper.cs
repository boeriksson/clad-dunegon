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

        private List<(int, int)> globalKrockCoordinates = new List<(int, int)>();

        public DunegonHelper() {
            randomGenerator = new DefaultRandom();
        }
        public DunegonHelper(RandomGenerator _randomGenerator) {
            this.randomGenerator = _randomGenerator;
        }

        public Segment.Segment DecideNextSegment(int x, int z, GlobalDirection gDirection, LevelMap levelMap, int forks, Segment.Segment parent) {
            var possibleSegments = new List<(SegmentType, int)>();
            int totalWeight = 0;

            foreach (SegmentType segmentType in Enum.GetValues(typeof(SegmentType))) {
                Segment.Segment segment = segmentType.GetSegmentByType(x, z, gDirection, forks, null);
                var localSpaceNeeded = segment.NeededSpace();
                var globalSpaceNeeded = DirectionConversion.GetGlobalCoordinatesFromLocal(localSpaceNeeded, x, z, gDirection);
                if (checkIfSpaceIsAvailiable(globalSpaceNeeded, levelMap, segmentType)) {
                    if (goOnWithStraightDespiteKrock) {
                        Debug.Log("XXXXX JoinSegment!!");
                        goOnWithStraightDespiteKrock = false;
                        var joinSegment = new JoinSegment(x, z, gDirection, parent);
                        (List<Segment.Segment> addOnSegments, (int, int) exitCoord, (int, int) joinCoord) = GetJoinAddOnSegments(joinSegment, globalKrockCoordinates);
                        joinSegment.SetAddOnSegments(addOnSegments);
                        joinSegment.JoinExitCoord = exitCoord;
                        joinSegment.JoinCoord = joinCoord;
                        return joinSegment;
                    } else {
                        int segmentWeight = segmentType.GetSegmentTypeWeight(forks);
                        totalWeight += segmentWeight;
                        possibleSegments.Add(item: (segmentType, segmentWeight));
                    }
                } 
            }

            int ran = randomGenerator.Generate(totalWeight);
            int collectWeight = 0;

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

        private (List<Segment.Segment>, (int, int), (int, int)) GetJoinAddOnSegments(Segment.Segment joinSegment, List<(int, int)> krockCoordList) {
            (int, int) joinCoord = GetJoinCoord(joinSegment.X, joinSegment.Z, krockCoordList);
            return FindPath(joinSegment, joinCoord);
        }

        public (List<Segment.Segment>, (int, int), (int, int)) FindPath(Segment.Segment joinSegment, (int, int) joinCoord) {
            (int x2, int z2) = joinCoord;

            int xc = joinSegment.X;                                     //cursor
            int zc = joinSegment.Z;                                     //
            GlobalDirection cDirection = joinSegment.GlobalDirection;   //

            var prePath = new List<(int, int, GlobalDirection)>();

            while (xc != x2 || zc != z2) {
                if (Math.Abs(xc - x2) >= Math.Abs(zc - z2)) {
                    if (xc > x2) {
                        xc--; //South
                        cDirection = GlobalDirection.South;
                    } else {
                        xc++; //North
                        cDirection = GlobalDirection.North;
                    }
                } else {
                    if (zc > z2) {
                        zc--; //West
                        cDirection = GlobalDirection.West;
                    } else {
                        zc++; //East
                        cDirection = GlobalDirection.East;
                    }
                }

                if (xc == x2 && zc == z2) {
                    break;
                } else {
                    prePath.Add((xc, zc, cDirection));
                }
            }

            var path = new List<Segment.Segment>();
            (int, int) exitCoord = (-999, -999);
            for(int i = 0; i < prePath.Count; i++) {
                var step = prePath[i];
                var parent = i == 0 ? joinSegment : path[i - 1];
                //var segment = Segment.Q;
                if (i < prePath.Count - 1) {
                    var nextStep = prePath[i + 1];
                    if (step.Item3 == GlobalDirection.North) {
                        if (nextStep.Item3 == GlobalDirection.North) {
                            path.Add(new StraightSegment(step.Item1, step.Item2, step.Item3, parent: parent));
                        } else if (nextStep.Item3 == GlobalDirection.East) {
                            path.Add(new LeftSegment(step.Item1, step.Item2, step.Item3, parent));
                        } else {
                            path.Add(new RightSegment(step.Item1, step.Item2, step.Item3, parent));
                        }
                    } else if (step.Item3 == GlobalDirection.South) {
                        if (nextStep.Item3 == GlobalDirection.South) {
                            path.Add(new StraightSegment(step.Item1, step.Item2, step.Item3, parent: parent));
                        } else if (nextStep.Item3 == GlobalDirection.East) {
                            path.Add(new RightSegment(step.Item1, step.Item2, step.Item3, parent));
                        } else {
                            path.Add(new LeftSegment(step.Item1, step.Item2, step.Item3, parent));
                        }
                    } else if (step.Item3 == GlobalDirection.East) {
                        if (nextStep.Item3 == GlobalDirection.East) {
                            path.Add(new StraightSegment(step.Item1, step.Item2, step.Item3, parent: parent));
                        } else if (nextStep.Item3 == GlobalDirection.North) {
                            path.Add(new RightSegment(step.Item1, step.Item2, step.Item3, parent));
                        } else {
                            path.Add(new LeftSegment(step.Item1, step.Item2, step.Item3, parent));
                        }
                    } else if (step.Item3 == GlobalDirection.West) {
                        if (nextStep.Item3 == GlobalDirection.West) {
                            path.Add(new StraightSegment(step.Item1, step.Item2, step.Item3, parent: parent));
                        } else if (nextStep.Item3 == GlobalDirection.North) {
                            path.Add(new LeftSegment(step.Item1, step.Item2, step.Item3, parent));
                        } else {
                            path.Add(new RightSegment(step.Item1, step.Item2, step.Item3, parent));
                        }
                    } 
                } else { // Last step before join - this is the joinSegment exit?!
                    (int sx, int sz, GlobalDirection direction) = step;
                    exitCoord = (sx, sz);
                    (int jx, int jz) = joinCoord;
                    if (direction == GlobalDirection.North) {
                        if (sz == jz) {
                            path.Add(new StraightSegment(step.Item1, step.Item2, step.Item3, parent: parent));
                        } else if (sz > jz) {
                            path.Add(new RightSegment(step.Item1, step.Item2, step.Item3, parent));
                        } else {
                            path.Add(new LeftSegment(step.Item1, step.Item2, step.Item3, parent));
                        }
                    }
                    if (direction == GlobalDirection.East) {
                        if (sx == jx) {
                            path.Add(new StraightSegment(step.Item1, step.Item2, step.Item3, parent: parent));
                        } else if (sx > jx) {
                            path.Add(new RightSegment(step.Item1, step.Item2, step.Item3, parent));
                        } else {
                            path.Add(new LeftSegment(step.Item1, step.Item2, step.Item3, parent));
                        }
                    }
                    if (direction == GlobalDirection.South) {
                        if (sz == jz) {
                            path.Add(new StraightSegment(step.Item1, step.Item2, step.Item3, parent: parent));
                        } else if (sz > jz) {
                            path.Add(new LeftSegment(step.Item1, step.Item2, step.Item3, parent));
                        } else {
                            path.Add(new RightSegment(step.Item1, step.Item2, step.Item3, parent));
                        }
                    }
                    if (direction == GlobalDirection.West) {
                        if (sx == jx) {
                            path.Add(new StraightSegment(step.Item1, step.Item2, step.Item3, parent: parent));
                        } else if (sx > jx) {
                            path.Add(new LeftSegment(step.Item1, step.Item2, step.Item3, parent));
                        } else {
                            path.Add(new RightSegment(step.Item1, step.Item2, step.Item3, parent));
                        }
                    }

                }
            }

            if (exitCoord.Item1 == -999) {
                throw new Exception("FindPath.exitCord not initialized!");
            }
            return (path, exitCoord, joinCoord);
        }

        // Gets coordinates of the closest joinable segment
        private (int, int) GetJoinCoord(int x, int z, List<(int, int)> krockCoordList) {
            (int, int) joinCoord = (999, 999);
            foreach((int, int) coord in krockCoordList) {
                if ((Math.Abs(coord.Item1 - x) + Math.Abs(coord.Item2 - z)) < (Math.Abs(joinCoord.Item1 - x) + Math.Abs(joinCoord.Item2 - z))) {
                    joinCoord = coord;
                }
            }
            return joinCoord;
        }
 
        public Boolean checkIfSpaceIsAvailiable(List<(int, int)> globalSpaceNeeded, LevelMap levelMap, SegmentType segmentType) {
            globalKrockCoordinates.Clear();
            foreach((int, int) space in globalSpaceNeeded) {
                if (levelMap.GetValueAtCoordinate(space) != 0) {
                    if (
                        (segmentType == SegmentType.Straight || segmentType == SegmentType.Left || segmentType == SegmentType.Right)  
                        && levelMap.GetValueAtCoordinate(space) == 1 
                        && randomGenerator.Generate(100) > 0
                        ) {
                            Debug.Log("goOnWithStraightDespiteKrock = true spaceCoord: " + space + " SegmentType: " + segmentType.ToString());
                        goOnWithStraightDespiteKrock = true;
                        globalKrockCoordinates.Add(space);
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
