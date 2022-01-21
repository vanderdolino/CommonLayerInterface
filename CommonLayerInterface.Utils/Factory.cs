using System.Globalization;
using System.Text;
using CommonLayerInterface.Classes;

namespace CommonLayerInterface.Utils
{
    public class CommonLayerInterfaceFactory
    {
        private const string HEADERSTART_TOKEN = "$$HEADERSTART";
        private const string HEADEREND_TOKEN = "$$HEADEREND";
        private const string ASCII_TOKEN = "$$ASCII";
        private const string BINARY_TOKEN = "$$BINARY";
        private const string UNITS_TOKEN = "$$UNITS";
        private const string VERSION_TOKEN = "$$VERSION";
        private const string LABEL_TOKEN = "$$LABEL";
        private const string DATE_TOKEN = "$$DATE";
        private const string DIMENSION_TOKEN = "$$DIMENSION";
        private const string ALIGN_TOKEN = "$$ALIGN";
        private const string LAYERS_TOKEN = "$$LAYERS";
        private const string USERDATA_TOKEN = "$$USERDATA";
        private const string GEOMETRYSTART_TOKEN = "$$GEOMETRYSTART";
        private const string GEOMETRYEND_TOKEN = "$$GEOMETRYEND";
        private const string LAYER_TOKEN = "$$LAYER";
        private const string POLYLINE_TOKEN = "$$POLYLINE";
        private const string HATCHES_TOKEN = "$$HATCHES";

        private CommonLayerInterfaceFactory() { }

        /// <summary>
        /// Create a <see cref="CommonLayerInterfaceFile"/> from the path to the .cli file
        /// </summary>
        /// <param name="filename">The path to the .cli file</param>
        /// <returns>A <see cref="CommonLayerInterfaceFile"/></returns>
        public static CommonLayerInterfaceFile CreateCommonLayerInterfaceFile(string filename)
        {
            var file = new CommonLayerInterfaceFile();
            using (var stream = new FileStream(filename, FileMode.Open))
            using (var reader = new BinaryReader(stream))
            {
                file.Header = processHeader(reader);
                file.Geometry = processGeometry(reader, file.Header);
            }
            return file;
        }

        private static Header processHeader(BinaryReader reader)
        {
            var header = new Header();
            var chars = new List<char>();
            var headerSection = new List<string>();
            while (true)
            {
                if (reader.PeekChar() == '\r' || reader.PeekChar() == '\n')
                {
                    reader.ReadChar();
                    while (reader.PeekChar() == '\r' || reader.PeekChar() == '\n')
                        reader.ReadChar();
                    headerSection.Add(new string(chars.ToArray()));
                    chars.Clear();
                }
                else
                {
                    var c = reader.ReadChar();
                    chars.Add(c);
                    if (chars.Count >= HEADEREND_TOKEN.Length && new string(chars.TakeLast(HEADEREND_TOKEN.Length).ToArray()) == HEADEREND_TOKEN)
                    {
                        // check for new line,
                        while (reader.PeekChar() == '\r' || reader.PeekChar() == '\n')
                            reader.ReadChar();
                        break;
                    }
                }
            }

            //// required properties
            // binary / ascii
            if (headerSection.Contains(BINARY_TOKEN)) header.FileType = FileType.Binary;
            else if (headerSection.Contains(ASCII_TOKEN)) header.FileType = FileType.Ascii;
            else throw new CliFileFormatException("Header has missing or invalid binary / ascii declaration");
            // units
            try
            {
                header.Units = float.Parse(headerSection.Single(s => s.StartsWith(UNITS_TOKEN)).Split("/")[1]);
            }
            catch
            {
                throw new CliFileFormatException("Header has missing or invalid units declaration");
            }
            // version
            try
            {
                header.Version = float.Parse(headerSection.Single(s => s.StartsWith(VERSION_TOKEN)).Split("/")[1]) / 100;
            }
            catch
            {
                throw new CliFileFormatException("Header has missing or invalid version declaration");
            }
            //// optional properties
            // date
            var dateSection = headerSection.SingleOrDefault(s => s.StartsWith(DATE_TOKEN));
            if (dateSection != null)
                try
                {
                    var dateValue = dateSection.Split("/")[1];
                    if (DateTime.TryParseExact(dateValue, "ddMMyy", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime value))
                        header.Date = DateOnly.FromDateTime(value);
                    else
                        header.Date = null;
                }
                catch
                {
                    throw new CliFileFormatException("Header has invalid date declaration");
                }
            // dimension
            var dimensionSection = headerSection.SingleOrDefault(s => s.StartsWith(DIMENSION_TOKEN));
            if (dimensionSection != null)
                try
                {
                    var dimensions = dimensionSection.Split("/")[1].Split(",").Select(s => float.Parse(s)).ToArray();
                    header.Dimension = new Dimension(new Point3D(dimensions[0], dimensions[1], dimensions[2]), new Point3D(dimensions[3], dimensions[4], dimensions[5]));
                }
                catch
                {
                    throw new CliFileFormatException("Header has invalid dimension declaration");
                }
            // layers
            var layersSection = headerSection.SingleOrDefault(s => s.StartsWith(LAYERS_TOKEN));
            if (layersSection != null)
                try
                {
                    header.Layers = short.Parse(layersSection.Split("/")[1]);
                }
                catch
                {
                    throw new CliFileFormatException("Header has invalid layers declaration");
                }
            // align
            header.Align = headerSection.Contains(ALIGN_TOKEN);
            // labels
            var labelSections = headerSection.Where(s => s.StartsWith(LABEL_TOKEN));
            if (labelSections.Any())
                try
                {
                    header.Labels = labelSections.Select(s =>
                    {
                        var split = s.Split("/")[1].Split(",");
                        return new Label() { ID = short.Parse(split[0]), Text = split[1] };
                    });
                }
                catch
                {
                    throw new CliFileFormatException("Header has invalid label declaration");
                }
            // user-data
            var userDataSection = headerSection.SingleOrDefault(s => s.StartsWith(USERDATA_TOKEN));
            if (userDataSection != null)
                try
                {
                    var userDatas = userDataSection.Split("/")[1].Split(",").ToArray();
                    header.UserData = new UserData(userDatas[0], int.Parse(userDatas[1]), userDatas[2]);
                }
                catch
                {
                    throw new CliFileFormatException("Header has invalid user data declaration");
                }
            return header;
        }

