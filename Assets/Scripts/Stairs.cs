using System.Collections.Generic;
using Direction;
using UnityEngine;
using util;

namespace Segment {
    public abstract class Stair : Segment {
        protected RandomGenerator randomGenerator;
        protected List<(int, int, int)> tiles;
        protected List<(int, int, int)> space;
        
        protected Stair(SegmentType type, int entryX, int entryZ, int entryY, GlobalDirection gDirection, Segment parent) : base(type, entryX, entryZ, entryY, gDirection, parent) {
            randomGenerator = new DefaultRandom();    
        }
    }
    
    public class StraightStair : Stair {
        protected readonly int vLocalChange;
        protected readonly VerticalDirection vDirection;
        public StraightStair(int x, int z, int y, GlobalDirection gDirection, VerticalDirection vDirection, Segment parent) : base(vDirection == VerticalDirection.Up ? SegmentType.SStairsUp : SegmentType.SStairsDown, x, z, y, gDirection, parent) {
            this.vDirection = vDirection;
            vLocalChange = vDirection == VerticalDirection.Down ? -1 : 1;
            exits = new List<SegmentExit> { new(entryX, entryZ, entryY, gDirection, 2, 0, vLocalChange, LocalDirection.Straight) };
            tiles = DirectionConversion.GetGlobalCoordinatesFromLocal(new List<(int, int, int)>() {(0, 0, 0), (1, 0, 0), (0, 0, vLocalChange), (1, 0, vLocalChange)}, x, z, y, gDirection);
            SetSpace();
            Debug.Log("Adding straight stair with Vertical direction: " + vDirection + " at (" + entryX + ", " +
                      entryZ + ", " + entryY + ")");
        }

        public override List<(int, int, int)> GetTiles() {
            return tiles;
        }

        private void SetSpace() {
            space = new List<(int, int, int)> {
                (0, 1, 0),
                (0, 0, 0),
                (0, -1, 0),
                (1, 1, 0),
                (1, 0, 0),
                (1, -1, 0),
                (2, 1, 0),
                (2, 0, 0),
                (2, -1, 0),
                (3, 1, 0),
                (3, 0, 0),
                (3, -1, 0),
                (0, 1, vLocalChange),
                (0, 0, vLocalChange),
                (0, -1, vLocalChange),
                (1, 1, vLocalChange),
                (1, 0, vLocalChange),
                (1, -1, vLocalChange),
                (2, 1, vLocalChange),
                (2, 0, vLocalChange),
                (2, -1, vLocalChange),
                (3, 1, vLocalChange),
                (3, 0, vLocalChange),
                (3, -1, vLocalChange)
            };
        }
        
        public override List<(int, int, int)> NeededSpace() {
            return space;
        }
        
        override public List<(int, int, int, GlobalDirection, float, GameObject)> GetGSegments(EnvironmentMgr environmentMgr) {
            var gSegments = new List<(int, int, int, GlobalDirection, float, GameObject)>();
            var rotations = new Dictionary<GlobalDirection, float>();
            var xEntry = entryX;
            var zEntry = entryZ;
            var yEntry = entryY;
            if (vDirection == VerticalDirection.Down) {
                rotations.Add(GlobalDirection.North, 270.0f);
                rotations.Add(GlobalDirection.East, 180.0f);
                rotations.Add(GlobalDirection.South, 90.0f);
                rotations.Add(GlobalDirection.West, 0.0f);
            } else { 
                rotations.Add(GlobalDirection.North, 90.0f);
                rotations.Add(GlobalDirection.East, 0.0f);
                rotations.Add(GlobalDirection.South, 270.0f);
                rotations.Add(GlobalDirection.West, 180.0f);
                var gCoords = DirectionConversion.GetGlobalCoordinateFromLocal((1, 0, 1), entryX, entryZ, entryY, gDirection);
                xEntry = gCoords.Item1;
                zEntry = gCoords.Item2;
                yEntry = gCoords.Item3;
            }

            gSegments.Add((xEntry, zEntry, yEntry, gDirection, getRotationByDirection(rotations), environmentMgr.straightStairs));
            return gSegments;
        }
    }
}