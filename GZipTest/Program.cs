using GZipTest.Compression;
using GZipTest.Processing;
using System;
using System.Diagnostics;
using System.Threading;

namespace GZipTest
{
    class Program
    {
        static Semaphore _semaphore = new Semaphore(0, 6);
        static void Main(string[] args)
        {
            string mode = args[0];
            string inputFileName = args[1];
            string outputFileName = args[2];
            //log4net.Config.BasicConfigurator.Configure();
            int procCnt = Environment.ProcessorCount;
            log4net.LogManager.GetLogger("ABC").Debug("StART");
            Stopwatch watch = new Stopwatch();
            watch.Start();
            if (mode == "compress")
                //new Compressor().Compress(inputFileName, outputFileName);
                new CompressData().ReadProcessWrite(new ReadProcessWriteInput(inputFileName, outputFileName, 1024));
            if (mode == "decompress")
                //new Decompressor().Decompress(inputFileName, outputFileName);
                new DecompressData().ReadProcessWrite(new ReadProcessWriteInput(inputFileName, outputFileName, 1024));

            watch.Stop();
            Console.WriteLine($"Time : {watch.Elapsed}");

            /*for(int i = 0; i < 12; i++)
            {
                new Thread(() => DoSth()).Start();
            }
            _semaphore.Release(6);*/
        }

        static void DoSth()
        {
            _semaphore.WaitOne();
            System.Console.WriteLine($"Thread : {Thread.CurrentThread.ManagedThreadId} working : {Environment.TickCount}");
            Thread.Sleep(100);
            _semaphore.Release();
        }
    }
}
