using System;
using GlobalDirection = Direction.GlobalDirection;
using Debug = UnityEngine.Debug;

namespace Segment {
    public enum SegmentType {
        Straight,
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
        Room4x5,
        Room5x4,
        Room5x5,
        Room5x6,
        Room6x5,
        Room6x6,
        DoubleStraight
    }
    
    public static class SegmentTypeExtension {
        public static Segment GetSegmentByType(this SegmentType segmentType, int x, int z, GlobalDirection gDirection, int forks, Segment parent, bool isReal = false) {
            switch (segmentType) {
                case SegmentType.Straight: {
                    return new StraightSegment(x, z, gDirection, parent);
                }
                case SegmentType.StraightNoCheck: {
                    return new StraightNoCheckSegment(x, z, gDirection, parent);
                }
                case SegmentType.Right: {
                    return new RightSegment(x, z, gDirection, parent);
                }
                case SegmentType.Left: {
                    return new LeftSegment(x, z, gDirection, parent);
                }
                case SegmentType.StraightRight: {
                    return new StraightRightSegment(x, z, gDirection, parent);
                }
                case SegmentType.StraightLeft: {
                    return new StraightLeftSegment(x, z, gDirection, parent);
                }
                case SegmentType.LeftRight: {
                    return new LeftRightSegment(x, z, gDirection, parent);
                }
                case SegmentType.LeftStraightRight: {
                    return new LeftStraightRightSegment(x, z, gDirection, parent);
                }
                case SegmentType.DoubleStraight: {
                    return new DoubleStraightSegment(x, z, gDirection, parent);
                }
                case SegmentType.Room3x3: {
                    return new Room3x3Segment(x, z, gDirection, forks, parent);
                }
                case SegmentType.Room3x4: {
                    return new Room3x4Segment(x, z, gDirection, forks, parent);
                }
                case SegmentType.Room4x4: {
                    return new RoomVariableSegment(x, z, gDirection, 4, 4, forks, parent, isReal);
                }
                case SegmentType.Room4x5: {
                    return new RoomVariableSegment(x, z, gDirection, 4, 5, forks, parent, isReal);
                }
                case SegmentType.Room5x4: {
                    return new RoomVariableSegment(x, z, gDirection, 5, 4, forks, parent, isReal);
                }
                case SegmentType.Room5x5: {
                    return new RoomVariableSegment(x, z, gDirection, 5, 5, forks, parent, isReal);
                }
                case SegmentType.Room5x6: {
                    return new RoomVariableSegment(x, z, gDirection, 5, 6, forks, parent, isReal);
                }
                case SegmentType.Room6x5: {
                    return new RoomVariableSegment(x, z, gDirection, 6, 5, forks, parent, isReal);
                }
                case SegmentType.Room6x6: {
                    return new RoomVariableSegment(x, z, gDirection, 6, 6, forks, parent, isReal);
                }
                default: {
                    return new StraightSegment(x, z, gDirection, parent);
                }
            }
        }

        public static int GetSegmentTypeWeight(this SegmentType segmentType, int forks) {
            var forksConstant = 1f;
            if (forks < 3) {
                forksConstant = 1.5f;
            } else if (forks > 10) {
                forksConstant = 0.5f;
            }   
        
            switch (segmentType) {
                case SegmentType.Straight: {
                    return 80;
                }
                case SegmentType.Right: {
                    return 15;
                }
                case SegmentType.Left: {
                    return 15;
                }
                case SegmentType.StraightRight: {
                    return (int)Math.Round(4 * forksConstant, 0);
                }
                case SegmentType.StraightLeft: {
                    return (int)Math.Round(4 * forksConstant, 0);
                }
                case SegmentType.LeftRight: {
                    return (int)Math.Round(6 * forksConstant, 0);
                }
                case SegmentType.LeftStraightRight: {
                    return (int)Math.Round(3 * forksConstant, 0);
                }
                case SegmentType.DoubleStraight: {
                    return 0;
                }
                case SegmentType.StraightNoCheck: {
                    return 0;
                }
                case SegmentType.Room3x3: {
                    return 10;
                }
                case SegmentType.Room3x4: {
                    return 5;
                }
                case SegmentType.Room4x4: {
                    return 4;
                }
                case SegmentType.Room4x5: {
                    return 3;
                }
                case SegmentType.Room5x4: {
                    return 3;
                }
                case SegmentType.Room5x5: {
                    return 2;
                }
                case SegmentType.Room5x6: {
                    return 1;
                }
                case SegmentType.Room6x5: {
                    return 1;
                }
                case SegmentType.Room6x6: {
                    return 1;
                }
                default: {
                    return 0;
                }
            }
        }
    }
}
