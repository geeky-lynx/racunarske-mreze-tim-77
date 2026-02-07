using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Client
{
    public class Client
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("[Client]: Hello, World!");

            Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint destinationEp = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 50_001);
            EndPoint receiverEp = new IPEndPoint(IPAddress.None, 0);

            Console.WriteLine("Commands:");
            Console.WriteLine("\tlogin (username) (password)");
            Console.WriteLine("\texit");


            string command = Console.ReadLine();
            byte[] msg = Encoding.UTF8.GetBytes(command);



            try
            {
                byte[] responseByte = new byte[1024];
                int bytesCount = clientSocket.SendTo(msg, msg.Length, SocketFlags.None, destinationEp);
                Console.WriteLine($"Sent {bytesCount} bytes");
                int receivedBytes = clientSocket.ReceiveFrom(responseByte, ref receiverEp);
                string received = Encoding.UTF8.GetString(responseByte);
                Console.WriteLine($"Got a message (len = {receivedBytes}) from {receiverEp}.");
                Console.WriteLine($"Message: {received}");
            }
            catch (SocketException exception)
            {
                Console.WriteLine("SocketException: Something happened...");
                Console.WriteLine(exception.ToString());
            }



            Console.WriteLine("Client has finished");
            clientSocket.Close();
            Console.ReadKey();
        }
    }
}
