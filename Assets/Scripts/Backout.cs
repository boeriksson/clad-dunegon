
using System;
using System.Linq;
using System.Collections.Generic;
using Segment;
using SegmentType = Segment.SegmentType;
using SegmentExit = Segment.SegmentExit;
using DirectionConversion = Direction.DirectionConversion;
using GlobalDirection = Direction.GlobalDirection;
using Debug = UnityEngine.Debug;

namespace Dunegon {
    public class Backout {
        private Action<Segment.Segment> ClearSegment;
        private Action<Segment.Segment, string> SetSegmentColor;
        private List<Segment.Segment> segmentList;
        private Action<Segment.Segment, bool, string> AddSegment;  
        private DunegonHelper dHelper;

        private int workingSetSize;
        private int restartAfterBackWhenWSIsBelow;
        public Backout(
            DunegonHelper _dHelper, 
            Action<Segment.Segment> _ClearSegment, 
            Action<Segment.Segment, string> _SetSegmentColor,
            Action<Segment.Segment, bool, string> _AddSegment,
            List<Segment.Segment> _segmentList, 
            int _workingSetSize, 
            int _restartAfterBackWhenWSIsBelow
        ) {
            dHelper = _dHelper;
            ClearSegment = _ClearSegment;
            SetSegmentColor = _SetSegmentColor;
            AddSegment = _AddSegment;
            segmentList = _segmentList;
            workingSetSize = _workingSetSize;
            restartAfterBackWhenWSIsBelow = _restartAfterBackWhenWSIsBelow;
        }

        public Segment.Segment BackoutDeadEnd(Segment.Segment segment, int exitX, int exitZ) {
            var backedOutSegment = segment;
            var segmentChildren = dHelper.GetChildrenOfSegment(segment, segmentList);
            if (isBackableSegment(segment, segmentChildren)) {
                //ClearSegment(segment);
                SetSegmentColor(segment, "grey");
                backedOutSegment = BackoutDeadEnd(segment.Parent, segment.X, segment.Z);
            } else { // We're gonna remove the exit in segment where we roll back to
                if (workingSetSize >= restartAfterBackWhenWSIsBelow) {
                    try {
                        var newSegment = RedoSegmentWithOneLessExit(
                            segment, 
                            (exitX, exitZ)
                        );
                        ClearSegment(segment);
                        AddSegment(newSegment, false, "blue");
                        dHelper.AddNewParentToChildren(newSegment, segmentChildren);
                    } catch (RedoSegmentException rsex) { // fail to replace backedoutsegment with exits -1, capping with stopsegment instead!
                        Debug.Log("RedoSegmentException message: " + rsex.Message);
                        var segmentExits = segment.Exits;
                        string segExits = "";
                        foreach (SegmentExit ex in segment.Exits) {
                            segExits += "   (" + ex.X + "," + ex.Z + ") direction: " + ex.Direction + "\n";
                        }
                        Debug.Log("Exit not found (" + exitX + ", " + exitZ + ")\n exits: " + segExits);
                        var segmentExit = segmentExits.Single(exit => exit.X == exitX && exit.Z == exitZ);
                        var stopSegment = SegmentType.Stop.GetSegmentByType(segmentExit.X, segmentExit.Z, segmentExit.Direction, workingSetSize, backedOutSegment, true);
                        AddSegment(stopSegment, true, "cyan");
                    }
                }
            }
            return backedOutSegment;
        }

        private bool isBackableSegment(Segment.Segment segment, List<Segment.Segment> segmentChildren) {
            //var backableSegmentsArray = new SegmentType[] {SegmentType.Straight, SegmentType.Left, SegmentType.Right, SegmentType.Stop, SegmentType.LeftRight, SegmentType.LeftStraightRight, SegmentType.StraightNoCheck, SegmentType.Join};
            //if (!backableSegmentsArray.Contains(segment.Type)) return false;
            if (segment is Room) return false;
            if (segment.Exits.Count <= 1) return true;
            if (segmentChildren.Count < 1) {
                Debug.Log("isBackableSegment segmentChildren: " + segmentChildren.Count + " returning true..");
                return true;
            }
            if (!segmentList.Exists(s => s.Parent == segment)) {
                Debug.Log("isBackable segment - segment with several exits is parent of none... backout! segment type: " + segment.Type);
                return true;
            }
            return false;
        }

        public Segment.Segment RedoSegmentWithOneLessExit(
                Segment.Segment redoSegment, 
                (int x, int z) exit
        ) {
            void LogProblem(Segment.Segment redoSegment, int leX, int leZ) {
                Debug.Log("RedoSegmentWithOneLessExit problem---------------------------------------\n" + 
                "redoSegment: {" + redoSegment.X + ", " + redoSegment.Z + "} gDirection: " + redoSegment.GlobalDirection + "\n" +
                "Remove exit on " + redoSegment.Type + " exitToRemove: {" + leX + ", " + leZ + "}\n" + 
                "----------------------------------------------------------------------------");
            }
            (int leX, int leZ) = dHelper.GetLocalCooridnatesForSegment(redoSegment, (exit.x, exit.z));
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
