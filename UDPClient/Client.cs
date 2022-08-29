using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Net.Sockets;

namespace UDPServer
{
    using System;
    using System.Configuration;
    using System.Globalization;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;

    public class UDPServer
    {
        private static int listenPort = 0;
        private static int statInterval = 0;
        private static int frameLength = 0;

        static SynchronizedCollection<double> list = new SynchronizedCollection<double>();

        Random random = new Random();

        public static async Task Main(string[] args)
        {
            var listenPortStr = ConfigurationSettings.AppSettings["listenPort"];
            listenPort = int.Parse(listenPortStr);

            var statIntervalStr = ConfigurationSettings.AppSettings["statInterval"];
            statInterval = int.Parse(statIntervalStr);

            var frameLengthStr = ConfigurationSettings.AppSettings["frameLength"];
            frameLength = int.Parse(frameLengthStr);

            Console.WriteLine($"Server App settings: listenPort={listenPort},statInterval={statInterval},frameLength={frameLength}");

            var tasks = new List<Task>()
            {
                StartListener(),
                CalcStat(statInterval)
            };

            try
            {
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error = {ex.Message}");
            }
        }

        private async static Task StartListener()
        {
            UdpClient listener = new UdpClient(listenPort);

            var listenIP = ConfigurationSettings.AppSettings["listenIP"];

            IPAddress lis = IPAddress.Parse(listenIP);

            IPEndPoint groupEP = new IPEndPoint(lis, listenPort);

            double[] frameList = Enumerable.Repeat<double>(-1, frameLength).ToArray<double>();

            try
            {
                Console.WriteLine("Waiting for receiving broadcast...");

                while (true)
                {
                    var receivedResult = await listener.ReceiveAsync();

                    string packet = Encoding.ASCII.GetString(receivedResult.Buffer);

                    string[] data = packet.Split(' ');

                    if (data.Count() == 2)
                    {                      
                        int num = int.Parse(data[0]);

                        double price = double.Parse(data[1], CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en"));

                        list.Add(price);

                        if (frameList[num] == -1)
                        {
                            frameList[num] = price;                            
                            Console.WriteLine($"received: N={num}, price={price}");
                        }

                        if (num == frameLength - 1)
                        {
                            var lostPackets = frameList.Select((value, index) => new { value, index })
                                              .Where(z => z.value == -1)
                                              .Select(z => z.index);

                            int lostN = 0;

                            foreach (int p in lostPackets)
                            {                               
                                Console.WriteLine($"-missed packet: N={p}");
                                lostN++;
                            }

                            Console.WriteLine($"***packet frame received, Missed total={lostN}");

                            frameList = Enumerable.Repeat<double>(-1, frameLength).ToArray<double>();
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Errored packet: {packet}");
                    }
                }
            }
            catch (SocketException se)
            {
                Console.WriteLine(se);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static async Task CalcStat(int interval)
        {
            Console.WriteLine("<After press Enter key to calc Stat>");

            while (true)
            {
                int period = interval;

                ConsoleKeyInfo info = await Task<ConsoleKeyInfo>.Run(() => Console.ReadKey());

                if (info.Key == ConsoleKey.Enter)
                {
                    int listLength = list.Count;

                    if (listLength < period)
                    {
                        period = listLength;
                    }

                    Console.WriteLine($"CALCULATED STAT INTERVAL = {period}: ");

                    List<double> listForStat = new List<double>();

                    for (int i = listLength - period; i <= listLength - 1; i++)
                    {
                        listForStat.Add(list[i]);
                    }

                    listForStat.Sort();

                    var statTasks = new List<Task>()
                    {
                        Task.Run(()=>CalcMean(listForStat)),
                        Task.Run(()=>CalcStdDev(listForStat)),
                        Task.Run(()=>CalcModa(listForStat)),
                        Task.Run(()=>CalcMedian(listForStat))
                    };

                    try
                    {
                        await Task.WhenAll(statTasks);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error = {ex.Message}");
                    }

                    /*
                    //Calc Mean
                    double meanAr = CalcMean(listForStat);
                    Console.WriteLine($"MeanAr = {meanAr}");

                    //Calc Stddev
                    double stdDev = CalcStdDev(listForStat);
                    Console.WriteLine($"StdDev = {stdDev}");

                    //Calc moda
                    double moda = CalcModa(listForStat);
                    Console.WriteLine($"Moda = {moda}");

                    //Calc mediana
                    double median = CalcMedian(listForStat);
                    Console.WriteLine($"Median = {median}");
                    */
                }
            }
        }

        private static double CalcMean(List<double> nList)
        {
            double meanAr = nList.Average();
            Console.WriteLine($"MeanAr = {meanAr}");

            return meanAr;
        }

        private static double CalcStdDev(List<double> nList)
        {
            double meanAr = CalcMean(nList);

            double deltaQsum = 0;

            foreach (double price in nList)
            {
                double delta = price - meanAr;

                double deltaQ = delta * delta;

                deltaQsum += deltaQ;
            }

            double stdDev = Math.Sqrt(deltaQsum / (nList.Count() - 1));            
            Console.WriteLine($"StdDev = {stdDev}");

            return stdDev;
        }

        private static double CalcModa(List<double> nList)
        {
            var moda = nList.GroupBy(i => i).OrderBy(i => i.Count()).Last().Key;
            Console.WriteLine($"Moda = {moda}");

            return moda;
        }

        private static double CalcMedian(List<double> nList)
        {
            int length = nList.Count;
            int mod = length % 2;
            double median = 0;

            if (mod == 0)
            {
                median = (nList[length / 2] + nList[length / 2 + 1]) / 2;
            }
            else
            {
                median = nList[length / 2];
            }

            Console.WriteLine($"Median = {median}");

            return median;
        }
    }
}
