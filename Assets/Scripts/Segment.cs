using System;
using System.Collections.Generic;
using SegmentType = Segment.SegmentType;
using LocalDirection = Direction.LocalDirection;
using GlobalDirection = Direction.GlobalDirection;
using DirectionConversion = Direction.DirectionConversion;
using SegmentExit = Segment.SegmentExit;
using Debug = UnityEngine.Debug;
using UnityEngine;

namespace Segment {
    public abstract class Segment {
        protected SegmentType type;
        protected List<SegmentExit> exits;
        protected int entryX;
        protected int entryZ;
        protected int entryY;
        protected GlobalDirection gDirection;

        private Segment parent;

        private bool join;

        private List<GameObject> instantiated;

        public Segment(SegmentType type, int entryX, int entryZ, int entryY, GlobalDirection gDirection, Segment parent) {
            this.type = type;
            this.entryX = entryX;
            this.entryZ = entryZ;
            this.entryY = entryY;
            this.gDirection = gDirection;
            this.parent = parent;
            instantiated = new List<GameObject>();
        }

        public SegmentType Type {
            get {
                return type;
            }
        }

        public List<SegmentExit> Exits {
            get {
                return exits;
            }
            set {
                exits = value;
            }
        }
        public bool Join {
            get {
                return join;
            }
            set {
                join = value;
            }
        }

        public int X {
            get {
                return entryX;
            }
        }

        public int Z {
            get {
                return entryZ;
            }
        }
        
        public int Y {
            get {
                return entryY;
            }
        }

        public GlobalDirection GlobalDirection {
            get {
                return gDirection;
            }
        }

        public Segment Parent {
            get {
                return parent;
            }
            set {
                parent = value;
            }
        }

        public List<GameObject> Instantiated {
            get {
                return instantiated;
            } 
            set {
                instantiated = value;
            }
        }

        public SegmentExit GetExitByCoord(int x, int z, int y) {
            var exitIndex = exits.FindIndex(exit => x == exit.X && z == exit.Z && y == exit.Y);
            if (exitIndex < 0) throw new Dunegon.RedoSegmentException("GetExitByCoord indexOutOfBounds ix: " + exitIndex);
            return exits[exitIndex];
        }

        public abstract List<(int, int, int)> GetTiles();
        public abstract List<(int, int, int)> NeededSpace();
        public virtual List<Segment> GetAddOnSegments() {
            return new List<Segment>();
        }
        public virtual List<(int, int, int, GlobalDirection, float, GameObject)> GetGSegments(EnvironmentMgr environmentMgr) {
            return new List<(int, int, int, GlobalDirection, float, GameObject)>();
        }

        protected float getRotationByDirection(Dictionary<GlobalDirection, float> rotations) {
            return rotations[gDirection];
        }
    }
    public class StraightSegment : Segment {
        private List<(int, int, int)> _space;
        private List<Segment> _addOnSegments;
        public StraightSegment(int x, int z, int y, GlobalDirection gDirection, Segment parent, bool noCheck = false) : base(SegmentType.Straight, x, z, y, gDirection, parent) {
            exits = new List<SegmentExit>();
            exits.Add(new SegmentExit(entryX, entryZ, entryY, gDirection, 1, 0, 0, LocalDirection.Straight));
            _space = new List<(int, int, int)>();
            if (!noCheck) {
                _space.Add((1, -1, 0));
                _space.Add((1, 0, 0));
                _space.Add((1, 1, 0));
                _space.Add((2, -1, 0));
                _space.Add((2, 0, 0));
                _space.Add((2, 1, 0));
            }
            _addOnSegments = new List<Segment>();
        }

        public override List<(int, int, int)> GetTiles()
        {
            List<(int, int, int)> tiles = new List<(int, int, int)>();
            tiles.Add((entryX, entryZ, entryY));
            return tiles;
        }

        /**
            Return needed spaces in relation to start (0, 0))
        */
        override public List<(int, int, int)> NeededSpace() {
            return _space;
        }

        override public List<Segment> GetAddOnSegments() {
            return _addOnSegments;
        }

