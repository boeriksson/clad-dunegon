using System;
using Direction;
using GlobalDirection = Direction.GlobalDirection;
using Debug = UnityEngine.Debug;

namespace Segment {
    public enum SegmentType {
        Straight,
        Start,
        Right,
        Left, 
        StraightRight,
        StraightLeft,
        LeftRight,
        LeftStraightRight,
        StraightNoCheck,
        Stop,
        Room3x3,
        Room3x4,
        Room4x4,
        Room4x4x2,
        Room4x5,
        Room4x5x2,
        Room5x4,
        Room5x4x2,
        Room5x5,
        Room5x5x2,
        Room5x5x3,
        Room5x6,
        Room5x6x2,
        Room5x6x3,
        Room6x5,
        Room6x5x2,
        Room6x5x3,
        Room6x6,
        Room6x6x2,
        Room6x6x3,
        Join,
        Room3x5,
        Room3x6,
        Room3x7,
        Room5x3,
        Room6x3,
        Room7x3,
        Room8x3,
        Room3x8,
        Room4x6,
        Room4x6x2,
        Room6x4,
        Room6x4x2,
        Room4x7,
        Room4x7x2,
        Room7x4,
        Room7x4x2,
        Room4x8,
        Room4x8x2,
        Room8x4,
        Room8x4x2,
        Room5x7,
        Room5x7x2,
        Room5x7x3,
        Room7x5,
        Room7x5x2,
        Room7x5x3,
        Room8x5,
        Room8x5x2,
        Room8x5x3,
        Room5x8,
        Room5x8x2,
        Room5x8x3,
        Room6x7,
        Room6x7x2,
        Room6x7x3,
        Room6x7x4,
        Room7x6,
        Room7x6x2,
        Room7x6x3,
        Room7x6x4,
        Room6x8,
        Room6x8x2,
        Room6x8x3,
        Room8x6,
        Room8x6x2,
        Room8x6x3,
        Room8x6x4,
        Room7x7,
        Room7x7x2,
        Room7x7x3,
        Room7x7x4,
        Room8x8,
        Room8x8x2,
        Room8x8x3,
        Room9x9,
        Room9x9x2,
        Room10x10,
        Room10x10x2,
        SStairsUp,
        SStairsDown

    }
    
