using System;
using System.Collections;
using System.Collections.Generic;
using Segment;
using GlobalDirection = Direction.GlobalDirection;
using Debug = UnityEngine.Debug;

namespace Dunegon {
    public class Join {

        public void doJoin(JoinSegment joinSegment, Action<Segment.Segment> AddSegment, Action<Segment.Segment> ClearSegment, List<Segment.Segment> segmentList) {
            (List<Segment.Segment> addOnSegments, (int, int) exitCoord, (int, int) joinCoord) = GetJoinAddOnSegments(joinSegment);
            Debug.Log("AddonSegments: " + addOnSegments.Count + " exitCoord: " + exitCoord.ToString() + " joinCoord: " + joinCoord.ToString());
            joinSegment.SetAddOnSegments(addOnSegments);
            joinSegment.JoinExitCoord = exitCoord;
            joinSegment.JoinCoord = joinCoord;
            var joiningSegment = GetSegmentWithTile(joinCoord, segmentList);
            Debug.Log("JoiningSegment type: " + joiningSegment.Type + " direction: " + joiningSegment.GlobalDirection);
            /*
            foreach (Segment.Segment addSegment in joinSegment.GetAddOnSegments()) {
                AddSegment(addSegment);
            }
            ReplaceJoiningSegmentWithPlusExitSegment(joiningSegment, exitCoord, AddSegment, ClearSegment);
            */
        }

        private (List<Segment.Segment>, (int, int), (int, int)) GetJoinAddOnSegments(JoinSegment joinSegment) {
            joinSegment.JoinCoord = GetJoinCoord(joinSegment.X, joinSegment.Z, joinSegment.KrockCoords);
            Debug.Log("JoinCoord: " + joinSegment.JoinCoord);
            return FindPath(joinSegment);
        }

        private (int, int) GetJoinCoord(int x, int z, List<(int, int)> krockCoordList) {
            (int, int) joinCoord = (999, 999);
            foreach((int, int) coord in krockCoordList) {
                if ((Math.Abs(coord.Item1 - x) + Math.Abs(coord.Item2 - z)) < (Math.Abs(joinCoord.Item1 - x) + Math.Abs(joinCoord.Item2 - z))) {
                    joinCoord = coord;
                }
            }
            return joinCoord;
        }

