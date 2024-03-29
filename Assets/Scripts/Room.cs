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
        protected List<(int, int, int)> tiles;
        protected List<(int, int, int)> space;
        protected int yLength;
        protected Room(SegmentType type, int entryX, int entryZ, int entryY, GlobalDirection gDirection, int yLength, Segment parent) : base(type, entryX, entryZ, entryY, gDirection, parent) {
            this.yLength = yLength;
            randomGenerator = new DefaultRandom();
        }
        protected Room(SegmentType type, int entryX, int entryZ, int entryY, GlobalDirection gDirection, int yLength, Segment parent, RandomGenerator _randomGenerator) : base(type, entryX, entryZ, entryY, gDirection, parent) {
            this.yLength = yLength;
            randomGenerator = _randomGenerator;
        }

        protected List<(int, int, int)> GetBoxCoordinates(List<(int, int, int, int, int, int)> boxList, Boolean removeCoordBeforeEntry = false) {
            var coordinateList = new List<(int, int, int)>();
            foreach(var (xMin, zMin, xMax, zMax, yMin, yMax) in boxList) {
                for (int y = yMin; y <= yMax; y++) {
                    for (int x = xMin; x <= xMax; x++) {
                        for (int z = zMin; z <= zMax; z++) {
                            coordinateList.Add((x, z, y));
                        }
                    }
                }
            }
            
            if (removeCoordBeforeEntry) {
                int ixOfEntry = -1;
                int i = 0;
                foreach (var coord in coordinateList) {
                    if (coord.Item1 == -1 && coord.Item2 == 0 && coord.Item3 == 0) {
                        ixOfEntry = i;
                    }
                    i++;
                }
                coordinateList.RemoveAt(ixOfEntry);
            }
            
            return coordinateList;
        }

        protected List<(int, int, int, int, int, int)> GetBoxList((int, int, int, int, int, int)[] boxCoord) {
            var boxList = new List<(int, int, int, int, int, int)>();
            foreach(var coord in boxCoord) {
                boxList.Add(coord);
            }
            return boxList;
        }

        protected List<SegmentExit> GetExits(int x, int z, int y, GlobalDirection gDirection, List<(int, int, int, LocalDirection)> potentialLocalExits, List<(int, int)> percentageOfExits) {
            var noOfExits = GetNumberOfExits(percentageOfExits);

            RemoveSurplusExits(potentialLocalExits, noOfExits);

            var exits = new List<SegmentExit>();
            foreach ((int, int, int, LocalDirection) potentialExit in potentialLocalExits) {
                exits.Add(new SegmentExit(x, z, y, gDirection, potentialExit.Item1, potentialExit.Item2, potentialExit.Item3, potentialExit.Item4));
            }
            return exits;
        }

        protected void RemoveSurplusExits(List<(int, int, int, LocalDirection)> potentialLocalExits, int noOfExits)
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

        public override List<(int, int, int)> GetTiles() {
            return tiles;
        }

        public override List<(int, int, int)> NeededSpace() {
            return space;
        }

        protected (int, int, int, int, int, int) getMinMax() {
            int minX = 999;
            int minZ = 999;
            int maxX = -999;
            int maxZ = -999;
            int minY = 999;
            int maxY = -999;

            foreach((int, int, int) coord in tiles) {
                if (coord.Item1 < minX) minX = coord.Item1;
                if (coord.Item1 > maxX) maxX = coord.Item1;
                if (coord.Item2 < minZ) minZ = coord.Item2;
                if (coord.Item2 > maxZ) maxZ = coord.Item2;
                if (coord.Item3 > maxY) maxY = coord.Item3;
                if (coord.Item3 < minY) minY = coord.Item3;
            }
            return (minX, maxX, minZ, maxZ, minY, maxY);
        }

        protected bool hasExit(int x, int z, int y) {
            return exits.Exists(exit => exit.X == x && exit.Z == z && exit.Y == y);
        }

        override public List<(int, int, int, GlobalDirection, float, GameObject)> GetGSegments(EnvironmentMgr environmentMgr) {
            (float, GameObject) SingleFloor((int, int, int, int, int, int) minMax, ref int x, ref int z, ref int y) {
                Debug.Log("SingleFloor (" + x + ", " + y + ", " + z+ ")");
                bool isEntry = entryX == x && entryZ == z && entryY == y;
                float rotation = 0f;
                GameObject gSegment;
                var (minX, maxX, minZ, maxZ, minY, maxY) = minMax;
                if (x == minX && z == minZ) { //SouthWest corner
                    if (hasExit(x - 1, z, y) && hasExit(x, z - 1, y)) {
                        gSegment = environmentMgr.floorCelingCornerLeftRightExit;
                    }
                    else if (hasExit(x - 1, z, y)) {
                        gSegment = environmentMgr.floorCelingCornerRightExit;
                    }
                    else if (hasExit(x, z - 1, y)) {
                        gSegment = environmentMgr.floorCelingCornerLeftExit;
                    }
                    else {
                        gSegment = environmentMgr.floorCelingCorner;
                    }

                    rotation = 90.0f;
                }
                else if (x == minX && z == maxZ) { //SouthEast corner
                    if (hasExit(x - 1, z, y) && hasExit(x, z + 1, y)) {
                        gSegment = environmentMgr.floorCelingCornerLeftRightExit;
                    }
                    else if (hasExit(x - 1, z, y)) {
                        gSegment = environmentMgr.floorCelingCornerLeftExit;
                    }
                    else if (hasExit(x, z + 1, y)) {
                        gSegment = environmentMgr.floorCelingCornerRightExit;
                    }
                    else {
                        gSegment = environmentMgr.floorCelingCorner;
                    }

                    rotation = 180.0f;
                }
                else if (x == maxX && z == maxZ) { //NorthEast corner
                    if (hasExit(x + 1, z, y) && hasExit(x, z + 1, y)) {
                        gSegment = environmentMgr.floorCelingCornerLeftRightExit;
                    }
                    else if (hasExit(x + 1, z, y)) {
                        gSegment = environmentMgr.floorCelingCornerRightExit;
                    }
                    else if (hasExit(x, z + 1, y)) {
                        gSegment = environmentMgr.floorCelingCornerLeftExit;
                    }
                    else {
                        gSegment = environmentMgr.floorCelingCorner;
                    }

                    rotation = 270.0f;
                }
                else if (x == maxX && z == minZ) { //NorthWest corner
                    if (hasExit(x + 1, z, y) && hasExit(x, z - 1, y)) {
                        gSegment = environmentMgr.floorCelingCornerLeftRightExit;
                    }
                    else if (hasExit(x + 1, z, y)) {
                        gSegment = environmentMgr.floorCelingCornerLeftExit;
                    }
                    else if (hasExit(x, z - 1, y)) {
                        gSegment = environmentMgr.floorCelingCornerRightExit;
                    }
                    else {
                        gSegment = environmentMgr.floorCelingCorner;
                    }

                    rotation = 0.0f;
                }
                else if (x == maxX) { //North wall
                    if (hasExit(x + 1, z, y) || isEntry) {
                        gSegment = environmentMgr.floorCelingExit;
                    }
                    else {
                        gSegment = environmentMgr.floorCelingWall;
                    }

                    rotation = 270.0f;
                }
                else if (x == minX) { //South wall
                    if (hasExit(x - 1, z, y) || isEntry) {
                        gSegment = environmentMgr.floorCelingExit;
                    }
                    else {
                        gSegment = environmentMgr.floorCelingWall;
                    }

                    rotation = 90.0f;
                }
                else if (z == maxZ) { //East wall
                    if (hasExit(x, z + 1, y) || isEntry) {
                        gSegment = environmentMgr.floorCelingExit;
                    }
                    else {
                        gSegment = environmentMgr.floorCelingWall;
                    }

                    rotation = 180.0f;
                }
                else if (z == minZ) { //West wall
                    if (hasExit(x, z - 1, y) || isEntry) {
                        gSegment = environmentMgr.floorCelingExit;
                    }
                    else {
                        gSegment = environmentMgr.floorCelingWall;
                    }

                    rotation = 0.0f;
                }
                else {
                    gSegment = environmentMgr.floorCeling;
                }

                return (rotation, gSegment);
            }
            (float, GameObject) BottomFloor((int, int, int, int, int, int) minMax, ref int x, ref int z, ref int y) {
                Debug.Log("MiddleFloow (" + x + ", " + y + ", " + z+ ")");
                bool isEntry = entryX == x && entryZ == z && entryY == y;
                float rotation = 0f;
                GameObject gSegment;
                var (minX, maxX, minZ, maxZ, minY, maxY) = minMax;
                if (x == minX && z == minZ) { //SouthWest corner
                    if (hasExit(x - 1, z, y) && hasExit(x, z - 1, y)) {
                        gSegment = environmentMgr.floorCornerLeftRightExit;
                    }
                    else if (hasExit(x - 1, z, y)) {
                        gSegment = environmentMgr.floorCornerRightExit;
                    }
                    else if (hasExit(x, z - 1, y)) {
                        gSegment = environmentMgr.floorCornerLeftExit;
                    }
                    else {
                        gSegment = environmentMgr.floorCorner;
                    }

                    rotation = 90.0f;
                }
                else if (x == minX && z == maxZ) { //SouthEast corner
                    if (hasExit(x - 1, z, y) && hasExit(x, z + 1, y)) {
                        gSegment = environmentMgr.floorCornerLeftRightExit;
                    }
                    else if (hasExit(x - 1, z, y)) {
                        gSegment = environmentMgr.floorCornerLeftExit;
                    }
                    else if (hasExit(x, z + 1, y)) {
                        gSegment = environmentMgr.floorCornerRightExit;
                    }
                    else {
                        gSegment = environmentMgr.floorCorner;
                    }

                    rotation = 180.0f;
                }
                else if (x == maxX && z == maxZ) { //NorthEast corner
                    if (hasExit(x + 1, z, y) && hasExit(x, z + 1, y)) {
                        gSegment = environmentMgr.floorCornerLeftRightExit;
                    }
                    else if (hasExit(x + 1, z, y)) {
                        gSegment = environmentMgr.floorCornerRightExit;
                    }
                    else if (hasExit(x, z + 1, y)) {
                        gSegment = environmentMgr.floorCornerLeftExit;
                    }
                    else {
                        gSegment = environmentMgr.floorCorner;
                    }

                    rotation = 270.0f;
                }
                else if (x == maxX && z == minZ) { //NorthWest corner
                    if (hasExit(x + 1, z, y) && hasExit(x, z - 1, y)) {
                        gSegment = environmentMgr.floorCornerLeftRightExit;
                    }
                    else if (hasExit(x + 1, z, y)) {
                        gSegment = environmentMgr.floorCornerLeftExit;
                    }
                    else if (hasExit(x, z - 1, y)) {
                        gSegment = environmentMgr.floorCornerRightExit;
                    }
                    else {
                        gSegment = environmentMgr.floorCorner;
                    }

                    rotation = 0.0f;
                }
                else if (x == maxX) { //North wall
                    if (hasExit(x + 1, z, y) || isEntry) {
                        gSegment = environmentMgr.floorExit;
                    }
                    else {
                        gSegment = environmentMgr.floorWall;
                    }

                    rotation = 270.0f;
                }
                else if (x == minX) { //South wall
                    if (hasExit(x - 1, z, y) || isEntry) {
                        gSegment = environmentMgr.floorExit;
                    }
                    else {
                        gSegment = environmentMgr.floorWall;
                    }

                    rotation = 90.0f;
                }
                else if (z == maxZ) { //East wall
                    if (hasExit(x, z + 1, y) || isEntry) {
                        gSegment = environmentMgr.floorExit;
                    }
                    else {
                        gSegment = environmentMgr.floorWall;
                    }

                    rotation = 180.0f;
                }
                else if (z == minZ) { //West wall
                    if (hasExit(x, z - 1, y) || isEntry) {
                        gSegment = environmentMgr.floorExit;
                    }
                    else {
                        gSegment = environmentMgr.floorWall;
                    }

                    rotation = 0.0f;
                }
                else {
                    gSegment = environmentMgr.floor;
                }

                return (rotation, gSegment);
            }
            (float, GameObject) TopFloor((int, int, int, int, int, int) minMax, ref int x, ref int z, ref int y) {
                Debug.Log("TopFloor (" + x + ", " + y + ", " + z+ ")");
                bool isEntry = entryX == x && entryZ == z && entryY == y;
                float rotation = 0f;
                GameObject gSegment;
                var (minX, maxX, minZ, maxZ, minY, maxY) = minMax;
                if (x == minX && z == minZ) { //SouthWest corner
                    if (hasExit(x - 1, z, y) && hasExit(x, z - 1, y)) {
                        gSegment = environmentMgr.celingCornerLeftRightExit;
                    }
                    else if (hasExit(x - 1, z, y)) {
                        gSegment = environmentMgr.celingCornerRightExit;
                    }
                    else if (hasExit(x, z - 1, y)) {
                        gSegment = environmentMgr.celingCornerLeftExit;
                    }
                    else {
                        gSegment = environmentMgr.celingCorner;
                    }

                    rotation = 90.0f;
                }
                else if (x == minX && z == maxZ) { //SouthEast corner
                    if (hasExit(x - 1, z, y) && hasExit(x, z + 1, y)) {
                        gSegment = environmentMgr.celingCornerLeftRightExit;
                    }
                    else if (hasExit(x - 1, z, y)) {
                        gSegment = environmentMgr.celingCornerLeftExit;
                    }
                    else if (hasExit(x, z + 1, y)) {
                        gSegment = environmentMgr.celingCornerRightExit;
                    }
                    else {
                        gSegment = environmentMgr.celingCorner;
                    }

                    rotation = 180.0f;
                }
                else if (x == maxX && z == maxZ) { //NorthEast corner
                    if (hasExit(x + 1, z, y) && hasExit(x, z + 1, y)) {
                        gSegment = environmentMgr.celingCornerLeftRightExit;
                    }
                    else if (hasExit(x + 1, z, y)) {
                        gSegment = environmentMgr.celingCornerRightExit;
                    }
                    else if (hasExit(x, z + 1, y)) {
                        gSegment = environmentMgr.celingCornerLeftExit;
                    }
                    else {
                        gSegment = environmentMgr.celingCorner;
                    }

                    rotation = 270.0f;
                }
                else if (x == maxX && z == minZ) { //NorthWest corner
                    if (hasExit(x + 1, z, y) && hasExit(x, z - 1, y)) {
                        gSegment = environmentMgr.celingCornerLeftRightExit;
                    }
                    else if (hasExit(x + 1, z, y)) {
                        gSegment = environmentMgr.celingCornerLeftExit;
                    }
                    else if (hasExit(x, z - 1, y)) {
                        gSegment = environmentMgr.celingCornerRightExit;
                    }
                    else {
                        gSegment = environmentMgr.celingCorner;
                    }

                    rotation = 0.0f;
                }
                else if (x == maxX) { //North wall
                    if (hasExit(x + 1, z, y) || isEntry) {
                        gSegment = environmentMgr.celingExit;
                    }
                    else {
                        gSegment = environmentMgr.celingWall;
                    }

                    rotation = 270.0f;
                }
                else if (x == minX) { //South wall
                    if (hasExit(x - 1, z, y) || isEntry) {
                        gSegment = environmentMgr.celingExit;
                    }
                    else {
                        gSegment = environmentMgr.celingWall;
                    }

                    rotation = 90.0f;
                }
                else if (z == maxZ) { //East wall
                    if (hasExit(x, z + 1, y) || isEntry) {
                        gSegment = environmentMgr.celingExit;
                    }
                    else {
                        gSegment = environmentMgr.celingWall;
                    }

                    rotation = 180.0f;
                }
                else if (z == minZ) { //West wall
                    if (hasExit(x, z - 1, y) || isEntry) {
                        gSegment = environmentMgr.celingExit;
                    }
                    else {
                        gSegment = environmentMgr.celingWall;
                    }

                    rotation = 0.0f;
                }
                else {
                    gSegment = environmentMgr.celing;
                }

                return (rotation, gSegment);
            }
            (float, GameObject) MiddleFloor((int, int, int, int, int, int) minMax, ref int x, ref int z, ref int y) {
                Debug.Log("MiddleFloow (" + x + ", " + y + ", " + z+ ")");
                bool isEntry = entryX == x && entryZ == z && entryY == y;
                float rotation = 0f;
                GameObject gSegment;
                var (minX, maxX, minZ, maxZ, minY, maxY) = minMax;
                if (x == minX && z == minZ) { //SouthWest corner
                    if (hasExit(x - 1, z, y) && hasExit(x, z - 1, y)) {
                        gSegment = environmentMgr.cornerLeftRightExit;
                    }
                    else if (hasExit(x - 1, z, y)) {
                        gSegment = environmentMgr.cornerRightExit;
                    }
                    else if (hasExit(x, z - 1, y)) {
                        gSegment = environmentMgr.cornerLeftExit;
                    }
                    else {
                        gSegment = environmentMgr.corner;
                    }

                    rotation = 90.0f;
                }
                else if (x == minX && z == maxZ) { //SouthEast corner
                    if (hasExit(x - 1, z, y) && hasExit(x, z + 1, y)) {
                        gSegment = environmentMgr.cornerLeftRightExit;
                    }
                    else if (hasExit(x - 1, z, y)) {
                        gSegment = environmentMgr.cornerLeftExit;
                    }
                    else if (hasExit(x, z + 1, y)) {
                        gSegment = environmentMgr.cornerRightExit;
                    }
                    else {
                        gSegment = environmentMgr.corner;
                    }

                    rotation = 180.0f;
                }
                else if (x == maxX && z == maxZ) { //NorthEast corner
                    if (hasExit(x + 1, z, y) && hasExit(x, z + 1, y)) {
                        gSegment = environmentMgr.cornerLeftRightExit;
                    }
                    else if (hasExit(x + 1, z, y)) {
                        gSegment = environmentMgr.cornerRightExit;
                    }
                    else if (hasExit(x, z + 1, y)) {
                        gSegment = environmentMgr.cornerLeftExit;
                    }
                    else {
                        gSegment = environmentMgr.corner;
                    }

                    rotation = 270.0f;
                }
                else if (x == maxX && z == minZ) { //NorthWest corner
                    if (hasExit(x + 1, z, y) && hasExit(x, z - 1, y)) {
                        gSegment = environmentMgr.cornerLeftRightExit;
                    }
                    else if (hasExit(x + 1, z, y)) {
                        gSegment = environmentMgr.cornerLeftExit;
                    }
                    else if (hasExit(x, z - 1, y)) {
                        gSegment = environmentMgr.cornerRightExit;
                    }
                    else {
                        gSegment = environmentMgr.corner;
                    }

                    rotation = 0.0f;
                }
                else if (x == maxX) { //North wall
                    if (hasExit(x + 1, z, y) || isEntry) {
                        gSegment = environmentMgr.exit;
                    }
                    else {
                        gSegment = environmentMgr.wall;
                    }

                    rotation = 270.0f;
                }
                else if (x == minX) { //South wall
                    if (hasExit(x - 1, z, y) || isEntry) {
                        gSegment = environmentMgr.exit;
                    }
                    else {
                        gSegment = environmentMgr.wall;
                    }

                    rotation = 90.0f;
                }
                else if (z == maxZ) { //East wall
                    if (hasExit(x, z + 1, y) || isEntry) {
                        gSegment = environmentMgr.exit;
                    }
                    else {
                        gSegment = environmentMgr.wall;
                    }

                    rotation = 180.0f;
                }
                else if (z == minZ) { //West wall
                    if (hasExit(x, z - 1, y) || isEntry) {
                        gSegment = environmentMgr.exit;
                    }
                    else {
                        gSegment = environmentMgr.wall;
                    }

                    rotation = 0.0f;
                }
                else {
                    gSegment = null;
                }

                return (rotation, gSegment);
            }

            var gSegments = new List<(int, int, int, GlobalDirection, float, GameObject)>();
            //var (minX, minZ, maxX, maxZ, minY, maxY) = getMinMax(); // (minX, minZ, maxX, maxZ)
            var minMax = getMinMax(); // (minX, minZ, maxX, maxZ)

            foreach((int, int, int) coord in tiles) {
                (int x, int z, int y) = coord;
                Debug.Log("GetGSegments foreach loop, Type: " + type + "(" + x + ", " + z + ", " + x + ") minMax.Item5: " + minMax.Item5 +
                          " minMax.Item6: " + minMax.Item6);
                float rotation;
                GameObject gSegment;
                if (yLength == 1) {
                    (rotation, gSegment) = SingleFloor(minMax, ref x, ref z, ref y);
                } else if (y == minMax.Item5) {
                    (rotation, gSegment) = BottomFloor(minMax, ref x, ref z, ref y);
                } else if (y == minMax.Item6) {
                    (rotation, gSegment) = TopFloor(minMax, ref x, ref z, ref y);
                } else {
                    (rotation, gSegment) = MiddleFloor(minMax, ref x, ref z, ref y);
                }

                if (gSegment != null) {
                    gSegments.Add((x, z, y, gDirection, rotation, gSegment));
                }
            }
            return gSegments;
        }

        private SegmentExit ShiftExitToSide(SegmentExit exit, GlobalDirection gDirection) {
            (int minX, int maxX, int minZ, int maxZ, int minY, int maxY) minMax = getMinMax();
            Debug.Log("ShiftExitToSide inc exit: (" + exit.X + ", " + exit.Z + ") exit.gDirection: " + exit.Direction + "  minX: " + minMax.minX + " minZ: " + minMax.minZ + " maxX: " + minMax.maxX + " maxZ: " + minMax.maxZ + " gDirection: " + gDirection);
            switch (gDirection) {
                case GlobalDirection.North: {
                    if (exit.X != (minMax.maxX + 1)) {
                        return new SegmentExit(minMax.maxX + 1, exit.Z, exit.Y, exit.Direction);
                    }
                    break;
                }
                case GlobalDirection.East: {
                    if (exit.Z != (minMax.maxZ + 1)) {
                        return new SegmentExit(exit.X, minMax.maxZ + 1, exit.Y, exit.Direction);
                    }
                    break;
                }
                case GlobalDirection.South: {
                    if (exit.X != (minMax.minX - 1)) {
                        return new SegmentExit(minMax.minX - 1, exit.Z, exit.Y, exit.Direction);
                    }
                    break;
                }
                case GlobalDirection.West: {
                    if (exit.Z != (minMax.minZ - 1)) {
                        return new SegmentExit(exit.X, minMax.minZ - 1, exit.Y, exit.Direction);
                    }
                    break;
                }
            }
            Debug.Log("ShiftExitToSide Returning original exit: (" + exit.X + ", " + exit.Z + ", " + exit.Y + ")");
            return exit;
        }

        protected void AddRemoveExit(SegmentExit addRemoveExit, GlobalDirection gDirection, bool add) {
            var shiftedExit = ShiftExitToSide(addRemoveExit, gDirection);
            Debug.Log("AddRemoveExit inc exit:  (" + addRemoveExit.X + ", " + addRemoveExit.Z + ") shifted to (" + shiftedExit.X + ", " + shiftedExit.Z + ")");
            var exitIndex = exits.FindIndex(exit => exit.X == shiftedExit.X && exit.Z == shiftedExit.Z);
            if (add) {
                var exitsCoordStr = String.Join(", ", exits.Select(x => "("+ x.X +","+ x.Z +")")); 
                Debug.Log("Room.AddRemoveExit exit not found, adding to _exits - adding at (" + shiftedExit.X + ", " + shiftedExit.Z + ") \n existing _exits: " + exitsCoordStr);
                exits.Add(shiftedExit);
            } else if (exitIndex >= 0) {
                Debug.Log("Room.AddRemoveExit exit exists - removing at (" + shiftedExit.X + ", " + shiftedExit.Z + ")" );
                exits.RemoveAt(exitIndex);
            }
        }
        public int YLength {
            get => yLength; 
        }
    }

    public class Room3x3Segment : Room {
        public Room3x3Segment(int x, int z, int y, GlobalDirection gDirection, int forks, Segment parent) : base(SegmentType.Room3x3, x, z, y, gDirection, 1, parent) {
            exits = Get3x3Exits(x, z, y, gDirection, forks);
            tiles = DirectionConversion.GetGlobalCoordinatesFromLocal(GetBoxCoordinates(GetBoxList(new []{(0, -1, 2, 1, 0, 0)})), X, Z, Y, gDirection);
            space = GetBoxCoordinates(GetBoxList(new []{(-1, -2, 3, 2, 0, 0)}), true);
        }

        public Room3x3Segment(Room3x3Segment oldRoom, SegmentExit addRemoveExit, GlobalDirection gDirection, bool add) : base(oldRoom.Type, oldRoom.X, oldRoom.Z, oldRoom.Y, oldRoom.GlobalDirection, 1, oldRoom.Parent) {
            exits = oldRoom.Exits;
            tiles = oldRoom.GetTiles();
            AddRemoveExit(addRemoveExit, gDirection, add);
            space = oldRoom.NeededSpace();
        }

        private List<SegmentExit> Get3x3Exits(int x, int z, int y, GlobalDirection gDirection, int forks) {
            var potentialLocalExits = new List<(int, int, int, LocalDirection)>(){
                (1, -2, 0, LocalDirection.Left),
                (1, 2, 0, LocalDirection.Right),
                (3, 0, 0, LocalDirection.Straight)
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
            return GetExits(x, z ,y, gDirection, potentialLocalExits, percentageOfExits);
            
        }
    }
    public class StartSegment : Room {
        public StartSegment(int x, int z, int y, GlobalDirection gDirection) : base(SegmentType.Start, x, z, y, gDirection, 1, null) {
            exits = new List<SegmentExit>();
            exits.Add(new SegmentExit(x, z, y, gDirection, 2, 0, 0, LocalDirection.Straight));
            tiles = DirectionConversion.GetGlobalCoordinatesFromLocal(GetBoxCoordinates(GetBoxList(new []{(0, -1, 2, 1, 0, 0)})), X, Z, Y, gDirection);
            space = new List<(int, int, int)>();
        }
        
        override public List<(int, int, int, GlobalDirection, float, GameObject)> GetGSegments(EnvironmentMgr environmentMgr) {
            var gSegments = new List<(int, int, int, GlobalDirection, float, GameObject)>();
            var rotation = 0.0f;
            switch (gDirection) {
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
            gSegments.Add((entryX, entryZ, entryY, gDirection, rotation, environmentMgr.start));
            return gSegments;
        }
    }

    public class Room3x4Segment : Room {
        public Room3x4Segment(int x, int z, int y, GlobalDirection gDirection, int forks, Segment parent) : base(SegmentType.Room3x4, x, z, y, gDirection, 1, parent) {
            exits = Get3x4Exits(x, z, y, gDirection, forks);
            tiles = DirectionConversion.GetGlobalCoordinatesFromLocal(GetBoxCoordinates(GetBoxList(new []{(0, -1, 3, 1, 0, 0)})), X, Z, Y, gDirection);
            space = GetBoxCoordinates(GetBoxList(new []{(-1, -2, 4, 2, 0, 0)}), true);
        }

        public Room3x4Segment(Room3x4Segment oldRoom, SegmentExit addRemoveExit, GlobalDirection gDirection, bool add) : base(oldRoom.Type, oldRoom.X, oldRoom.Z, oldRoom.Y, oldRoom.GlobalDirection, 1, oldRoom.Parent) {
            exits = oldRoom.Exits;
            tiles = oldRoom.GetTiles();
            AddRemoveExit(addRemoveExit, gDirection, add);
            space = oldRoom.NeededSpace();
        }

        private List<SegmentExit> Get3x4Exits(int x, int z, int y, GlobalDirection gDirection, int forks) {
            var potentialLocalExits = new List<(int, int, int, LocalDirection)>(){
                (1, -2, 0, LocalDirection.Left),
                (2, 2, 0, LocalDirection.Right),
                (4, 0, 0, LocalDirection.Straight)
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
            return GetExits(x, z , y, gDirection, potentialLocalExits, percentageOfExits);
        }
    }

    public class RoomVariableSegment : Room {
        int xLength;
        int zLength;
        int entry;

        public int XLength { get => xLength; }
        public int ZLength { get => zLength; }
        public int Entry { get => entry; }

        public RoomVariableSegment(int x, int z, int y, GlobalDirection gDirection, int xLength, int zLength, int yLength, int forks, Segment parent, bool isReal, SegmentType segmentType) : base(segmentType, x, z, y, gDirection, yLength, parent) {
            this.xLength = xLength;
            this.zLength = zLength;
            this.yLength = yLength;
            type = segmentType;
            (exits, entry) = GetVariableLengthExits(gDirection, this.xLength, this.zLength, this.yLength, forks);
            tiles = DirectionConversion.GetGlobalCoordinatesFromLocal(GetBoxCoordinates(GetBoxList(new[]{(0, -entry, this.xLength - 1, (this.zLength - 1) - entry, 0, this.yLength - 1)})), X, Z, Y, gDirection);
            space = GetBoxCoordinates(GetBoxList(new []{(-1, (-entry - 1), xLength, this.zLength - entry, 0, this.yLength - 1)}), true);
        }

        public RoomVariableSegment(RoomVariableSegment oldRoom, SegmentExit addRemoveExit, GlobalDirection gDirection, bool add) : base(oldRoom.Type, oldRoom.X, oldRoom.Z, oldRoom.Y, oldRoom.GlobalDirection, oldRoom.YLength, oldRoom.Parent)  {
            xLength = oldRoom.XLength;
            zLength = oldRoom.ZLength;
            yLength = oldRoom.YLength;
            entry = oldRoom.Entry;
            exits = oldRoom.Exits;
            tiles = oldRoom.GetTiles();
            AddRemoveExit(addRemoveExit, gDirection, add);
            space = oldRoom.NeededSpace();
        }
        private (List<SegmentExit>, int entry) GetVariableLengthExits(GlobalDirection gDirection, int xLength, int zLength, int yLength, int forks) {
            var potentialXExits = GetPotentialExitsByLength(xLength);
            var potentialZExits = GetPotentialExitsByLength(zLength);
            var potentialLocalExits = new List<(int, int, int, LocalDirection)>();
            var entry = randomGenerator.Generate(potentialZExits.Count);
            var percentageOfExitsBase = GetPercentageOfExitsBase(forks);
            List<SegmentExit> exitList = new List<SegmentExit>();
            
            for (int y = 0; y < yLength; y++) {
                foreach (int xExit in potentialXExits) {
                    potentialLocalExits.Add((xExit, -entry - 1, y, LocalDirection.Left));
                    potentialLocalExits.Add((xExit, zLength - entry, y, LocalDirection.Right));
                }
                foreach (int zExit in potentialZExits) {
                    if (zExit != entry) potentialLocalExits.Add((-1, zExit - entry, y, LocalDirection.Back));
                    potentialLocalExits.Add((xLength, zExit - entry, y, LocalDirection.Straight));
                }
                var percentageOfExits = new List<(int, int)>();
                for (int i = 0; i < (potentialXExits.Count + potentialZExits.Count); i++) {
                    var percent = i < percentageOfExitsBase.Count ? percentageOfExitsBase[i] : 1;
                    percentageOfExits.Add((i, percent));
                }
                exitList.AddRange(GetExits(X, Z, Y, gDirection, potentialLocalExits, percentageOfExits));
            }

            return (exitList, entry);
        }
        private List<int> GetPercentageOfExitsBase(int forks) {
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
