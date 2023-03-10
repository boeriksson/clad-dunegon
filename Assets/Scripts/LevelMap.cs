using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace level {
    public class LevelMap {
        private int[,] map;
        private int mapSize;
        public LevelMap(int mapSize) {
            this.mapSize = mapSize;
            map = new int[mapSize, mapSize];
        }

        public int GetValueAtCoordinate((int, int) coordinate) {
            try {
                int mapX = coordinate.Item1 + mapSize / 2;
                int mapY = coordinate.Item2 + mapSize /2;
                return map[mapX, mapY];
            } catch (IndexOutOfRangeException) {
                return 9;
            }
        }

        public void AddCooridnates(List<(int, int)> coordinates, int content) {
            foreach ((int, int) coordinate in coordinates) {
                int mapX = coordinate.Item1 + mapSize / 2;
                int mapY = coordinate.Item2 + mapSize /2;
                map[mapX, mapY] = content;
            }
        }

        private string getCoordStr(List<(int, int)> coordinates) {
            var coordStr = "";
            foreach ((int, int) coord in coordinates) {
                coordStr += "(" + coord.Item1 + ", " + coord.Item2 + ") ,";
            }
            return coordStr;
        }

        public void RemoveCoordinates(List<(int, int)> coordinates) {
            AddCooridnates(coordinates, 0);
        }

        public void ClearContent(int content) {
            var uBound0 = map.GetUpperBound(0);
            var uBound1 = map.GetUpperBound(1);
            for (int i = 0; i < uBound0; i++) {
                for (int j = 0; j < uBound1; j++) {
                    if (map[i, j] == content) {
                        map[i, j] = 0;
                    }
                }
            }
        }

        public int[,] Map {
            get {
                return map;
            }
        }
        public int MapSize {
            get {
                return mapSize;
            }
        }

        public (int, int, int, int) GetMinMaxPopulated() {
            var xBound0 = map.GetUpperBound(0);
            var zBound1 = map.GetUpperBound(1);
            int maxX = 0, maxZ = 0, minX = 0, minZ = 0;
            for (int x = 0; x < xBound0; x++) {
                for (int z = 0; z < zBound1; z++) {
                    if (map[x, z] == 1) {
                        if (x > maxX) maxX = x;
                        if (x < minX || minX == 0) minX = x;
                        if (z > maxZ) maxZ = z;
                        if (z < minZ || minZ == 0) minZ = z;
                    }
                }
            }
            return (maxX - mapSize/2, maxZ - mapSize/2, minX - mapSize/2, minZ - mapSize/2); 
        }
    }

}
