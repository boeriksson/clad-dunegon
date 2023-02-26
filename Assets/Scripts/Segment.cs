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
        protected SegmentType _type;
        protected List<SegmentExit> _exits;
        protected int _entryX;
        protected int _entryZ;
        protected GlobalDirection _gDirection;

        private Segment _parent;

        private bool _join;

        private List<GameObject> _instantiated;

        public Segment(SegmentType type, int entryX, int entryZ, GlobalDirection gDirection, Segment parent) {
            _type = type;
            _entryX = entryX;
            _entryZ = entryZ;
            _gDirection = gDirection;
            _parent = parent;
            _instantiated = new List<GameObject>();
        }

        public SegmentType Type {
            get {
                return _type;
            }
        }

        public List<SegmentExit> Exits {
            get {
                return _exits;
            }
            set {
                _exits = value;
            }
        }
        public bool Join {
            get {
                return _join;
            }
            set {
                _join = value;
            }
        }

        public int X {
            get {
                return _entryX;
            }
        }

        public int Z {
            get {
                return _entryZ;
            }
        }

        public GlobalDirection GlobalDirection {
            get {
                return _gDirection;
            }
        }

        public Segment Parent {
            get {
                return _parent;
            }
            set {
                _parent = value;
            }
        }

        public List<GameObject> Instantiated {
            get {
                return _instantiated;
            } 
            set {
                _instantiated = value;
            }
        }

        public SegmentExit GetExitByCoord(int x, int z) {
            var exitIndex = _exits.FindIndex(exit => x == exit.X && z == exit.Z);
            if (exitIndex < 0) throw new Dunegon.RedoSegmentException("GetExitByCoord indexOutOfBounds ix: " + exitIndex);
            return _exits[exitIndex];
        }

        public abstract List<(int, int)> GetTiles();
        public abstract List<(int, int)> NeededSpace();
        public virtual List<Segment> GetAddOnSegments() {
            return new List<Segment>();
        }
        public virtual List<(int, int, GlobalDirection, float, GameObject)> GetGSegments(EnvironmentMgr environmentMgr) {
            return new List<(int, int, GlobalDirection, float, GameObject)>();
        }

        protected float getRotationByDirection(Dictionary<GlobalDirection, float> rotations) {
            return rotations[_gDirection];
        }
    }
    public class StraightSegment : Segment {
        private List<(int, int)> _space;
        private List<Segment> _addOnSegments;
        public StraightSegment(int x, int z, GlobalDirection gDirection, Segment parent, bool noCheck = false) : base(SegmentType.Straight, x, z, gDirection, parent) {
            _exits = new List<SegmentExit>();
            _exits.Add(new SegmentExit(_entryX, _entryZ, gDirection, 1, 0, LocalDirection.Straight));
            _space = new List<(int, int)>();
            if (!noCheck) {
                _space.Add((1, -1));
                _space.Add((1, 0));
                _space.Add((1, 1));
                _space.Add((2, -1));
                _space.Add((2, 0));
                _space.Add((2, 1));
            }
            _addOnSegments = new List<Segment>();
        }

        public override List<(int, int)> GetTiles()
        {
            List<(int, int)> tiles = new List<(int, int)>();
            tiles.Add((_entryX, _entryZ));
            return tiles;
        }

        /**
            Return needed spaces in relation to start (0, 0))
        */
        override public List<(int, int)> NeededSpace() {
            return _space;
        }

        override public List<Segment> GetAddOnSegments() {
            return _addOnSegments;
        }

        override public List<(int, int, GlobalDirection, float, GameObject)> GetGSegments(EnvironmentMgr environmentMgr) {
            var gSegments = new List<(int, int, GlobalDirection, float, GameObject)>();
            var rotations = new Dictionary<GlobalDirection, float>();
            rotations.Add(GlobalDirection.North, 90.0f);
            rotations.Add(GlobalDirection.East, 0.0f);
            rotations.Add(GlobalDirection.South, 90.0f);
            rotations.Add(GlobalDirection.West, 0.0f);
            gSegments.Add((_entryX, _entryZ, _gDirection, getRotationByDirection(rotations), environmentMgr.straight));
            return gSegments;
        }
    }
    
    /**
        Joinsegment is a non-instantiated segment initiating a chain of segments connecting to another "branch" of segments
        It acts as a value carrier for setting up the Join
    */
    public class JoinSegment : Segment {
        private List<(int, int)> _space;
        private List<Segment> _addOnSegments;
        private (int, int) _joinExitCoord;
        private List<(int, int)> _krockCoords;
        private (int, int) _joinCoord;

        public JoinSegment(int x, int z, GlobalDirection gDirection, Segment parent) : base(SegmentType.Join, x, z, gDirection, parent) {
            _exits = new List<SegmentExit>();
            _exits.Add(new SegmentExit(_entryX, _entryZ, gDirection, 0, 0, LocalDirection.Straight));
            _space = new List<(int, int)>();
            _addOnSegments = new List<Segment>();
        }

        public override List<(int, int)> GetTiles()
        {
            List<(int, int)> tiles = new List<(int, int)>();
            return tiles;
        }

        /**
            Return needed spaces in relation to start (0, 0))
        */
        override public List<(int, int)> NeededSpace() {
            return _space;
        }

        override public List<Segment> GetAddOnSegments() {
            return _addOnSegments;
        }

        public void SetAddOnSegments(List<Segment> addOnSegments) {
            _addOnSegments = addOnSegments;
        }

        public (int, int) JoinExitCoord {
            get {
                return _joinExitCoord;
            }
            set {
                _joinExitCoord = value;
            }
        }
        public (int, int) JoinCoord {
            get {
                return _joinCoord;
            }
            set {
                _joinCoord = value;
            }
        }
        public List<(int, int)> KrockCoords {
            get {
                return _krockCoords;
            }
            set {
                _krockCoords = value;
            }
        }
    }

    public class StopSegment : Segment {
        public StopSegment(int x, int z, GlobalDirection gDirection, Segment parent) : base(SegmentType.Stop, x, z, gDirection, parent) {
            _exits = new List<SegmentExit>();
        }

        public override List<(int, int)> GetTiles()
        {
            var tiles = new List<(int, int)>();
            tiles.Add((_entryX, _entryZ));
            return tiles;
        }
        override public List<(int, int)> NeededSpace() {
            var space = new List<(int, int)>();
            space.Add((1, -1));
            space.Add((1, 0));
            space.Add((1, 1));
            space.Add((2, -1));
            space.Add((2, 0));
            space.Add((2, 1));
            return space;
        }

        override public List<(int, int, GlobalDirection, float, GameObject)> GetGSegments(EnvironmentMgr environmentMgr) {
            var gSegments = new List<(int, int, GlobalDirection, float, GameObject)>();
            var rotations = new Dictionary<GlobalDirection, float>();
            rotations.Add(GlobalDirection.North, 270.0f);
            rotations.Add(GlobalDirection.East, 180.0f);
            rotations.Add(GlobalDirection.South, 90.0f);
            rotations.Add(GlobalDirection.West, 0.0f);
            gSegments.Add((_entryX, _entryZ, _gDirection, getRotationByDirection(rotations), environmentMgr.deadEnd));
            return gSegments;
        }
    }

    public class RightSegment : Segment {
        public RightSegment(int x, int z, GlobalDirection gDirection, Segment parent) : base(SegmentType.Right, x, z, gDirection, parent) {
            _exits = new List<SegmentExit>();
            _exits.Add(new SegmentExit(_entryX, _entryZ, gDirection, 0, 1, LocalDirection.Right));
        }

        public override List<(int, int)> GetTiles()
        {
            var tiles = new List<(int, int)>();
            tiles.Add((_entryX, _entryZ));
            return tiles;
        }

        override public List<(int, int)> NeededSpace() {
            var space = new List<(int, int)>();
            space.Add((1, 1));
            space.Add((0, 1));
            space.Add((-1, 1));
            space.Add((1, 2));
            space.Add((0, 2));
            space.Add((-1,2));
            //space.Add((1, 0));
            return space;
        }

        override public List<(int, int, GlobalDirection, float, GameObject)> GetGSegments(EnvironmentMgr environmentMgr) {
            var gSegments = new List<(int, int, GlobalDirection, float, GameObject)>();
            var rotations = new Dictionary<GlobalDirection, float>();
            rotations.Add(GlobalDirection.North, 0.0f);
            rotations.Add(GlobalDirection.East, 270.0f);
            rotations.Add(GlobalDirection.South, 180.0f);
            rotations.Add(GlobalDirection.West, 90.0f);
            gSegments.Add((_entryX, _entryZ, _gDirection, getRotationByDirection(rotations), environmentMgr.cornerSquare));
            return gSegments;
        }
    }

    public class LeftSegment : Segment {
        public LeftSegment(int x, int z, GlobalDirection gDirection, Segment parent) : base(SegmentType.Left, x, z, gDirection, parent) {
            _exits = new List<SegmentExit>();
            _exits.Add(new SegmentExit(_entryX, _entryZ, gDirection, 0, -1, LocalDirection.Left));
        }

        public override List<(int, int)> GetTiles()
        {
            var tiles = new List<(int, int)>();
            tiles.Add((_entryX, _entryZ));
            return tiles;
        }

        override public List<(int, int)> NeededSpace() {
            var space = new List<(int, int)>();
            space.Add((1, -1));
            space.Add((0, -1));
            space.Add((-1, -1));
            space.Add((1, -2));
            space.Add((0, -2));
            space.Add((-1, -2));
            //space.Add((1, 0));
            return space;
        }

        override public List<(int, int, GlobalDirection, float, GameObject)> GetGSegments(EnvironmentMgr environmentMgr) {
            var gSegments = new List<(int, int, GlobalDirection, float, GameObject)>();
            var rotations = new Dictionary<GlobalDirection, float>();
            rotations.Add(GlobalDirection.North, 270.0f);
            rotations.Add(GlobalDirection.East, 180.0f);
            rotations.Add(GlobalDirection.South, 90.0f);
            rotations.Add(GlobalDirection.West, 0.0f);
            gSegments.Add((_entryX, _entryZ, _gDirection,  getRotationByDirection(rotations), environmentMgr.cornerSquare));
            return gSegments;
        }
    }

    public class LeftRightSegment : Segment {
        public LeftRightSegment(int x, int z, GlobalDirection gDirection, Segment parent) : base(SegmentType.LeftRight, x, z, gDirection, parent) {
            _exits = new List<SegmentExit>();
            _exits.Add(new SegmentExit(_entryX, _entryZ, gDirection, 0, -1, LocalDirection.Left));
            _exits.Add(new SegmentExit(_entryX, _entryZ, gDirection, 0, 1, LocalDirection.Right));
        }

        public override List<(int, int)> GetTiles()
        {
            var tiles = new List<(int, int)>();
            tiles.Add((_entryX, _entryZ));
            return tiles;
        }

        override public List<(int, int)> NeededSpace() {
            var space = new List<(int, int)>();
            space.Add((1, -1));
            space.Add((0, -1));
            space.Add((-1, -1));
            space.Add((1, -2));
            space.Add((0, -2));
            space.Add((-1, -2));
            space.Add((1, 1));
            space.Add((0, 1));
            space.Add((-1, 1));
            space.Add((1, 2));
            space.Add((0, 2));
            space.Add((-1,2));
            return space;
        }

        override public List<(int, int, GlobalDirection, float, GameObject)> GetGSegments(EnvironmentMgr environmentMgr) {
            var gSegments = new List<(int, int, GlobalDirection, float, GameObject)>();
            var rotations = new Dictionary<GlobalDirection, float>();
            rotations.Add(GlobalDirection.North, 180.0f);
            rotations.Add(GlobalDirection.East, 90.0f);
            rotations.Add(GlobalDirection.South, 0.0f);
            rotations.Add(GlobalDirection.West, 270.0f);
            gSegments.Add((_entryX, _entryZ, _gDirection, getRotationByDirection(rotations), environmentMgr.cross3));
            return gSegments;
        }
    }
    public class StraightRightSegment : Segment {
        private List<(int, int)> _tiles;
        public StraightRightSegment(int x, int z, GlobalDirection gDirection, Segment parent) : base(SegmentType.StraightRight, x, z, gDirection, parent) {
            _exits = new List<SegmentExit>();
            _exits.Add(new SegmentExit(_entryX, _entryZ, gDirection, 1, 0, LocalDirection.Straight));
            _exits.Add(new SegmentExit(_entryX, _entryZ, gDirection, 0, 1, LocalDirection.Right));
            var localTiles = new List<(int, int)>();
            localTiles.Add((0, 0));
            _tiles = DirectionConversion.GetGlobalCoordinatesFromLocal(localTiles, _entryX, _entryZ, gDirection);
        }

        public override List<(int, int)> GetTiles() {
            return _tiles;
        }

        override public List<(int, int)> NeededSpace() {
            var space = new List<(int, int)>();
            space.Add((3, -1));
            space.Add((3, 0));
            space.Add((3, 1));
            space.Add((2, -1));
            space.Add((2, 0));
            space.Add((2, 1));
            space.Add((1, 2));
            space.Add((0, 2));
            space.Add((-1, 2));
            space.Add((1, 3));
            space.Add((0, 3));
            space.Add((-1, 3));
            return space;
        }
        override public List<Segment> GetAddOnSegments() {
            var addOnSegments = new List<Segment>();
            foreach(SegmentExit exit in _exits) {
                var segment = new StraightSegment(exit.X, exit.Z, exit.Direction, this, true);
                addOnSegments.Add(segment);
            }
            return addOnSegments;
        }

        override public List<(int, int, GlobalDirection, float, GameObject)> GetGSegments(EnvironmentMgr environmentMgr) {
            var gSegments = new List<(int, int, GlobalDirection, float, GameObject)>();
            var rotations = new Dictionary<GlobalDirection, float>();
            rotations.Add(GlobalDirection.North, 270.0f);
            rotations.Add(GlobalDirection.East, 180.0f);
            rotations.Add(GlobalDirection.South, 90.0f);
            rotations.Add(GlobalDirection.West, 0.0f);
            gSegments.Add((_entryX, _entryZ, _gDirection, getRotationByDirection(rotations), environmentMgr.cross3));
            return gSegments;
        }
    }

    public class StraightLeftSegment : Segment {
        private List<(int, int)> _tiles;
        public StraightLeftSegment(int x, int z, GlobalDirection gDirection, Segment parent) : base(SegmentType.StraightLeft, x, z, gDirection, parent) {
            _exits = new List<SegmentExit>();
            _exits.Add(new SegmentExit(_entryX, _entryZ, gDirection, 1, 0, LocalDirection.Straight));
            _exits.Add(new SegmentExit(_entryX, _entryZ, gDirection, 0, -1, LocalDirection.Left));
            var localTiles = new List<(int, int)>();
            localTiles.Add((0, 0));
            _tiles = DirectionConversion.GetGlobalCoordinatesFromLocal(localTiles, _entryX, _entryZ, gDirection);
        }

        public override List<(int, int)> GetTiles() {
            return _tiles;
        }

        override public List<(int, int)> NeededSpace() {
            var space = new List<(int, int)>();
            space.Add((3, -1));
            space.Add((3, 0));
            space.Add((3, 1));
            space.Add((2, -1));
            space.Add((2, 0));
            space.Add((2, 1));
            space.Add((1, -2));
            space.Add((0, -2));
            space.Add((-1, -2));
            space.Add((1, -3));
            space.Add((0, -3));
            space.Add((-1, -3));
            return space;
        }

        override public List<Segment> GetAddOnSegments() {
            var addOnSegments = new List<Segment>();
            foreach(SegmentExit exit in _exits) {
                var segment = new StraightSegment(exit.X, exit.Z, exit.Direction, this, true);
                addOnSegments.Add(segment);
            }
            return addOnSegments;
        }

        override public List<(int, int, GlobalDirection, float, GameObject)> GetGSegments(EnvironmentMgr environmentMgr) {
            var gSegments = new List<(int, int, GlobalDirection, float, GameObject)>();
            var rotations = new Dictionary<GlobalDirection, float>();
            rotations.Add(GlobalDirection.North, 90.0f);
            rotations.Add(GlobalDirection.East, 0.0f);
            rotations.Add(GlobalDirection.South, 270.0f);
            rotations.Add(GlobalDirection.West, 180.0f);
            gSegments.Add((_entryX, _entryZ, _gDirection, getRotationByDirection(rotations), environmentMgr.cross3));
            return gSegments;
        }
    }

    public class LeftStraightRightSegment : Segment {
        private List<(int, int)> _tiles;

        public LeftStraightRightSegment(int x, int z, GlobalDirection gDirection, Segment parent) : base(SegmentType.Left, x, z, gDirection, parent) {
            _exits = new List<SegmentExit>();
            _exits.Add(new SegmentExit(_entryX, _entryZ, gDirection, 0, -1, LocalDirection.Left));
            _exits.Add(new SegmentExit(_entryX, _entryZ, gDirection, 0, 1, LocalDirection.Right));
            _exits.Add(new SegmentExit(_entryX, _entryZ, gDirection, 1, 0, LocalDirection.Straight));
            var localTiles = new List<(int, int)>();
            localTiles.Add((0, 0));
            _tiles = DirectionConversion.GetGlobalCoordinatesFromLocal(localTiles, _entryX, _entryZ, gDirection);
        }

        public override List<(int, int)> GetTiles()
        {
            return _tiles;
        }

        override public List<(int, int)> NeededSpace() {
            var space = new List<(int, int)>();
            //Left
            space.Add((1, -2));
            space.Add((0, -2));
            space.Add((-1, -2));
            space.Add((1, -3));
            space.Add((0, -3));
            space.Add((-1, -3));
            //Straight(2, -1));
            space.Add((2, 0));
            space.Add((2, 1));
            space.Add((2, -1));
            space.Add((3, -1));
            space.Add((3, 0));
            space.Add((3, 1));
            //Right(1, 2));
            space.Add((0, 2));
            space.Add((1, 2));
            space.Add((-1, 2));
            space.Add((1, 3));
            space.Add((0, 3));
            space.Add((-1,3));
            return space;
        }

        override public List<Segment> GetAddOnSegments() {
            var addOnSegments = new List<Segment>();
            foreach(SegmentExit exit in _exits) {
                var segment = new StraightSegment(exit.X, exit.Z, exit.Direction, this, true);
                addOnSegments.Add(segment);
            }
            return addOnSegments;
        }

        override public List<(int, int, GlobalDirection, float, GameObject)> GetGSegments(EnvironmentMgr environmentMgr) {
            var gSegments = new List<(int, int, GlobalDirection, float, GameObject)>();
            gSegments.Add((_entryX, _entryZ, _gDirection, 0f, environmentMgr.cross4));
            return gSegments;
        }
    }
}
