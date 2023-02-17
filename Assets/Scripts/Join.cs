using System;
using System.Collections;
using System.Collections.Generic;
using Segment;
using System.Linq;
using GlobalDirection = Direction.GlobalDirection;
using LocalDirection = Direction.LocalDirection;
using Debug = UnityEngine.Debug;
using LevelMap = level.LevelMap;

namespace Dunegon {
    public class Join {
        private DunegonHelper dHelper = new DunegonHelper();

        public void doJoin(JoinSegment joinSegment, Action<Segment.Segment, bool> AddSegment, Action<Segment.Segment> ClearSegment, List<Segment.Segment> segmentList, LevelMap levelMap) {
            (List<Segment.Segment> addOnSegments, (int, int) exitCoord, (int, int) joinCoord) = GetJoinAddOnSegments(joinSegment, levelMap);
            Debug.Log("AddonSegments: " + addOnSegments.Count + " exitCoord: " + exitCoord.ToString() + " joinCoord: " + joinCoord.ToString());
            joinSegment.SetAddOnSegments(addOnSegments);
            joinSegment.JoinExitCoord = exitCoord;
            joinSegment.JoinCoord = joinCoord;
            var joiningSegment = GetSegmentWithTile(joinCoord, segmentList);
            Debug.Log("JoiningSegment type: " + joiningSegment.Type + " direction: " + joiningSegment.GlobalDirection);
            
            foreach (Segment.Segment addSegment in joinSegment.GetAddOnSegments()) {
                AddSegment(addSegment, false);
            }
            ReplaceJoiningSegmentWithPlusExitSegment(joiningSegment, joinSegment, exitCoord, AddSegment, ClearSegment);
        }

