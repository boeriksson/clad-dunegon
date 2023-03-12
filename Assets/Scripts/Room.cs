using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GlobalDirection = Direction.GlobalDirection;
using LocalDirection = Direction.LocalDirection;
using DirectionConversion = Direction.DirectionConversion;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;
using RandomGenerator = util.RandomGenerator;
using DefaultRandom = util.DefaultRandom;
using GameObject = UnityEngine.GameObject;

namespace Segment {
    public abstract class Room : Segment {
        protected RandomGenerator randomGenerator;
        protected List<(int, int)> _tiles;
        protected List<(int, int)> _space;
        //protected Logger logger = new Logger("./Logs/dunegon.log");
        protected Room(SegmentType type, int entryX, int entryZ, GlobalDirection gDirection, Segment parent) : base(type, entryX, entryZ, gDirection, parent) {
            randomGenerator = new DefaultRandom();
        }
        protected Room(SegmentType type, int entryX, int entryZ, GlobalDirection gDirection, Segment parent, RandomGenerator _randomGenerator) : base(type, entryX, entryZ, gDirection, parent) {
            randomGenerator = _randomGenerator;
        }

        protected List<(int, int)> GetBoxCoordinates(List<(int, int, int, int)> boxList, int x, int z, GlobalDirection gDirection, Boolean removeCoordBeforeEntry = false) {
            var coordinateList = new List<(int, int)>();
            foreach((int, int, int, int) box in boxList) {
                for (int i = box.Item1; i <= box.Item3; i++) {
                    for (int j = box.Item2; j <= box.Item4; j++) {
                        coordinateList.Add((i, j));
                    }
                }
            }
            
            if (removeCoordBeforeEntry) {
                int ixOfEntry = -1;
                int i = 0;
                foreach (var coord in coordinateList) {
                    if (coord.Item1 == -1 && coord.Item2 == 0) {
                        ixOfEntry = i;
                    }
                    i++;
                }
                coordinateList.RemoveAt(ixOfEntry);
            }
            
            //LocaltoGlobal transform
            return coordinateList; //DirectionConversion.GetGlobalCoordinatesFromLocal(coordinateList, x, z, gDirection);
        }

        protected List<(int, int, int, int)> GetBoxList((int, int, int, int)[] boxCoord) {
            var boxList = new List<(int, int, int, int)>();
            foreach(var coord in boxCoord) {
                boxList.Add(coord);
            }
            return boxList;
        }

        protected List<SegmentExit> GetExits(int x, int z, GlobalDirection gDirection, List<(int, int, LocalDirection)> potentialLocalExits, List<(int, int)> percentageOfExits)
        {
            var noOfExits = GetNumberOfExits(percentageOfExits);

            RemoveSurplusExits(potentialLocalExits, noOfExits);

            var exits = new List<SegmentExit>();
            foreach ((int, int, LocalDirection) potentialExit in potentialLocalExits) {
                exits.Add(new SegmentExit(x, z, gDirection, potentialExit.Item1, potentialExit.Item2, potentialExit.Item3));
            }
            return exits;
        }

        protected void RemoveSurplusExits(List<(int, int, LocalDirection)> potentialLocalExits, int noOfExits)
        {
            var exitsToRemove = potentialLocalExits.Count - noOfExits;
            for (int i = 0; i < exitsToRemove; i++) {
                var randomExitToRemove = randomGenerator.Generate(potentialLocalExits.Count) - 1;
                potentialLocalExits.RemoveAt(randomExitToRemove);
            }
        }

        protected int GetNumberOfExits(List<(int, int)> percentageOfExits) {
            int totalPerc = 0;
            
            foreach ((int, int) exDef in percentageOfExits){
                totalPerc += exDef.Item2;
            }

            int noOfExits = 0;
            var ran = randomGenerator.Generate(totalPerc);


            int collectPerc = 0;
            foreach ((int, int) exDef in percentageOfExits) {
                collectPerc += exDef.Item2;
                if (collectPerc >= ran) {
                    noOfExits = exDef.Item1;
                    break;
                }
            }

            return noOfExits;
        }

        public override List<(int, int)> GetTiles() {
            return _tiles;
        }

        public override List<(int, int)> NeededSpace() {
            return _space;
        }

