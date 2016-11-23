using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace PPlab1
{
    class Program
    {
        static bool openNextCage = false;
        static Object thisLock = new Object();


        static Random rnd = new Random();
        static Stopwatch stopwatch = new Stopwatch();
        static int[] array;
        static int results;
        const long ArraySize = 100000000;
        const int minArrayBlock = 10000;

        static void makeArray()
        {
            array = new int[ArraySize];
            for (long i = 0; i < ArraySize; i++)
            {
                array[i] = rnd.Next(100000);
            }
        }

        static void CFOF_linear()
        {
            stopwatch.Start();
            int factors = 0;
            for (int i = 0; i < ArraySize; i++)
            {
                int t = array[i];
                if (t % 5 == 0)
                    factors++;
            }
            Console.WriteLine("Linear: {0} factors, time is {1} ticks and {2} ms", factors, stopwatch.ElapsedTicks, stopwatch.ElapsedMilliseconds);
            stopwatch.Stop();
        }

        //Всё что выше этого комментария - работает!

        static void CountingThread(long startIndex, long inds, int myindex)
        {
            //КРИТ. СЕКЦИЯ НУЖНА, БЕЗ МАССИВА results[]
            //грануляция
            openNextCage = true;
            int localResult = 0;
            for (long ii = startIndex; ii < startIndex + inds; ii++)
            {
                if (array[ii] % 5 == 0)
                {
                    localResult++;
                }
            }
            lock (thisLock)
            {
                //увеличить грануляцию
                results += localResult;
            }
        }

        static int ThreadsToCreate()
        {
            int thrds = System.Environment.ProcessorCount;
            if (thrds > ArraySize / minArrayBlock)
                thrds = (int)(ArraySize / minArrayBlock);
            return thrds;
        }



        static void MCFOF(int ThreadsToCreate)
        {
            //int factors = 0;
            long indexesPerThread = 0;
            long StartIndex = 0;
            //factors = 0;
            results = 0;//new int[ThreadsToCreate];
            //for (int i = 0; i < ThreadsToCreate; i++)
            //    results[i] = 0;
            indexesPerThread = ArraySize / ThreadsToCreate;

            List<Thread> thrds = new List<Thread>();

            stopwatch.Restart();

            for (int i = 0; i < ThreadsToCreate - 1; i++)
            {
                //последний кусок массива рассчитывать НЕ тут
                openNextCage = false;
                StartIndex = i * indexesPerThread;
                Thread newThread = new Thread(() => CountingThread(StartIndex, indexesPerThread, i));
                newThread.Start();
                thrds.Add(newThread);
                while (!openNextCage)
                {
                    Thread.Yield();
                }
            }
            int localResults = 0;
            for (long i = (ThreadsToCreate - 1) * indexesPerThread; i < ArraySize; i++)
            {
                if (array[i] % 5 == 0)
                {
                    localResults++;
                }
            }

            lock (thisLock)
            {
                results += localResults;
            }

            foreach (Thread thr in thrds)
                thr.Join();


            stopwatch.Stop();

            //for (int i = 0; i < ThreadsToCreate; i++)
            //    factors += results[i];
            Console.WriteLine("{3} threads: found {0} factors in {1} ticks and {2} ms", results, stopwatch.ElapsedTicks, stopwatch.ElapsedMilliseconds, ThreadsToCreate);
        }



        static void Main(string[] args)
        {
            Console.Write("Generating array of {0} elements...", ArraySize);
            makeArray();
            Console.WriteLine(" done.");

            //Непараллельное вычисление
            Console.WriteLine("\n Single thread: ");
            CFOF_linear();
            //Console.Write("\n{0}\n", RandomSomething);
            //RandomSomething = 0;

            //По количеству ядер
            Console.WriteLine("\n Per-CPU threads: ");
            MCFOF(ThreadsToCreate());

            //По руками заданному значению
            Console.WriteLine("\n Many threads: ");
            MCFOF(2 * ThreadsToCreate());

            Console.ReadKey(true);
        }
    }
}