        private (List<Segment.Segment>, (int, int), (int, int)) GetJoinAddOnSegments(JoinSegment joinSegment, LevelMap levelMap) {
            joinSegment.JoinCoord = GetJoinCoord(joinSegment.X, joinSegment.Z, joinSegment.KrockCoords);
            Debug.Log("JoinCoord: " + joinSegment.JoinCoord);
            return FindPath(joinSegment, levelMap);
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

        public void GetPrePathRecursive(List<(int, int, GlobalDirection)> prePath, ref int xc, ref int zc, ref int ix, GlobalDirection cDirection, int xj, int zj, LevelMap levelMap) {
            ix++;
            if (ix > 10) {
                return;
            }
            (int, GlobalDirection) xSelection(int xc, int xj) => xc > xj ? (-1, GlobalDirection.South) : (1, GlobalDirection.North); 
            (int, GlobalDirection) zSelection(int zc, int zj) => zc > zj ? (-1, GlobalDirection.West) : (1, GlobalDirection.East); 
            bool exitCondition(int xc, int xcAdd, int zc, int zcAdd) => (xc + xcAdd) == xj && (zc + zcAdd) == zj;
            bool isTiled(int x, int z) => levelMap.GetValueAtCoordinate((x, z)) == 1;
            var xcAdd = 0;
            var zcAdd = 0;
            if (Math.Abs(xc - xj) >= Math.Abs(zc - zj)) { 
                (xcAdd, cDirection) = xSelection(xc, xj);
            } else {
                (zcAdd, cDirection) = zSelection(zc, zj);
            }

            if (isTiled(xc + xcAdd, zc + zcAdd) && !exitCondition( xc, xcAdd, zc, zcAdd)) { // We've hit paved dunegon, try the other axel
                if (zcAdd == 0) {
                    xcAdd = 0;
                    (zcAdd, cDirection) = zSelection(zc, zj);
                } else {
                    zcAdd = 0;
                    (xcAdd, cDirection) = xSelection(xc, xj);
                }
                if (isTiled(xc + xcAdd, zc + zcAdd)) { 
                    throw new JoinException("GetPrePathRecursive tried both axels, still blocked...");
                }

                var xCheck = xc + xcAdd;
                var zCheck = zc + zcAdd;
                if (prePath.Exists(((int, int, GlobalDirection) pp) => pp.Item1 == xCheck && pp.Item2 == zCheck)) {
                    throw new JoinException("prePath exists, backtracking..");
                }
            }
            if (exitCondition( xc, xcAdd, zc, zcAdd)) {
                return;
            }
            xc += xcAdd;
            zc += zcAdd;
            prePath.Add((xc, zc, cDirection));
            GetPrePathRecursive(prePath, ref xc, ref zc, ref ix, cDirection, xj, zj, levelMap);
        } 

        public (List<Segment.Segment>, (int, int), (int, int)) FindPath(JoinSegment joinSegment, LevelMap levelMap) {
            (int xj, int zj) = joinSegment.JoinCoord;
            var ix = 0;
            var joinExit = joinSegment.Exits[0];
            int xc = joinExit.X;                                      //cursor
            int zc = joinExit.Z;                                      //
            GlobalDirection cDirection = joinSegment.GlobalDirection; //

            var prePath = new List<(int, int, GlobalDirection)>();
            prePath.Add((xc, zc, joinExit.Direction));

            GetPrePathRecursive(prePath, ref xc, ref zc, ref ix, cDirection, xj, zj, levelMap);

            var path = new List<Segment.Segment>();
            (int, int) exitCoord = (xj, zj);
            for(int i = 0; i < prePath.Count; i++) {
                var step = prePath[i];
                var parent = i == 0 ? joinSegment : path[i - 1];
                //var segment = Segment.Q;
                if (i == 0) { //first addonSegment after join - check/modify type of join!
                    SegmentExit jExit = joinSegment.Exits[0];
                    if (jExit.X != step.Item1 || jExit.Z != step.Item2) {
                        Debug.Log("Wooohooooo joinExit not connected to stepSegment! joinExit: (" + jExit.X + ", " + jExit.Z + ") step0: (" + step.Item1 + "," + step.Item2 + ")");
                    }
                } 
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
                            path.Add(new LeftSegment(step.Item1, step.Item2, step.Item3, parent));
                        } else {
                            path.Add(new RightSegment(step.Item1, step.Item2, step.Item3, parent));
                        }
                    } else if (step.Item3 == GlobalDirection.West) {
                        if (nextStep.Item3 == GlobalDirection.West) {
                            path.Add(new StraightSegment(step.Item1, step.Item2, step.Item3, parent: parent));
                        } else if (nextStep.Item3 == GlobalDirection.North) {
                            path.Add(new RightSegment(step.Item1, step.Item2, step.Item3, parent));
                        } else {
                            path.Add(new LeftSegment(step.Item1, step.Item2, step.Item3, parent));
                        }
                    } 
                } else { // Last step before join - this is the joinSegment exit?!
                    (int sx, int sz, GlobalDirection direction) = step;
                    exitCoord = (sx, sz);
                    if (direction == GlobalDirection.North) {
                        if (sz == zj) {
                            path.Add(new StraightSegment(step.Item1, step.Item2, step.Item3, parent: parent));
                        } else if (sz > zj) {
                            path.Add(new LeftSegment(step.Item1, step.Item2, step.Item3, parent));
                        } else {
                            path.Add(new RightSegment(step.Item1, step.Item2, step.Item3, parent));
                        }
                    }
                    if (direction == GlobalDirection.East) {
                        if (sx == xj) {
                            Debug.Log("Gd.East sx == xj");
                            path.Add(new StraightSegment(step.Item1, step.Item2, step.Item3, parent: parent));
                        } else if (sx > xj) {
                            Debug.Log("Gd.East sx > xj");
                            path.Add(new LeftSegment(step.Item1, step.Item2, step.Item3, parent));
                        } else {
                            Debug.Log("Gd.East sx < xj");
                            path.Add(new RightSegment(step.Item1, step.Item2, step.Item3, parent));
                        }
                    }
                    if (direction == GlobalDirection.South) {
                        if (sz == zj) {
                            Debug.Log("Gd.South sz == zj");
                            path.Add(new StraightSegment(step.Item1, step.Item2, step.Item3, parent: parent));
                        } else if (sz > zj) {
                            Debug.Log("Gd.South sz > zj");
                            path.Add(new RightSegment(step.Item1, step.Item2, step.Item3, parent));
                        } else {
                            Debug.Log("Gd.South sz < zj");
                            path.Add(new LeftSegment(step.Item1, step.Item2, step.Item3, parent));
                        }
                    }
                    if (direction == GlobalDirection.West) {
                        if (sx == xj) {
                            Debug.Log("Gd.West sx == xj");
                            path.Add(new StraightSegment(step.Item1, step.Item2, step.Item3, parent: parent));
                        } else if (sx > xj) {
                            Debug.Log("Gd.South sx > xj");
                            path.Add(new LeftSegment(step.Item1, step.Item2, step.Item3, parent));
                        } else {
                            Debug.Log("Gd.South sx < xj");
                            path.Add(new RightSegment(step.Item1, step.Item2, step.Item3, parent));
                        }
                    }

                }
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
            throw new JoinException("GetSegmentWithTile - segmentNotFound!");
        }

        private void ReplaceJoiningSegmentWithPlusExitSegment(Segment.Segment joiningSegment, JoinSegment joinSegment, (int, int) exitCoord, Action<Segment.Segment, bool> AddSegment, Action<Segment.Segment> ClearSegment) {
            var localExitCoordinates = dHelper.GetLocalCooridnatesForSegment(joiningSegment, exitCoord);
            var newSegment = RedoSegmentWithAdditionalExit(joiningSegment, joinSegment, localExitCoordinates);
            ClearSegment(joiningSegment);
            AddSegment(newSegment, false);
        }

