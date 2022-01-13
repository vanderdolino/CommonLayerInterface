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
        public Geometry Geometry { get; set; }
    }

    public class BinaryCommonLayerInterfaceFile : ICommonLayerInterfaceFile
    {
        public Header Header { get; set; }
        public Geometry Geometry { get; set; }
    }

    public interface ICommonLayerInterfaceFile
    {
        Header Header { get; set; }
        Geometry Geometry { get; set; }
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

    public class Geometry
    {
        public IEnumerable<Model> Models { get; set; }
    }

    public class Model
    {
        public Model(short iD) => ID = iD;

        public short ID { get; set; }
        public List<Layer> Layers { get; set; }
    }

    public class Layer
    {
        private List<PolyLine> polyLines;

        public Layer() { }
        public Layer(short z) => Z = z;
        public short Z { get; set; }
        private float area = float.NaN;
        private float perimiter = float.NaN;
        public List<PolyLine> PolyLines
        {
            get
            {
                return polyLines;
            }
            set
            {
                polyLines = value;
                area = float.NaN;
            }
        }
        public float Area
        {
            get
            {
                if(float.IsNaN(area))
                    area = PolyLines.Sum(p => p.Area);
                return area;
            }
        }

        public float Perimiter
        {
            get
            {
                if (float.IsNaN(perimiter))
                    perimiter = PolyLines.Sum(p => p.Perimiter);
                return perimiter;
            }
        }
    }

    public class PolyLine 
    {
        protected List<Point2D> points;
        public List<Point2D> Points 
        {
            get 
            {
                return Points;
            }
            set
            {
                points = value;
                area = float.NaN;
                perimeter = float.NaN;
            }
        }
        public Direction Direction { get; set; }
        public short N { get { return (short)(points?.Count ?? 0); } }
        private float area = float.NaN;
        private float perimeter = float.NaN;
        public float Area
        { 
            get
            {
                if (float.IsNaN(area))
                {
                    short n = N;
                    float sum = 0;
                    for (int i = 0; i < points.Count; i++)
                        sum += points[i].X * points[(i + 1) % n].Y - points[(i + 1) % n].X * points[i].Y;
                    float multiplier;
                    if (Direction == Direction.Clockwise)
                        multiplier = -1;
                    else if (Direction == Direction.CounterClockwise)
                        multiplier = 1;
                    else
                        multiplier = 0;
                    area = multiplier * sum / 2;
                }
                return area;
            }
        }
        public float Perimiter
        {
            get
            {
                if (float.IsNaN(perimeter))
                {
                    short n = N;
                    float sum = 0;
                    for (int i = 0; i < points.Count; i++)
                    {
                        sum += (float)Math.Sqrt(Math.Pow(points[(i + 1) % n].Y - points[i].Y, 2) + Math.Pow(points[(i + 1) % n].X - points[i].X, 2));
                    }
                    perimeter = sum;
                }
                return perimeter;
            }
        }
    }

    public class Hatch
    {
        protected List<PointHatch> points;
        public virtual List<PointHatch> Points { get; set; }
        public short N { get { return (short)(points?.Count ?? 0); } }
    }

    public enum Direction { Clockwise = 0, CounterClockwise = 1, OpenLine = 2}

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

    public class PointHatch
    {
        public PointHatch(float xs, float xe, float ys, float ye)
        {
            Xs = xs;
            Xe = xe;
            Ys = ys;
            Ye = ye;
        }
        public float Xs { get; set; }
        public float Xe { get; set; }
        public float Ys { get; set; }
        public float Ye { get; set; }
        public override string ToString() => $"Xs: {Xs}, Xe: {Xe}, Ys: {Ys}, Ye: {Ye}";
    }

    public class Label
    {
        public short ID { get; set; }
        public string Text { get; set; }
        public override string ToString() => $"ID: {ID}, Text: {Text}";
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

    public class CliFileFormatException : Exception
    {
        public CliFileFormatException() 
            : base() { }
        public CliFileFormatException(string message) 
            : base(message) { }
        public CliFileFormatException(string message, Exception innerException) 
            : base(message, innerException) {  }
    }

}