        override public List<(int, int, int, GlobalDirection, float, GameObject)> GetGSegments(EnvironmentMgr environmentMgr) {
            var gSegments = new List<(int, int, int, GlobalDirection, float, GameObject)>();
            var rotations = new Dictionary<GlobalDirection, float>();
            rotations.Add(GlobalDirection.North, 90.0f);
            rotations.Add(GlobalDirection.East, 0.0f);
            rotations.Add(GlobalDirection.South, 90.0f);
            rotations.Add(GlobalDirection.West, 0.0f);
            gSegments.Add((entryX, entryZ, entryY, gDirection, getRotationByDirection(rotations), environmentMgr.straight));
            return gSegments;
        }
    }
    
    /**
        Joinsegment is a non-instantiated segment initiating a chain of segments connecting to another "branch" of segments
        It acts as a value carrier for setting up the Join
    */
    public class JoinSegment : Segment {
        private List<(int, int, int)> _space;
        private List<Segment> _addOnSegments;
        private (int, int, int) _joinExitCoord;
        private List<(int, int, int)> _krockCoords;
        private (int, int, int) _joinCoord;

        public JoinSegment(int x, int z, int y, GlobalDirection gDirection, Segment parent) : base(SegmentType.Join, x, z, y, gDirection, parent) {
            exits = new List<SegmentExit>();
            exits.Add(new SegmentExit(entryX, entryZ, entryY, gDirection, 0, 0, 0, LocalDirection.Straight));
            _space = new List<(int, int, int)>();
            _addOnSegments = new List<Segment>();
        }

        public override List<(int, int, int)> GetTiles()
        {
            List<(int, int, int)> tiles = new List<(int, int, int)>();
            return tiles;
        }

        /**
            Return needed spaces in relation to start (0, 0))
        */
        override public List<(int, int, int)> NeededSpace() {
            return _space;
        }

        override public List<Segment> GetAddOnSegments() {
            return _addOnSegments;
        }

        public void SetAddOnSegments(List<Segment> addOnSegments) {
            _addOnSegments = addOnSegments;
        }

        public (int, int, int) JoinExitCoord {
            get {
                return _joinExitCoord;
            }
            set {
                _joinExitCoord = value;
            }
        }
        public (int, int, int) JoinCoord {
            get {
                return _joinCoord;
            }
            set {
                _joinCoord = value;
            }
        }
        public List<(int, int, int)> KrockCoords {
            get {
                return _krockCoords;
            }
            set {
                _krockCoords = value;
            }
        }
    }

    public class StopSegment : Segment {
        public StopSegment(int x, int z, int y, GlobalDirection gDirection, Segment parent) : base(SegmentType.Stop, x, z, y, gDirection, parent) {
            exits = new List<SegmentExit>();
        }

        public override List<(int, int, int)> GetTiles()
        {
            var tiles = new List<(int, int, int)>();
            tiles.Add((entryX, entryZ, entryY));
            return tiles;
        }
        override public List<(int, int, int)> NeededSpace() {
            var space = new List<(int, int, int)>();
            space.Add((1, -1, 0));
            space.Add((1, 0, 0));
            space.Add((1, 1, 0));
            space.Add((2, -1, 0));
            space.Add((2, 0, 0));
            space.Add((2, 1, 0));
            return space;
        }

        override public List<(int, int, int, GlobalDirection, float, GameObject)> GetGSegments(EnvironmentMgr environmentMgr) {
            var gSegments = new List<(int, int, int, GlobalDirection, float, GameObject)>();
            var rotations = new Dictionary<GlobalDirection, float>();
            rotations.Add(GlobalDirection.North, 270.0f);
            rotations.Add(GlobalDirection.East, 180.0f);
            rotations.Add(GlobalDirection.South, 90.0f);
            rotations.Add(GlobalDirection.West, 0.0f);
            gSegments.Add((entryX, entryZ, entryY, gDirection, getRotationByDirection(rotations), environmentMgr.deadEnd));
            return gSegments;
        }
    }

    public class RightSegment : Segment {
        public RightSegment(int x, int z, int y, GlobalDirection gDirection, Segment parent) : base(SegmentType.Right, x, z, y, gDirection, parent) {
            exits = new List<SegmentExit>();
            exits.Add(new SegmentExit(entryX, entryZ, entryY, gDirection, 0, 1, 0, LocalDirection.Right));
        }

