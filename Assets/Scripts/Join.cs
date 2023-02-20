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
        private List<Segment.Segment> segmentList;
        private LevelMap levelMap;
        private JoinSegment joinSegment;
        private List<(SegmentExit, Segment.Segment)> workingSet;

        public Join(
            JoinSegment _joinSegment, 
            Action<Segment.Segment, bool, string> AddSegment, 
            Action<Segment.Segment> ClearSegment,  
            List<Segment.Segment> _segmentList, 
            LevelMap _levelMap,
            List<(SegmentExit, Segment.Segment)> workingSet
        ) {
            joinSegment = _joinSegment;
            Debug.Log("Start join joiningSegment at: (" + joinSegment.X + ", " + joinSegment.Z + ") ==========================================================================");
            segmentList = _segmentList;
            levelMap = _levelMap;

            (List<Segment.Segment> addOnSegments, (int, int) exitCoord, (int, int) joinCoord) = GetJoinAddOnSegments();
            Debug.Log("AddonSegments: " + addOnSegments.Count + " exitCoord: " + exitCoord.ToString() + " joinCoord: " + joinCoord.ToString());
            joinSegment.SetAddOnSegments(addOnSegments);
            joinSegment.JoinExitCoord = exitCoord;
            joinSegment.JoinCoord = joinCoord;
            var joiningSegment = GetSegmentWithTile(joinCoord);
            Debug.Log("JoiningSegment type: " + joiningSegment.Type + " direction: " + joiningSegment.GlobalDirection);
            
            foreach (Segment.Segment addSegment in joinSegment.GetAddOnSegments()) {
                AddSegment(addSegment, false, "yellow");
            }
            try {
                dHelper.RemoveDanglingWorkingTreads(workingSet, joiningSegment);
                ReplaceJoiningSegmentWithPlusExitSegment(
                    joiningSegment, 
                    joinSegment, 
                    exitCoord, 
                    AddSegment, 
                    ClearSegment
                ); 
            } catch (JoinException ex) {
                foreach (Segment.Segment addSegment in joinSegment.GetAddOnSegments()) {
                    ClearSegment(addSegment);
                }
                throw new JoinException(ex.Message);
            }
            Debug.Log("End join joiningSegment at: (" + joinSegment.X + ", " + joinSegment.Z + ") ==========================================================================");
        }

        private (List<Segment.Segment>, (int, int), (int, int)) GetJoinAddOnSegments() {
            joinSegment.JoinCoord = GetJoinCoord(joinSegment.X, joinSegment.Z, joinSegment.KrockCoords);
            Debug.Log("JoinCoord: " + joinSegment.JoinCoord);
            return FindPath();
        }

        private (int, int) GetJoinCoord(int x, int z, List<(int, int)> krockCoordList) {
            var orderedKrockCoordList = krockCoordList.OrderBy(coord => GetDistanceCompBetweenCoords(coord, (x, z))).ToList();
            return orderedKrockCoordList[0];
        }

        private float GetDistanceCompBetweenCoords((int, int) coord1, (int, int) coord2) {
            (int x1, int z1) = coord1;
            (int x2, int z2) = coord2;
            return ((x1 - x2) * (x1 - x2) + (z1 - z2) * (z1 - z2)); 
        }

        public void GetPrePathRecursive(List<(int, int, GlobalDirection)> prePath, ref int xc, ref int zc, ref int ix, GlobalDirection cDirection, int xj, int zj) {
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

            if (exitCondition( xc, xcAdd, zc, zcAdd)) {
                return;
            }
            if (isTiled(xc + xcAdd, zc + zcAdd)) { // We've hit paved dunegon, try the other axel
                Debug.Log("GetPrePathRecursive is tiled but not exitCoordinate.. xc/zc: (" + xc + ", " + zc + ")");
                if (zcAdd == 0) {
                    xcAdd = 0;
                    (zcAdd, cDirection) = zSelection(zc, zj);
                } else {
                    zcAdd = 0;
                    (xcAdd, cDirection) = xSelection(xc, xj);
                }
                if (xc == xj && zc == zj) {
                    Debug.Log("We're already at joincoord");
                    return;
                }
                if (isTiled(xc + xcAdd, zc + zcAdd)) { 
                    Debug.Log("GetPrePathRecursive tried both axels, still blocked...xc: " + xc + " xcAdd: " + xcAdd + " zc: " + zc + " zcAdd: " + zcAdd + " xj: " + xj + " zj: " + zj);
                    Debug.Log("joinSegment.joinCoord is  (" + xj + ", " + zj + ")  we're trying to add (" + (xc + xcAdd) + ", " + (zc + zcAdd) + ") to path..");
                    throw new Exception("Error in FindPath");
                }

                var xCheck = xc + xcAdd;
                var zCheck = zc + zcAdd;
                if (prePath.Exists(((int, int, GlobalDirection) pp) => pp.Item1 == xCheck && pp.Item2 == zCheck)) {
                    throw new JoinException("prePath exists, backtracking..");
                }
            }
            xc += xcAdd;
            zc += zcAdd;
            Debug.Log("GetPrePathRecursive Adding this to path -> xc/zc: (" + xc + ", " + zc + ") GlobalDirection: " + cDirection);
            prePath.Add((xc, zc, cDirection));
            GetPrePathRecursive(prePath, ref xc, ref zc, ref ix, cDirection, xj, zj);
        } 

        public (List<Segment.Segment>, (int, int), (int, int)) FindPath() {
            (int xj, int zj) = joinSegment.JoinCoord;
            var ix = 0;
            var joinExit = joinSegment.Exits[0];
            int xc = joinSegment.X;                                      //cursor
            int zc = joinSegment.Z;                                      //
            GlobalDirection cDirection = joinSegment.GlobalDirection; //

            var prePath = new List<(int, int, GlobalDirection)>();
            Debug.Log("FindPath xc/zc: (" + xc + ", " + zc + ") xj/zj: (" + xj + ", " + zj + ")");
            if (!(xc == xj && zc == zj)) {
                prePath.Add((xc, zc, joinSegment.GlobalDirection));
                GetPrePathRecursive(prePath, ref xc, ref zc, ref ix, cDirection, xj, zj);
            } else {
                Debug.Log("joinSegment is joiningSegments exit");
                return (new List<Segment.Segment>(), (joinSegment.X, joinSegment.Z), joinSegment.JoinCoord);
            }

            var path = new List<Segment.Segment>();
            (int, int) exitCoord = joinSegment.JoinCoord;
            (xj, zj) = joinSegment.JoinCoord;
            for(int i = 0; i < prePath.Count; i++) {
                var step = prePath[i];
                var parent = i == 0 ? joinSegment.Parent : path[i - 1];
                //var segment = Segment.Q;
                if (i == 0) { //first addonSegment after join - Set parent to joinSegment's parent!
                    if (joinSegment.X != step.Item1 || joinSegment.Z != step.Item2) {
                        Debug.Log("Wooohooooo first addon not at joinSegment?: (" + joinSegment.X  + ", " + joinSegment.Z + ") step0: (" + step.Item1 + "," + step.Item2 + ")");
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
                            path.Add(new RightSegment(step.Item1, step.Item2, step.Item3, parent));
                        } else {
                            Debug.Log("Gd.East sx < xj");
                            path.Add(new LeftSegment(step.Item1, step.Item2, step.Item3, parent));
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

        private Segment.Segment GetSegmentWithTile((int, int) tileCoord) {
            foreach(Segment.Segment segment in segmentList) {
                var tiles = segment.GetTiles();
                if (tiles.Exists(tile => tile.Item1 == tileCoord.Item1 && tile.Item2 == tileCoord.Item2)) {
                    return segment;
                }
            }
            throw new JoinException("GetSegmentWithTile (" + tileCoord.Item1 + ", " + tileCoord.Item2 + ") - segmentNotFound!");
        }

        private void ReplaceJoiningSegmentWithPlusExitSegment(
            Segment.Segment joiningSegment, 
            JoinSegment joinSegment, 
            (int, int) exitCoord, 
            Action<Segment.Segment, bool, string> AddSegment, 
            Action<Segment.Segment> ClearSegment
        ) {
            var localExitCoordinates = dHelper.GetLocalCooridnatesForSegment(joiningSegment, exitCoord);
            var joiningSegmentChildren = dHelper.GetChildrenOfSegment(joiningSegment, segmentList);
            var newSegment = RedoSegmentWithAdditionalExit(joiningSegment, joinSegment, localExitCoordinates);
            ClearSegment(joiningSegment);
            AddSegment(newSegment, false, "green");
            dHelper.AddNewParentToChildren(newSegment, joiningSegmentChildren);
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
            Debug.Log("RedoSegmentWithAdditionalExit joiningSegment Type: " + joiningSegment.Type + " joiningSide: " + joiningSide + " le.x: " + le.x + " le.z: " + le.z);
            switch (joiningSegment.Type) {
                case SegmentType.Straight: {
                    if ((le.x == 0 && le.z == 1) || (le.x == 0 && le.z == 0 && joiningSide == LocalDirection.Right)) {
                        return SegmentType.StraightRight.GetSegmentByType(joiningSegment.X, joiningSegment.Z, joiningSegment.GlobalDirection, 0, joiningSegment.Parent);
                    } else if (le.x == 0 && le.z == -1|| (le.x == 0 && le.z == 0 && joiningSide == LocalDirection.Left)) {
                        return SegmentType.StraightLeft.GetSegmentByType(joiningSegment.X, joiningSegment.Z, joiningSegment.GlobalDirection, 0, joiningSegment.Parent);
                    } else if (le.x == -1 && le.z == 0 && joiningSide == LocalDirection.Back) {
                        Debug.Log("This side really? from behind!");
                        return joiningSegment;
                    } else if (le.x == 1 && le.z == 0 && joiningSide == LocalDirection.Straight) {
                        Debug.Log("This side really? headjob!");
                        return joiningSegment; 
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
            
            throw new JoinException("RedoSegmentWithAdditionalExit - unreqognized joinSegment: " 
                + joiningSegment.Type + " joining side: " + joiningSide + " {" + le.x + ", " + le.z + "} \n"
                + "joiningSegment.GlobalDirection: " + joiningSegment.GlobalDirection + " \n"
                + "joinSegment.GlobalDirection: " + joinSegment.GlobalDirection + " \n"
                + "joinSegment coord: (" + joinSegment.X + ", " + joinSegment.Z + ")");
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
