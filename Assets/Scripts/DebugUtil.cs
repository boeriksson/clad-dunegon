using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugUtil {
    public static string printTupleList(List<(int, int)> tupleList) {
        var result = "";
        foreach ((int, int) tuple in tupleList) {
            result += ", (" + tuple.Item1 + ", " + tuple.Item2 + ")";
        }
        return result;
    }
}
