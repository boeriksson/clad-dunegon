using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Dunegon;
using JoinSegment = Segment.JoinSegment;
using Direction;
using GlobalDirection = Direction.GlobalDirection;
using Debug = UnityEngine.Debug;

public class JoinTests
{
    private Dunegon.Join join = new Dunegon.Join();

    // A Test behaves as an ordinary method
    [Test]
    public void FindPathSimple()
    {
        var js = new JoinSegment(0, 0, GlobalDirection.North, null);
        js.JoinCoord = (3,1);
        (List<Segment.Segment> addSegments, (int exit_x, int exit_z), (int join_x, int join_z)) = join.FindPath(js);
        Debug.Log("addSegments: " + addSegments.Count + " exitCoord: (" + exit_x + ", " + exit_z + ")");
    }

    [Test]
    public void FindPathShort()
    {
        var js = new JoinSegment(2, 0, GlobalDirection.North, null);
        js.JoinCoord = (3,0);
        (List<Segment.Segment> addSegments, (int exit_x, int exit_z), (int join_x, int join_z)) = join.FindPath(js);
        Debug.Log("addSegments: " + addSegments.Count + " exitCoord: (" + exit_x + ", " + exit_z + ")");
    }

}
