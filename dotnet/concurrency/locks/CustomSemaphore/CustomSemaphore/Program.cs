using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace CustomSemaphore
{
    class Program
    {    // Семафор на 3 разрешения. Максимум — 3.
        private static readonly CustomSemaphore Semaphore = new CustomSemaphore(3, 3);

        static void Main(string[] args)
        {

            for (int i = 1; i <= 6; i++)
            {
                int workerId = i;
                new Thread(() => DoWork(workerId)).Start();
            }

            Console.ReadLine();
        }

        static void DoWork(int id)
        {
            Console.WriteLine($"Поток {id} ожидает разрешение...");

            Semaphore.Wait(); // Захватываем разрешение

            try
            {
                Console.WriteLine($"Поток {id} получил разрешение и работает.");
                Thread.Sleep(1000); // Имитация полезной работы
                Console.WriteLine($"Поток {id} завершил работу.");
            }
            finally
            {
                // Освобождаем разрешение в finally, чтобы гарантировать Release даже при исключении.
                Semaphore.Release();
                Console.WriteLine($"Поток {id} освободил разрешение.");
            }
        }
    }
}
