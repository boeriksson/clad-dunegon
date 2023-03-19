
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
        private Action<Segment.Segment, string> SetSegmentColor;
        private Func<Segment.Segment, List<Segment.Segment>> GetChildrenOfSegment;
        private Action<Segment.Segment, Segment.Segment> ChangeParentOfChildren;
        private Action<Segment.Segment, Segment.Segment> ReplaceSegmentWithNewSegmentInWorkingSet;
        private Func<Segment.Segment, (bool, Segment.Segment)> IsBackableSegment;
        private Action<Segment.Segment> RemoveSegment;
        private Func<Segment.Segment, bool, string, bool> AddSegment;  
        private Action<Segment.Segment, Segment.Segment, string> UpdateSegment;
        private DunegonHelper dHelper;
        public Backout(
            DunegonHelper dHelper,
            Action<Segment.Segment> RemoveSegment, 
            Action<Segment.Segment, string> SetSegmentColor,
            Func<Segment.Segment, bool, string, bool> AddSegment,
            Action<Segment.Segment, Segment.Segment, string> UpdateSegment, 
            Func<Segment.Segment, List<Segment.Segment>> GetChildrenOfSegment,
            Action<Segment.Segment, Segment.Segment> ChangeParentOfChildren,
            Action<Segment.Segment, Segment.Segment> ReplaceSegmentWithNewSegmentInWorkingSet,
            Func<Segment.Segment, (bool, Segment.Segment)> IsBackableSegment
        ) {
            this.dHelper = dHelper;
            this.RemoveSegment = RemoveSegment;
            this.SetSegmentColor = SetSegmentColor;
            this.AddSegment = AddSegment;
            this.UpdateSegment = UpdateSegment;
            this.GetChildrenOfSegment = GetChildrenOfSegment;
            this.ReplaceSegmentWithNewSegmentInWorkingSet = ReplaceSegmentWithNewSegmentInWorkingSet;
            this.ChangeParentOfChildren = ChangeParentOfChildren;
            this.IsBackableSegment = IsBackableSegment;
        }

        public Segment.Segment BackoutDeadEnd(Segment.Segment segment, int exitX, int exitZ, int exitY, int wsCount) {
            var backedOutSegment = segment;
            //var segmentChildren = GetChildrenOfSegment(segment);
            var (isBackable, actualOldSegment) = IsBackableSegment(segment);
            if (isBackable) {
                //dHelper.RemoveDanglingWorkingTreads(workingSet, segment);
                //RemoveSement(segment);
                Debug.Log("Backout removing/greying segment (" + actualOldSegment.X + ", " + actualOldSegment.Z + ", " + actualOldSegment.Y + ") ref: "+ RuntimeHelpers.GetHashCode(actualOldSegment));
                SetSegmentColor(segment, "grey");
                backedOutSegment = BackoutDeadEnd(actualOldSegment.Parent, actualOldSegment.X, actualOldSegment.Z, actualOldSegment.Y, wsCount);
            } else { // We're gonna remove the exit in segment where we roll back to
                if (exitX == 0 && exitZ == 0) {
                    Debug.Log("Uh oh -> BackoutSegment exitX & exitZ is 0 in the nonBackable RedoSegmentWithOneLessExit segment...");
                }
                try {
                    var newSegment = RedoSegmentWithOneLessExit(
                        actualOldSegment, 
                        (exitX, exitZ, exitY)
                    );
                    Debug.Log("BackoutDeadEnd oldSegment ref: " + RuntimeHelpers.GetHashCode(actualOldSegment));
                    Debug.Log("BackoutDeadEnd newSegment ref: " + RuntimeHelpers.GetHashCode(newSegment));
                    ReplaceSegmentWithNewSegmentInWorkingSet(actualOldSegment, newSegment);
                    ChangeParentOfChildren(newSegment, actualOldSegment);
                    UpdateSegment(newSegment, segment, "blue");
                } catch (RedoSegmentException rsex) { // fail to replace backedoutsegment with exits -1, capping with stopsegment instead!
                    Debug.Log("RedoSegmentException message: " + rsex.Message);
                    var segmentExits = actualOldSegment.Exits;
                    string segExits = "";
                    foreach (SegmentExit ex in actualOldSegment.Exits) {
                        segExits += "   (" + ex.X + ", " + ex.Z + ", " + ex.Y + ") direction: " + ex.Direction + "\n";
                    }
                    Debug.Log("Exit not found (" + exitX + ", " + exitZ + ", " + exitY + ")\n exits: " + segExits);
                    var segmentExit = segmentExits.Single(exit => exit.X == exitX && exit.Z == exitZ && exit.Y == exitY);
                    var stopSegment = SegmentType.Stop.GetSegmentByType(segmentExit.X, segmentExit.Z, segmentExit.Y, segmentExit.Direction, wsCount, backedOutSegment, true);
                    AddSegment(stopSegment, true, "cyan");
                }
            }
            return backedOutSegment;
        }

        public Segment.Segment RedoSegmentWithOneLessExit(
                Segment.Segment redoSegment, 
                (int x, int z, int y) exit
        ) {
            void LogProblem(Segment.Segment redoSegment, int leX, int leZ) {
                Debug.Log("RedoSegmentWithOneLessExit problem---------------------------------------\n" + 
                "redoSegment: {" + redoSegment.X + ", " + redoSegment.Z + ", " + redoSegment.Y + "} gDirection: " + redoSegment.GlobalDirection + "\n" +
                "Remove exit on " + redoSegment.Type + " exitToRemove: {" + leX + ", " + leZ + "}\n" + 
                "----------------------------------------------------------------------------");
            }
            (int leX, int leZ, int leY) = dHelper.GetLocalCooridnatesForSegment(redoSegment, (exit.x, exit.z, exit.y));
            Debug.Log("RedoSegmentWithOneLessExit  (" + redoSegment.X + ", " + redoSegment.Z + ", " + redoSegment.Y + ") type: " + redoSegment.Type + " localCoord: (" + leX + ", " + leZ + ", " + leY + ")" );
            switch (redoSegment.Type) {
                case SegmentType.LeftRight: {
                    if (leX == 0 && leZ == -1) {
                        Debug.Log("LeftRight - left");
                        return SegmentType.Right.GetSegmentByType(redoSegment.X, redoSegment.Z, redoSegment.Y, redoSegment.GlobalDirection, 0, redoSegment.Parent);
                    } 
                    if (leX == 0 && leZ == 1) {
                        Debug.Log("LeftRight - right");
                        return SegmentType.Left.GetSegmentByType(redoSegment.X, redoSegment.Z, redoSegment.Y,  redoSegment.GlobalDirection, 0, redoSegment.Parent);
                    }
                    break;
                }
                case SegmentType.StraightRight: {
                    if (leX == 1 && leZ == 0) {
                        Debug.Log("StraightRight - right");
                        return SegmentType.Right.GetSegmentByType(redoSegment.X, redoSegment.Z, redoSegment.Y, redoSegment.GlobalDirection, 0, redoSegment.Parent);
                    } 
                    if (leX == 0 && leZ == 1) {
                        Debug.Log("StraightRight - straight");
                        return SegmentType.Straight.GetSegmentByType(redoSegment.X, redoSegment.Z, redoSegment.Y, redoSegment.GlobalDirection, 0, redoSegment.Parent);
                    }
                    break;
                }
                case SegmentType.StraightLeft: {
                    if (leX == 1 && leZ == 0) {
                        Debug.Log("StraightLeft - left");
                        return SegmentType.Left.GetSegmentByType(redoSegment.X, redoSegment.Z, redoSegment.Y, redoSegment.GlobalDirection, 0, redoSegment.Parent);
                    } 
                    if (leX == 0 && leZ == -1) {
                        Debug.Log("StraightLeft - straight");
                        return SegmentType.Straight.GetSegmentByType(redoSegment.X, redoSegment.Z, redoSegment.Y, redoSegment.GlobalDirection, 0, redoSegment.Parent);
                    }
                    break;
                }
                case SegmentType.LeftStraightRight: {
                    if (leX == 1 && leZ == 0) {
                        Debug.Log("LeftStraightRight - leftRight");
                        return SegmentType.LeftRight.GetSegmentByType(redoSegment.X, redoSegment.Z, redoSegment.Y, redoSegment.GlobalDirection, 0, redoSegment.Parent);
                    } 
                    if (leX == 0 && leZ == 1) {
                        Debug.Log("LeftStraightRight - straightRight");
                        return SegmentType.StraightRight.GetSegmentByType(redoSegment.X, redoSegment.Z, redoSegment.Y, redoSegment.GlobalDirection, 0, redoSegment.Parent);
                    }
                    if (leX == 0 && leZ == -1) {
                        Debug.Log("LeftStraightRight - straightLeft");
                        return SegmentType.StraightLeft.GetSegmentByType(redoSegment.X, redoSegment.Z, redoSegment.Y, redoSegment.GlobalDirection, 0, redoSegment.Parent);
                    }
                    break;
                }
                default: {
                    if (redoSegment is Room) {
                        SegmentExit sExit = null;
                        try {
                            (int geX, int geZ, int geY) = DirectionConversion.GetGlobalCoordinatesFromLocal(new List<(int, int, int)>() {(leX, leZ, leY)}, redoSegment.X, redoSegment.Z, redoSegment.Y, redoSegment.GlobalDirection)[0];
                            Debug.Log("RedoSegmentWithOneLessExit global exit coord: (" + geX + ", " + geZ + ", " + geY + ")");
                            sExit = redoSegment.GetExitByCoord(geX, geZ, geY);
                            Debug.Log("redoSegment Room exit found!");
                        } catch (RedoSegmentException) {
                            string redoSegExits = "";
                            foreach (SegmentExit ex in redoSegment.Exits) {
                                redoSegExits += "   (" + ex.X + ", " + ex.Z + ", " + ex.Y + ") direction: " + ex.Direction + "\n";
                            }
                            Debug.Log("RedoSegmentWithOneLessExit problem---------------------------------------\n" + 
                            "redoSegment: (" + redoSegment.X + ", " + redoSegment.Z + ", " + redoSegment.Y + ") gDirection: " + redoSegment.GlobalDirection + "\n" +
                            "Remove exit on " + redoSegment.Type + " exitToRemove: (" + leX + ", " + leZ + ", " + leY + ")\n" + 
                            "exits on redoSegment: \n" + 
                            redoSegExits +
                            "----------------------------------------------------------------------------");
                        }
                        if (redoSegment is Room3x3Segment) {
                            return new Room3x3Segment((Room3x3Segment)redoSegment, sExit, sExit.Direction, false);
                        } else if (redoSegment is Room3x4Segment) {
                            return new Room3x4Segment((Room3x4Segment)redoSegment, sExit, sExit.Direction, false);
                        } else if (redoSegment is RoomVariableSegment) {
                            return new RoomVariableSegment((RoomVariableSegment)redoSegment, sExit, sExit.Direction, false);
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
