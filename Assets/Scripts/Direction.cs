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
        public static List<(int, int)> GetGlobalCoordinatesFromLocal(List<(int, int)> localCoordinates, int startX, int startZ, GlobalDirection gDirection) {
            var globalCoordinates = new List<(int, int)>();
            switch(gDirection) {
                case GlobalDirection.North: {
                    foreach((int x, int z) in localCoordinates) {
                        globalCoordinates.Add((startX + x, startZ + z));
                    }
                    break;
                }
                case GlobalDirection.East: {
                    foreach((int x, int z) in localCoordinates) {
                        globalCoordinates.Add((startX - z, startZ + x));
                    }
                    break;
                }
                case GlobalDirection.South: {
                    foreach((int x, int z) in localCoordinates) {
                        globalCoordinates.Add((startX - x, startZ - z));
                    }
                    break;
                }
                case GlobalDirection.West: {
                    foreach((int x, int z) in localCoordinates) {
                        globalCoordinates.Add((startX + z, startZ - x));
                    }
                    break;
                }
            }
            return globalCoordinates;
        }
    }
}
