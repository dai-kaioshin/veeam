using GZipTest.Chunks;
using GZipTest.Queue;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Threading;

namespace GZipTest.Compression
{
    class CompressionWorker
    {
        
    }

    public class Compressor
    {
        private static log4net.ILog _logger = log4net.LogManager.GetLogger(typeof(Compressor));

        private FixedSizeQueue<DataChunk> _readData = new FixedSizeQueue<DataChunk>(6);

        private Queue<DataChunk> _compressedData = new Queue<DataChunk>();

        private object _readLock = new object();

        private AutoResetEvent _readEvent = new AutoResetEvent(false);

        private AutoResetEvent _comressingFinishedEvent = new AutoResetEvent(false);

        private bool _readingFinished = false;

        private bool _compressionFinished = false;

        private object _writeLock = new object();

        private AutoResetEvent _writeEvent = new AutoResetEvent(false);

        private AutoResetEvent _endEvent = new AutoResetEvent(false);

        public void Compress(string inputFileName, string outputFileName)
        {
            const int bufferSize = 1024;
            byte[] buffer = new byte[bufferSize];
            using (FileStream fileStream = new FileStream(inputFileName, FileMode.Open, FileAccess.Read))
            {
                int read = 0;

                using (var outputFileStream = File.Create(outputFileName))
                {
                }

                new Thread(() => this.Compress()).Start();
                new Thread(() => this.Compress()).Start();
                new Thread(() => this.Compress()).Start();
                new Thread(() => this.WriteToOutput(outputFileName)).Start();
                int part = 0;
                long position = 0;
                while ((read = fileStream.Read(buffer, 0, bufferSize)) > 0)
                {
                    byte[] chunk = new byte[read];
                    Array.Copy(buffer, chunk, read);

                    DataChunk dataChunk = new DataChunk(part++, position, chunk);
                    _logger.Debug($"Enqueueing chunk : {dataChunk}");
                    _readData.Enqueue(dataChunk);

                    position += read;
                    _readEvent.Set();
                }
            }
            lock (_readLock)
            {
                _readingFinished = true;
            }
            _readEvent.Set();
            _comressingFinishedEvent.WaitOne();
            lock (_writeLock)
            {
                _compressionFinished = true;
            }
            _endEvent.WaitOne();  
        }

        private void Compress()
        {
            while(true)
            {
                DataChunk chunk = null;
                bool canFinish = false;
                _readData.Dequeue(out chunk);
                lock (_readLock)
                {
                    if(chunk == null && _readingFinished)
                    {
                        canFinish = true;
                    }
                }
                if (canFinish)
                {
                    _comressingFinishedEvent.Set();
                    _writeEvent.Set();
                    return;
                }
                if (chunk == null)
                {
                    _readEvent.WaitOne(100);
                    continue;
                }
                _logger.Debug($"Compressing chunk : {chunk}");
                using (var memoryStream = new MemoryStream())
                {
                    using (var gzipStream = new GZipStream(memoryStream, CompressionLevel.Optimal))
                    {
                        gzipStream.Write(chunk.Data, 0, chunk.Data.Length);
                        gzipStream.Flush();
                        DataChunk compressedChunk = new DataChunk(chunk.Part, chunk.Position, memoryStream.ToArray());
        
                        lock(_writeLock)
                        {
                            _compressedData.Enqueue(compressedChunk);
                        }
                        _writeEvent.Set();
                    }
                }
                
            }
        }

        private void WriteToOutput(string outputFileName)
        {
            while(true)
            {
                DataChunk chunk = null;
                bool canFinish = false;
                lock(_writeLock)
                {
                    if(_compressedData.Count > 0)
                    {
                        chunk = _compressedData.Dequeue();
                    }
                    else if(_compressedData.Count == 0 && _compressionFinished)
                    {
                        canFinish = true;
                    }
                }
                if (canFinish)
                {
                    _endEvent.Set();
                    return;
                }
                if(chunk == null)
                {
                    _writeEvent.WaitOne(100);
                    continue;
                }
                _logger.Debug($"Writing chunk : {chunk}");
                using (var outputFileStream = new FileStream(outputFileName, FileMode.Append))
                {
                    outputFileStream.Write(BitConverter.GetBytes(chunk.Part), 0, sizeof(int));
                    outputFileStream.Write(BitConverter.GetBytes(chunk.Position), 0, sizeof(long));
                    outputFileStream.Write(BitConverter.GetBytes(chunk.DecompressedSize), 0, sizeof(int));
                    outputFileStream.Write(BitConverter.GetBytes(chunk.Data.Length), 0, sizeof(int));
                    outputFileStream.Write(chunk.Data, 0, chunk.Data.Length);
                }
            }
        }
    }
}
