using System;
using CommonLayerInterface.Utils;
using CommonLayerInterface.Classes;

namespace CommonLayerInterface.CLI // Note: actual namespace depends on the project name.
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var runWithArgs = args.Any();
            var fileNames = new List<string>();
            var verbose = args.Any(a => a.ToUpper() == "-V");
            foreach (var arg in args)
            {
                if (arg.ToUpper() != "-V")
                    fileNames.Add(Path.GetFullPath(arg));
            }

            var files = fileNames.Select(fileName => CommonLayerInterfaceFactory.CreateCommonLayerInterfaceFile(fileName)).ToList();

            foreach (var file in files)
                file.PrintToConsole(verbose);

            Console.ReadLine();
        }
    }
}