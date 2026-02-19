using Common.Models;
//using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Server
{
    public class Server
    {
        private static Dictionary<string, string> registeredUsers = new Dictionary<string, string>();
        private static Socket loginSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        private static Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private static Socket userSocket;


        private static int roundRobinIndex = 0;
        private const int QUANT = 3; // seconds
        private static int serverPort = 64_000;
        private static EndPoint senderEp = new IPEndPoint(IPAddress.Any, 0);
        private static IPEndPoint loginEp = new IPEndPoint(IPAddress.Any, 50_001);
        private static IPEndPoint userEp = new IPEndPoint(IPAddress.Any, serverPort);

        private static List<Process> processes = new List<Process>();
        private static bool isRunning = true;
        private static bool isUserConnected = false;
        private static double cpuUsage = 0.0,
                              ramUsage = 0.0;
        private static byte[]  receiveBuffer = new byte[1024];

        private static SchedulerMode schedMode = SchedulerMode.NONE;
        private static bool schedulerRunning = true;
        private static bool engineRunning = true;
        private static Mutex mutex = new Mutex();



        public static void Main(string[] args)
        {
            // Initialize available users
            registeredUsers.Add("bale32", "qwertyzaz");
            registeredUsers.Add("mile", "12341234");
            registeredUsers.Add("admin", "admin123");

            Console.WriteLine("[Server]: Hello, World!");

            Thread schedulerEngine = new Thread(new ThreadStart(SchedulerEngine));
            schedulerEngine.Start();

            loginSocket.Bind(loginEp);

            Console.WriteLine($"Server is listening on: {loginEp}");



            while (isRunning)
            {
                try
                {
                    int byteCount;
                    EndPoint _ep;
                    if (isUserConnected == false)
                    {
                        _ep = senderEp;
                        byteCount = loginSocket.ReceiveFrom(receiveBuffer, ref senderEp);
                    }
                    else
                    {
                        _ep = userEp;
                        byteCount = userSocket.Receive(receiveBuffer);
                    }
                    string message = Encoding.UTF8.GetString(receiveBuffer, 0, byteCount);
                    Console.WriteLine($"Received message (length = {byteCount}) from {_ep}.");
                    Console.WriteLine($"Message: {message}");

                    //byte[] sendBuffer = new byte[1024];
                    string[] parts = message.Split(' ');

                    if (parts.Length < 1)
                    {
                        Console.WriteLine("Error: Empty command is sent");
                    }

                    string commandName = parts[0].ToLower();
                    string[] parameters = parts.Skip(1).ToArray();


                    // Execute commands
                    if (commandName.Equals("login"))
                        CommandLogin(parameters);

                    else if (commandName.Equals("logout"))
                        CommandLogout();

                    else if (commandName.Equals("list"))
                        CommandList();

                    else if (commandName.Equals("spawn"))
                        CommandSpawn(parameters);

                    else if (commandName.Equals("sched"))
                        CommandSched(parameters);

                    else if (commandName.Equals("start"))
                        CommandStart(parameters);

                    else if (commandName.Equals("stop"))
                        CommandStop(parameters);

                    else if (commandName.Equals("exit"))
                        isRunning = false;

                    else
                        CommandNotFound(commandName);

                    //int byteCount2 = loginSocket.SendTo(sendBuffer, ref senderEp);
                }
                catch (SocketException exception)
                {
                    Console.WriteLine("SocketException: Something happened...");
                    Console.WriteLine(exception.ToString());
                }
            }



            // Server end
            Console.WriteLine("Server has finished");
            registeredUsers.Clear();
            loginSocket.Close();
            //serverSocket.Close();
            Console.ReadKey();
        }



        private static void CommandLogin(string[] parameters)
        {
            if (isUserConnected)
            {
                Console.WriteLine("User is already logged in. Skip this command");
                return;
            }

            if (parameters.Length != 2)
            {
                string _msg = $"Error: Login command requires 2 parameters, but {parameters.Length} is given instead";
                Console.WriteLine(_msg);
                loginSocket.SendTo(Encoding.UTF8.GetBytes(_msg), senderEp);
                return;
            }

            string inputUsername = parameters[0];
            string inputPassword = parameters[1];
            string password = string.Empty;
            bool isUserInDatabase = registeredUsers.TryGetValue(inputUsername, out password);
            string msg;

            if (isUserInDatabase == false)
            {
                msg = $"Error: No user registered as: {inputUsername}";
            }
            else if (!inputPassword.Equals(password))
            {
                msg = "Error: Incorrect password";
            }
            else
            {
                msg = $"Success: TcpSocket={64000}";
            }

            Console.WriteLine(msg);
            byte[] toSend = Encoding.UTF8.GetBytes(msg);
            _ = loginSocket.SendTo(toSend, senderEp);

            if (!isUserInDatabase || !inputPassword.Equals(password))
                return;


            serverSocket.Bind(userEp);
            serverSocket.Listen();
            userSocket = serverSocket.Accept();
            isUserConnected = true;
        }



        private static void CommandLogout()
        {
            if (!isUserConnected)
            {
                Console.WriteLine("[Server]: No user is logged in");
                return;
            }
            Console.WriteLine("[Server]: User is logging out");
            userSocket.Close();
            isUserConnected = false;
        }



        private static void CommandList()
        {
            if (!isUserConnected)
            {
                Console.WriteLine("[Server]: User is not logged in. Skip");
                return;
            }

            Console.WriteLine("[Server]: Sending list of processes");

            string msg = PackProcessInfoForSending();
            byte[] toSend = Encoding.UTF8.GetBytes(msg);
            _ = userSocket.Send(toSend);
        }



        private static void CommandSpawn(string[] parameters)
        {
            // TODO
            if (!isUserConnected)
            {
                Console.WriteLine("[Server]: User is not logged in. Skip");
                return;
            }


            if (parameters.Length != 5)
            {
                Console.WriteLine($"[Server]: `spawn` command expects 5 arguments, but {parameters.Length} is given");
                return;
            }

            // Validate parameters
            string name = parameters[0]; // Just use it
            int execTime = -1;
            int prio = -1;
            double cpu = -1.0;
            double ram = -1.0;

            if (!int.TryParse(parameters[1], out execTime))
            {
                Console.WriteLine($"[Server]: `execTime` must be an int value, but \'{parameters[1]}\' is given");
                return;
            }

            if (!int.TryParse(parameters[2], out prio))
            {
                Console.WriteLine($"[Server]: `priority` must be an int value, but \'{parameters[2]}\' is given");
                return;
            }

            if (!double.TryParse(parameters[3], out cpu))
            {
                Console.WriteLine($"[Server]: `cpuUsage` must be an float value, but \'{parameters[3]}\' is given");
                return;
            }

            if (!double.TryParse(parameters[4], out ram))
            {
                Console.WriteLine($"[Server]: `ramUsage` must be an float value, but \'{parameters[4]}\' is given");
                return;
            }

            string msg;
            if (GetTotalCpuUsage() + cpu > 1.0 || GetTotalMemoryUsage() + ram > 1.0)
            {
                msg = $"Error: Could not spawn a new process. At the moment, CPU usage is at {cpu * 100}%, and Memory usage is at {ram * 100}%";
            }
            else
            {
                msg = "Success: Spawned a process and added to the queue";
            }

            lock (mutex)
            {
                processes.Add(new Process(name, execTime, prio, cpu, ram));
                Monitor.Pulse(mutex);
            }
            Console.WriteLine($"[Server]: {msg}");
            byte[] toSend = Encoding.UTF8.GetBytes(msg);
            _ = userSocket.Send(toSend);
        }



        private static void CommandSched(string[] parameters)
        {
            // TODO
        }



        private static void CommandStart(string[] parameters)
        {
            // TODO
        }



        private static void CommandStop(string[] parameters)
        {
            // TODO
        }



        private static void CommandNotFound(string command)
        {
            string _msg = $"Error: Command \'{command}\' is not found";
            Console.WriteLine(_msg);
            loginSocket.SendTo(Encoding.UTF8.GetBytes(_msg), senderEp);
        }



        private static double GetTotalCpuUsage()
        {
            double cpuUsage = 0;
            foreach (var process in processes)
                cpuUsage += process.CpuUsage;
            return cpuUsage;
        }



        private static double GetTotalMemoryUsage()
        {
            double memoryUsage = 0;
            foreach (var process in processes)
                memoryUsage += process.MemoryUsage;
            return memoryUsage;
        }



        private static string PackProcessInfoForSending()
        {
            if (processes.Count <= 0)
                return "";
            string infos = $"{processes[0].Name}:{processes[0].ExecutionTime}:{processes[0].Priority}:{processes[0].CpuUsage}:{processes[0].MemoryUsage}";
            foreach (var process in processes.Skip(1))
                infos += $",{process.Name}:{process.ExecutionTime}:{process.Priority}:{process.CpuUsage}:{process.MemoryUsage}";
            return infos;
        }



        private static void SchedulerEngine()
        {
            Console.WriteLine("[SCHEDULER]: Hello, World!");

            int _processIndex = -1;
            while (engineRunning)
            {
                lock (mutex)
                {
                    // Pick the next available 
                    while (schedulerRunning == false || processes.Count < 0)
                        Monitor.Wait(mutex);

                    if (schedMode == SchedulerMode.ROUND_ROBIN)
                        _processIndex = GetNextForRoundRobin(_processIndex);
                    // else if (schedMode == SchedulerMode.SHORTEST_FIRST
                    //      _processIndex = GetNextForShortestFirst()
                }

                Process info = processes[_processIndex];
                string modeName = schedMode == SchedulerMode.ROUND_ROBIN ? "Round Robin" : "Shortest First";
                Console.WriteLine($"[SCHEDULER]: Scheduler Mode: ${modeName}");
                Console.WriteLine($"[SCHEDULER]: Doing work of process with: Name = {info.Name}, Execution Time = {info.ExecutionTime}, Priority = {info.Priority}, CPU Usage = {info.CpuUsage}, Memory Usage = {info.MemoryUsage}");

                if (schedMode == SchedulerMode.ROUND_ROBIN)
                    DoRoundRobin(_processIndex);
                // else if (schedMode == SchedulerMode.SHORTEST_FIRST
                //      DoShortestFirst(_processIndex)
            }
        }



        private static void DoRoundRobin(int processIndex)
        {
            //
            int execTime = processes[processIndex].ExecutionTime;
            int amountToSleep = int.Min(QUANT, execTime);
            Thread.Sleep(amountToSleep);
            if (execTime < QUANT)
            {
                Console.WriteLine($"[SCHEDULER]: The process with name \'{processes[processIndex]}\' has finished. Removing from queue");
                lock (mutex)
                {
                    processes.RemoveAt(processIndex);
                }
            }
            else
            {
                Console.WriteLine($"[SCHEDULER]: The process with name \'{processes[processIndex]}\' has done its turn. Now it\'s paused, waiting its turn again");
                lock (mutex)
                {
                    processes[processIndex].ExecutionTime -= execTime;
                }
            }
        }



        private static int GetNextForRoundRobin(int processIndex)
        {
            return (processIndex + 1) % processes.Count;
        }
    }
}
