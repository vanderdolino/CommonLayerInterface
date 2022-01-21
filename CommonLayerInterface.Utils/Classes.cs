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
        /// <summary>
        /// Rounds the float to the specified number of decimals per the requirement
        /// </summary>
        /// <param name="input">The float to be rounded</param>
        /// <returns></returns>
        public static float Round(this float input)
        {
            return (float)Math.Round(input, 3);
        }

        /// <summary>
        /// Reads a line from a BinaryReader taking into account \r, \n
        /// </summary>
        /// <param name="reader">BinaryReader</param>
        /// <returns></returns>
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
                catch (EndOfStreamException)
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

        /// <summary>
        /// Converts a float to a specified unit.
        /// </summary>
        /// <param name="number">The float to be converted</param>
        /// <param name="units">The number of mm each quantity represents</param>
        /// <returns></returns>
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

        /// <summary>
        /// Formats the float to be displayed by the program. 
        /// Used for display only as the underlying numbers should retain precision
        /// </summary>
        /// <param name="number">The float to be formatted</param>
        /// <returns></returns>
        public static string Format(this float number)
        {
            return $"{number:0.000}";
        }
    }
}

namespace CommonLayerInterface.Classes 
{
    /// <summary>
    /// The model for a Common Layer Interface file 
    /// according to COMMON LAYER INTERFACE 2.0.
    /// See <see cref="https://www.hmilch.net/downloads/cli_format.html"/> 
    /// </summary>
    public class CommonLayerInterfaceFile 
    {
        /// <summary>
        /// Holds the <see cref="CommonLayerInterface.Classes.Header"/> section of the file
        /// </summary>
        public Header Header { get; set; }

        /// <summary>
        /// Holds the <see cref="CommonLayerInterface.Classes.Geometry"/> section of the file
        /// </summary>
        public Geometry Geometry { get; set; }

        /// <summary>
        /// Outputs the file to the console, in a non-verbose way (not including <see cref="CommonLayerInterface.Classes.Header"/> section)
        /// </summary>
        public void PrintToConsole()
        {
            PrintToConsole(false);
        }