    public static class SegmentTypeExtension {
        public static Segment GetSegmentByType(this SegmentType segmentType, int x, int z, int y, GlobalDirection gDirection, int forks, Segment parent, bool isReal = false) {
            switch (segmentType) {
                case SegmentType.Straight: {
                    return new StraightSegment(x, z, y, gDirection, parent);
                }
                case SegmentType.Start: {
                    return new StartSegment(x, z, y, gDirection);
                }
                case SegmentType.StraightNoCheck: {
                    //return new StraightNoCheckSegment(x, z, y, gDirection, parent);
                    return new StraightSegment(x, z, y, gDirection, parent, true);
                }
                case SegmentType.Stop: {
                    return new StopSegment(x, z, y, gDirection, parent);
                }
                case SegmentType.Join: {
                    return new JoinSegment(x, z, y, gDirection, parent);
                }
                case SegmentType.Right: {
                    return new RightSegment(x, z, y, gDirection, parent);
                }
                case SegmentType.Left: {
                    return new LeftSegment(x, z, y, gDirection, parent);
                }
                case SegmentType.StraightRight: {
                    return new StraightRightSegment(x, z, y, gDirection, parent);
                }
                case SegmentType.StraightLeft: {
                    return new StraightLeftSegment(x, z, y, gDirection, parent);
                }
                case SegmentType.LeftRight: {
                    return new LeftRightSegment(x, z, y, gDirection, parent);
                }
                case SegmentType.LeftStraightRight: {
                    return new LeftStraightRightSegment(x, z, y, gDirection, parent);
                }
                case SegmentType.Room3x3: {
                    return new Room3x3Segment(x, z, y, gDirection, forks, parent);
                }
                case SegmentType.Room3x4: {
                    return new Room3x4Segment(x, z, y, gDirection, forks, parent);
                }
                case SegmentType.Room3x5: {
                    return new RoomVariableSegment(x, z, y, gDirection, 3, 5, 1, forks, parent, isReal, SegmentType.Room3x5);
                }
                case SegmentType.Room3x6: {
                    return new RoomVariableSegment(x, z, y, gDirection, 3, 6, 1, forks, parent, isReal, SegmentType.Room3x6);
                }
                case SegmentType.Room3x7: {
                    return new RoomVariableSegment(x, z, y, gDirection, 3, 7, 1, forks, parent, isReal, SegmentType.Room3x7);
                }
                case SegmentType.Room5x3: {
                    return new RoomVariableSegment(x, z, y, gDirection, 5, 3, 1, forks, parent, isReal, SegmentType.Room5x3);
                }
                case SegmentType.Room6x3: {
                    return new RoomVariableSegment(x, z, y, gDirection, 6, 3, 1, forks, parent, isReal, SegmentType.Room6x3);
                }
                case SegmentType.Room7x3: {
                    return new RoomVariableSegment(x, z, y, gDirection, 7, 3, 1, forks, parent, isReal, SegmentType.Room7x3);
                }
                case SegmentType.Room8x3: {
                    return new RoomVariableSegment(x, z, y, gDirection, 8, 3, 1, forks, parent, isReal, SegmentType.Room8x3);
                }
                case SegmentType.Room3x8: {
                    return new RoomVariableSegment(x, z, y, gDirection, 3, 8, 1, forks, parent, isReal, SegmentType.Room3x8);
                }
                case SegmentType.Room4x4: {
                    return new RoomVariableSegment(x, z, y, gDirection, 4, 4, 1, forks, parent, isReal, SegmentType.Room4x4);
                }
                case SegmentType.Room4x4x2: {
                    return new RoomVariableSegment(x, z, y, gDirection, 4, 4, 2, forks, parent, isReal, SegmentType.Room4x4x2);
                }
                case SegmentType.Room4x5: {
                    return new RoomVariableSegment(x, z, y, gDirection, 4, 5, 1, forks, parent, isReal, SegmentType.Room4x5);
                }
                case SegmentType.Room4x5x2: {
                    return new RoomVariableSegment(x, z, y, gDirection, 4, 5, 2, forks, parent, isReal, SegmentType.Room4x5x2);
                }
                case SegmentType.Room5x4: {
                    return new RoomVariableSegment(x, z, y, gDirection, 5, 4, 1, forks, parent, isReal, SegmentType.Room5x4);
                }
                case SegmentType.Room5x4x2: {
                    return new RoomVariableSegment(x, z, y, gDirection, 5, 4, 2, forks, parent, isReal, SegmentType.Room5x4x2);
                }
                case SegmentType.Room4x6: {
                    return new RoomVariableSegment(x, z, y, gDirection, 4, 6, 1, forks, parent, isReal, SegmentType.Room4x6);
                }
                case SegmentType.Room4x6x2: {
                    return new RoomVariableSegment(x, z, y, gDirection, 4, 6, 2, forks, parent, isReal, SegmentType.Room4x6x2);
                }
                case SegmentType.Room6x4: {
                    return new RoomVariableSegment(x, z, y, gDirection, 6, 4, 1, forks, parent, isReal, SegmentType.Room6x4);
                }
                case SegmentType.Room6x4x2: {
                    return new RoomVariableSegment(x, z, y, gDirection, 6, 4, 2, forks, parent, isReal, SegmentType.Room6x4x2);
                }
                case SegmentType.Room7x4: {
                    return new RoomVariableSegment(x, z, y, gDirection, 7, 4, 1, forks, parent, isReal, SegmentType.Room7x4);
                }
                case SegmentType.Room7x4x2: {
                    return new RoomVariableSegment(x, z, y, gDirection, 7, 4, 2, forks, parent, isReal, SegmentType.Room7x4x2);
                }
                case SegmentType.Room4x7: {
                    return new RoomVariableSegment(x, z, y, gDirection, 4, 7, 1, forks, parent, isReal, SegmentType.Room4x7);
                }
                case SegmentType.Room4x7x2: {
                    return new RoomVariableSegment(x, z, y, gDirection, 4, 7, 2, forks, parent, isReal, SegmentType.Room4x7x2);
                }
                case SegmentType.Room8x4: {
                    return new RoomVariableSegment(x, z, y, gDirection, 8, 4, 1, forks, parent, isReal, SegmentType.Room8x4);
                }
                case SegmentType.Room8x4x2: {
                    return new RoomVariableSegment(x, z, y, gDirection, 8, 4, 2, forks, parent, isReal, SegmentType.Room8x4x2);
                }
                case SegmentType.Room4x8: {
                    return new RoomVariableSegment(x, z, y, gDirection, 4, 8, 1, forks, parent, isReal, SegmentType.Room4x8);
                }
                case SegmentType.Room4x8x2: {
                    return new RoomVariableSegment(x, z, y, gDirection, 4, 8, 2, forks, parent, isReal, SegmentType.Room4x8x2);
                }
                case SegmentType.Room5x5: {
                    return new RoomVariableSegment(x, z, y, gDirection, 5, 5, 1, forks, parent, isReal,SegmentType.Room5x5);
                }
                case SegmentType.Room5x5x2: {
                    return new RoomVariableSegment(x, z, y, gDirection, 5, 5, 2, forks, parent, isReal,SegmentType.Room5x5x2);
                }
                case SegmentType.Room5x5x3: {
                    return new RoomVariableSegment(x, z, y, gDirection, 5, 5, 3, forks, parent, isReal,SegmentType.Room5x5x3);
                }
                case SegmentType.Room5x6: {
                    return new RoomVariableSegment(x, z, y, gDirection, 5, 6, 1, forks, parent, isReal, SegmentType.Room5x6);
                }
                case SegmentType.Room5x6x2: {
                    return new RoomVariableSegment(x, z, y, gDirection, 5, 6, 2, forks, parent, isReal, SegmentType.Room5x6x2);
                }
                case SegmentType.Room5x6x3: {
                    return new RoomVariableSegment(x, z, y, gDirection, 5, 6, 3, forks, parent, isReal, SegmentType.Room5x6x3);
                }
                case SegmentType.Room6x5: {
                    return new RoomVariableSegment(x, z, y, gDirection, 6, 5, 1, forks, parent, isReal, SegmentType.Room6x5);
                }
                case SegmentType.Room6x5x2: {
                    return new RoomVariableSegment(x, z, y, gDirection, 6, 5, 2, forks, parent, isReal, SegmentType.Room6x5x2);
                }
                case SegmentType.Room6x5x3: {
                    return new RoomVariableSegment(x, z, y, gDirection, 6, 5, 3, forks, parent, isReal, SegmentType.Room6x5x3);
                }
                case SegmentType.Room7x5: {
                    return new RoomVariableSegment(x, z, y, gDirection, 7, 5, 1, forks, parent, isReal, SegmentType.Room7x5);
                }
                case SegmentType.Room7x5x2: {
                    return new RoomVariableSegment(x, z, y, gDirection, 7, 5, 2, forks, parent, isReal, SegmentType.Room7x5x2);
                }
                case SegmentType.Room7x5x3: {
                    return new RoomVariableSegment(x, z, y, gDirection, 7, 5, 3, forks, parent, isReal, SegmentType.Room7x5x3);
                }
                case SegmentType.Room5x7: {
                    return new RoomVariableSegment(x, z, y, gDirection, 5, 7, 1, forks, parent, isReal, SegmentType.Room5x7);
                }
                case SegmentType.Room5x7x2: {
                    return new RoomVariableSegment(x, z, y, gDirection, 5, 7, 2, forks, parent, isReal, SegmentType.Room5x7x2);
                }
                case SegmentType.Room5x7x3: {
                    return new RoomVariableSegment(x, z, y, gDirection, 5, 7, 3, forks, parent, isReal, SegmentType.Room5x7x3);
                }
                case SegmentType.Room8x5: {
                    return new RoomVariableSegment(x, z, y, gDirection, 8, 5, 1, forks, parent, isReal, SegmentType.Room8x5);
                }
                case SegmentType.Room8x5x2: {
                    return new RoomVariableSegment(x, z, y, gDirection, 8, 5, 2, forks, parent, isReal, SegmentType.Room8x5x2);
                }
                case SegmentType.Room8x5x3: {
                    return new RoomVariableSegment(x, z, y, gDirection, 8, 5, 2, forks, parent, isReal, SegmentType.Room8x5x3);
                }
                case SegmentType.Room5x8: {
                    return new RoomVariableSegment(x, z, y, gDirection, 5, 8, 1, forks, parent, isReal, SegmentType.Room5x8);
                }
                case SegmentType.Room5x8x2: {
                    return new RoomVariableSegment(x, z, y, gDirection, 5, 8, 2, forks, parent, isReal, SegmentType.Room5x8x2);
                }
                case SegmentType.Room5x8x3: {
                    return new RoomVariableSegment(x, z, y, gDirection, 5, 8, 3, forks, parent, isReal, SegmentType.Room5x8x3);
                }
                case SegmentType.Room6x6: {
                    return new RoomVariableSegment(x, z, y, gDirection, 6, 6, 1, forks, parent, isReal, SegmentType.Room6x6);
                }
                case SegmentType.Room6x6x2: {
                    return new RoomVariableSegment(x, z, y, gDirection, 6, 6, 2, forks, parent, isReal, SegmentType.Room6x6x2);
                }
                case SegmentType.Room6x6x3: {
                    return new RoomVariableSegment(x, z, y, gDirection, 6, 6, 3, forks, parent, isReal, SegmentType.Room6x6x3);
                }
                case SegmentType.Room6x7: {
                    return new RoomVariableSegment(x, z, y, gDirection, 6, 7, 1, forks, parent, isReal, SegmentType.Room6x7);
                }
                case SegmentType.Room6x7x2: {
                    return new RoomVariableSegment(x, z, y, gDirection, 6, 7, 2, forks, parent, isReal, SegmentType.Room6x7x2);
                }
                case SegmentType.Room6x7x3: {
                    return new RoomVariableSegment(x, z, y, gDirection, 6, 7, 3, forks, parent, isReal, SegmentType.Room6x7x3);
                }
                case SegmentType.Room6x7x4: {
                    return new RoomVariableSegment(x, z, y, gDirection, 6, 7, 3, forks, parent, isReal, SegmentType.Room6x7x3);
                }
                case SegmentType.Room7x6: {
                    return new RoomVariableSegment(x, z, y, gDirection, 7, 6, 1, forks, parent, isReal, SegmentType.Room7x6);
                }
                case SegmentType.Room7x6x2: {
                    return new RoomVariableSegment(x, z, y, gDirection, 7, 6, 2, forks, parent, isReal, SegmentType.Room7x6x2);
                }
                case SegmentType.Room7x6x3: {
                    return new RoomVariableSegment(x, z, y, gDirection, 7, 6, 3, forks, parent, isReal, SegmentType.Room7x6x3);
                }
                case SegmentType.Room7x6x4: {
                    return new RoomVariableSegment(x, z, y, gDirection, 7, 6, 4, forks, parent, isReal, SegmentType.Room7x6x4);
                }
                case SegmentType.Room6x8: {
                    return new RoomVariableSegment(x, z, y, gDirection, 6, 8, 1, forks, parent, isReal, SegmentType.Room6x8);
                }
                case SegmentType.Room6x8x2: {
                    return new RoomVariableSegment(x, z, y, gDirection, 6, 8, 2, forks, parent, isReal, SegmentType.Room6x8x2);
                }
                case SegmentType.Room6x8x3: {
                    return new RoomVariableSegment(x, z, y, gDirection, 6, 8, 3, forks, parent, isReal, SegmentType.Room6x8x3);
                }
                case SegmentType.Room8x6: {
                    return new RoomVariableSegment(x, z, y, gDirection, 8, 6, 1, forks, parent, isReal, SegmentType.Room8x6);
                }
                case SegmentType.Room8x6x2: {
                    return new RoomVariableSegment(x, z, y, gDirection, 8, 6, 2, forks, parent, isReal, SegmentType.Room8x6x2);
                }
                case SegmentType.Room8x6x3: {
                    return new RoomVariableSegment(x, z, y, gDirection, 8, 6, 3, forks, parent, isReal, SegmentType.Room8x6x3);
                }
                case SegmentType.Room8x6x4: {
                    return new RoomVariableSegment(x, z, y, gDirection, 8, 6, 4, forks, parent, isReal, SegmentType.Room8x6x4);
                }
                case SegmentType.Room7x7: {
                    return new RoomVariableSegment(x, z, y, gDirection, 7, 7, 1, forks, parent, isReal, SegmentType.Room7x7);
                }
                case SegmentType.Room7x7x2: {
                    return new RoomVariableSegment(x, z, y, gDirection, 7, 7, 2, forks, parent, isReal, SegmentType.Room7x7x2);
                }
                case SegmentType.Room7x7x3: {
                    return new RoomVariableSegment(x, z, y, gDirection, 7, 7, 3, forks, parent, isReal, SegmentType.Room7x7x3);
                }
                case SegmentType.Room7x7x4: {
                    return new RoomVariableSegment(x, z, y, gDirection, 7, 7, 4, forks, parent, isReal, SegmentType.Room7x7x4);
                }
                case SegmentType.Room8x8: {
                    return new RoomVariableSegment(x, z, y, gDirection, 8, 8, 1, forks, parent, isReal, SegmentType.Room8x8);
                }
                case SegmentType.Room8x8x2: {
                    return new RoomVariableSegment(x, z, y, gDirection, 8, 8, 2, forks, parent, isReal, SegmentType.Room8x8x2);
                }
                case SegmentType.Room8x8x3: {
                    return new RoomVariableSegment(x, z, y, gDirection, 8, 8, 3, forks, parent, isReal, SegmentType.Room8x8x3);
                }
                case SegmentType.Room9x9: {
                    return new RoomVariableSegment(x, z, y, gDirection, 9, 9, 1, forks, parent, isReal, SegmentType.Room9x9);
                }
                case SegmentType.Room9x9x2: {
                    return new RoomVariableSegment(x, z, y, gDirection, 9, 9, 2, forks, parent, isReal, SegmentType.Room9x9x2);
                }
                case SegmentType.Room10x10: {
                    return new RoomVariableSegment(x, z, y, gDirection, 10, 10, 1, forks, parent, isReal, SegmentType.Room10x10);
                }
                case SegmentType.Room10x10x2: {
                    return new RoomVariableSegment(x, z, y, gDirection, 10, 10, 2, forks, parent, isReal, SegmentType.Room10x10x2);
                }
                case SegmentType.SStairsUp: {
                    return new StraightStair(x, z, y, gDirection, VerticalDirection.Up, parent);
                }
                case SegmentType.SStairsDown: {
                    return new StraightStair(x, z, y, gDirection, VerticalDirection.Down, parent);
                }
                default: {
                    return new StraightSegment(x, z, y, gDirection, parent);
                }
            }
        }