        protected (int, int, int, int) getMinMax() {
            int minX = 999;
            int minZ = 999;
            int maxX = -999;
            int maxZ = -999;

            foreach((int, int) coord in _tiles) {
                if (coord.Item1 < minX) minX = coord.Item1;
                if (coord.Item2 < minZ) minZ = coord.Item2;
                if (coord.Item1 > maxX) maxX = coord.Item1;
                if (coord.Item2 > maxZ) maxZ = coord.Item2;
            }
            return (minX, minZ, maxX, maxZ);
        }

        protected bool hasExit(int x, int z) {
            return _exits.Exists(exit => exit.X == x && exit.Z == z);
        }

        override public List<(int, int, GlobalDirection, float, GameObject)> GetGSegments(EnvironmentMgr environmentMgr) {
            var gSegments = new List<(int, int, GlobalDirection, float, GameObject)>();
            var (minX, minZ, maxX, maxZ) = getMinMax(); // (minX, minZ, maxX, maxZ)

            foreach((int, int) coord in _tiles) {
                int x = coord.Item1;
                int z = coord.Item2;
                bool isEntry = _entryX == x && _entryZ == z;
                float rotation = 0f;
                GameObject gSegment;
                if (x == minX && z == minZ) { //SouthWest corner
                    if (hasExit(x - 1, z) && hasExit(x, z - 1)) {
                        gSegment = environmentMgr.floorCelingCornerLeftRightExit;
                    } else if (hasExit(x - 1, z)) { 
                        gSegment = environmentMgr.floorCelingCornerRightExit;
                    } else if (hasExit(x, z - 1)) {
                        gSegment = environmentMgr.floorCelingCornerLeftExit;
                    } else {
                        gSegment = environmentMgr.floorCelingCorner;
                    }
                    rotation = 90.0f;
                } else if (x == minX && z == maxZ) {//SouthEast corner
                    if (hasExit(x - 1, z) && hasExit(x, z + 1)) {
                        gSegment = environmentMgr.floorCelingCornerLeftRightExit;
                    } else if (hasExit(x - 1, z)) { 
                        gSegment = environmentMgr.floorCelingCornerLeftExit;
                    } else if (hasExit(x, z + 1)) {
                        gSegment = environmentMgr.floorCelingCornerRightExit;
                    } else {
                        gSegment = environmentMgr.floorCelingCorner;
                    }
                    rotation = 180.0f;
                } else if (x == maxX && z == maxZ) { //NorthEast corner
                    if (hasExit(x + 1, z) && hasExit(x, z + 1)) {
                        gSegment = environmentMgr.floorCelingCornerLeftRightExit;
                    } else if (hasExit(x + 1, z)) { 
                        gSegment = environmentMgr.floorCelingCornerRightExit;
                    } else if (hasExit(x, z + 1)) {
                        gSegment = environmentMgr.floorCelingCornerLeftExit;
                    } else {
                        gSegment = environmentMgr.floorCelingCorner;
                    }
                    rotation = 270.0f;
                } else if (x == maxX && z == minZ) { //NorthWest corner
                    if (hasExit(x + 1, z) && hasExit(x, z - 1)) {
                        gSegment = environmentMgr.floorCelingCornerLeftRightExit;
                    } else if (hasExit(x + 1, z)) { 
                        gSegment = environmentMgr.floorCelingCornerLeftExit;
                    } else if (hasExit(x, z - 1)) {
                        gSegment = environmentMgr.floorCelingCornerRightExit;
                    } else {
                        gSegment = environmentMgr.floorCelingCorner;
                    }
                    rotation = 0.0f;
                } else if (x == maxX) { //North wall
                    if (hasExit(x + 1, z) || isEntry) {
                        gSegment = environmentMgr.floorCelingExit;
                    } else {
                        gSegment = environmentMgr.floorCelingWall;
                    }
                    rotation = 270.0f;
                } else if (x == minX) { //South wall
                    if (hasExit(x - 1, z) || isEntry) {
                        gSegment = environmentMgr.floorCelingExit;
                    } else {
                        gSegment = environmentMgr.floorCelingWall;
                    }
                    rotation = 90.0f;
                } else if (z == maxZ) { //East wall
                    if (hasExit(x, z + 1) || isEntry) {
                        gSegment = environmentMgr.floorCelingExit;
                    } else {
                        gSegment = environmentMgr.floorCelingWall;
                    }
                    rotation = 180.0f;
                } else if (z == minZ) { //West wall
                    if (hasExit(x, z - 1) || isEntry) {
                        gSegment = environmentMgr.floorCelingExit;
                    } else {
                        gSegment = environmentMgr.floorCelingWall;
                    }
                    rotation = 0.0f;
                }
                else {
                    gSegment = environmentMgr.floorCeling;
                }

                gSegments.Add((x, z, _gDirection, rotation, gSegment));
            }
            return gSegments;
        }

