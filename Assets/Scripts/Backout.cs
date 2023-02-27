
using System;
using System.Linq;
using System.Collections.Generic;
using Segment;
using SegmentType = Segment.SegmentType;
using SegmentExit = Segment.SegmentExit;
using DirectionConversion = Direction.DirectionConversion;
using GlobalDirection = Direction.GlobalDirection;
using Debug = UnityEngine.Debug;
using RuntimeHelpers = System.Runtime.CompilerServices.RuntimeHelpers;

namespace Dunegon {
    public class Backout {
        private Action<Segment.Segment> ClearSegment;
        private Action<Segment.Segment, string> SetSegmentColor;
        private Func<List<Segment.Segment>> GetSegmentList;
        private Func<Segment.Segment, List<Segment.Segment>> GetChildrenOfSegment;
        private Action<Segment.Segment, Segment.Segment> ChangeParentOfChildren;
        private Action<Segment.Segment, Segment.Segment> ReplaceSegmentWithNewSegmentInWorkingSet;
        private Func<Segment.Segment, bool> IsBackableSegment;
        private Action<Segment.Segment, bool, string> AddSegment;  
        private DunegonHelper dHelper;
        private int restartAfterBackWhenWSIsBelow;
        public Backout(
            DunegonHelper dHelper, 
            Action<Segment.Segment> ClearSegment, 
            Action<Segment.Segment, string> SetSegmentColor,
            Action<Segment.Segment, bool, string> AddSegment,
            Func<List<Segment.Segment>> GetSegmentList,
            Func<Segment.Segment, List<Segment.Segment>> GetChildrenOfSegment,
            Action<Segment.Segment, Segment.Segment> ChangeParentOfChildren,
            Action<Segment.Segment, Segment.Segment> ReplaceSegmentWithNewSegmentInWorkingSet,
            Func<Segment.Segment, bool> IsBackableSegment,
            int restartAfterBackWhenWSIsBelow
        ) {
            this.dHelper = dHelper;
            this.ClearSegment = ClearSegment;
            this.SetSegmentColor = SetSegmentColor;
            this.AddSegment = AddSegment;
            this.GetSegmentList = GetSegmentList;
            this.GetChildrenOfSegment = GetChildrenOfSegment;
            this.ReplaceSegmentWithNewSegmentInWorkingSet = ReplaceSegmentWithNewSegmentInWorkingSet;
            this.ChangeParentOfChildren = ChangeParentOfChildren;
            this.IsBackableSegment = IsBackableSegment;
            this.restartAfterBackWhenWSIsBelow = restartAfterBackWhenWSIsBelow;
        }

        public Segment.Segment BackoutDeadEnd(Segment.Segment segment, int exitX, int exitZ, int wsCount) {
            var backedOutSegment = segment;
            //var segmentChildren = GetChildrenOfSegment(segment);
            if (IsBackableSegment(segment)) {
                //dHelper.RemoveDanglingWorkingTreads(workingSet, segment);
                //ClearSegment(segment);
                SetSegmentColor(segment, "grey");
                Debug.Log("Backout removing/greying segment (" + segment.X + ", " + segment.Z + ") ref: "+ RuntimeHelpers.GetHashCode(segment));
                backedOutSegment = BackoutDeadEnd(segment.Parent, segment.X, segment.Z, wsCount);
            } else { // We're gonna remove the exit in segment where we roll back to
                if (exitX == 0 && exitZ == 0) {
                    Debug.Log("Uh oh -> BackoutSegment exitX & exitZ is 0 in the nonBackable RedoSegmentWithOneLessExit segment...");
                }
                if (wsCount >= restartAfterBackWhenWSIsBelow) {
                    try {
                        var newSegment = RedoSegmentWithOneLessExit(
                            segment, 
                            (exitX, exitZ)
                        );
                        Debug.Log("BackoutDeadEnd oldSegment ref: " + RuntimeHelpers.GetHashCode(segment));
                        Debug.Log("BackoutDeadEnd newSegment ref: " + RuntimeHelpers.GetHashCode(newSegment));
                        ReplaceSegmentWithNewSegmentInWorkingSet(segment, newSegment);
                        ClearSegment(segment);
                        AddSegment(newSegment, false, "blue");
                        //dHelper.AddNewParentToChildren(newSegment, segmentChildren);
                        ChangeParentOfChildren(newSegment, segment);
                    } catch (RedoSegmentException rsex) { // fail to replace backedoutsegment with exits -1, capping with stopsegment instead!
                        Debug.Log("RedoSegmentException message: " + rsex.Message);
                        var segmentExits = segment.Exits;
                        string segExits = "";
                        foreach (SegmentExit ex in segment.Exits) {
                            segExits += "   (" + ex.X + "," + ex.Z + ") direction: " + ex.Direction + "\n";
                        }
                        Debug.Log("Exit not found (" + exitX + ", " + exitZ + ")\n exits: " + segExits);
                        var segmentExit = segmentExits.Single(exit => exit.X == exitX && exit.Z == exitZ);
                        var stopSegment = SegmentType.Stop.GetSegmentByType(segmentExit.X, segmentExit.Z, segmentExit.Direction, wsCount, backedOutSegment, true);
                        AddSegment(stopSegment, true, "cyan");
                    }
                }
            }
            return backedOutSegment;
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
            Debug.Log("RedoSegmentWithOneLessExit  (" + redoSegment.X + ", " + redoSegment.Z + ") type: " + redoSegment.Type + " localCoord: (" + leX + ", " + leZ + ")" );
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
                            Debug.Log("RedoSegmentWithOneLessExit global exit coord: (" + geX + ", " + geZ + ")");
                            sExit = redoSegment.GetExitByCoord(geX, geZ);
                            Debug.Log("redoSegment Room exit found!");
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
