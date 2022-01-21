using System;
using CommonLayerInterface.Utils;
using CommonLayerInterface.Classes;

namespace CommonLayerInterface.CLI 
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
                    if (File.Exists(arg))
                        fileNames.Add(arg);
                    else throw new FileNotFoundException();
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

        private static void ProcessFiles(IEnumerable<string> fileNames, bool verbose)
        {
            var files = fileNames.Select(fileName => CommonLayerInterfaceFactory.CreateCommonLayerInterfaceFile(fileName)).ToList();
            foreach (var file in files)
                file.PrintToConsole(verbose);
        }
    }
}