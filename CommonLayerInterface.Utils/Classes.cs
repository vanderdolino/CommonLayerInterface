using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLayerInterface.Utils
{

    public class AsciiCommonLayerInterfaceFile : ICommonLayerInterfaceFile
    {
        public Header Header { get; set; }
        public Dictionary<float, List<PolyLine>> PolyLines { get; set; }
    }

    public class BinaryCommonLayerInterfaceFile : ICommonLayerInterfaceFile
    {
        public Header Header { get; set; }
        public Dictionary<float, List<PolyLine>> PolyLines { get; set; }
    }

    public interface ICommonLayerInterfaceFile
    {
        Header Header { get; set; }
        Dictionary<float, List<PolyLine>> PolyLines { get; set; }
    }

    public class Header
    {
        // Required properties
        public FileType FileType { get; set; }
        public float Units { get; set; }
        public float Version { get; set; }
        // Optional Properties
        public DateOnly? Date { get; set; }
        public short? Layers { get; set; }
        public Dimension Dimension { get; set; }
        public bool Align { get; set; } = false; 
        public IEnumerable<Label> Labels { get; set; }
        public UserData UserData { get; set; }
    }

    public enum FileType { Ascii = 0, Binary = 1 }

    public class Dimension
    {
        public Dimension(Point3D point1, Point3D point2)
        {
            Point1 = point1;
            Point2 = point2;
        }
        public Point3D Point1 { get; set; }
        public Point3D Point2 { get; set; }
        public override string ToString() => $"Point1: ({Point1}), Point2: ({Point2})";
    }

    public class Point2D
    {
        public Point2D(float x, float y)
        {
            X = x;
            Y = y;
        }
        public float X { get; set; }
        public float Y { get; set; }
        public override string ToString() => $"X: {X}, Y: {Y}";
    }

    public class Point3D
    {
        public Point3D(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public override string ToString() => $"X: {X}, Y: {Y}, Z: {Z}";
    }

    public class Label
    {
        public short ID { get; set; }
        public string Text { get; set; }
        public override string ToString() => $"ID: {ID}, Text: {Text}";
    }

    public class PolyLine
    {
        public List<Point2D> Points { get; set; }
        public short Dir { get; set; }
        public short N { get { return (short)(Points?.Count ?? 0); } }
    }

    public class UserData
    {
        public UserData(string uid, int len, object data)
        {
            Uid = uid;
            Len = len;
            Data = data;
        }
        public string Uid { get; set; }
        public int Len { get; set; }
        public object Data { get; set; }
        public override string ToString() => $"Uid: {Uid}, Len: {Len}, Data: {Data}";
    }
}