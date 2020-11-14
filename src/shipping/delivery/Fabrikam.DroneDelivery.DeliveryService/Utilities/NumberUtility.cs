using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fabrikam.DroneDelivery.DeliveryService.Utilities
{
    public class NumberUtility
    {
        public static void GetPrimeNumbers()
        {
            var threadList = new List<Thread>();
            for (int i = 0; i < Environment.ProcessorCount; i++)
            {
                var th = new Thread(() =>
                {
                    FindPrimeNumber(20000);
                });
                th.Start();
                threadList.Add(th);
            }

            Thread.Sleep(10);

            do
            {
                Thread.Sleep(10);
            } while (threadList.Any(t => t.ThreadState == System.Threading.ThreadState.Running));
        }

        public static long FindPrimeNumber(int n)
        {
            int count = 0;
            long a = 2;
            while (count < n)
            {
                long b = 2;
                int primeNumber = 1;
                while (b * b <= a)
                {
                    if (a % b == 0)
                    {
                        primeNumber = 0;
                        break;
                    }
                    b++;
                }
                if (primeNumber > 0)
                {
                    count++;
                }
                a++;
            }
            return (--a);
        }
    }
}
