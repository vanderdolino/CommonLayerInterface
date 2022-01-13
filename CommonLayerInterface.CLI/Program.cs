using System;
using CommonLayerInterface.Utils;

namespace MyApp // Note: actual namespace depends on the project name.
{
    internal class Program
    {
        static void Main(string[] args)
        {
            
            var file = CommonLayerInterfaceFactory.CreateCommonLayerInterfaceFile("sample files\\box_cli_ascii.cli");
            // required parameters
            Console.WriteLine($"FileType: {file.Header.FileType}");
            Console.WriteLine($"Units: {file.Header.Units}");
            Console.WriteLine($"Version: {file.Header.Version}");
            // optional parameters
            Console.WriteLine($"Date: {file.Header.Date}");
            Console.WriteLine($"Dimension: {file.Header.Dimension}");
            Console.WriteLine($"Layers: {file.Header.Layers}");
            Console.WriteLine($"Align: {file.Header.Align}");
            foreach (var label in file.Header.Labels)
                Console.WriteLine($"Label: {label}");
            Console.WriteLine($"UserData: {file.Header.UserData}");
            foreach (var model in file.Geometry.Models)
            {
                Console.WriteLine($"Model ID: {model.ID}, Layers: {model.Layers.Count}");
                foreach (var layer in model.Layers)
                {
                    Console.WriteLine($"Layer Z: {layer.Z}, Area: {layer.Area}, Perimiter: {layer.Perimiter}");
                }
            }
            Console.ReadLine();
        }
    }
}