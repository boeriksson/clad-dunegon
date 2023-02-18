using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Dunegon;
using JoinSegment = Segment.JoinSegment;
using Direction;
using LevelMap = level.LevelMap;
using GlobalDirection = Direction.GlobalDirection;
using Debug = UnityEngine.Debug;

/*
public class JoinTests
{
    var join = new Join()
    // A Test behaves as an ordinary method
    [Test]
    public void FindPathSimple()
    {
        var js = new JoinSegment(0, 0, GlobalDirection.North, null);
        js.JoinCoord = (3,1);
        var levelMap = new LevelMap(); 
        (List<Segment.Segment> addSegments, (int exit_x, int exit_z), (int join_x, int join_z)) = join.FindPath();
        Debug.Log("addSegments: " + addSegments.Count + " exitCoord: (" + exit_x + ", " + exit_z + ")");
    }

    [Test]
    public void FindPathShort()
    {
        var js = new JoinSegment(2, 0, GlobalDirection.North, null);
        js.JoinCoord = (3,0);
        var levelMap = new LevelMap();
        (List<Segment.Segment> addSegments, (int exit_x, int exit_z), (int join_x, int join_z)) = join.FindPath();
        Debug.Log("addSegments: " + addSegments.Count + " exitCoord: (" + exit_x + ", " + exit_z + ")");
    }

    [Test]
    public void FindPathWithBlock()
    {
        var js = new JoinSegment(0, 0, GlobalDirection.East, null);
        js.JoinCoord = (1, 3);
        var levelMap = new LevelMap();
        var blockCoordinates = new List<(int, int)>() {(1, 2), (1, 3)};
        levelMap.AddCooridnates(blockCoordinates, 1);
        (List<Segment.Segment> addSegments, (int exit_x, int exit_z), (int join_x, int join_z)) = join.FindPath();
        Debug.Log("addSegments: " + addSegments.Count + " exitCoord: (" + exit_x + ", " + exit_z + ")");
        Assert.True(exit_x == 0);
        Assert.True(exit_z == 3);
    }

    [Test]
    public void FindPathWithBug()
    {
        var js = new JoinSegment(0, 4, GlobalDirection.West, null);
        js.JoinCoord = (1, 3);
        var levelMap = new LevelMap();
        var blockCoordinates = new List<(int, int)>() {(2, 3), (1, 3)};
        levelMap.AddCooridnates(blockCoordinates, 1);
        (List<Segment.Segment> addSegments, (int exit_x, int exit_z), (int join_x, int join_z)) = join.FindPath();
        Debug.Log("addSegments: " + addSegments.Count + " exitCoord: (" + exit_x + ", " + exit_z + ")");
        var addSegment = addSegments[0];
        var exit = addSegment.Exits[0];
        Debug.Log("addSegment: " + addSegment.Type + " coord: (" + addSegment.X + ", " + addSegment.Z + ") gDirection: " + addSegment.GlobalDirection + " exitCoord: (" + exit.X + ", " + exit.Z + ")");
    }

    [Test]
    public void GetPrePathRecursiveTestXBlock() {
        var levelMap = new LevelMap();
        var blockCoordinates = new List<(int, int)>() {(1, 2), (1, 3)};
        levelMap.AddCooridnates(blockCoordinates, 1);

        var prePath = new List<(int, int, GlobalDirection)>();
        var xj = 1;
        var zj = 3;
        var xc = 0;
        var zc = 0;
        var ix = 0;
        GlobalDirection cDirection = GlobalDirection.East;

        join.GetPrePathRecursive(prePath, ref xc, ref zc, ref ix, cDirection, xj, zj);
        Debug.Log("Back in unittest-------------------------------");
        foreach((int xp, int zp, GlobalDirection pDirection) in prePath) {
            Debug.Log("prePath step: (" + xp + ", " + zp + ") direction: " + pDirection);
        }
        Assert.True(xc == 0);
        Assert.True(zc == 3);
        Assert.True(prePath.Count == 3);
    }

    [Test]
    public void GetPrePathRecursiveTestZBlock() {
        var levelMap = new LevelMap();
        var blockCoordinates = new List<(int, int)>() {(0, 2), (1, 3)};
        levelMap.AddCooridnates(blockCoordinates, 1);

        var prePath = new List<(int, int, GlobalDirection)>();
        var xj = 1;
        var zj = 3;
        var xc = 0;
        var zc = 0;
        var ix = 0;
        GlobalDirection cDirection = GlobalDirection.East;

        join.GetPrePathRecursive(prePath, ref xc, ref zc, ref ix, cDirection, xj, zj);
        Debug.Log("Back in unittest-------------------------------");
        foreach((int xp, int zp, GlobalDirection pDirection) in prePath) {
            Debug.Log("prePath step: (" + xp + ", " + zp + ") direction: " + pDirection);
        }
        Assert.True(xc == 1);
        Assert.True(zc == 2);
        Assert.True(prePath.Count == 3);
    }

    [Test]
    public void GetPrePathRecursiveTestNegativeZBlock() {
        var levelMap = new LevelMap();
        var blockCoordinates = new List<(int, int)>() {(-4, -2), (-4, -1)};
        levelMap.AddCooridnates(blockCoordinates, 1);

        var prePath = new List<(int, int, GlobalDirection)>();
        var xj = -4;
        var zj = -3;
        var xc = 0;
        var zc = 0;
        var ix = 0;
        GlobalDirection cDirection = GlobalDirection.East;

        join.GetPrePathRecursive(prePath, ref xc, ref zc, ref ix, cDirection, xj, zj);
        Debug.Log("Back in unittest-------------------------------");
        foreach((int xp, int zp, GlobalDirection pDirection) in prePath) {
            Debug.Log("prePath step: (" + xp + ", " + zp + ") direction: " + pDirection);
        }
        Assert.True(xc == -3);
        Assert.True(zc == -3);
        Assert.True(prePath.Count == 6);
    }

    [Test]
    public void GetPrePathRecursiveTestNegativeBlockLoop() {
        var levelMap = new LevelMap();
        var blockCoordinates = new List<(int, int)>() {(-4, -2), (-3, -2)};
        levelMap.AddCooridnates(blockCoordinates, 1);

        var prePath = new List<(int, int, GlobalDirection)>();
        var xj = -4;
        var zj = -3;
        var xc = 0;
        var zc = 0;
        var ix = 0;
        GlobalDirection cDirection = GlobalDirection.East;

        join.GetPrePathRecursive(prePath, ref xc, ref zc, ref ix, cDirection, xj, zj);
        Debug.Log("Back in unittest-------------------------------");
        foreach((int xp, int zp, GlobalDirection pDirection) in prePath) {
            Debug.Log("prePath step: (" + xp + ", " + zp + ") direction: " + pDirection);
        }
        
        //Goes into a loop alternating between (-1, -3) och (-1, -4) - may need to check if coordinate already exist in path!?
        // Assert.True(xc == -3);
        // Assert.True(zc == -3);
        // Assert.True(prePath.Count == 6);
    }
}
*/
