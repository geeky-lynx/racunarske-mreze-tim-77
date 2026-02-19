using Common.Models;
using System;
//using System.Diagnostics;
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
            Console.WriteLine("\tlist");
            Console.WriteLine("\tspawn (name) (execTime) (priority) (cpuUage) (memoryUsage)");
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
                    else if (commandName.Equals("list"))
                        CommandList(command);
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



        private static void CommandList(string command)
        {
            if (!isLoggedIn)
            {
                Console.WriteLine("[Client]: User is not logged in. Skip");
                return;
            }

            string[] parts = command.Split(' ');

            if (parts.Length != 1)
            {
                Console.WriteLine($"[Client]: `list` command expects 1 argument, but {parts.Length - 1} is given");
                return;
            }

            byte[] msg = Encoding.UTF8.GetBytes(command);
            int bytesCount = clientSocket.Send(msg);


            Console.WriteLine($"[Client]: Sent {bytesCount} bytes");

            byte[] responseBytes = new byte[1024];
            int receivedBytes = clientSocket.Receive(responseBytes, 1024, SocketFlags.None);
            string received = Encoding.UTF8.GetString(responseBytes);

            Console.WriteLine($"[Client]: Got a message (len = {receivedBytes}).");
            Console.WriteLine($"[Client]: Message received:");
            Console.WriteLine(received);

            string[] list = received.Split(',');
            for (int i = 0; i < list.Length; i++)
            {
                string[] fields = list[i].Split(':');
                Console.WriteLine($"  #{i}: Name = {fields[0]}, Execution Time = {fields[1]}, Priority = {fields[2]}, CPU Usage = {fields[3]}, Memory Usage = {fields[4]}");
            }
        }



        private static void CommandSpawn(string command)
        {
            if (!isLoggedIn)
            {
                Console.WriteLine("[Client]: User is not logged in. Skip");
                return;
            }

            string[] parts = command.Split(' ');

            if (parts.Length != 6)
            {
                Console.WriteLine($"[Client]: `spawn` command expects 5 arguments, but {parts.Length - 1} is given");
                return;
            }

            // Validate parameters
            string name = parts[1]; // Just use it
            int execTime = -1;
            int prio = -1;
            double cpu = -1.0;
            double ram = -1.0;

            if (!int.TryParse(parts[2], out execTime))
            {
                Console.WriteLine($"[Client]: `execTime` must be an int value, but \'{parts[2]}\' is given");
                return;
            }

            if (!int.TryParse(parts[3], out prio))
            {
                Console.WriteLine($"[Client]: `priority` must be an int value, but \'{parts[3]}\' is given");
                return;
            }

            if (!double.TryParse(parts[4], out cpu))
            {
                Console.WriteLine($"[Client]: `cpuUsage` must be an float value, but \'{parts[4]}\' is given");
                return;
            }

            if (!double.TryParse(parts[5], out ram))
            {
                Console.WriteLine($"[Client]: `ramUsage` must be an float value, but \'{parts[5]}\' is given");
                return;
            }

            byte[] msg = Encoding.UTF8.GetBytes(command);
            int bytesCount = clientSocket.Send(msg);


            Console.WriteLine($"[Client]: Sent {bytesCount} bytes");

            byte[] responseBytes = new byte[1024];
            int receivedBytes = clientSocket.Receive(responseBytes, 1024, SocketFlags.None);
            string received = Encoding.UTF8.GetString(responseBytes);

            Console.WriteLine($"[Client]: Got a message (len = {receivedBytes}).");
            Console.WriteLine($"[Client]: Message received:");
            Console.WriteLine(received);

            if (!received.Contains("Success"))
            {
                Console.WriteLine("[Client]: Server returned error message.");
                pendingProcesses.Add(new Process(name, execTime, prio, cpu, ram));
            }
            else
            {
                Console.WriteLine("[Client]: Server has successfully spawned a process");
            }
        }



        private static void CommandSched(string command)
        {
            if (!isLoggedIn)
            {
                Console.WriteLine("[Client]: User is not logged in. Skip");
                return;
            }

            string[] parts = command.Split(' ');

            if (parts.Length != 2)
            {
                Console.WriteLine($"[Client]: `sched` command expects 1 argument, but {parts.Length - 1} is given");
                return;
            }

            string alg = parts[1].ToLower();
            if (!alg.Equals("roundrobin") || !alg.Equals("shortestfirst"))
            {
                Console.WriteLine($"[Client]: Argument should be either \"roundrobin\" or \"shortestfirst\", but \"{alg}\" is given");
                return;
            }

            byte[] msg = Encoding.UTF8.GetBytes(command);
            int bytesCount = clientSocket.Send(msg);
            Console.WriteLine($"[Client]: Sent {bytesCount} bytes");

            byte[] responseBytes = new byte[1024];
            int receivedBytes = clientSocket.Receive(responseBytes, 1024, SocketFlags.None);
            string received = Encoding.UTF8.GetString(responseBytes);

            Console.WriteLine($"[Client]: Got a message (len = {receivedBytes}).");
            Console.WriteLine($"[Client]: Message received:");
            Console.WriteLine(received);

            if (!received.Contains("Success"))
            {
                Console.WriteLine("[Client]: Server returned error message.");
            }
        }



        private static void CommandStart(string command)
        {
            if (!isLoggedIn)
            {
                Console.WriteLine("[Client]: User is not logged in. Skip");
                return;
            }

            string[] parts = command.Split(' ');

            if (parts.Length != 1)
            {
                Console.WriteLine($"[Client]: `start` command expects 0 arguments, but {parts.Length - 1} is given");
                return;
            }


            byte[] msg = Encoding.UTF8.GetBytes(command);
            int bytesCount = clientSocket.Send(msg);
            Console.WriteLine($"[Client]: Sent {bytesCount} bytes");

            byte[] responseBytes = new byte[1024];
            int receivedBytes = clientSocket.Receive(responseBytes, 1024, SocketFlags.None);
            string received = Encoding.UTF8.GetString(responseBytes);

            Console.WriteLine($"[Client]: Got a message (len = {receivedBytes}).");
            Console.WriteLine($"[Client]: Message received:");
            Console.WriteLine(received);

            if (!received.Contains("Success"))
            {
                Console.WriteLine("[Client]: Server returned error message.");
            }
        }



        private static void CommandStop(string command)
        {
            if (!isLoggedIn)
            {
                Console.WriteLine("[Client]: User is not logged in. Skip");
                return;
            }

            string[] parts = command.Split(' ');

            if (parts.Length != 1)
            {
                Console.WriteLine($"[Client]: `stop` command expects 0 arguments, but {parts.Length - 1} is given");
                return;
            }


            byte[] msg = Encoding.UTF8.GetBytes(command);
            int bytesCount = clientSocket.Send(msg);
            Console.WriteLine($"[Client]: Sent {bytesCount} bytes");

            byte[] responseBytes = new byte[1024];
            int receivedBytes = clientSocket.Receive(responseBytes, 1024, SocketFlags.None);
            string received = Encoding.UTF8.GetString(responseBytes);

            Console.WriteLine($"[Client]: Got a message (len = {receivedBytes}).");
            Console.WriteLine($"[Client]: Message received:");
            Console.WriteLine(received);

            if (!received.Contains("Success"))
            {
                Console.WriteLine("[Client]: Server returned error message.");
            }
        }



        private static void CommandExit(string command)
        {
            if (isLoggedIn)
            {
                Console.WriteLine("[Client]: Logging out...");
                CommandLogout(command);
            }
            isRunning = false;
        }



        // Used when logging out or exiting the program
        private static string PreparePendingProcessesForSending()
        {
            if (pendingProcesses.Count <= 0)
                return "";
            var first = pendingProcesses.First();
            string infos = $"{first.Name}:{first.ExecutionTime}:{first.Priority}:{first.CpuUsage}:{first.MemoryUsage}";
            foreach (var process in pendingProcesses.Skip(1))
                infos += $",{process.Name}:{process.ExecutionTime}:{process.Priority}:{process.CpuUsage}:{process.MemoryUsage}";
            return infos;
        }
    }
}