        private static Geometry processGeometry(BinaryReader reader, Header header)
        {
            var geometry = new Geometry();
            int layerIndex = 0;
            switch (header.FileType)
            {
                case FileType.Ascii:
                    {
                        List<string> geometrySection = new List<string>();
                        string line;
                        do
                        {
                            line = reader.ReadLine();
                            if (line != GEOMETRYSTART_TOKEN && line != GEOMETRYEND_TOKEN)
                                geometrySection.Add(line);
                        } while (!(line == GEOMETRYEND_TOKEN));
                        var models = new List<Model>();
                        for (int i = 0; i < geometrySection.Count; i++)
                        {
                            if (geometrySection[i].StartsWith(LAYER_TOKEN))
                            {
                                var z = float.Parse(geometrySection[i].Split("/")[1]);
                                i++;
                                while (i < geometrySection.Count && (geometrySection[i].StartsWith(POLYLINE_TOKEN) || geometrySection[i].StartsWith(HATCHES_TOKEN)))
                                {
                                    if (geometrySection[i].StartsWith(POLYLINE_TOKEN))
                                        ProcessPolyLineAscii(header, geometrySection, models, i, z, ref layerIndex);
                                    if (geometrySection[i].StartsWith(HATCHES_TOKEN))
                                        ProcessHatchAscii(header, geometrySection, models, i, z, ref layerIndex);
                                    i++;
                                }
                                i--;
                            }
                        }
                        geometry.Models = models;
                    }
                    break;
                case FileType.Binary:
                    {
                        var models = new List<Model>();
                        float z = float.NaN;
                        do
                        {
                            var command = (CommandType)reader.ReadUInt16();
                            switch (command)
                            {
                                case CommandType.NewLine: // newline
                                    break;
                                case CommandType.LayerShort: // Layer 2B
                                case CommandType.LayerLong: // Layer 4B
                                    ProcessLayerBinary(reader, ref z, command);
                                    break;
                                case CommandType.PolyLineShort: // PolyLine 2B 
                                case CommandType.PolyLineLong: // PolyLine 4B
                                    ProcessPolyLineBinary(header, reader, models, z, command, ref layerIndex);
                                    break;
                                case CommandType.HatchShort: // Hatches 2B
                                case CommandType.HatchLong: // Hatches 4B
                                    ProcessHatchBinary(header, reader, models, z, command, ref layerIndex);
                                    break;
                                default:
                                    Console.WriteLine("unrecognized command!");
                                    break;
                            }
                        } while (reader.BaseStream.Position != reader.BaseStream.Length);
                        geometry.Models = models;
                    }
                    break;
                default:
                    return null;
            }
            return geometry;
        }

        private static void ProcessHatchAscii(Header header, List<string> geometrySection, List<Model> models, int i, float z, ref int layerIndex)
        {
            var points = new List<PointHatch>();
            var args = geometrySection[i].Split("/")[1].Split(",");
            var id = short.Parse(args[0]);
            var n = int.Parse(args[1]);
            for (int j = 0; j < 4 * n; j += 4)
                points.Add(new PointHatch(float.Parse(args[j + 2]), float.Parse(args[j + 3]), float.Parse(args[j + 4]), float.Parse(args[j + 5]), header.Units));
            var hatch = new Hatch(points, CommandType.HatchAscii, header.Units);
            var model = models.SingleOrDefault(m => m.ID == id);
            if (model == null)
            {
                model = new Model(id);
                models.Add(model);
            }
            var layer = model.Layers.SingleOrDefault(l => zEqualityPredicate(l.Z, z.Unitize(header.Units)));
            if (layer == null)
            {
                layer = new Layer(z, header.Units, layerIndex, CommandType.HatchAscii);
                layerIndex++;
                model.Layers.Add(layer);
            }
            layer.AddHatch(hatch);
        }