        private Segment.Segment RedoSegmentWithAdditionalExit(Segment.Segment joiningSegment, JoinSegment joinSegment, (int x, int z) le) {
            void LogProblem() {
                Debug.Log("RedoSegmentWithAdditionalExit problem---------------------------------------\n" + 
                "JoinSegment: {" + joinSegment.X + ", " + joinSegment.Z + "} gDirection: " + joinSegment.GlobalDirection + " addOnSegments: " + joinSegment.GetAddOnSegments().Count + "\n" +
                "JoiningSegment Type: " + joiningSegment.Type + " {" + joiningSegment.X + ", " + joiningSegment.Z + "} gDirection: " + joiningSegment.GlobalDirection + "\n" + 
                "Estimated new exit on joiningSegment: {" + le.x + ", " + le.z + "}\n" + 
                "----------------------------------------------------------------------------");
            }
            LocalDirection joiningSide = GetJoiningSide(joiningSegment, joinSegment);
            Debug.Log("RedoSegmentWithAdditionalExit joiningSegment Type: " + joiningSegment.Type + " joiningSide: " + joiningSide);
            switch (joiningSegment.Type) {
                case SegmentType.Straight: {
                    if ((le.x == 0 && le.z == 1) || (le.x == 0 && le.z == 0 && joiningSide == LocalDirection.Right)) {
                        return SegmentType.StraightRight.GetSegmentByType(joiningSegment.X, joiningSegment.Z, joiningSegment.GlobalDirection, 0, joiningSegment.Parent);
                    } else if (le.x == 0 && le.z == -1|| (le.x == 0 && le.z == 0 && joiningSide == LocalDirection.Left)) {
                        return SegmentType.StraightLeft.GetSegmentByType(joiningSegment.X, joiningSegment.Z, joiningSegment.GlobalDirection, 0, joiningSegment.Parent);
                    }
                    break;
                }
                case SegmentType.Join: {
                    if ((le.x == 0 && le.z == 1) || (le.x == 0 && le.z == 0 && joiningSide == LocalDirection.Right)) {
                        return SegmentType.StraightRight.GetSegmentByType(joiningSegment.X, joiningSegment.Z, joiningSegment.GlobalDirection, 0, joiningSegment.Parent);
                    } else if (le.x == 0 && le.z == -1|| (le.x == 0 && le.z == 0 && joiningSide == LocalDirection.Left)) {
                        return SegmentType.StraightLeft.GetSegmentByType(joiningSegment.X, joiningSegment.Z, joiningSegment.GlobalDirection, 0, joiningSegment.Parent);
                    }
                    break;
                }
                case SegmentType.Stop: {
                    if ((le.x == 1 && le.z == 0) || (le.x == 0 && le.z == 0 && joiningSide == LocalDirection.Straight)) {
                        return SegmentType.Straight.GetSegmentByType(joiningSegment.X, joiningSegment.Z, joiningSegment.GlobalDirection, 0, joiningSegment.Parent);
                    } else if (le.x == 0 && le.z == -1|| (le.x == 0 && le.z == 0 && joiningSide == LocalDirection.Left)) {
                        return SegmentType.Left.GetSegmentByType(joiningSegment.X, joiningSegment.Z, joiningSegment.GlobalDirection, 0, joiningSegment.Parent);
                    } else if (le.x == 0 && le.z == 1|| (le.x == 0 && le.z == 0 && joiningSide == LocalDirection.Right)) {
                        return SegmentType.Right.GetSegmentByType(joiningSegment.X, joiningSegment.Z, joiningSegment.GlobalDirection, 0, joiningSegment.Parent);
                    }
                    break;
                }
                case SegmentType.Right: {
                    if (le.x == 1 && le.z == 0|| (le.x == 0 && le.z == 0 && joiningSide == LocalDirection.Straight)) {
                        return SegmentType.StraightRight.GetSegmentByType(joiningSegment.X, joiningSegment.Z, joiningSegment.GlobalDirection, 0, joiningSegment.Parent);
                    } else if (le.Item1 == 0 && le.z == -1|| (le.x == 0 && le.z == 0 && joiningSide == LocalDirection.Left)) {
                        return SegmentType.LeftRight.GetSegmentByType(joiningSegment.X, joiningSegment.Z, joiningSegment.GlobalDirection, 0, joiningSegment.Parent);
                    }
                    break;
                }
                case SegmentType.Left: {
                    if (le.x == 1 && le.z == 0|| (le.x == 0 && le.z == 0 && joiningSide == LocalDirection.Straight)) {
                        return SegmentType.StraightLeft.GetSegmentByType(joiningSegment.X, joiningSegment.Z, joiningSegment.GlobalDirection, 0, joiningSegment.Parent);
                    } else if (le.Item1 == 0 && le.z == 1) {
                        return SegmentType.LeftRight.GetSegmentByType(joiningSegment.X, joiningSegment.Z, joiningSegment.GlobalDirection, 0, joiningSegment.Parent);
                    }
                    break;
                }
                case SegmentType.LeftRight: {
                    if (le.x == 1 && le.z == 0|| (le.x == 0 && le.z == 0 && joiningSide == LocalDirection.Straight)) {
                        return SegmentType.LeftStraightRight.GetSegmentByType(joiningSegment.X, joiningSegment.Z, joiningSegment.GlobalDirection, 0, joiningSegment.Parent);
                    } 
                    break;
                }
                case SegmentType.StraightRight: {
                    if (le.x == 0 && le.z == 1 || (le.x == 0 && le.z == 0 && joiningSide == LocalDirection.Left)) {
                        return SegmentType.LeftStraightRight.GetSegmentByType(joiningSegment.X, joiningSegment.Z, joiningSegment.GlobalDirection, 0, joiningSegment.Parent);
                    } 
                    break;  
                }
                case SegmentType.StraightLeft: {
                    if (le.x == 0 && le.z == 1 || (le.x == 0 && le.z == 0 && joiningSide == LocalDirection.Right)) {
                        return SegmentType.LeftStraightRight.GetSegmentByType(joiningSegment.X, joiningSegment.Z, joiningSegment.GlobalDirection, 0, joiningSegment.Parent);
                    } 
                    break;  
                }
                default: {
                    if (joiningSegment is Room) {
                        Debug.Log("JoiningSegment IS Room .. Type: " + joiningSegment.Type);
                        var newExit = new SegmentExit(joiningSegment.X, joiningSegment.Z, joiningSegment.GlobalDirection, le.x, le.z, joiningSide);
                        Debug.Log("newExit: (" + newExit.X + "," + newExit.Z + ")");
                        if (joiningSegment is Room3x3Segment) {
                            return new Room3x3Segment((Room3x3Segment)joiningSegment, newExit);
                        } else if (joiningSegment is Room3x4Segment) {
                            return new Room3x4Segment((Room3x4Segment)joiningSegment, newExit);
                        } else if (joiningSegment is RoomVariableSegment) {
                            return new RoomVariableSegment((RoomVariableSegment)joiningSegment, newExit);
                        }
                        Debug.Log("Fail to decide which room?! ");
                    }
                    break;
                }
            }
            LogProblem();
            
            throw new JoinException("RedoSegmentWithAdditionalExit - unreqognized joinSegment: " + joiningSegment.Type);
        }

