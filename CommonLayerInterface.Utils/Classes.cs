using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonLayerInterface.Utils;

namespace CommonLayerInterface.Utils
{
    public static class Extensions
    {
        public static float Round(this float input)
        {
            return (float)Math.Round(input, 3);
        }

        public static String ReadLine(this BinaryReader reader)
        {
            var result = new StringBuilder();
            bool foundEndOfLine = false;
            char ch;
            while (!foundEndOfLine)
            {
                try
                {
                    ch = reader.ReadChar();
                }
                catch (EndOfStreamException ex)
                {
                    if (result.Length == 0) return null;
                    else break;
                }
                switch (ch)
                {
                    case '\r':
                        if (reader.PeekChar() == '\n') reader.ReadChar();
                        foundEndOfLine = true;
                        break;
                    case '\n':
                        foundEndOfLine = true;
                        break;
                    default:
                        result.Append(ch);
                        break;
                }
            }
            return result.ToString();
        }

        public static float Unitize(this float number, float units)
        {
            // Command : units are u [mm]
            // Syntax: $$UNITS / u
            // Parameter u : REAL
            // u indicates the units of the coordinates in mm.
            // 
            // so if units = 1, we return the same number
            // if units = 0.001, the numbers are um,
            // so we must multiply by 0.001 to produce fractional mm
            return units * number;
        }

        public static string Format(this float number)
        {
            return $"{number:0.###}";
        }
    }
}

namespace CommonLayerInterface.Classes 
{ 
    public class CommonLayerInterfaceFile 
    {
        public Header Header { get; set; }
        public Geometry Geometry { get; set; }

        public void PrintToConsole()
        {
            PrintToConsole(false);
        }

        public void PrintToConsole(bool verbose)
        {
            if (verbose)
            {
                // required parameters
                Console.WriteLine($"thisType: {this.Header.FileType}");
                Console.WriteLine($"Units: {this.Header.Units}");
                Console.WriteLine($"Version: {this.Header.Version}");
                // optional parameters
                Console.WriteLine($"Date: {this.Header.Date}");
                Console.WriteLine($"Dimension: {this.Header.Dimension}");
                Console.WriteLine($"Layers: {this.Header.Layers}");
                Console.WriteLine($"Align: {this.Header.Align}");
                foreach (var label in this.Header.Labels)
                    Console.WriteLine($"Label: {label}");
                Console.WriteLine($"UserData: {this.Header.UserData}");
            }
            // layers
            foreach (var model in this.Geometry.Models)
            {
                for (int i = 0; i < model.Layers.Count; i++)
                {
                    var layer = model.Layers[i];
                    Console.WriteLine($"Layer index: {layer.Index}, Layer height: {layer.Z.Format()}, Layer part area: {layer.Area.Format()}");
                }
            }
        }
    }