        private SegmentExit ShiftExitToSide(SegmentExit exit, GlobalDirection gDirection) {
            (int minX, int minZ, int maxX, int maxZ) minMax = getMinMax();
            Debug.Log("ShiftExitToSide inc exit: (" + exit.X + ", " + exit.Z + ") gDirection: " + exit.Direction + "  minX: " + minMax.minX + " minZ: " + minMax.minZ + " maxX: " + minMax.maxX + " maxZ: " + minMax.maxZ + " gDirection: " + gDirection);
            switch (gDirection) {
                case GlobalDirection.North: {
                    if (exit.X != (minMax.maxX + 1)) {
                        return new SegmentExit(minMax.maxX + 1, exit.Z, exit.Direction);
                    }
                    break;
                }
                case GlobalDirection.East: {
                    if (exit.Z != (minMax.maxZ + 1)) {
                        return new SegmentExit(exit.X, minMax.maxZ + 1, exit.Direction);
                    }
                    break;
                }
                case GlobalDirection.South: {
                    if (exit.X != (minMax.minX - 1)) {
                        return new SegmentExit(minMax.minX - 1, exit.Z, exit.Direction);
                    }
                    break;
                }
                case GlobalDirection.West: {
                    if (exit.Z != (minMax.minZ - 1)) {
                        return new SegmentExit(exit.X, minMax.minZ - 1, exit.Direction);
                    }
                    break;
                }
            }
            Debug.Log("ShiftExitToSide Returning original exit: (" + exit.X + ", " + exit.Z + ")");
            return exit;
        }

        protected void AddRemoveExit(SegmentExit addRemoveExit, GlobalDirection gDirection) {
            var shiftedExit = ShiftExitToSide(addRemoveExit, gDirection);
            Debug.Log("AddRemoveExit inc exit:  (" + addRemoveExit.X + ", " + addRemoveExit.Z + ") shifted to (" + shiftedExit.X + ", " + shiftedExit.Z + ")");
            var exitIndex = _exits.FindIndex(exit => exit.X == shiftedExit.X && exit.Z == shiftedExit.Z);
            if (exitIndex >= 0) {
                Debug.Log("Room.AddRemoveExit exit exists - removing at (" + shiftedExit.X + ", " + shiftedExit.Z + ")" );
                _exits.RemoveAt(exitIndex);
            } else {
                var exitsCoordStr = String.Join(", ", _exits.Select(x => "("+ x.X +","+ x.Z +")")); 
                Debug.Log("Room.AddRemoveExit exit not found, adding to _exits - adding at (" + shiftedExit.X + ", " + shiftedExit.Z + ") \n existing _exits: " + exitsCoordStr);
                _exits.Add(shiftedExit);
            }
        }
    }

    public class Room3x3Segment : Room {
        public Room3x3Segment(int x, int z, GlobalDirection gDirection, int forks, Segment parent) : base(SegmentType.Room3x3, x, z, gDirection, parent) {
            _exits = Get3x3Exits(x, z, gDirection, forks);
            _tiles = DirectionConversion.GetGlobalCoordinatesFromLocal(GetBoxCoordinates(GetBoxList(new []{(0, -1, 2, 1)}), x, z, gDirection), X, Z, gDirection);
            _space = GetBoxCoordinates(GetBoxList(new []{(-1, -2, 3, 2)}), x, z, gDirection, true);
        }

        public Room3x3Segment(Room3x3Segment oldRoom, SegmentExit addRemoveExit, GlobalDirection gDirection) : base(oldRoom.Type, oldRoom.X, oldRoom.Z, oldRoom.GlobalDirection, oldRoom.Parent) {
            _exits = oldRoom.Exits;
            _tiles = oldRoom.GetTiles();
            AddRemoveExit(addRemoveExit, gDirection);
            _space = oldRoom.NeededSpace();
        }

