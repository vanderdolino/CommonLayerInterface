using System.Globalization;

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

        public static CommonLayerInterfaceFile CreateCommonLayerInterfaceFile(string filename)
        {
            CommonLayerInterfaceFile result = new();
            //string fileContents;
            using (FileStream stream = new FileStream(filename, FileMode.Open))
            {
                
                result.Header = getHeader(stream);
                //result.Geometry = GetGeometry(fileContents, result.Header);
            }
            return result;
        }

        private static Header getHeader(Stream stream)
        {
            var header = new Header();
            
            List<string> headerSection = new();

            using (StreamReader streamReader = new StreamReader(stream))
            {
                do
                {
                    headerSection.Add(streamReader.ReadLine());
                } while (!headerSection.Last().StartsWith(HEADEREND_TOKEN));
            }

            // headerSection = fileContents.Substring(HEADERSTART_TOKEN.Length, fileContents.IndexOf(HEADEREND_TOKEN)).Split(new[]{"\n", "\r", Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries);
            //// required properties
            // binary / ascii
            if (headerSection.Contains(BINARY_TOKEN))
                header.FileType = FileType.Binary;
            else if (headerSection.Contains(ASCII_TOKEN))
                header.FileType = FileType.Ascii;
            else
                throw new CliFileFormatException("Header has missing or invalid binary / ascii declaration");
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
            {
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
            }
            // dimension
            var dimensionSection = headerSection.SingleOrDefault(s => s.StartsWith(DIMENSION_TOKEN));
            if (dimensionSection != null)
            {
                try
                {
                    var dimensions = dimensionSection.Split("/")[1].Split(",").Select(s => float.Parse(s)).ToArray();
                    header.Dimension = new Dimension(
                        new Point3D(dimensions[0], dimensions[1], dimensions[2]),
                        new Point3D(dimensions[3], dimensions[4], dimensions[5]));
                }
                catch
                {
                    throw new CliFileFormatException("Header has invalid dimension declaration");
                }
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
            {
                try
                {
                    var userDatas = userDataSection.Split("/")[1].Split(",").ToArray();
                    header.UserData = new UserData(userDatas[0], int.Parse(userDatas[1]), userDatas[2]);
                }
                catch
                {
                    throw new CliFileFormatException("Header has invalid user data declaration");
                }
            }
            return header;
        }

        private static Geometry GetGeometry(string fileContents, Header header)
        {
            Geometry geometry = null;

            switch (header.FileType)
            {
                case FileType.Ascii:
                    geometry = GetGeometryAscii(fileContents);
                    break;
                case FileType.Binary:
                    geometry = GetGeometryBinary(fileContents);
                    break;
            }
            return geometry;
        }

        private static Geometry GetGeometryAscii(string fileContents)
        {
            Geometry geometry = new();
            var geometrySection = fileContents.Substring(fileContents.IndexOf(GEOMETRYSTART_TOKEN), fileContents.IndexOf(GEOMETRYEND_TOKEN) - fileContents.IndexOf(GEOMETRYSTART_TOKEN)).Split(Environment.NewLine);
            var models = new List<Model>();
            for (int i = 0; i < geometrySection.Length; i++)
            {
                if (geometrySection[i].StartsWith(LAYER_TOKEN))
                {
                    var z = float.Parse(geometrySection[i].Split("/")[1]);
                    i++;
                    while (geometrySection[i].StartsWith(POLYLINE_TOKEN) || geometrySection[i].StartsWith(HATCHES_TOKEN))
                    {
                        if (geometrySection[i].StartsWith(POLYLINE_TOKEN))
                        {
                            var polyLine = new PolyLine();
                            var points = new List<Point2D>();
                            var args = geometrySection[i].Split("/")[1].Split(",");
                            var id = short.Parse(args[0]);
                            var dir = (Direction)int.Parse(args[1]);
                            var n = int.Parse(args[2]);
                            for (int j = 0; j < 2 * n; j += 2)
                                points.Add(new Point2D(float.Parse(args[j + 3]), float.Parse(args[j + 4])));
                            polyLine.Direction = dir;
                            polyLine.Points = points;
                            var model = models.SingleOrDefault(m => m.ID == id);
                            if (model == null)
                            {
                                model = new Model(id);
                                models.Add(model);
                            }
                            var layer = model.Layers.SingleOrDefault(l => l.Z == z);
                            if (layer == null)
                            {
                                layer = new Layer(z);
                                model.Layers.Add(layer);
                            }
                            layer.PolyLines.Add(polyLine);
                        }
                        if (geometrySection[i].StartsWith(HATCHES_TOKEN))
                        {
                            var hatch = new Hatch();
                            var points = new List<PointHatch>();
                            var args = geometrySection[i].Split("/")[1].Split(",");
                            var id = short.Parse(args[0]);
                            var n = int.Parse(args[1]);
                            for (int j = 0; j < 4 * n; j += 4)
                                points.Add(new PointHatch(
                                    float.Parse(args[j + 2]), 
                                    float.Parse(args[j + 3]), 
                                    float.Parse(args[j + 4]), 
                                    float.Parse(args[j + 5])));
                            hatch.Points = points;
                            var model = models.SingleOrDefault(m => m.ID == id);
                            if (model == null)
                            {
                                model = new Model(id);
                                models.Add(model);
                            }
                            var layer = model.Layers.SingleOrDefault(l => l.Z == z);
                            if (layer == null)
                            {
                                layer = new Layer(z);
                                model.Layers.Add(layer);
                            }
                            layer.Hatches.Add(hatch);
                        }
                        i++;
                    }
                    i--;
                }
            }
            geometry.Models = models;
            return geometry;
        }

        private static Geometry GetGeometryBinary(string fileContents)
        {
            Geometry geometry = new();
            var models = new List<Model>();
            Layer layer;
            Model model;

            var headerEndLocation = fileContents.IndexOf(HEADEREND_TOKEN) + HEADEREND_TOKEN.Length;
            using (var fs = new FileStream("sample files\\VulcanFormsSamplePartA.cli", FileMode.Open))
            using (var reader = new BinaryReader(fs))
            {
                float z = float.NaN;
                reader.BaseStream.Position = headerEndLocation;
                do
                {
                    var command = reader.ReadUInt16();
                    switch (command)
                    {

                        case 127: // Layer 4B
                            {
                                z = reader.ReadSingle();
                            }
                            break;
                        case 128: // Layer 2B
                            {
                                z = reader.ReadUInt16();
                            }
                            break;
                        case 129: // PolyLine 2B 
                            break;
                        case 130: // PolyLine 4B
                            {
                                var polyLine = new PolyLine();
                                var points = new List<Point2D>();
                                var id = (short)reader.ReadInt32();
                                var dir = (Direction)reader.ReadInt32();
                                var n = reader.ReadInt32();
                                var args = Enumerable.Range(0, n * 2).Select(_ => reader.ReadSingle()).ToArray();
                                for (int j = 0; j < args.Length ; j += 2)
                                    points.Add(new Point2D(args[j], args[j + 1]));
                                polyLine.Direction = dir;
                                polyLine.Points = points;
                                model = models.SingleOrDefault(m => m.ID == id);
                                if (model == null)
                                {
                                    model = new Model(id);
                                    models.Add(model);
                                }
                                layer = model.Layers.SingleOrDefault(l => l.Z.Round() == z.Round());
                                if (layer == null)
                                {
                                    layer = new Layer(z);
                                    model.Layers.Add(layer);
                                }
                                layer.PolyLines.Add(polyLine);
                            }
                            break;
                        case 131: // Hatches 2B
                            {
                                var polyLine = new PolyLine();
                                var points = new List<Point2D>();
                                var id = (short)reader.ReadUInt16();
                                var dir = (Direction)reader.ReadUInt16();
                                var n = reader.ReadUInt16();
                                var args = Enumerable.Range(0, n * 2).Select(_ => reader.ReadUInt16()).ToArray();
                                for (int j = 0; j < args.Length; j += 2)
                                    points.Add(new Point2D(args[j], args[j + 1]));
                                polyLine.Direction = dir;
                                polyLine.Points = points;
                                model = models.SingleOrDefault(m => m.ID == id);
                                if (model == null)
                                {
                                    model = new Model(id);
                                    models.Add(model);
                                }
                                layer = model.Layers.SingleOrDefault(l => l.Z.Round() == z.Round());
                                if (layer == null)
                                {
                                    layer = new Layer(z);
                                    model.Layers.Add(layer);
                                }
                                layer.PolyLines.Add(polyLine);
                            }
                            break;
                        case 132: // Hatches 4B
                            break;
                        default:
                            Console.WriteLine("unrecognized command!");
                            break;
                    }
                } while (reader.BaseStream.Position != reader.BaseStream.Length);

            }
            geometry.Models = models;
            return geometry;
        }

    }
}