        public override List<(int, int, int)> GetTiles() {
            var tiles = new List<(int, int, int)>();
            tiles.Add((entryX, entryZ, entryY));
            return tiles;
        }

        override public List<(int, int, int)> NeededSpace() {
            var space = new List<(int, int, int)>();
            space.Add((1, 1, 0));
            space.Add((0, 1, 0));
            space.Add((-1, 1, 0));
            space.Add((1, 2, 0));
            space.Add((0, 2, 0));
            space.Add((-1, 2, 0));
            return space;
        }
        override public List<(int, int, int, GlobalDirection, float, GameObject)> GetGSegments(EnvironmentMgr environmentMgr) {
            var gSegments = new List<(int, int, int, GlobalDirection, float, GameObject)>();
            var rotations = new Dictionary<GlobalDirection, float>();
            rotations.Add(GlobalDirection.North, 0.0f);
            rotations.Add(GlobalDirection.East, 270.0f);
            rotations.Add(GlobalDirection.South, 180.0f);
            rotations.Add(GlobalDirection.West, 90.0f);
            gSegments.Add((entryX, entryZ, entryY, gDirection, getRotationByDirection(rotations), environmentMgr.cornerSquare));
            return gSegments;
        }
    }

    public class LeftSegment : Segment {
        public LeftSegment(int x, int z, int y, GlobalDirection gDirection, Segment parent) : base(SegmentType.Left, x, z, y, gDirection, parent) {
            exits = new List<SegmentExit>();
            exits.Add(new SegmentExit(entryX, entryZ, entryY, gDirection, 0, -1, 0, LocalDirection.Left));
        }

        public override List<(int, int, int)> GetTiles()
        {
            var tiles = new List<(int, int, int)>();
            tiles.Add((entryX, entryZ, entryY));
            return tiles;
        }

        override public List<(int, int, int)> NeededSpace() {
            var space = new List<(int, int, int)>();
            space.Add((1, -1, 0));
            space.Add((0, -1, 0));
            space.Add((-1, -1, 0));
            space.Add((1, -2, 0));
            space.Add((0, -2, 0));
            space.Add((-1, -2, 0));
            return space;
        }

        override public List<(int, int, int, GlobalDirection, float, GameObject)> GetGSegments(EnvironmentMgr environmentMgr) {
            var gSegments = new List<(int, int, int, GlobalDirection, float, GameObject)>();
            var rotations = new Dictionary<GlobalDirection, float>();
            rotations.Add(GlobalDirection.North, 270.0f);
            rotations.Add(GlobalDirection.East, 180.0f);
            rotations.Add(GlobalDirection.South, 90.0f);
            rotations.Add(GlobalDirection.West, 0.0f);
            gSegments.Add((entryX, entryZ, entryY, gDirection, getRotationByDirection(rotations), environmentMgr.cornerSquare));
            return gSegments;
        }
    }

    public class LeftRightSegment : Segment {
        public LeftRightSegment(int x, int z, int y, GlobalDirection gDirection, Segment parent) : base(SegmentType.LeftRight, x, z, y, gDirection, parent) {
            exits = new List<SegmentExit>();
            exits.Add(new SegmentExit(entryX, entryZ, entryY, gDirection, 0, -1, 0, LocalDirection.Left));
            exits.Add(new SegmentExit(entryX, entryZ, entryY, gDirection, 0, 1, 0, LocalDirection.Right));
        }

        public override List<(int, int, int)> GetTiles() {
            var tiles = new List<(int, int, int)>();
            tiles.Add((entryX, entryZ, entryY));
            return tiles;
        }

        override public List<(int, int, int)> NeededSpace() {
            var space = new List<(int, int, int)>();
            space.Add((1, -1, 0));
            space.Add((0, -1, 0));
            space.Add((-1, -1, 0));
            space.Add((1, -2, 0));
            space.Add((0, -2, 0));
            space.Add((-1, -2, 0));
            space.Add((1, 1, 0));
            space.Add((0, 1, 0));
            space.Add((-1, 1, 0));
            space.Add((1, 2, 0));
            space.Add((0, 2, 0));
            space.Add((-1, 2, 0));
            return space;
        }

