using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace level {
    public class LevelMap {
        private int defaultMapSize = 100;
        private int[,] map;
        public LevelMap() {
            map = new int[defaultMapSize, defaultMapSize];
        }

        public int GetValueAtCoordinate((int, int) coordinate) {
            try {
                int mapX = coordinate.Item1 + defaultMapSize / 2;
                int mapY = coordinate.Item2 + defaultMapSize /2;
                return map[mapX, mapY];
            } catch (IndexOutOfRangeException) {
                return 9;
            }
        }

        public void AddCooridnates(List<(int, int)> coordinates, int content) {
            foreach ((int, int) coordinate in coordinates) {
                int mapX = coordinate.Item1 + defaultMapSize / 2;
                int mapY = coordinate.Item2 + defaultMapSize /2;
                map[mapX, mapY] = content;
            }
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

    }

}