        private LocalDirection GetJoiningSide(Segment.Segment joiningSegment, JoinSegment joinSegment) {
            switch (joiningSegment.GlobalDirection) {
                case GlobalDirection.North: {
                    switch (joinSegment.GlobalDirection) {
                        case GlobalDirection.North: {
                            return LocalDirection.Back;
                        }
                        case GlobalDirection.East: {
                            return LocalDirection.Left;
                        }
                        case GlobalDirection.West: {
                            return LocalDirection.Right;
                        }
                        case GlobalDirection.South: {
                            return LocalDirection.Straight;
                        }
                    }
                    break;
                }
                case GlobalDirection.West: {
                    switch (joinSegment.GlobalDirection) {
                        case GlobalDirection.North: {
                            return LocalDirection.Left;
                        }
                        case GlobalDirection.East: {
                            return LocalDirection.Straight;
                        }
                        case GlobalDirection.West: {
                            return LocalDirection.Back;
                        }
                        case GlobalDirection.South: {
                            return LocalDirection.Right;
                        }
                    }
                    break;
                }
                case GlobalDirection.South: {
                    switch (joinSegment.GlobalDirection) {
                        case GlobalDirection.North: {
                            return LocalDirection.Straight;
                        }
                        case GlobalDirection.East: {
                            return LocalDirection.Right;
                        }
                        case GlobalDirection.West: {
                            return LocalDirection.Left;
                        }
                        case GlobalDirection.South: {
                            return LocalDirection.Back;
                        }
                    }
                    break;
                }
                case GlobalDirection.East: {
                    switch (joinSegment.GlobalDirection) {
                        case GlobalDirection.North: {
                            return LocalDirection.Right;
                        }
                        case GlobalDirection.East: {
                            return LocalDirection.Back;
                        }
                        case GlobalDirection.West: {
                            return LocalDirection.Straight;
                        }
                        case GlobalDirection.South: {
                            return LocalDirection.Left;
                        }
                    }
                    break;
                }
            }
            throw new JoinException("GetJoiningSide unknown globalDirection...");
        }

    }

    public class JoinException : Exception {
        public JoinException(string message) : base(message) {
        }
    }
}
