# veeam
The idea is to split the file into 1MB chunks the output file conststs of a header and multiple parts format is like this:

HEADER(1) : [HEADER_STRING("Veam-Compression-v1.0"), DECOMPRESSED_FILE_SIZE, CHUNK_SIZE]
* HEADER_STRING is basically just for us to be able to tell if the file was compressed by our utility upfront without trying to decompress it.
* DECOMPRESSED_FILE_SIZE file size before the compression - we don't need to sore it but it's handy as we can check  before decompression if wwe have enough space on target drive.
* CHUNK_SIZE - size of the chunk after decompression (right now 1MB always).

PARTS(N): [POSITION, COMPRESSED_SIZE, DATA)]
* POSITION - position of the decompressed chunk in decompressed file
* COMPRESSED_SIZE - size of the chunk in compressed file 
* DATA - GzipCompressed chunk of original size 1MB (or less if last chunk was smaller)

One thing to mention here is that parts are not put in any particular order in the compressed file (that said due to the fact that we don't really have that many processing threads it is unlikely that two parts that are very far away would be actually put far from each other). 
That's why we save the position so we are able to put back the file in the right order during decompression.

The number of parts is of course size_of_compressed_file / 1MB
The program uses one reader and one writer thread and multiple processing threads.
* Reader thread is reading the file in parts of 1MB
* Processing threads are compressing or decompressing the chunks
* Writer thread writes the compressed/decompressed chunks into output file.

The reader and writer threads are doing pure IO operations as they just either read or write to the disk.
The processing threads are doing mostly calculations (compression or decompression) and thus their number is basically the machine numer of logical processors.

The communication between threads is done by combination of locks and AutoResetEvents.
Basically there is a queue between reader and processing threads and another queue between processing threads and writer thread.
Size of both queues is number_of_processing_threads * 2.
That means that the reader can be producing more chunks to process then processing threads are able to process at the same time but it should not produce too many in order not to clutter the memory. Same idea is between processing threads and writer (in general I chose this number arbitrarily - probably would need more investigation however I had pretty good results on my 8 core machine the speed up in comparison to single threaded GZip was about 300 - 500% on 100 MB file, on 2GB file it was simimiliar - 2 minutes vs 35 seconds.) and average memory usage wasn't higher then 120 MB. The performance is of course heavily dependant on the speed of the hard drive (I made my tests reading and writing to the same drive).

How is the code structured?
As mentioned there is a nice level of abstraction to notice here that basically both compression and decompression can in essence be reduced to:
* Read some data
* Process it in some way
* Write processed data
That's what I was trying to achive.

In general there is interface IReadProcessWrite with abstract implementation AbstractReadProcessWrite (which is basically like a template of read->process->write it creates threads manages them etc). 
And the concrete implementations of this abstract class are validating if the compression / decompression inputs are correct and create a small utility classes for the threads that are either compressing / decompressing data.