        public static int GetSegmentTypeWeight(this SegmentType segmentType, int forks, int straigthParentChain) {
            var forksConstant = 1f; //if there are few threads active, we increase forking segments weight
            if (forks == 1) {
                forksConstant = 2f;
            } else if (forks < 3) {
                forksConstant = 1.5f;
            } else if (forks > 10) {
                forksConstant = 0.5f;
            }   
            var straightConstraint = 1f; //if there are many nonbranching segments in a row, we lower their weight
            if (straigthParentChain > 2) {
                straightConstraint = 1.5f;
            } else if (straigthParentChain > 5) {
                straightConstraint = 2f;
            } else if (straigthParentChain > 8) {
                straightConstraint = 3f;
            }
        
            switch (segmentType) {
                case SegmentType.Straight: {
                    return (int)Math.Round(600/Math.Max(straightConstraint, forksConstant), 0);
                }
                case SegmentType.Right: {
                    return (int)Math.Round(150/straightConstraint, 0);
                }
                case SegmentType.Left: {
                    return (int)Math.Round(150/straightConstraint, 0);
                }
                case SegmentType.StraightRight: {
                    return (int)Math.Round(40 * forksConstant, 0);
                }
                case SegmentType.StraightLeft: {
                    return (int)Math.Round(40 * forksConstant, 0);
                }
                case SegmentType.LeftRight: {
                    return (int)Math.Round(60 * forksConstant, 0);
                }
                case SegmentType.LeftStraightRight: {
                    return (int)Math.Round(30 * forksConstant, 0);
                }
                case SegmentType.Join: {
                    return 0;
                }
                case SegmentType.Start: {
                    return 0;
                }
                case SegmentType.StraightNoCheck: {
                    return 0;
                }
                case SegmentType.Room3x3: {
                    return 100;
                }
                case SegmentType.Room3x4: {
                    return 50;
                }
                case SegmentType.Room4x4: {
                    return 40;
                }
                case SegmentType.Room4x4x2: {
                    return 20;
                }
                case SegmentType.Room4x5: {
                    return 30;
                }
                case SegmentType.Room4x5x2: {
                    return 15;
                }
                case SegmentType.Room5x4: {
                    return 30;
                }
                case SegmentType.Room5x4x2: {
                    return 15;
                }
                case SegmentType.Room5x5: {
                    return 20;
                }
                case SegmentType.Room5x5x2: {
                    return 12;
                }
                case SegmentType.Room5x5x3: {
                    return 8;
                }
                case SegmentType.Room5x6: {
                    return 14;
                }
                case SegmentType.Room5x6x2: {
                    return 8;
                }
                case SegmentType.Room5x6x3: {
                    return 3;
                }
                case SegmentType.Room6x5: {
                    return 12;
                }
                case SegmentType.Room6x5x2: {
                    return 6;
                }
                case SegmentType.Room6x5x3: {
                    return 3;
                }
                case SegmentType.Room6x6: {
                    return 10;
                }
                case SegmentType.Room6x6x2: {
                    return 6;
                }
                case SegmentType.Room6x6x3: {
                    return 3;
                }
                case SegmentType.Room3x5: {
                    return 25;
                }
                case SegmentType.Room3x6: {
                    return 20;
                }
                case SegmentType.Room3x7: {
                    return 12;
                }
                case SegmentType.Room5x3: {
                    return 25;
                }
                case SegmentType.Room6x3: {
                    return 20;
                }
                case SegmentType.Room7x3: {
                    return 12;
                }
                case SegmentType.Room8x3: {
                    return 9;
                }
                case SegmentType.Room3x8: {
                    return 9;
                }
                case SegmentType.Room4x6: {
                    return 15;
                }
                case SegmentType.Room4x6x2: {
                    return 8;
                }
                case SegmentType.Room6x4: {
                    return 15;
                }
                case SegmentType.Room6x4x2: {
                    return 8;
                }
                case SegmentType.Room4x7: {
                    return 10;
                }
                case SegmentType.Room4x7x2: {
                    return 5;
                }
                case SegmentType.Room7x4: {
                    return 9;
                }
                case SegmentType.Room7x4x2: {
                    return 4;
                }
                case SegmentType.Room8x4: {
                    return 6;
                }
                case SegmentType.Room8x4x2: {
                    return 3;
                }
                case SegmentType.Room4x8: {
                    return 6;
                }
                case SegmentType.Room4x8x2: {
                    return 3;
                }
                case SegmentType.Room5x7: {
                    return 7;
                }
                case SegmentType.Room5x7x2: {
                    return 3;
                }
                case SegmentType.Room5x7x3: {
                    return 1;
                }
                case SegmentType.Room7x5: {
                    return 7;
                }
                case SegmentType.Room7x5x2: {
                    return 3;
                }
                case SegmentType.Room7x5x3: {
                    return 1;
                }
                case SegmentType.Room5x8: {
                    return 4;
                }
                case SegmentType.Room5x8x2: {
                    return 2;
                }
                case SegmentType.Room5x8x3: {
                    return 1;
                }
                case SegmentType.Room8x5: {
                    return 4;
                }
                case SegmentType.Room8x5x2: {
                    return 2;
                }
                case SegmentType.Room8x5x3: {
                    return 1;
                }
                case SegmentType.Room7x6: {
                    return 5;
                }
                case SegmentType.Room7x6x2: {
                    return 3;
                }
                case SegmentType.Room7x6x3: {
                    return 1;
                }
                case SegmentType.Room7x6x4: {
                    return 1;
                }
                case SegmentType.Room6x7: {
                    return 5;
                }
                case SegmentType.Room6x7x2: {
                    return 3;
                }
                case SegmentType.Room6x7x3: {
                    return 1;
                }
                case SegmentType.Room6x7x4: {
                    return 1;
                }
                case SegmentType.Room6x8: {
                    return 3;
                }
                case SegmentType.Room6x8x2: {
                    return 2;
                }
                case SegmentType.Room6x8x3: {
                    return 1;
                }
                case SegmentType.Room8x6: {
                    return 3;
                }
                case SegmentType.Room8x6x2: {
                    return 2;
                }
                case SegmentType.Room8x6x3: {
                    return 1;
                }
                case SegmentType.Room7x7: {
                    return 3;
                }
                case SegmentType.Room7x7x2: {
                    return 2;
                }
                case SegmentType.Room7x7x3: {
                    return 1;
                }
                case SegmentType.Room7x7x4: {
                    return 1;
                }
                case SegmentType.Room8x8: {
                    return 2;
                }
                case SegmentType.Room8x8x2: {
                    return 1;
                }
                case SegmentType.Room8x8x3: {
                    return 1;
                }
                case SegmentType.Room9x9: {
                    return 1;
                }
                case SegmentType.Room9x9x2: {
                    return 1;
                }
                case SegmentType.Room10x10: {
                    return 1;
                }
                case SegmentType.Room10x10x2: {
                    return 1;
                }
                case SegmentType.SStairsUp: {
                    return 100;
                }
                case SegmentType.SStairsDown: {
                    return 100;
                }
                default: {
                    return 0;
                }
            }
        }
    }
}
