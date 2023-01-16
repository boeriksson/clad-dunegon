using System;
using System.IO;
using System.Collections.Generic;
using Debug = UnityEngine.Debug;
using SegmentType = Segment.SegmentType;

public class Logger
{

    public Logger(string logFilePath)
    {
        if(!logFilePath.EndsWith(".log"))
            logFilePath += ".log";
        LogFilePath = logFilePath;
        if(!File.Exists(LogFilePath))
            File.Create(LogFilePath).Close();
        WriteLine("New Session Started");
    }

    public string LogFilePath { get; private set; }

    public void WriteLine(object message)
    {
        Debug.Log(message);
        using(StreamWriter writer = new StreamWriter(LogFilePath, true))
            writer.WriteLine(DateTime.Now.ToString() + ": " + message.ToString());
    }

    public String PrintTupleList(List<(int, int)> tupleList) {
        String printStr = "";
        foreach((int, int) tuple in tupleList) {
            printStr += " {" + tuple.Item1 + ", " + tuple.Item2 + "}"; 
        }
        return printStr;
    }

    public String PrintPossibleSegments(List<(SegmentType, int)> tupleList) {
        String printStr = "";
        foreach((SegmentType, int) tuple in tupleList) {
            printStr += " {" + tuple.Item1 + ", " + tuple.Item2 + "}"; 
        }
        return printStr;
    }

}