        private static void ProcessPolyLineAscii(Header header, List<string> geometrySection, List<Model> models, int i, float z, ref int layerIndex)
        {
            var points = new List<Point2D>();
            var args = geometrySection[i].Split("/")[1].Split(",");
            var id = short.Parse(args[0]);
            var dir = (Direction)int.Parse(args[1]);
            var n = int.Parse(args[2]);
            for (int j = 0; j < 2 * n; j += 2)
                points.Add(new Point2D(float.Parse(args[j + 3]), float.Parse(args[j + 4]), header.Units));
            var polyLine = new PolyLine(dir, points, CommandType.PolyLineAscii, header.Units);
            var model = models.SingleOrDefault(m => m.ID == id);
            if (model == null)
            {
                model = new Model(id);
                models.Add(model);
            }
            var layer = model.Layers.SingleOrDefault(l => zEqualityPredicate(l.Z, z.Unitize(header.Units)));
            if (layer == null)
            {
                layer = new Layer(z, header.Units, layerIndex, CommandType.PolyLineAscii);
                layerIndex++;
                model.Layers.Add(layer);
            }
            layer.AddPolyLine(polyLine);
        }

        private static void ProcessLayerBinary(BinaryReader reader, ref float z, CommandType command)
        {
            switch (command)
            {
                case CommandType.LayerShort:
                    z = reader.ReadUInt16();
                    break;
                case CommandType.LayerLong:
                    z = reader.ReadSingle();
                    break;
                default:
                    z = float.NaN;
                    break;
            }
        }

        private static void ProcessPolyLineBinary(Header header, BinaryReader reader, List<Model> models, float z, CommandType command, ref int layerIndex)
        {
            Func<int> parameterReader;
            Func<float> valueReader;
            CommandType layerCommand;
            switch (command)
            {
                case CommandType.PolyLineShort:
                    parameterReader = () => reader.ReadUInt16();
                    valueReader = () => reader.ReadUInt16();
                    layerCommand = CommandType.LayerShort;
                    break;
                case CommandType.PolyLineLong:
                    parameterReader = () => reader.ReadInt32();
                    valueReader = () => reader.ReadSingle();
                    layerCommand = CommandType.LayerLong;
                    break;
                default:
                    // it's really impossible to get here
                    throw new CliFileFormatException("Unrecognized command");
            }
            var points = new List<Point2D>();
            var id = parameterReader();
            var dir = (Direction)parameterReader();
            var n = parameterReader();
            var args = Enumerable.Range(0, n * 2).Select(_ => valueReader()).ToArray();
            for (int j = 0; j < args.Length; j += 2)
                points.Add(new Point2D(args[j], args[j + 1], header.Units));
            var polyLine = new PolyLine(dir, points, command, header.Units);
            var model = models.SingleOrDefault(m => m.ID == id);
            if (model == null)
            {
                model = new Model(id);
                models.Add(model);
            }
            var layer = model.Layers.SingleOrDefault(l => zEqualityPredicate(l.Z, z.Unitize(header.Units)));
            if (layer == null)
            {
                layer = new Layer(z, header.Units, layerIndex, layerCommand);
                layerIndex++;
                model.Layers.Add(layer);
            }
            layer.AddPolyLine(polyLine);
        }

        private static void ProcessHatchBinary(Header header, BinaryReader reader, List<Model> models, float z, CommandType command, ref int layerIndex)
        {
            Func<int> parameterReader;
            Func<float> valueReader;
            CommandType layerCommand;
            switch (command)
            {
                case CommandType.HatchShort:
                    parameterReader = () => reader.ReadUInt16();
                    valueReader = () => reader.ReadUInt16();
                    layerCommand = CommandType.LayerShort;
                    break;
                case CommandType.HatchLong:
                    parameterReader = () => reader.ReadInt32();
                    valueReader = () => reader.ReadSingle();
                    layerCommand = CommandType.LayerLong;
                    break;
                default:
                    // it's really impossible to get here
                    throw new CliFileFormatException("Unrecognized command");
            }
            var points = new List<PointHatch>();
            var id = parameterReader();
            var n = parameterReader();
            var args = Enumerable.Range(0, n * 4).Select(_ => valueReader()).ToArray();
            for (int j = 0; j < args.Length; j += 2)
                points.Add(new PointHatch(args[j], args[j + 1], args[j + 2], args[j + 3], header.Units));
            var hatch = new Hatch(points, command, header.Units);
            var model = models.SingleOrDefault(m => m.ID == id);
            if (model == null)
            {
                model = new Model(id);
                models.Add(model);
            }
            var layer = model.Layers.SingleOrDefault(l => zEqualityPredicate(l.Z, z.Unitize(header.Units)));
            if (layer == null)
            {
                layer = new Layer(z, header.Units, layerIndex, layerCommand);
                model.Layers.Add(layer);
            }
            layer.AddHatch(hatch);
        }

        /// <summary>
        /// Compares z with z using rounding
        /// </summary>
        /// <param name="z1"></param>
        /// <param name="z2"></param>
        /// <returns></returns>
        private static bool zEqualityPredicate(float z1, float z2) => z1.Round() == z2.Round();
        
    }
}