        override public List<(int, int, int, GlobalDirection, float, GameObject)> GetGSegments(EnvironmentMgr environmentMgr) {
            var gSegments = new List<(int, int, int, GlobalDirection, float, GameObject)>();
            var rotations = new Dictionary<GlobalDirection, float>();
            rotations.Add(GlobalDirection.North, 180.0f);
            rotations.Add(GlobalDirection.East, 90.0f);
            rotations.Add(GlobalDirection.South, 0.0f);
            rotations.Add(GlobalDirection.West, 270.0f);
            gSegments.Add((entryX, entryZ, entryY, gDirection, getRotationByDirection(rotations), environmentMgr.cross3));
            return gSegments;
        }
    }
    public class StraightRightSegment : Segment {
        private List<(int, int, int)> tiles;
        public StraightRightSegment(int x, int z, int y, GlobalDirection gDirection, Segment parent) : base(SegmentType.StraightRight, x, z, y, gDirection, parent) {
            exits = new List<SegmentExit>();
            exits.Add(new SegmentExit(entryX, entryZ, entryY, gDirection, 1, 0, 0, LocalDirection.Straight));
            exits.Add(new SegmentExit(entryX, entryZ, entryY, gDirection, 0, 1, 0, LocalDirection.Right));
            var localTiles = new List<(int, int, int)>();
            localTiles.Add((0, 0, 0));
            tiles = DirectionConversion.GetGlobalCoordinatesFromLocal(localTiles, entryX, entryZ, entryY, gDirection);
        }

        public override List<(int, int, int)> GetTiles() {
            return tiles;
        }

        override public List<(int, int, int)> NeededSpace() {
            var space = new List<(int, int, int)>();
            space.Add((3, -1, 0));
            space.Add((3, 0, 0));
            space.Add((3, 1, 0));
            space.Add((2, -1, 0));
            space.Add((2, 0, 0));
            space.Add((2, 1, 0));
            space.Add((1, 2, 0));
            space.Add((0, 2, 0));
            space.Add((-1, 2, 0));
            space.Add((1, 3, 0));
            space.Add((0, 3, 0));
            space.Add((-1, 3, 0));
            return space;
        }
        override public List<Segment> GetAddOnSegments() {
            var addOnSegments = new List<Segment>();
            foreach(SegmentExit exit in exits) {
                var segment = new StraightSegment(exit.X, exit.Z, exit.Y, exit.Direction, this, true);
                addOnSegments.Add(segment);
            }
            return addOnSegments;
        }

        override public List<(int, int, int, GlobalDirection, float, GameObject)> GetGSegments(EnvironmentMgr environmentMgr) {
            var gSegments = new List<(int, int, int, GlobalDirection, float, GameObject)>();
            var rotations = new Dictionary<GlobalDirection, float>();
            rotations.Add(GlobalDirection.North, 270.0f);
            rotations.Add(GlobalDirection.East, 180.0f);
            rotations.Add(GlobalDirection.South, 90.0f);
            rotations.Add(GlobalDirection.West, 0.0f);
            gSegments.Add((entryX, entryZ, entryY, gDirection, getRotationByDirection(rotations), environmentMgr.cross3));
            return gSegments;
        }
    }

    public class StraightLeftSegment : Segment {
        private List<(int, int, int)> tiles;
        public StraightLeftSegment(int x, int z, int y, GlobalDirection gDirection, Segment parent) : base(SegmentType.StraightLeft, x, z, y, gDirection, parent) {
            exits = new List<SegmentExit>();
            exits.Add(new SegmentExit(entryX, entryZ, entryY, gDirection, 1, 0, 0, LocalDirection.Straight));
            exits.Add(new SegmentExit(entryX, entryZ, entryY, gDirection, 0, -1, 0, LocalDirection.Left));
            var localTiles = new List<(int, int, int)>();
            localTiles.Add((0, 0, 0));
            tiles = DirectionConversion.GetGlobalCoordinatesFromLocal(localTiles, entryX, entryZ, entryY, gDirection);
        }

        public override List<(int, int, int)> GetTiles() {
            return tiles;
        }

