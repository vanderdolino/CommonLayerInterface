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

        public static ICommonLayerInterfaceFile CreateCommonLayerInterfaceFile(string filename)
        {
            ICommonLayerInterfaceFile result;
            string fileContents;
            using (StreamReader streamReader = new StreamReader(filename))
                fileContents = streamReader.ReadToEnd();
            var header = getHeader(fileContents);
            if (header.FileType == FileType.Binary)
                result = new BinaryCommonLayerInterfaceFile();
            else
                result = new AsciiCommonLayerInterfaceFile();
            result.Header = header;
            result.Geometry = GetGeometry(fileContents, header);
            return result;
        }

        private static Header getHeader(string fileContents)
        {
            var header = new Header();
            var headerSection = fileContents.Substring(HEADERSTART_TOKEN.Length, fileContents.IndexOf(HEADEREND_TOKEN)).Split(Environment.NewLine);
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
                case FileType.Ascii :
                    geometry = GetGeometryAscii(fileContents);
                    break;
                case FileType.Binary :
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
            var layers = new List<Layer>();
            for(int i = 0; i < geometrySection.Length; i++)
            {
                if(geometrySection[i].StartsWith(LAYER_TOKEN))
                {
                    var z = short.Parse(geometrySection[i].Split("/")[1]);
                    // var layer = new Layer(z);
                    i++;
                    if(geometrySection[i].StartsWith(POLYLINE_TOKEN))
                    {
                        var polyLine = new PolyLine();
                        var points = new List<Point2D>();
                        var args = geometrySection[i].Split("/")[1].Split(",");
                        var id = short.Parse(args[0]);
                        var dir = (Direction)int.Parse(args[1]);
                        var n = int.Parse(args[2]);
                        for(int j = 0; j < 2 * n; j += 2)
                            points.Add(new Point2D(float.Parse(args[j + 3]), float.Parse(args[j + 4])));
                        polyLine.Direction = dir;
                        polyLine.Points = points;
                        var model = models.SingleOrDefault(m => m.ID == id);
                        if (model == null)
                        {
                            model = new Model(id);
                            model.Layers = new List<Layer>();
                            models.Add(model);
                        }
                        var layer = model.Layers.SingleOrDefault(l => l.Z == z);
                        if(layer == null)
                        {
                            layer = new Layer(z);
                            layer.PolyLines = new List<PolyLine>();
                            model.Layers.Add(layer);
                        }
                        layer.PolyLines.Add(polyLine);
                                
                    }
                    if (geometrySection[i].StartsWith(HATCHES_TOKEN))
                    {
                        Console.WriteLine("Hatch found!");
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
            return geometry;
        }

    }
}
