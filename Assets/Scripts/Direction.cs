using System;
using System.Collections.Generic;
using Enum = System.Enum;
using ArgumentException = System.ArgumentException;

namespace Direction {
    public enum GlobalDirection {
        North = 0, // X
        East = 1, // Z
        South = 2, //-X
        West = 3 // -Z
    }

    public enum LocalDirection {
        Straight,
        Right, 
        Left, 
        Back
    }

    public static class DirectionConversion {
        public static GlobalDirection GetDirection(GlobalDirection gd, LocalDirection ld) {
            switch (ld) {
                case LocalDirection.Straight: 
                    return gd;
                case LocalDirection.Right: {
                    if (gd == GlobalDirection.West) {
                        return GlobalDirection.North;
                    }
                    int directionValue = ((int)gd) + 1;
                    return (GlobalDirection)Enum.ToObject(typeof(GlobalDirection), directionValue);
                }
                case LocalDirection.Left: {
                    if (gd == GlobalDirection.North) {
                        return GlobalDirection.West;
                    }
                    int directionValue = ((int)gd) - 1;
                    return (GlobalDirection)Enum.ToObject(typeof(GlobalDirection), directionValue);
                }
                case LocalDirection.Back: {
                    if (gd == GlobalDirection.North)
                        return GlobalDirection.South;
                    if (gd == GlobalDirection.East)
                        return GlobalDirection.West;
                    int directionValue = ((int)gd) - 2;
                    return (GlobalDirection)Enum.ToObject(typeof(GlobalDirection), directionValue); 
                }
                default: {
                    throw new ArgumentException("GetDirection - LocalDirection not reqognized!");
                }
            }
        }
        public static (int, int, int) GetGlobalCoordinateFromLocal((int, int, int) localCoordinate, int startX, int startZ, int startY, GlobalDirection gDirection) {
            return GetGlobalCoordinatesFromLocal(new List<(int, int, int)> {(localCoordinate.Item1, localCoordinate.Item2, localCoordinate.Item3)}, startX, startZ, startY, gDirection)[0];
        }
        public static List<(int, int, int)> GetGlobalCoordinatesFromLocal(List<(int, int, int)> localCoordinates, int startX, int startZ, int startY, GlobalDirection gDirection) {
            var globalCoordinates = new List<(int, int, int)>();
            switch(gDirection) {
                case GlobalDirection.North: {
                    foreach((int x, int z, int y) in localCoordinates) {
                        globalCoordinates.Add((startX + x, startZ + z, startY));
                    }
                    break;
                }
                case GlobalDirection.East: {
                    foreach((int x, int z, int y) in localCoordinates) {
                        globalCoordinates.Add((startX - z, startZ + x, startY));
                    }
                    break;
                }
                case GlobalDirection.South: {
                    foreach((int x, int z, int y) in localCoordinates) {
                        globalCoordinates.Add((startX - x, startZ - z, startY));
                    }
                    break;
                }
                case GlobalDirection.West: {
                    foreach((int x, int z, int y) in localCoordinates) {
                        globalCoordinates.Add((startX + z, startZ - x, startY));
                    }
                    break;
                }
            }
            return globalCoordinates;
        }
    }
}
