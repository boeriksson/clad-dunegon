using System.Collections;
using System.Collections.Generic;
using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Debug = UnityEngine.Debug;

public enum GlobalDirection {
    North = 0, // X
    East = 1, // Z
    South = 2, //-X
    West = 3 // -Z
}

public enum Segment {
    S, L, R, Q
}

public class JoinTests {
    private string[,] matrix;

    [SetUp]
    public void Init() {
        matrix = new string[5, 5];
        for(int i = 0; i < matrix.GetLength(0); i++) {
            for(int j = 0; j < matrix.GetLength(1); j++) {
                matrix[i, j] = "0";
            }
        }
    }

    private void printArray(string[,] array) {
        for (int i = 0; i < array.GetLength(0); i++) {
            var row = "";
            for (int j = 0; j < array.GetLength(1); j++) {
                row += array[i, j] + " ";
            }
            Debug.Log(row);
        }
    }

    [Test]
    public void JoinTestsSimplePasses() {
        FindPath(0, 1, (4, 0), GlobalDirection.South);
        printArray(matrix);
    }

    [Test]
    public void JoinTestsSimplePasses2() {
        FindPath(0, 0, (3, 0), GlobalDirection.South);
        printArray(matrix);
    }

    [Test]
    public void JoinTestsSimplePasses3() {
        FindPath(4, 4, (1, 2), GlobalDirection.South);
        printArray(matrix);
    }

    [Test]
    public void JoinTestsSimplePasses4() {
        FindPath(0, 4, (4, 0), GlobalDirection.South);
        printArray(matrix);
    }

    public (List<(int, int, Segment)>, (int, int)) FindPath(int x1, int z1, (int, int) joinCoord, GlobalDirection gDirection) {
        matrix[x1, z1] = "2";
        (int x2, int z2) = joinCoord;
        matrix[x2, z2] = "3";

        int xc = x1;    //cursor
        int zc = z1;
        GlobalDirection cDirection = gDirection;

        var prePath = new List<(int, int, GlobalDirection)>();

        while (xc != x2 || zc != z2) {
            if (Math.Abs(xc - x2) >= Math.Abs(zc - z2)) {
                if (x1 > x2) {
                    xc--; //South
                    cDirection = GlobalDirection.South;
                } else {
                    xc++; //North
                    cDirection = GlobalDirection.North;
                }
            } else {
                if (z1 > z2) {
                    zc--; //West
                    cDirection = GlobalDirection.West;
                } else {
                    zc++; //East
                    cDirection = GlobalDirection.East;
                }
            }

            if (xc == x2 && zc == z2) {
                break;
            } else {
                prePath.Add((xc, zc, cDirection));
                //matrix[xc, zc] = segment.ToString();
                //Debug.Log("xc: " + xc + " zx: " + zc + " direction: " + cDirection);
            }
        }

        var path = new List<(int, int, Segment)>();
        (int, int) exitCoord = (-999, -999);
        for(int i = 0; i < prePath.Count; i++) {
            var step = prePath[i];
            var segment = Segment.Q;
            if (i < prePath.Count - 1) {
                var nextStep = prePath[i + 1];
                if (step.Item3 == GlobalDirection.North) {
                    if (nextStep.Item3 == GlobalDirection.North) {
                        segment = Segment.S;
                    } else if (nextStep.Item3 == GlobalDirection.East) {
                        segment = Segment.L;
                    } else {
                        segment = Segment.R;
                    }
                } else if (step.Item3 == GlobalDirection.South) {
                    if (nextStep.Item3 == GlobalDirection.South) {
                        segment = Segment.S;
                    } else if (nextStep.Item3 == GlobalDirection.East) {
                        segment = Segment.R;
                    } else {
                        segment = Segment.L;
                    }
                } else if (step.Item3 == GlobalDirection.East) {
                    if (nextStep.Item3 == GlobalDirection.East) {
                        segment = Segment.S;
                    } else if (nextStep.Item3 == GlobalDirection.North) {
                        segment = Segment.R;
                    } else {
                        segment = Segment.L;
                    }
                } else if (step.Item3 == GlobalDirection.West) {
                    if (nextStep.Item3 == GlobalDirection.West) {
                        segment = Segment.S;
                    } else if (nextStep.Item3 == GlobalDirection.North) {
                        segment = Segment.L;
                    } else {
                        segment = Segment.R;
                    }
                } 
            } else { // Last step before join - this is the joinSegment exit?!
                (int sx, int sz, GlobalDirection direction) = step;
                exitCoord = (sx, sz);
                (int jx, int jz) = joinCoord;
                if (direction == GlobalDirection.North) {
                    if (sz == jz) {
                        segment = Segment.S;
                    } else if (sz > jz) {
                        segment = Segment.R;
                    } else {
                        segment = Segment.L;
                    }
                }
                if (direction == GlobalDirection.East) {
                    if (sx == jx) {
                        segment = Segment.S;
                    } else if (sx > jx) {
                        segment = Segment.R;
                    } else {
                        segment = Segment.L;
                    }
                }
                if (direction == GlobalDirection.South) {
                    if (sz == jz) {
                        segment = Segment.S;
                    } else if (sz > jz) {
                        segment = Segment.L;
                    } else {
                        segment = Segment.R;
                    }
                }
                if (direction == GlobalDirection.West) {
                    if (sx == jx) {
                        segment = Segment.S;
                    } else if (sx > jx) {
                        segment = Segment.L;
                    } else {
                        segment = Segment.R;
                    }
                }

            }
            path.Add((step.Item1, step.Item2, segment));
        }

        foreach((int, int, Segment) step in path) {
            matrix[step.Item1, step.Item2] = step.Item3.ToString();
        }
        if (exitCoord.Item1 == -999) {
            throw new Exception("FindPath.exitCord not initialized!");
        }
        return (path, exitCoord);
    }
}