        private List<SegmentExit> Get3x3Exits(int x, int z, GlobalDirection gDirection, int forks) {
            var potentialLocalExits = new List<(int, int, LocalDirection)>(){
                (1, -2, LocalDirection.Left),
                (1, 2, LocalDirection.Right),
                (3, 0, LocalDirection.Straight)
            };
            var percentageOfExits = new List<(int, int)>(){
                (0, 40),
                (1, 30),
                (2, 20),
                (3, 10)
            };
            if (forks == 1) {
                percentageOfExits[0] = (0, 0);
            }
            return GetExits(x, z , gDirection, potentialLocalExits, percentageOfExits);
            
        }
    }
    public class StartSegment : Room {
        public StartSegment(int x, int z, GlobalDirection gDirection) : base(SegmentType.Start, x, z, gDirection, null) {
            _exits = new List<SegmentExit>();
            _exits.Add(new SegmentExit(x, z, gDirection, 2, 0, LocalDirection.Straight));
            _tiles = DirectionConversion.GetGlobalCoordinatesFromLocal(GetBoxCoordinates(GetBoxList(new []{(0, -1, 2, 1)}), x, z, gDirection), X, Z, gDirection);
            _space = new List<(int, int)>();
        }
        /*
        public StartSegment(StartSegment oldRoom, SegmentExit addRemoveExit) : base(oldRoom.Type, oldRoom.X, oldRoom.Z, oldRoom.GlobalDirection, oldRoom.Parent) {
            _exits = oldRoom.Exits;
            AddRemoveExit(addRemoveExit);
            _tiles = oldRoom.GetTiles();
            _space = oldRoom.NeededSpace();
        }
        */

        override public List<(int, int, GlobalDirection, float, GameObject)> GetGSegments(EnvironmentMgr environmentMgr) {
            var gSegments = new List<(int, int, GlobalDirection, float, GameObject)>();
            var rotation = 0.0f;
            switch (_gDirection) {
                case GlobalDirection.North: {
                    rotation = 270.0f;
                    break;
                } 
                case GlobalDirection.East: {
                    rotation = 180.0f;
                    break;
                }
                case GlobalDirection.South: {
                    rotation = 90.0f;
                    break;
                }
                case GlobalDirection.West: {
                    rotation = 0.0f;
                    break;
                }
            }
            gSegments.Add((_entryX, _entryZ, _gDirection, rotation, environmentMgr.start));
            return gSegments;
        }
    }

    public class Room3x4Segment : Room {
        public Room3x4Segment(int x, int z, GlobalDirection gDirection, int forks, Segment parent) : base(SegmentType.Room3x4, x, z, gDirection, parent) {
            _exits = Get3x4Exits(x, z, gDirection, forks);
            _tiles = DirectionConversion.GetGlobalCoordinatesFromLocal(GetBoxCoordinates(GetBoxList(new []{(0, -1, 3, 1)}), x, z, gDirection), X, Z, gDirection);
            _space = GetBoxCoordinates(GetBoxList(new []{(-1, -2, 4, 2)}), x, z, gDirection, true);
        }

        public Room3x4Segment(Room3x4Segment oldRoom, SegmentExit addRemoveExit, GlobalDirection gDirection) : base(oldRoom.Type, oldRoom.X, oldRoom.Z, oldRoom.GlobalDirection, oldRoom.Parent) {
            _exits = oldRoom.Exits;
            _tiles = oldRoom.GetTiles();
            AddRemoveExit(addRemoveExit, gDirection);
            _space = oldRoom.NeededSpace();
        }

        private List<SegmentExit> Get3x4Exits(int x, int z, GlobalDirection gDirection, int forks) {
            var potentialLocalExits = new List<(int, int, LocalDirection)>(){
                (1, -2, LocalDirection.Left),
                (2, 2, LocalDirection.Right),
                (4, 0, LocalDirection.Straight)
            };
            var percentageOfExits = new List<(int, int)>(){
                (0, 40),
                (1, 30),
                (2, 20),
                (3, 10)
            };
            if (forks == 1) {
                percentageOfExits[0] = (0, 0);
            }
            return GetExits(x, z , gDirection, potentialLocalExits, percentageOfExits);
        }
    }

