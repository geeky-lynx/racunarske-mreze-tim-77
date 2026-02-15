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
            bool isRunning = true;
            bool isLoggedIn = false;

            Console.WriteLine("Commands:");
            Console.WriteLine("\tlogin (username) (password)");
            Console.WriteLine("\tlogout");
            Console.WriteLine("\tspawn (name) (exec-time) [priority] [cpuUage] [memoryUsage]");
            Console.WriteLine("\tsched <roundrobin>/<shortestfirst>");
            Console.WriteLine("\tstart");
            Console.WriteLine("\tstop");
            Console.WriteLine("\texit");



            while (isRunning)
            {
                try
                {
                    Console.Write("> ");
                    string? command = Console.ReadLine();

                    // Validate user input
                    if (command == null)
                    {
                        Console.WriteLine("Error: User input is null");
                        continue;
                    }

                    string[] parts = command.Split(' ');
                    if (parts.Length < 1)
                    {
                        Console.WriteLine("Error: User input is empty");
                        continue;
                    }

                    string commandName = parts[0].ToLower();
                    if (commandName.Equals("login"))
                    {
                        byte[] msg = Encoding.UTF8.GetBytes(command);

                        byte[] responseByte = new byte[1024];
                        int bytesCount = clientSocket.SendTo(msg, msg.Length, SocketFlags.None, destinationEp);
                        Console.WriteLine($"Sent {bytesCount} bytes");

                        int receivedBytes = clientSocket.ReceiveFrom(responseByte, 1024, SocketFlags.None, ref receiverEp);
                        string received = Encoding.UTF8.GetString(responseByte);

                        Console.WriteLine($"Got a message (len = {receivedBytes}) from {receiverEp}.");
                        Console.WriteLine($"Message received:");
                        Console.WriteLine(received);

                        if (!received.Contains("Success"))
                        {
                            Console.WriteLine("Server returned error message.");
                            continue;
                        }

                        int port = -1;
                        bool portSuccess = int.TryParse(received.Split('=')[1], out port);
                        if (!portSuccess)
                        {
                            Console.WriteLine("Failed to parse the received port.");
                            continue;
                        }

                        // Connect to the new given Tcp socket (with a new port)
                        //clientSocket.Close();
                        //clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        //destinationEp.Port = port;
                        //clientSocket.Connect(destinationEp);
                        //isLoggedIn = true;
                    }
                    else
                    {
                        Console.WriteLine($"Command \'{commandName}\' doesn\'t exist");
                    }

                }
                catch (SocketException exception)
                {
                    Console.WriteLine("SocketException: Something happened...");
                    Console.WriteLine(exception.ToString());
                }
            }



            Console.WriteLine("Client has finished");
            clientSocket.Close();
            Console.ReadKey();
        }



        //private static bool ValidateUserInput(string? input)
        //{
        //    if (input == null)
        //    {
        //        Console.WriteLine("Error: User input is null");
        //        return false;
        //    }

        //    string[] parts = input.Split(' ');
        //    if (parts.Length < 1)
        //    {
        //        Console.WriteLine("Error: User input is empty");
        //        return false;
        //    }
        //    return true;
        //}
    }
}