    //public interface ICommonLayerInterfaceFile
    //{
    //    Header Header { get; set; }
    //    Geometry Geometry { get; set; }
    //}

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
        public Model(int iD) => this.ID = iD;
        public int ID { get; set; }
        public List<Layer> Layers { get; set; } = new List<Layer>();
    }

    public class Layer
    {
        private List<PolyLine> polyLines = new();
        private float area = float.NaN;
        private float perimiter = float.NaN;

        public CommandType Command { get; }
        public float Units { get; }
        public float Z { get; }
        public int Index { get; }
        public List<PolyLine> PolyLines
        {
            get
            {
                return polyLines;
            }
        }
        public List<Hatch> Hatches { get; }
        public float Area
        {
            get
            {
                if (float.IsNaN(area))
                    this.area = PolyLines.Sum(p => p.Area);
                return area;
            }
        }
        public float Perimiter
        {
            get
            {
                if (float.IsNaN(perimiter))
                    this.perimiter = PolyLines.Sum(p => p.Perimiter);
                return perimiter;
            }
        }

        public Layer(float z, float units, int index)
        {
            this.Z = ((float)Math.Round(z, 3)).Unitize(units);
            this.Units = units;
            this.Index = index;
        }
    }

    public class PolyLine
    {

        private float area = float.NaN;
        private float perimeter = float.NaN;

        public Direction Direction { get; set; }
        public CommandType Command { get; }
        public float Units { get; set; } = 0.001f;
        public short N { get { return (short)(Points?.Count ?? 0); } }
        public float Area
        {
            get
            {
                if (float.IsNaN(area))
                {
                    short n = N;
                    // The idea of multiplier was to differentiate additive and subtractive areas (holes)
                    // but I found that when using the shoelace formula the points are presented in correct
                    // order to result in the proper sign, so multiplier needn't be anything but 1 when
                    // subtracting the subtractive shapes' areas from the additive shapes'.
                    // I left it in place to nullify a polygon with an unknown direction.
                    float multiplier = 0;
                    if (Direction == Direction.Clockwise) multiplier = 1;
                    else if (Direction == Direction.CounterClockwise) multiplier = 1;
                    this.area = multiplier / 2 * Points.Select((p, i) => Points[i].X * Points[(i + 1) % n].Y - Points[(i + 1) % n].X * Points[i].Y).Sum();
                    this.area = area.Unitize(Units);
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
                    this.perimeter = Points.Select((p, i) => (float)Math.Sqrt(Math.Pow(Points[(i + 1) % n].Y - Points[i].Y, 2) + Math.Pow(Points[(i + 1) % n].X - Points[i].X, 2))).Sum();
                    this.perimeter = perimeter.Unitize(Units);
                }
                return perimeter;
            }
        }
        public List<Point2D> Points { get; }

        public PolyLine(Direction direction, IEnumerable<Point2D> points, CommandType command, float units)
        {
            this.Direction = direction;
            this.Points = points.ToList();
            this.Command = command;
            this.Units = units;
            this.area = float.NaN;
            this.perimeter = float.NaN;
        }
    }

    public class Hatch
    {
        public List<PointHatch> Points { get; }
        public short N { get { return (short)(Points?.Count ?? 0); } }
        public CommandType Command { get; }
        public float Units { get; }
        // todo: learn what a Hatch is :)
        // and create relevant properties such as Area and Perimeter

        public Hatch(IEnumerable<PointHatch> points, CommandType command, float units)
        {
            this.Points = points.ToList();
            this.Command = command;
            this.Units = units;
        }
    }

    public class Dimension
    {
        public Dimension(Point3D point1, Point3D point2)
        {
            this.Point1 = point1;
            this.Point2 = point2;
        }
        public Point3D Point1 { get; set; }
        public Point3D Point2 { get; set; }
        public override string ToString() => $"Point1: ({Point1}), Point2: ({Point2})";
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
            this.Uid = uid;
            this.Len = len;
            this.Data = data;
        }
        public string Uid { get; set; }
        public int Len { get; set; }
        public object Data { get; set; }
        public override string ToString() => $"Uid: {Uid}, Len: {Len}, Data: {Data}";
    }

    public class Point2D
    {
        public Point2D(float xRaw, float yRaw, float units)
        {
            this.X = xRaw.Unitize(units);
            this.Y = yRaw.Unitize(units);
        }
        public Point2D(float x, float y) : this(x, y, 1) { }
        public float X { get; set; }
        public float Y { get; set; }
        public override string ToString() => $"X: {X.Format()}, Y: {Y.Format()}";
    }

    public class Point3D
    {
        public Point3D(float xRaw, float yRaw, float zRaw, float units)
        {
            this.X = xRaw.Unitize(units);
            this.Y = yRaw.Unitize(units);
            this.Z = zRaw.Unitize(units);
        }
        public Point3D(float x, float y, float z) : this(x, y, z, 1) { }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public override string ToString() => $"X: {X.Format()}, Y: {Y.Format()}, Z: {Z.Format()}";
    }

    public class PointHatch
    {
        public PointHatch(float xsRaw, float xeRaw, float ysRaw, float yeRaw, float units)
        {
            this.Xs = xsRaw.Unitize(units);
            this.Xe = xeRaw.Unitize(units);
            this.Ys = ysRaw.Unitize(units);
            this.Ye = yeRaw.Unitize(units);
        }
        public float Xs { get; set; }
        public float Xe { get; set; }
        public float Ys { get; set; }
        public float Ye { get; set; }
        public override string ToString() => $"Xs: {Xs.Format()}, Xe: {Xe.Format()}, Ys: {Ys.Format()}, Ye: {Ye.Format()}";
    }

    public class CliFileFormatException : Exception
    {
        public CliFileFormatException()
            : base() { }
        public CliFileFormatException(string message)
            : base(message) { }
        public CliFileFormatException(string message, Exception innerException)
            : base(message, innerException) { }
    }

    public enum Direction { Clockwise = 0, CounterClockwise = 1, OpenLine = 2 }

    public enum FileType { Ascii = 0, Binary = 1 }

    //public enum LayerType : ushort { Long = 127, Short = 128, Ascii = 0 }

    //public enum PolyLineType : ushort { Long = 130, Short = 129, Ascii = 0 }

    //public enum HatchType : ushort { Short = 131, Long = 132, Ascii = 0 }

    public enum CommandType : ushort
    {
        PolyLineAscii = 1,
        HatchAscii = 2,
        NewLine = 10,
        LayerLong = 127,
        LayerShort = 128,
        PolyLineShort = 129,
        PolyLineLong = 130,
        HatchShort = 131,
        HatchLong = 132
    }
}