        override public List<(int, int, int)> NeededSpace() {
            var space = new List<(int, int, int)>();
            space.Add((3, -1, 0));
            space.Add((3, 0, 0));
            space.Add((3, 1, 0));
            space.Add((2, -1, 0));
            space.Add((2, 0, 0));
            space.Add((2, 1, 0));
            space.Add((1, -2, 0));
            space.Add((0, -2, 0));
            space.Add((-1, -2, 0));
            space.Add((1, -3, 0));
            space.Add((0, -3, 0));
            space.Add((-1, -3, 0));
            return space;
        }

        override public List<Segment> GetAddOnSegments() {
            var addOnSegments = new List<Segment>();
            foreach(SegmentExit exit in exits) {
                var segment = new StraightSegment(exit.X, exit.Z, exit.Y, exit.Direction, this, true);
                addOnSegments.Add(segment);
            }
            return addOnSegments;
        }

        override public List<(int, int, int, GlobalDirection, float, GameObject)> GetGSegments(EnvironmentMgr environmentMgr) {
            var gSegments = new List<(int, int, int, GlobalDirection, float, GameObject)>();
            var rotations = new Dictionary<GlobalDirection, float>();
            rotations.Add(GlobalDirection.North, 90.0f);
            rotations.Add(GlobalDirection.East, 0.0f);
            rotations.Add(GlobalDirection.South, 270.0f);
            rotations.Add(GlobalDirection.West, 180.0f);
            gSegments.Add((entryX, entryZ, entryY, gDirection, getRotationByDirection(rotations), environmentMgr.cross3));
            return gSegments;
        }
    }

    public class LeftStraightRightSegment : Segment {
        private List<(int, int, int)> _tiles;

        public LeftStraightRightSegment(int x, int z, int y, GlobalDirection gDirection, Segment parent) : base(SegmentType.Left, x, z, y, gDirection, parent) {
            exits = new List<SegmentExit>();
            exits.Add(new SegmentExit(entryX, entryZ, entryY, gDirection, 0, -1, 0, LocalDirection.Left));
            exits.Add(new SegmentExit(entryX, entryZ, entryY, gDirection, 0, 1, 0, LocalDirection.Right));
            exits.Add(new SegmentExit(entryX, entryZ, entryY, gDirection, 1, 0, 0, LocalDirection.Straight));
            var localTiles = new List<(int, int, int)>();
            localTiles.Add((0, 0, 0));
            _tiles = DirectionConversion.GetGlobalCoordinatesFromLocal(localTiles, entryX, entryZ, entryY, gDirection);
        }

        public override List<(int, int, int)> GetTiles()
        {
            return _tiles;
        }

        override public List<(int, int, int)> NeededSpace() {
            var space = new List<(int, int, int)>();
            //Left
            space.Add((1, -2, 0));
            space.Add((0, -2, 0));
            space.Add((-1, -2, 0));
            space.Add((1, -3, 0));
            space.Add((0, -3, 0));
            space.Add((-1, -3, 0));
            space.Add((2, 0, 0));
            space.Add((2, 1, 0));
            space.Add((2, -1, 0));
            space.Add((3, -1, 0));
            space.Add((3, 0, 0));
            space.Add((3, 1, 0));
            space.Add((0, 2, 0));
            space.Add((1, 2, 0));
            space.Add((-1, 2, 0));
            space.Add((1, 3, 0));
            space.Add((0, 3, 0));
            space.Add((-1, 3, 0));
            return space;
        }

        override public List<Segment> GetAddOnSegments() {
            var addOnSegments = new List<Segment>();
            foreach(SegmentExit exit in exits) {
                var segment = new StraightSegment(exit.X, exit.Z, exit.Y, exit.Direction, this, true);
                addOnSegments.Add(segment);
            }
            return addOnSegments;
        }

        override public List<(int, int, int, GlobalDirection, float, GameObject)> GetGSegments(EnvironmentMgr environmentMgr) {
            var gSegments = new List<(int, int, int, GlobalDirection, float, GameObject)>();
            gSegments.Add((entryX, entryZ, entryY, gDirection, 0f, environmentMgr.cross4));
            return gSegments;
        }
    }
}
