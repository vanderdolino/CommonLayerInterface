using System;
using CommonLayerInterface.Utils;
using CommonLayerInterface.Classes;

namespace CommonLayerInterface.CLI // Note: actual namespace depends on the project name.
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var fileNames = new List<string>();
            var verbose = args.Any(a => a.ToUpper() == "-V");
            foreach (var arg in args.Where(a => a.ToUpper() != "-V"))
            {
                try
                {
                    fileNames.Add(Path.GetFullPath(arg));
                }
                catch
                {
                    Console.WriteLine($"Command line argument path '{arg}' not found.");
                }
            }
            ProcessFiles(fileNames, verbose);
            Console.WriteLine("Press any key to exit.");
            Console.Read();
        }

        private static bool ReadLineIsY() => Console.ReadLine()?.ToUpper() == "Y";

        private static void ProcessFiles(IEnumerable<string> fileNames, bool verbose)
        {
            var files = fileNames.Select(fileName => CommonLayerInterfaceFactory.CreateCommonLayerInterfaceFile(fileName)).ToList();
            foreach (var file in files)
                file.PrintToConsole(verbose);
        }
    }
}