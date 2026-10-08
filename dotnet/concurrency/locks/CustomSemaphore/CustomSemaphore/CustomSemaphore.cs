using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CustomSemaphore
{

    /// <summary>
    /// Учебная реализация счётного семафора.
    /// Не является потокобезопасной заменой встроенных примитивов .NET,
    /// но демонстрирует базовую логику работы.
    /// </summary>
    class CustomSemaphore
    {
        // Объект для блокировки и ожидания/пробуждения потоков.
        // Все операции с счётчиком происходят под этим замком.
        private readonly object _lock = new object();

        // Максимально допустимое количество разрешений.
        private readonly int _maxCount;

        // Текущее количество доступных разрешений.
        private int _currentCount;


        /// <summary>
        /// Создаёт семафор.
        /// </summary>
        /// <param name="initialCount">Начальное количество доступных разрешений.</param>
        /// <param name="maxCount">Максимальное количество разрешений.</param>
        public CustomSemaphore(int initialCount, int maxCount)
        {
            if (initialCount < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCount), "Начальное количество не может быть отрицательным.");
            if (maxCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxCount), "Максимальное количество должно быть больше нуля.");
            if (initialCount > maxCount)
                throw new ArgumentException("Начальное количество не может превышать максимальное.");

            _maxCount = maxCount;
            _currentCount = initialCount;
        }

        /// <summary>
        /// Захватывает одно разрешение. Если разрешений нет — блокирует поток.
        /// </summary>
        public void Wait()
        {
            // Вход в lock 
            lock (_lock)
            {
                // Пока разрешений нет, поток ждёт сигнала.
                // Цикл while защищает от ложных пробуждений (spurious wakeups).
                while (_currentCount == 0)
                {
                    // Monitor.Wait освобождает блокировку _lock на время ожидания.
                    // Когда другой поток вызовет Monitor.Pulse, текущий поток
                    // проснётся и снова попытается захватить блокировку.
                    Monitor.Wait(_lock);
                }

                // Разрешение доступно — уменьшаем счётчик.
                _currentCount--;
            }
        }

        /// <summary>
        /// Пытается захватить разрешение с таймаутом.
        /// </summary>
        /// <param name="millisecondsTimeout">Таймаут в миллисекундах. -1 — бесконечное ожидание.</param>
        /// <returns>true, если разрешение получено; иначе false.</returns>
        public bool Wait(int millisecondsTimeout)
        {
            lock (_lock)
            {
                // Быстрый путь: разрешение уже есть.
                if (_currentCount > 0)
                {
                    _currentCount--;
                    return true;
                }

                // Бесконечное ожидание.
                if (millisecondsTimeout < 0)
                {
                    while (_currentCount == 0)
                    {
                        Monitor.Wait(_lock);
                    }
                    _currentCount--;
                    return true;
                }

                // Ожидание с таймаутом.
                DateTime deadline = DateTime.UtcNow.AddMilliseconds(millisecondsTimeout);

                while (_currentCount == 0)
                {
                    TimeSpan remaining = deadline - DateTime.UtcNow;
                    if (remaining <= TimeSpan.Zero)
                    {
                        // Время вышло, разрешение не получено.
                        return false;
                    }

                    // Monitor.Wait с таймаутом. Если время вышло, он вернёт false,
                    // но мы всё равно проверим условие в цикле.
                    Monitor.Wait(_lock, remaining);
                }

                _currentCount--;
                return true;
            }
        }

        /// <summary>
        /// Освобождает одно разрешение и пробуждает один ожидающий поток.
        /// </summary>
        public void Release()
        {
            lock (_lock)
            {
                // Нельзя освободить больше разрешений, чем было захвачено.
                if (_currentCount >= _maxCount)
                {
                    throw new InvalidOperationException(
                        "Семафор переполнен: освобождено больше разрешений, чем было захвачено.");
                }

                _currentCount++;

                // Пробуждаем один поток, ожидающий в Wait().
                // Pulse (а не PulseAll) эффективнее, так как освободилось только одно разрешение.
                Monitor.Pulse(_lock);
            }
        }

        /// <summary>
        /// Текущее количество доступных разрешений (для отладки).
        /// </summary>
        public int CurrentCount
        {
            get
            {
                lock (_lock)
                {
                    return _currentCount;
                }
            }
        }
    }
}
