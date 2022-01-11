using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public static ICommonLayerInterfaceFile CreateCommonLayerInterfaceFile(string filename)
        {
            ICommonLayerInterfaceFile result;
            string fileContents;
            using(StreamReader streamReader = new StreamReader(filename))
                fileContents = streamReader.ReadToEnd();            
            var header = getHeader(fileContents);
            if (header.FileType == FileType.Binary)
                result = new BinaryCommonLayerInterfaceFile();
            else
                result = new AsciiCommonLayerInterfaceFile();
            result.Header = header;
            return result;
        }

        private static Header getHeader(string fileContents)
        {
            var header = new Header();
            var headerSection = fileContents.Substring(HEADERSTART_TOKEN.Length, fileContents.IndexOf(HEADEREND_TOKEN)).Split(Environment.NewLine);
            // required properties
            // binary / ascii
            if (headerSection.Contains(BINARY_TOKEN))
                header.FileType = FileType.Binary;
            else if (headerSection.Contains(ASCII_TOKEN))
                header.FileType = FileType.Ascii;
            // units
            header.Units = float.Parse(headerSection.Single(s => s.StartsWith(UNITS_TOKEN)).Split("/")[1]);
            // version
            header.Version = float.Parse(headerSection.Single(s => s.StartsWith(VERSION_TOKEN)).Split("/")[1]) / 100;
            // optional properties
            // date
            var dateSection = headerSection.SingleOrDefault(s => s.StartsWith(DATE_TOKEN));
            if (dateSection != null)
            {
                var dateValue = dateSection.Split("/")[1];
                if (DateTime.TryParseExact(dateValue, "ddMMyy", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime value))
                    header.Date = DateOnly.FromDateTime(value);
                else
                    header.Date = null;
            }
            // dimension
            var dimensionSection = headerSection.SingleOrDefault(s => s.StartsWith(DIMENSION_TOKEN));
            if (dimensionSection != null)
            {
                var dimensions = dimensionSection.Split("/")[1].Split(",").Select(s => float.Parse(s)).ToArray();
                header.Dimension = new Dimension(
                    new Point3D(dimensions[0], dimensions[1], dimensions[2]), 
                    new Point3D(dimensions[3], dimensions[4], dimensions[5]));
            }
            // layers
            var layersSection = headerSection.SingleOrDefault(s => s.StartsWith(LAYERS_TOKEN));
            if (layersSection != null)
                header.Layers = short.Parse(layersSection.Split("/")[1]);
            // align
            header.Align = headerSection.Contains(ALIGN_TOKEN);
            // labels
            var labelSections = headerSection.Where(s => s.StartsWith(LABEL_TOKEN));
            if (labelSections.Any())
                header.Labels = labelSections.Select(s =>
                {
                    var split = s.Split("/")[1].Split(",");
                    return new Label() { ID = short.Parse(split[0]), Text = split[1] };
                });
            // user-data
            var userDataSection = headerSection.SingleOrDefault(s => s.StartsWith(USERDATA_TOKEN));
            if (userDataSection != null)
            {
                var userDatas = userDataSection.Split("/")[1].Split(",").ToArray();
                header.UserData = new UserData(userDatas[0], int.Parse(userDatas[1]), userDatas[2]);
            }
            return header;
        }
    }
}
