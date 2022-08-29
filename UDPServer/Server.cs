using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Configuration;
using System.Globalization;

namespace UDPServ
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Random random = new Random();

            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            var broadcastIP = ConfigurationSettings.AppSettings["broadcastIP"];
            IPAddress broadcast = IPAddress.Parse(broadcastIP);

            var frameStr = ConfigurationSettings.AppSettings["frame"];
            int frame = int.Parse(frameStr);

            var emulLostLevelStr = ConfigurationSettings.AppSettings["emulLostLevel"];
            double emulLostLevel = double.Parse(emulLostLevelStr, CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en"));
            
            int n = 0;

            int lostN = 0;

            Console.WriteLine($"Console App settings: broadcast={broadcastIP},frame={frame},emulLostLevel={emulLostLevel}");

            while (true)
            {
                double price = Math.Round(random.NextDouble(), 1);
              
                byte[] sendbuf = Encoding.ASCII.GetBytes(n + " " + price.ToString());
 
                IPEndPoint ep = new IPEndPoint(broadcast, 11000);

                if  (price < emulLostLevel) //emulLostLevel = 2, no lost packets
                {
                    s.SendTo(sendbuf, ep);
                    Console.WriteLine($"sent: N={n}, price={price}");
                }
                else 
                {
                    Console.WriteLine($"-lost packet: N={n}, price={price}");
                    lostN++;
                }

                if (++n == frame)
                {                   
                    Console.WriteLine($"***packet frame sent, Lost total={lostN}");
                    n = 0;
                    lostN = 0;
                }

                Task.Delay(1000).Wait();
            }
        }
    }
}
