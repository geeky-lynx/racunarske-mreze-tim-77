using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Client
{
    public class Client
    {
        private static bool isLoggedIn = false;
        private static bool isRunning = true;
        private static Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        private static IPEndPoint destinationEp = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 50_001);
        private static EndPoint receiverEp = new IPEndPoint(IPAddress.None, 0);
        private static string loggedInUsername = "";
        private static List<Process> pendingProcesses = new List<Process>();


        public static void Main(string[] args)
        {
            Console.WriteLine("[Client]: Hello, World!");


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
                    Console.Write($"{loggedInUsername}> ");
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
                        CommandLogin(command);
                    else if (commandName.Equals("logout"))
                        CommandLogout(command);
                    else if (commandName.Equals("spawn"))
                        CommandSpawn(command);
                    else if (commandName.Equals("sched"))
                        CommandSched(command);
                    else if (commandName.Equals("start"))
                        CommandStart(command);
                    else if (commandName.Equals("stop"))
                        CommandStop(command);
                    else if (commandName.Equals("exit"))
                        CommandExit(command);
                    else
                        Console.WriteLine($"Command \'{commandName}\' doesn\'t exist");
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



        private static void CommandLogin(string command)
        {
            if (isLoggedIn)
            {
                Console.WriteLine("Already logged in. Try other command");
                return;
            }

            string[] parts = command.Split(' ');
            if (parts.Length != 3)
            {
                Console.WriteLine($"Error: `login` command expexts 2 arguments, but {parts.Length - 1} is given");
                Console.WriteLine("Info: Format: `login (username) (password)`");
                return;
            }

            byte[] msg = Encoding.UTF8.GetBytes(command);

            byte[] responseByte = new byte[1024];
            int bytesCount = -1;
            if (!isLoggedIn)
                bytesCount = clientSocket.SendTo(msg, msg.Length, SocketFlags.None, destinationEp);
            else
                bytesCount = clientSocket.Send(msg);
            Console.WriteLine($"Sent {bytesCount} bytes");

            int receivedBytes = clientSocket.ReceiveFrom(responseByte, 1024, SocketFlags.None, ref receiverEp);
            string received = Encoding.UTF8.GetString(responseByte);

            Console.WriteLine($"Got a message (len = {receivedBytes}) from {receiverEp}.");
            Console.WriteLine($"Message received:");
            Console.WriteLine(received);

            if (!received.Contains("Success"))
            {
                Console.WriteLine("Server returned error message.");
                return;
            }

            int port = -1;
            bool portSuccess = int.TryParse(received.Split('=')[1], out port);
            if (!portSuccess)
            {
                Console.WriteLine("Failed to parse the received port.");
                return;
            }

            // Connect to the new given Tcp socket (with a new port)
            clientSocket.Close();
            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            destinationEp.Port = port;
            clientSocket.Connect(destinationEp);
            isLoggedIn = true;
            loggedInUsername = parts[1];
        }



        private static void CommandLogout(string command)
        {
            if (!isLoggedIn)
            {
                Console.WriteLine("[Client]: User is not logged in. Skip");
                return;
            }
            clientSocket.Close();
        }



        private static void CommandSpawn(string command)
        {
            if (!isLoggedIn)
            {
                Console.WriteLine("[Client]: User is not logged in. Skip");
                return;
            }

            string[] parts = command.Split(' ');
            // TODO: Finish the function
        }



        private static void CommandSched(string command)
        {
            // TODO
        }



        private static void CommandStart(string command)
        {
            // TODO
        }



        private static void CommandStop(string command)
        {
            // TODO
        }



        private static void CommandExit(string command)
        {
            // TODO
        }



        // Used when logging out or exiting the program
        private static string PreparePendingProcessesForSending()
        {
            // TODO
            return "";
        }
    }
}