    public class RoomVariableSegment : Room {
        int _xLength;
        int _zLength;
        int _entry;

        public int XLength { get => _xLength; }
        public int ZLength { get => _zLength; }
        public int Entry { get => _entry; }

        public RoomVariableSegment(int x, int z, GlobalDirection gDirection, int xLength, int zLength, int forks, Segment parent, bool isReal, SegmentType segmentType) : base(segmentType, x, z, gDirection, parent) {
            _xLength = xLength;
            _zLength = zLength;
            _type = segmentType;
            (_exits, _entry) = GetVariableLengthExits(x, z, gDirection, _xLength, _zLength, forks);
            _tiles = DirectionConversion.GetGlobalCoordinatesFromLocal(GetBoxCoordinates(GetBoxList(new []{(0, -_entry, _xLength - 1, (_zLength - 1) - _entry)}), x, z, gDirection), X, Z, gDirection);
            _space = GetBoxCoordinates(GetBoxList(new []{(-1, (-_entry - 1), _xLength, _zLength - _entry)}), x, z, gDirection, true);
        }

        public RoomVariableSegment(RoomVariableSegment oldRoom, SegmentExit addRemoveExit, GlobalDirection gDirection) : base(oldRoom.Type, oldRoom.X, oldRoom.Z, oldRoom.GlobalDirection, oldRoom.Parent)
        {
            _xLength = oldRoom.XLength;
            _zLength = oldRoom.ZLength;
            _entry = oldRoom.Entry;
            _exits = oldRoom.Exits;
            _tiles = oldRoom.GetTiles();
            AddRemoveExit(addRemoveExit, gDirection);
            _space = oldRoom.NeededSpace();
        }

        private string GetExitCoord(List<SegmentExit> segmentExits) {
            var result = "";
            foreach (SegmentExit exit in segmentExits) {
                result += "  {" + exit.X + ", " + exit.Z + "} - " + exit.Direction;
            }
            return result;
        }

        private (List<SegmentExit>, int entry) GetVariableLengthExits(int x, int z, GlobalDirection gDirection, int xLength, int zLength, int forks) {
            var potentialXExits = GetPotentialExitsByLength(xLength);
            var potentialZExits = GetPotentialExitsByLength(zLength);
            var potentialLocalExits = new List<(int, int, LocalDirection)>();
            var entry = randomGenerator.Generate(potentialZExits.Count);
          
            foreach (int xExit in potentialXExits) {
                potentialLocalExits.Add((xExit, -entry - 1, LocalDirection.Left));
                potentialLocalExits.Add((xExit, zLength - entry, LocalDirection.Right));
            }
            foreach (int zExit in potentialZExits) {
                if (zExit != entry) potentialLocalExits.Add((-1, zExit - entry, LocalDirection.Back));
                potentialLocalExits.Add((xLength, zExit - entry, LocalDirection.Straight));
            }
            var percentageOfExitsBase = getPercentageOfExitsBase(forks); new List<int>() {40,30,20,12,7,4,3,2,1};
            var percentageOfExits = new List<(int, int)>();
            for (int i = 0; i < (potentialXExits.Count + potentialZExits.Count); i++) {
                var percent = i < percentageOfExitsBase.Count ? percentageOfExitsBase[i] : 1;
                percentageOfExits.Add((i, percent));
            }
            return (GetExits(X, Z, gDirection, potentialLocalExits, percentageOfExits), entry);
        }
        private List<int> getPercentageOfExitsBase(int forks) {
            if (forks == 2) {
                return new List<int>() {10,20,20,12,7,4,3,2,1};
            }
            if (forks == 1) {
                return new List<int>() {0,10,20,12,7,4,3,2,1};
            }
            return new List<int>() {30,30,20,12,7,4,3,2,1};
        }

        private List<int> GetPotentialExitsByLength(int length) {
            var result = new List<int>();
            if (length % 2 == 0) {
                for (int i = 0; i < length; i++) {
                    if (i % 2 != 0) {
                        result.Add(i);
                    }
                }
            } else {
                for (int i = 0; i < length; i++) {
                    if (i % 2 == 0) {
                        result.Add(i);
                    }
                }
            }
            return result;
        }
    }
}
