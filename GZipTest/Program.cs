﻿using GZipTest.Processing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace GZipTest
{
    class Program
    {

        private static Dictionary<string, ReadProcessWriteMode> _nameToModeMap = new Dictionary<string, ReadProcessWriteMode>
        {
            { "compress", ReadProcessWriteMode.Compress},
            { "decompress", ReadProcessWriteMode.Decompress }
        };

        private static ReadProcessWriteMode Parse(string mode)
        {
            if(!_nameToModeMap.ContainsKey(mode))
            {
                throw new Exception($"Unkown mode : {mode}, accepted modes are [compress|decompress]");
            }
            return _nameToModeMap[mode];
        }

        static int Main(string[] args)
        {
            try
            {

                if (args.Length != 3)
                {
                    throw new Exception("Wrong number of parameters : accepted parameters are mode:[compress|decompress] inputFile outputFile");
                }
                string mode = args[0];
                string inputFileName = args[1];
                string outputFileName = args[2];

                ReadProcessWriteMode modeEnum = Parse(mode);

                IReadProcessWrite readProcessWrite = ReadProcessWriteFactory.Create(modeEnum);
                readProcessWrite.ProcessingProgress += ReadProcessWrite_ProcessingProgress;

                ReadProcessWriteInput input = new ReadProcessWriteInput(inputFileName, outputFileName);

                log4net.Config.XmlConfigurator.Configure(new FileInfo("log4net.config"));

                string work = modeEnum == ReadProcessWriteMode.Compress ? "Compressing" : "Decompressing";

                Console.WriteLine($"{work} {inputFileName} into {outputFileName}");
                Console.WriteLine();

                Stopwatch watch = new Stopwatch();
                watch.Start();

                readProcessWrite.ReadProcessWrite(input);

                watch.Stop();
                string jobName = modeEnum == ReadProcessWriteMode.Compress ? "Compression" : "Decompression";
                Console.WriteLine($"{jobName} done. Work time : {watch.Elapsed}");
                return 0;
                
            }
            catch(Exception exception)
            {
                Console.WriteLine(exception.Message);
            }
            return 1;
        }

        private static void ReadProcessWrite_ProcessingProgress(double percentDone)
        {
            Console.SetCursorPosition(0, Console.CursorTop - 1);
            Console.WriteLine($"Done : {percentDone} %");
        }
    }
}