        public (List<Segment.Segment>, (int, int), (int, int)) FindPath(JoinSegment joinSegment) {
            (int xj, int zj) = joinSegment.JoinCoord;

            int xc = joinSegment.X;                                     //cursor
            int zc = joinSegment.Z;                                     //
            GlobalDirection cDirection = joinSegment.GlobalDirection;   //

            var prePath = new List<(int, int, GlobalDirection)>();

            while (xc != xj || zc != zj) {
                if (Math.Abs(xc - xj) >= Math.Abs(zc - zj)) {
                    if (xc > xj) {
                        xc--; //South
                        cDirection = GlobalDirection.South;
                    } else {
                        xc++; //North
                        cDirection = GlobalDirection.North;
                    }
                } else {
                    if (zc > zj) {
                        zc--; //West
                        cDirection = GlobalDirection.West;
                    } else {
                        zc++; //East
                        cDirection = GlobalDirection.East;
                    }
                }

                if (xc == xj && zc == zj) {
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
                    if (direction == GlobalDirection.North) {
                        if (sz == zj) {
                            path.Add(new StraightSegment(step.Item1, step.Item2, step.Item3, parent: parent));
                        } else if (sz > zj) {
                            path.Add(new RightSegment(step.Item1, step.Item2, step.Item3, parent));
                        } else {
                            path.Add(new LeftSegment(step.Item1, step.Item2, step.Item3, parent));
                        }
                    }
                    if (direction == GlobalDirection.East) {
                        if (sx == xj) {
                            path.Add(new StraightSegment(step.Item1, step.Item2, step.Item3, parent: parent));
                        } else if (sx > xj) {
                            path.Add(new RightSegment(step.Item1, step.Item2, step.Item3, parent));
                        } else {
                            path.Add(new LeftSegment(step.Item1, step.Item2, step.Item3, parent));
                        }
                    }
                    if (direction == GlobalDirection.South) {
                        if (sz == zj) {
                            path.Add(new StraightSegment(step.Item1, step.Item2, step.Item3, parent: parent));
                        } else if (sz > zj) {
                            path.Add(new LeftSegment(step.Item1, step.Item2, step.Item3, parent));
                        } else {
                            path.Add(new RightSegment(step.Item1, step.Item2, step.Item3, parent));
                        }
                    }
                    if (direction == GlobalDirection.West) {
                        if (sx == xj) {
                            path.Add(new StraightSegment(step.Item1, step.Item2, step.Item3, parent: parent));
                        } else if (sx > xj) {
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
            return (path, exitCoord, joinSegment.JoinCoord);
        }

        private Segment.Segment GetSegmentWithTile((int, int) tileCoord, List<Segment.Segment> segmentList) {
            foreach(Segment.Segment segment in segmentList) {
                var tiles = segment.GetTiles();
                if (tiles.Exists(tile => tile.Item1 == tileCoord.Item1 && tile.Item2 == tileCoord.Item2)) {
                    return segment;
                }
            }
            throw new Exception("GetSegmentWithTile - segmentNotFound!");
        }

        private void ReplaceJoiningSegmentWithPlusExitSegment(Segment.Segment joiningSegment, (int, int) exitCoord, Action<Segment.Segment> AddSegment, Action<Segment.Segment> ClearSegment) {
            Debug.Log("### ReplaceJoiningSegmentWithPlusExitSegment ###");
            var localExitCoordinates = GetLocalCooridnatesForSegment(joiningSegment, exitCoord);
            var newSegment = RedoSegmentWithAdditionalExit(joiningSegment, localExitCoordinates);
            Debug.Log("NewSegment Type: " + newSegment.Type);
            ClearSegment(joiningSegment);
            AddSegment(newSegment);
        }

        private Segment.Segment RedoSegmentWithAdditionalExit(Segment.Segment oldSegment, (int, int) localExitCoordinates) {
            Debug.Log("OldSegment Type: " + oldSegment.Type.Equals(SegmentType.Straight));
            switch (oldSegment.Type) {
                case SegmentType.Straight: {
                    if (localExitCoordinates.Item1 == 0 && localExitCoordinates.Item2 == 1) {
                        return SegmentType.StraightRight.GetSegmentByType(oldSegment.X, oldSegment.Z, oldSegment.GlobalDirection, 0, oldSegment.Parent);
                    } else if (localExitCoordinates.Item1 == 0 && localExitCoordinates.Item2 == -1) {
                        return SegmentType.StraightLeft.GetSegmentByType(oldSegment.X, oldSegment.Z, oldSegment.GlobalDirection, 0, oldSegment.Parent);
                    }
                    break;
                }
                case SegmentType.Right: {
                    if (localExitCoordinates.Item1 == 1 && localExitCoordinates.Item2 == 0) {
                        return SegmentType.StraightRight.GetSegmentByType(oldSegment.X, oldSegment.Z, oldSegment.GlobalDirection, 0, oldSegment.Parent);
                    } else if (localExitCoordinates.Item1 == 0 && localExitCoordinates.Item2 == -1) {
                        return SegmentType.LeftRight.GetSegmentByType(oldSegment.X, oldSegment.Z, oldSegment.GlobalDirection, 0, oldSegment.Parent);
                    }
                    break;
                }
                case SegmentType.Left: {
                    if (localExitCoordinates.Item1 == 1 && localExitCoordinates.Item2 == 0) {
                        return SegmentType.StraightLeft.GetSegmentByType(oldSegment.X, oldSegment.Z, oldSegment.GlobalDirection, 0, oldSegment.Parent);
                    } else if (localExitCoordinates.Item1 == 0 && localExitCoordinates.Item2 == 1) {
                        return SegmentType.LeftRight.GetSegmentByType(oldSegment.X, oldSegment.Z, oldSegment.GlobalDirection, 0, oldSegment.Parent);
                    }
                    break;
                }
                case SegmentType.LeftRight: {
                    if (localExitCoordinates.Item1 == 1 && localExitCoordinates.Item2 == 0) {
                        return SegmentType.LeftStraightRight.GetSegmentByType(oldSegment.X, oldSegment.Z, oldSegment.GlobalDirection, 0, oldSegment.Parent);
                    } 
                    break;
                }
                case SegmentType.StraightRight: {
                    if (localExitCoordinates.Item1 == 0 && localExitCoordinates.Item2 == -1) {
                        return SegmentType.LeftStraightRight.GetSegmentByType(oldSegment.X, oldSegment.Z, oldSegment.GlobalDirection, 0, oldSegment.Parent);
                    } 
                    break;  
                }
                case SegmentType.StraightLeft: {
                    if (localExitCoordinates.Item1 == 0 && localExitCoordinates.Item2 == -1) {
                        return SegmentType.LeftStraightRight.GetSegmentByType(oldSegment.X, oldSegment.Z, oldSegment.GlobalDirection, 0, oldSegment.Parent);
                    } 
                    break;  
                }
                default: {
                    if (oldSegment is Room) {
                        //Later.. 
                    }
                    break;
                }
            }
            
            throw new Exception("RedoSegmentWithAdditionalExit - unreqognized joinSegment: " + oldSegment.Type);
        }

        private (int, int) GetLocalCooridnatesForSegment(Segment.Segment segment, (int, int) gCoord) { //ToDo: Move to Direction
            switch(segment.GlobalDirection) {
                case GlobalDirection.North: {
                    return (gCoord.Item1 - segment.X, gCoord.Item2 - segment.Z);
                }
                case GlobalDirection.East: {
                    return (segment.X - gCoord.Item1, gCoord.Item2 - segment.Z);
                }
                case GlobalDirection.South: {
                    return (segment.X - gCoord.Item1, segment.Z - gCoord.Item2);
                }
                case GlobalDirection.West: {
                    return (gCoord.Item1 - segment.X, segment.Z - gCoord.Item2);
                }
            }
            throw new Exception("Segment.Globaldirection not reqognized..");
        }

    }
}