        /// <summary>
        /// Outputs the file to the console
        /// </summary>
        /// <param name="verbose">If true, will output the <see cref="CommonLayerInterface.Classes.Header"/> information then <see cref="CommonLayerInterface.Classes.Geometry"/></param>
        public void PrintToConsole(bool verbose)
        {
            if (verbose)
            {
                // required parameters
                Console.WriteLine($"File Type: {this.Header.FileType}");
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

    /// <summary>
    /// Metadata about the file and models
    /// </summary>
    public class Header
    {
        #region Required Properties
        /// <summary>
        /// Indicates the data in the <see cref="Geometry"/> section to be <see cref="FileType.Binary"/> or <see cref="FileType.Ascii"/>
        /// </summary>
        public FileType FileType { get; set; }
        /// <summary>
        /// Indicates the units of the coordinates in mm
        /// </summary>
        public float Units { get; set; }
        /// <summary>
        /// Version of the file
        /// </summary>
        public float Version { get; set; }
        #endregion
        #region Optional Properties
        /// <summary>
        /// File was built on date
        /// </summary>
        public DateOnly? Date { get; set; }
        /// <summary>
        /// Number of <see cref="Layer"/>s inside the file
        /// </summary>
        public short? Layers { get; set; }
        /// <summary>
        /// Describes the dimensions of the outline box 
        /// which completely contains the part in absolute coordinates (in mm) 
        /// with respect to the origin.
        /// </summary>
        public Dimension Dimension { get; set; }
        /// <summary>
        /// Align data in <see cref="Geometry"/> section to 32 bit
        /// </summary>
        public bool Align { get; set; } = false;
        /// <summary>
        /// Holds <see cref="Label"/>s for the parts, one for each model
        /// </summary>
        public IEnumerable<Label> Labels { get; set; }
        /// <summary>
        /// User-specific data
        /// </summary>
        public UserData UserData { get; set; }
        #endregion
    }

    /// <summary>
    /// Start of a layer with upper surface at height z (z*units [mm]). 
    /// All layers must be sorted in ascending order with respect to z. 
    /// The thickness of the layer is given by the difference between the z values of the current and previous layers. 
    /// A thickness for the first (lowest) layer can be specified by including a "zero-layer" with a given z value but with no polyline.
    /// </summary>
    public class Layer
    {
        private float area = float.NaN;
        private float perimiter = float.NaN;
        private readonly List<PolyLine> polyLines = new();
        private readonly List<Hatch> hatches = new();

        /// <summary>
        /// The command that generated this <see cref="Layer"/>. See <see cref="CommandType"/>
        /// </summary>
        public CommandType Command { get; }
        /// <summary>
        /// <see cref="Layer"/> height in mm
        /// </summary>
        public float Z { get; }
        /// <summary>
        /// The index of this <see cref="Layer"/> relative to other <see cref="Layer"/>s in the <see cref="Model"/>
        /// </summary>
        public int Index { get; }
        /// <summary>
        /// Readonly <see cref="IEnumerable{T}"/> of <see cref="PolyLine"/> which is created when containing <see cref="Layer"/> is constructed
        /// </summary>
        public IEnumerable<PolyLine> PolyLines { get { return polyLines; } }
        /// <summary>
        /// Readonly <see cref="IEnumerable{T}"/> of <see cref="Hatch"/> which is created when containing <see cref="Layer"/> is constructed
        /// </summary>
        public IEnumerable<Hatch> Hatches { get { return hatches; } }
        /// <summary>
        /// Readonly property which returns the area of the <see cref="PolyLines"/>. 
        /// Calculation is deferred until this property is accessed.
        /// Area is only recalculated when the a <see cref="PolyLine"/> is added via <see cref="AddPolyLine(PolyLine)"/> or <see cref="ClearPolyLines"/> is called
        /// </summary>
        public float Area
        {
            get
            {
                if (float.IsNaN(area))
                    this.area = PolyLines.Sum(p => p.Area);
                return area;
            }
        }
        /// <summary>
        /// Readonly property which returns the perimiter of the <see cref="PolyLines"/>. 
        /// Calculation is deferred until this property is accessed.
        /// Perimeter is only recalculated when the a <see cref="PolyLine"/> is added via <see cref="AddPolyLine(PolyLine)"/> or <see cref="ClearPolyLines"/> is called
        /// </summary>
        public float Perimiter
        {
            get
            {
                if (float.IsNaN(perimiter))
                    this.perimiter = PolyLines.Sum(p => p.Perimiter);
                return perimiter;
            }
        }
        /// <summary>
        /// Add a <see cref="PolyLine"/> to the collection underlying <see cref="PolyLines"/>.
        /// Invalidates area and perimiter calculations which will be recalculated when next accessed
        /// </summary>
        /// <param name="polyLine"></param>
        public void AddPolyLine(PolyLine polyLine)
        {
            polyLines.Add(polyLine);
            area = float.NaN;
            perimiter = float.NaN;
        }
        /// <summary>
        /// Add a <see cref="Hatch"/> to the collection underlying <see cref="Hatches"/>.
        /// Invalidates hatch calculations which will be recalculated when next accessed
        /// </summary>
        /// <param name="hatch"></param>
        public void AddHatch(Hatch hatch)
        {
            hatches.Add(hatch);
            resetHatchCalculations();
        }
        /// <summary>
        /// Clears the collection underlying <see cref="PolyLines"/>.
        /// Invalidates <see cref="Area"/> and <see cref="Perimiter"/> calculations which will be recalculated when next accessed
        /// </summary>
        public void ClearPolyLines()
        {
            polyLines.Clear();
            resetPolyLineCalculations();
        }
        /// <summary>
        /// Clears the collection underlying <see cref="Hatches"/>.
        /// Invalidates hatch-related calculations which will be recalculated when next accessed
        /// </summary>
        public void ClearHatches()
        {
            hatches.Clear();
            resetHatchCalculations();
        }

        private void resetPolyLineCalculations()
        {
            area = float.NaN;
            perimiter = float.NaN;
        }
        private void resetHatchCalculations()
        {
            // TODO: reset any relevant calculations related to hatches
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Layer"/> class 
        /// </summary>
        /// <param name="z">Height of this <see cref="Layer"/> in the original units. Pass <paramref name="units"/> to let the constructor convert to mm.</param>
        /// <param name="units">Units as specified in <see cref="Header.Units"/> to be used to convert <paramref name="z"/> to mm</param>
        /// <param name="index">The index of this <see cref="Layer"/> relative to other <see cref="Layer"/>s in the <see cref="Model"/></param>
        public Layer(float z, float units, int index, CommandType command)
        {
            this.Z = ((float)Math.Round(z, 3)).Unitize(units);
            this.Index = index;
            this.Command = command;
            resetPolyLineCalculations();
            resetHatchCalculations();
        }
    }

    /// <summary>
    /// The geometrical information of the intersection of a 3D-model with a plane is called a slice. 
    /// The volume between two parallel slices is called a layer. 
    /// The 2 1/2D-representation of a model is the sum total of layer-descriptions. 
    /// The slicing plane is parallel to the xy- plane of a right hand cartesian coordinate system. 
    /// It is assumed that the building direction is the positive z-axis.
    /// </summary>
    public class Geometry
    {
        /// <summary>
        /// <see cref="Model"/>s contained in this <see cref="Geometry"/>
        /// </summary>
        public IEnumerable<Model> Models { get; set; }
    }

    /// <summary>
    /// Holds a collection of layers called a model with an ID specified by each Label in the header
    /// </summary>
    public class Model
    {
        /// <summary>
        /// Corresponds to <see cref="Label.ID"/> in the <see cref="Header"/>, and serves to group <see cref="Layer"/>s into distinct <see cref="Model"/>s
        /// </summary>
        public int ID { get; }
        /// <summary>
        /// Readonly <see cref="List{T}"/> of <see cref="Layer"/> which is created when containing <see cref="Model"/> is constructed
        /// </summary>
        public List<Layer> Layers { get; } = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="Model"/> class 
        /// </summary>
        /// <param name="id">Groups <see cref="Layers"/> into distinct <see cref="Model"/>s</param>
        public Model(int id) => this.ID = id;
    }

    /// <summary>
    /// Holds a collection of <see cref="Point2D"/> defining a flat shape which can either be additive (<see cref="Direction"/> = 
    /// </summary>
    public class PolyLine : Shape<Point2D>
    {
        protected override void calculateArea()
        {
            short n = N;
            // The idea of multiplier was to differentiate additive and subtractive areas (holes)
            // but I found that when using the shoelace formula the points are presented in correct
            // order to result in the proper sign, so multiplier needn't be anything but 1 when
            // subtracting the subtractive shapes' areas from the additive shapes'.
            // I left it in place to nullify a polygon with an unknown or other direction.
            float multiplier = 0;
            if (Direction == Direction.Clockwise) multiplier = 1;
            else if (Direction == Direction.CounterClockwise) multiplier = 1;
            else if (Direction == Direction.OpenLine) multiplier = 0;
            this.area = multiplier / 2 * points.Select((p, i) => points[i].X * points[(i + 1) % n].Y - points[(i + 1) % n].X * points[i].Y).Sum();
            this.area = area.Unitize(Units);            
        }

        protected override void calculatePerimiter()
        {
            short n = N;
            // this calculates perimiter of all shapes, internal and external
            this.perimeter = points.Select((p, i) => (float)Math.Sqrt(Math.Pow(points[(i + 1) % n].Y - points[i].Y, 2) + Math.Pow(points[(i + 1) % n].X - points[i].X, 2))).Sum();
            // alternatively, one could calculate only external shapes' perimiters with the following
            //this.perimeter = (Direction == Direction.CounterClockwise) ? 
            //    points.Select((p, i) => (float)Math.Sqrt(Math.Pow(points[(i + 1) % n].Y - points[i].Y, 2) + Math.Pow(points[(i + 1) % n].X - points[i].X, 2))).Sum() : 
            //    0;
            this.perimeter = perimeter.Unitize(Units);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PolyLine"/> class 
        /// </summary>
        /// <param name="direction">Orientation of the line when viewing in the negative z-direction</param>
        /// <param name="points">A collection of <see cref="Point2D"/> in this <see cref="PolyLine"/></param>
        /// <param name="command">The command that generated this <see cref="PolyLine"/>. See <see cref="CommandType"/></param>
        /// <param name="units">Indicates the units of the coordinates in mm</param>
        public PolyLine(Direction direction, IEnumerable<Point2D> points, CommandType command, float units) : base(points, command, units, direction) { }
    }

    public class Hatch : Shape<PointHatch>
    {
        // todo: learn what a Hatch is :)

        /// <summary>
        /// Readonly property which returns the area of this <see cref="PointHatch"/>. 
        /// Calculation is deferred until this property is accessed and will not happen again since the <para cref="Points"/> can't be modified
        /// </summary>
        protected override void calculateArea() => throw new NotImplementedException();

        /// <summary>
        /// Readonly property which returns the perimiter of this <see cref="PolyLine"/>. 
        /// Calculation is deferred until this property is accessed and will not happen again since the <see cref="Points"/> can't be modified
        /// </summary>
        protected override void calculatePerimiter() => throw new NotImplementedException();

        public Hatch(IEnumerable<PointHatch> points, CommandType command, float units) : base(points, command, units) { }

    }

    public abstract class Shape<T>
    {
        protected List<T> points = new();
        protected float area = float.NaN;
        protected float perimeter = float.NaN;

        /// <summary>
        /// Readonly collection of points
        /// </summary>
        public IEnumerable<T> Points { get { return points; } } 
        /// <summary>
        /// Indicates the number of <see cref="T"/> in <see cref="Points"/>
        /// </summary>
        protected short N { get { return (short)(points?.Count ?? 0); } }
        /// <summary>
        /// Orientation of the line when viewing in the negative z-direction
        /// </summary>
        public Direction Direction { get; }
        /// <summary>
        /// Readonly property which returns the area. 
        /// Calculation is deferred until this property is accessed and will not happen again since the <see cref="Points"/> can't be modified
        /// </summary>
        public float Area
        {
            get
            {
                if (float.IsNaN(area))
                    calculateArea();
                return area;
            }
        }
        /// <summary>
        /// Readonly property which returns the perimiter. 
        /// Calculation is deferred until this property is accessed and will not happen again since the <see cref="Points"/> can't be modified
        /// </summary>
        public float Perimiter
        {
            get
            {
                if (float.IsNaN(perimeter))
                    calculatePerimiter();
                return perimeter;
            }
        }
        /// <summary>
        /// The command that generated this shape. See <see cref="CommandType"/>
        /// </summary>
        public CommandType Command { get; }
        /// <summary>
        /// Indicates the units of the coordinates in mm
        /// </summary>
        public float Units { get; }

        protected abstract void calculateArea();

        protected abstract void calculatePerimiter();
       
        protected Shape(IEnumerable<T> points, CommandType command, float units)
        {
            this.points = points.ToList();
            Command = command;
            Units = units;
            area = float.NaN;
            perimeter = float.NaN;
        }

        protected Shape(IEnumerable<T> points, CommandType command, float units, Direction direction) : this(points, command, units)
        {
            Direction = direction;
        }
    }

    /// <summary>
    /// Describes the dimensions of the outline box which completely contains the part in absolute coordinates (in mm) with respect to the origin.
    /// </summary>
    public class Dimension
    {
        /// <summary>
        /// The lower left coordinate of the outline box when looking at the x,y plane normal to the z axis with z = 0
        /// </summary>
        public Point3D Point1 { get; set; }
        /// <summary>
        /// The upper right coordinate of the outline box when looking at the x,y plane normal to the z axis with z = max
        /// </summary>
        public Point3D Point2 { get; set; }
        public override string ToString() => $"Point1: ({Point1}), Point2: ({Point2})";

        /// <summary>
        /// Initializes a new instance of the <see cref="Dimension"/> class 
        /// </summary>
        /// <param name="point1">The lower left coordinate of the outline box when looking at the x,y plane normal to the z axis with z = 0</param>
        /// <param name="point2">The upper right coordinate of the outline box when looking at the x,y plane normal to the z axis with z = max</param>
        /// <exception cref="ArgumentException"></exception>
        public Dimension(Point3D point1, Point3D point2)
        {
            if (point1.X < point2.X &&
                point1.Y < point2.Y &&
                point1.Z < point2.Z)
            {
                this.Point1 = point1;
                this.Point2 = point2;
            }
            else throw new ArgumentException("The conditions x1 < x2 , y1 < y2 and z1 < z2 must be satisfied.");
        }
    }

    /// <summary>
    /// A label for a part
    /// </summary>
    public class Label
    {
        /// <summary>
        /// Corresponds to <see cref="Model.ID"/> in the <see cref="Header"/>, and serves to group <see cref="Layer"/>s into distinct <see cref="Model"/>s
        /// </summary>
        public short ID { get; set; }
        /// <summary>
        /// An ASCII string that gives some comment on the part.
        /// </summary>
        public string Text { get; set; }
        public override string ToString() => $"ID: {ID}, Text: {Text}";
    }

    /// <summary>
    /// User-specific data to the header
    /// </summary>
    public class UserData
    {
        /// <summary>
        /// user identifier - identifies user and the following user-data. uid and user-data shall be cleared and published by a central coordinator (e.g. task coordinator).
        /// </summary>
        public string Uid { get; set; }
        /// <summary>
        /// Defines the length of user-data in bytes from the byte after the comma after the parameter len to the byte before the following $$command. user-data: field of user-specific data; the length of this field is defined by the parameter len; the contents of this field is defined
        /// </summary>
        public int Len { get; set; }
        /// <summary>
        /// field of data (binary or ASCII); length is <see cref="Len"/> bytes
        /// </summary>
        public object Data { get; set; }
        public override string ToString() => $"Uid: {Uid}, Len: {Len}, Data: {Data}";

        /// <summary>
        /// Initializes a new instance of the <see cref="UserData"/> class 
        /// </summary>
        /// <param name="uid">User identifier</param>
        /// <param name="len">Length of data</param>
        /// <param name="data">User data</param>
        public UserData(string uid, int len, object data)
        {
            this.Uid = uid;
            this.Len = len;
            this.Data = data;
        }
    }

    /// <summary>
    /// A two-dimension point with X and Y to be used in <see cref="PolyLine"/>
    /// </summary>
    public class Point2D
    {
        public float X { get; set; }
        public float Y { get; set; }
        public override string ToString() => $"X: {X.Format()}, Y: {Y.Format()}";

        /// <summary>
        /// Initializes a new instance of the <see cref="Point2D"/> class using raw values, passing <paramref name="units"/> to convert final values to mm
        /// </summary>
        /// <param name="xRaw">Raw value of x</param>
        /// <param name="yRaw">Raw value of y</param>
        /// <param name="units">Units to convert raw values to mm</param>
        public Point2D(float xRaw, float yRaw, float units)
        {
            this.X = xRaw.Unitize(units);
            this.Y = yRaw.Unitize(units);
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="Point2D"/> class using values in mm
        /// </summary>
        /// <param name="x">X value in mm</param>
        /// <param name="y">Y value in mm</param>
        public Point2D(float x, float y) : this(x, y, 1) { }
    }

    /// <summary>
    /// A three-dimension point with X, Y, and Z to be used in <see cref="Dimension"/>
    /// </summary>
    public class Point3D
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public override string ToString() => $"X: {X.Format()}, Y: {Y.Format()}, Z: {Z.Format()}";

        /// <summary>
        /// Initializes a new instance of the <see cref="Point3D"/> class using raw values, passing <paramref name="units"/> to convert final values to mm
        /// </summary>
        /// <param name="xRaw">Raw value of x</param>
        /// <param name="yRaw">Raw value of y</param>
        /// <param name="zRaw">Raw value of z</param>
        /// <param name="units">Units to convert raw values to mm</param>
        public Point3D(float xRaw, float yRaw, float zRaw, float units)
        {
            this.X = xRaw.Unitize(units);
            this.Y = yRaw.Unitize(units);
            this.Z = zRaw.Unitize(units);
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="Point3D"/> class using values in mm
        /// </summary>
        /// <param name="x">X value in mm</param>
        /// <param name="y">Y value in mm</param>
        /// <param name="z">Z value in mm</param>
        public Point3D(float x, float y, float z) : this(x, y, z, 1) { }
    }

    public class PointHatch
    {
        public float Xs { get; set; }
        public float Xe { get; set; }
        public float Ys { get; set; }
        public float Ye { get; set; }
        public override string ToString() => $"Xs: {Xs.Format()}, Xe: {Xe.Format()}, Ys: {Ys.Format()}, Ye: {Ye.Format()}";

        /// <summary>
        /// Initializes a new instance of the <see cref="PointHatch"/> class using raw values, passing <paramref name="units"/> to convert final values to mm
        /// </summary>
        /// <param name="xsRaw">Raw value of x start</param>
        /// <param name="xeRaw">Raw value of x end</param>
        /// <param name="ysRaw">Raw value of y start</param>
        /// <param name="yeRaw">Raw value of y end</param>
        /// <param name="units"></param>
        public PointHatch(float xsRaw, float xeRaw, float ysRaw, float yeRaw, float units)
        {
            this.Xs = xsRaw.Unitize(units);
            this.Xe = xeRaw.Unitize(units);
            this.Ys = ysRaw.Unitize(units);
            this.Ye = yeRaw.Unitize(units);
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="PointHatch"/> class using values in mm
        /// </summary>
        /// <param name="xs">X start value in mm</param>
        /// <param name="xe">X end value in mm</param>
        /// <param name="ys">Y start value in mm</param>
        /// <param name="ye">Y end value in mm</param>
        public PointHatch(float xs, float xe, float ys, float ye) : this(xs, xe, ys, ye, 1) { }
    }

    /// <summary>
    /// Represents an error with the CLI file format
    /// </summary>
    public class CliFileFormatException : Exception
    {
        public CliFileFormatException()
            : base() { }
        public CliFileFormatException(string message)
            : base(message) { }
        public CliFileFormatException(string message, Exception innerException)
            : base(message, innerException) { }
    }

    /// <summary>
    /// Direction of a shape used to indicate internal (<see cref="Direction.Clockwise"/>) or external (<see cref="Direction.CounterClockwise"/>), or other / unknown
    /// </summary>
    public enum Direction 
    {
        /// <summary>
        /// Indiciates an internal <see cref="Shape{T}"/>
        /// </summary>
        Clockwise = 0, 
        /// <summary>
        /// Indicates an external <see cref="Shape{T}"/>
        /// </summary>
        CounterClockwise = 1,
        /// <summary>
        /// Indicate a non-closed <see cref="Shape{T}"/>. This can be used as an input for correction and editing tools based on the CLI format.
        /// Area and perimiter will not be calculated for open line shapes
        /// </summary>
        OpenLine = 2
    }

    /// <summary>
    /// Indication that the <see cref="Geometry"/> section of the file is in ASCII or binary
    /// </summary>
    public enum FileType 
    {
        /// <summary>
        /// Default value, encountering this value in production should indicate an error
        /// </summary>
        Unknown = 0,
        /// <summary>
        /// Indicates that the <see cref="Geometry"/> is defined by ASCII characters
        /// </summary>
        Ascii = 1, 
        /// <summary>
        /// Indicates that the <see cref="Geometry"/> is defined by binary data
        /// </summary>
        Binary = 2 
    }

    /// <summary>
    /// Mixture of ASCII and binary commands used for parsing the <see cref="Geometry"/> section of the file
    /// </summary>
    public enum CommandType : ushort
    {
        /// <summary>
        /// Default value, encountering this value in production should indicate an error
        /// </summary>
        Unknown = 0,
        /// <summary>
        /// A <see cref="Layer"/> in an ASCII <see cref="Geometry"/>
        /// </summary>
        LayerAscii = 1,
        /// <summary>
        /// A <see cref="PolyLine"/> in an ASCII <see cref="Geometry"/>
        /// </summary>
        PolyLineAscii = 2,
        /// <summary>
        /// A <see cref="Hatch"/> in an ASCII <see cref="Geometry"/>
        /// </summary>
        HatchAscii = 3,
        /// <summary>
        /// Not a command, but a new line. These are effectively ignored if encountered in a binary file
        /// </summary>
        NewLine = 10,
        /// <summary>
        /// A <see cref="Layer"/> with 32 bit shapes in a binary <see cref="Geometry"/>
        /// </summary>
        LayerLong = 127,
        /// <summary>
        /// A <see cref="Layer"/> with 16 bit shapes in a binary <see cref="Geometry"/>
        /// </summary>
        LayerShort = 128,
        /// <summary>
        /// A <see cref="PolyLine"/> made of 16 bit values in a binary <see cref="Geometry"/>
        /// </summary>
        PolyLineShort = 129,
        /// <summary>
        /// A <see cref="PolyLine"/> made of 32 bit values in a binary <see cref="Geometry"/>
        /// </summary>
        PolyLineLong = 130,
        /// <summary>
        /// A <see cref="Hatch"/> made of 16 bit values in a binary <see cref="Geometry"/>
        /// </summary>
        HatchShort = 131,
        /// <summary>
        /// A <see cref="Hatch"/> made of 32 bit values in a binary <see cref="Geometry"/>
        /// </summary>
        HatchLong = 132
    }

}