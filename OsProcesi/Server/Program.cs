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
        // private static Socket userSocket;
        private static List<Socket> userSockets = new List<Socket>();
        private static List<Socket> waitingSockets = new List<Socket>();
        private static List<Socket> erroneousSockets = new List<Socket>();


        private const int QUANT = 3; // seconds
        private static int serverPort = 64_000;
        private static EndPoint senderEp = new IPEndPoint(IPAddress.Any, 0);
        private static IPEndPoint loginEp = new IPEndPoint(IPAddress.Any, 50_001);
        private static IPEndPoint userEp = new IPEndPoint(IPAddress.Any, serverPort);

        private static List<Process> processes = new List<Process>();
        private static bool isRunning = true;
        // private static bool isUserConnected = false;
        private static byte[] receiveBuffer = new byte[1024];

        private static SchedulerMode schedMode = SchedulerMode.SHORTEST_FIRST;
        private static bool schedulerRunning = false;
        private static bool engineRunning = true;
        private static Mutex mutex = new Mutex();

        
        private static double maxCpuUsage = 0.0;
        private static double maxRamUsage = 0.0;
        private static Process? shortestProcess = null;



        public static void Main(string[] args)
        {
            // Initialize available users
            registeredUsers.Add("bale32", "qwertyzaz");
            registeredUsers.Add("mile", "12341234");
            registeredUsers.Add("admin", "admin123");
            
            Console.WriteLine("[Server]: Hello, World!");

            Thread schedulerEngine = new Thread(new ThreadStart(SchedulerEngine));
            schedulerEngine.Start();
            
            serverSocket.Bind(userEp);
            loginSocket.Bind(loginEp);
            serverSocket.Blocking = false;
            loginSocket.Blocking = false;

            Console.WriteLine($"Server is listening on: {loginEp}");



            while (isRunning)
            {
                try
                {
                    if (loginSocket.Poll(1_000_000, SelectMode.SelectRead))
                    {   
                        int byteCount = loginSocket.ReceiveFrom(receiveBuffer, ref senderEp);

                        string message = Encoding.UTF8.GetString(receiveBuffer, 0, byteCount);
                        if (byteCount <= 0)
                        {
                            Console.WriteLine($"Error: Empty command is sent by {senderEp}");
                            continue;
                        }

                        Console.WriteLine($"Received message (length = {byteCount}) from {senderEp}.");
                        Console.WriteLine($"Message: {message}");

                        string[] parts = message.Split(' ');

                        string commandName = parts[0].ToLower();
                        string[] parameters = parts.Skip(1).ToArray();

                        // Execute commands
                        if (commandName.Equals("login"))
                            CommandLogin(parameters);
                        else
                            CommandNotFound(loginSocket, commandName);
                    }


                    if (userSockets.Count > 0)
                    {
                        foreach (var _userSocket in userSockets)
                        {
                            waitingSockets.Add(_userSocket);
                            erroneousSockets.Add(_userSocket);
                        }
                        Socket.Select(waitingSockets, null, erroneousSockets, 1_000_000);
                    }

                    if (waitingSockets.Count <= 0)
                        continue;

                    foreach (var waiting in waitingSockets)
                    {
                        int byteCount = waiting.Receive(receiveBuffer);

                        string message = Encoding.UTF8.GetString(receiveBuffer, 0, byteCount);
                        if (byteCount <= 0)
                        {
                            Console.WriteLine($"Error: Empty command is sent by {senderEp}");
                            continue;
                        }

                        Console.WriteLine($"Received message (length = {byteCount}) from {userEp}.");
                        Console.WriteLine($"Message: {message}");

                        string[] parts = message.Split(' ');

                        string commandName = parts[0].ToLower();
                        string[] parameters = parts.Skip(1).ToArray();

                        if (commandName.Equals("logout"))
                            CommandLogout(waiting, parameters);

                        else if (commandName.Equals("list"))
                            CommandList(waiting);

                        else if (commandName.Equals("spawn"))
                            CommandSpawn(waiting, parameters);

                        else if (commandName.Equals("sched"))
                            CommandSched(waiting, parameters);

                        else if (commandName.Equals("start"))
                            CommandStart(waiting);

                        else if (commandName.Equals("stop"))
                            CommandStop(waiting);

                        else if (commandName.Equals("terminate"))
                            CommandTerminate();

                        else
                            CommandNotFound(waiting, commandName);
                    }
                    waitingSockets.Clear();
                    erroneousSockets.Clear();
                }
                catch (SocketException exception)
                {
                    Console.WriteLine("SocketException: Something happened...");
                    Console.WriteLine(exception.ToString());
                }
            }



            // Server end
            schedulerRunning = false;
            engineRunning = false;
            Console.WriteLine("Server has finished");

            
            Console.WriteLine("\n===== SERVER STATISTICS =====");
            Console.WriteLine($"Max CPU usage: {maxCpuUsage * 100}%");
            Console.WriteLine($"Max Memory usage: {maxRamUsage * 100}%");

            if (shortestProcess != null)
            {
                Console.WriteLine("Process with shortest execution time:");
                Console.WriteLine($"Name: {shortestProcess.Name}");
                Console.WriteLine($"Execution Time: {shortestProcess.ExecutionTime}");
                Console.WriteLine($"Priority: {shortestProcess.Priority}");
                Console.WriteLine($"CPU Usage: {shortestProcess.CpuUsage}");
                Console.WriteLine($"Memory Usage: {shortestProcess.MemoryUsage}");
            }

            Console.ReadKey();
            registeredUsers.Clear();
            loginSocket.Close();
            serverSocket.Close();
            foreach (var socket in userSockets)
                socket.Close();
            foreach (var socket in erroneousSockets)
                socket.Close();
        }



        private static void CommandLogin(string[] parameters)
        {
            if (parameters.Length != 2)
            {
                string _msg = $"[Server]: Error: Login command requires 2 parameters, but {parameters.Length} is given instead";
                Console.WriteLine(_msg);
                loginSocket.SendTo(Encoding.UTF8.GetBytes(_msg), senderEp);
                return;
            }

            string inputUsername = parameters[0];
            string inputPassword = parameters[1];
            string? password = string.Empty;
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

            Console.WriteLine($"[Server]: {msg}");
            byte[] toSend = Encoding.UTF8.GetBytes(msg);
            _ = loginSocket.SendTo(toSend, senderEp);

            if (!isUserInDatabase || !inputPassword.Equals(password))
                return;


            
            serverSocket.Listen();
            if (serverSocket.Poll(1_000_000, SelectMode.SelectRead))
            {
                var userSocket = serverSocket.Accept();
                userSocket.Blocking = false;
                userSockets.Add(userSocket);
            }
            else
            {
                Console.WriteLine("[Server]: Something happened while user was connecting (dropped package?)");
            }
            // isUserConnected = true;
        }



        private static void CommandLogout(Socket userSocket, string[] parameters)
        {
            if (parameters.Length != 1)
            {
                Console.WriteLine($"[Server]: `spawn` command expects 1 argument, but {parameters.Length} is given");
                return;
            }

            Console.WriteLine("[Server]: User is logging out");

            string[] list = parameters[0].Split(',');
            Console.WriteLine($"[Server]: Number of pending processes: {list.Length}");
            for (int i = 0; i < list.Length; i++)
            {
                string[] fields = list[i].Split(':');
                string name = fields[0];
                int execTime, prio;
                double cpu, ram;

                if (!int.TryParse(fields[1], out execTime))
                {
                    Console.WriteLine($"[Server]: Warning: Could not parse Execution Time value: {fields[3]}. Skip");
                    continue;
                }

                if (!int.TryParse(fields[2], out prio))
                {
                    Console.WriteLine($"[Server]: Warning: Could not parse Priority value: {fields[3]}. Skip");
                    continue;
                }

                if (!double.TryParse(fields[3], out cpu))
                {
                    Console.WriteLine($"[Server]: Warning: Could not parse CPU Usage value: {fields[3]}. Skip");
                    continue;
                }

                if (!double.TryParse(fields[4], out ram))
                {
                    Console.WriteLine($"[Server]: Warning: Could not parse RAM Usage value: {fields[4]}. Skip");
                    continue;
                }

                double _totalCpuUsage = GetTotalCpuUsage();
                double _totalRamUsage = GetTotalMemoryUsage();
                if (_totalCpuUsage + cpu > 1.0 ||  _totalRamUsage + ram > 1.0)
                {
                    Console.WriteLine($"[Server]: Can\'t push to queue because resources are taken up (CPU = {_totalCpuUsage}%, Memory = {_totalRamUsage}%. Skip");
                    continue;
                }
                lock (mutex)
                {
                    processes.Add(new Process(name, execTime, prio, cpu, ram));
                    Monitor.Pulse(mutex);
                }
            }

            userSocket.Close();
            userSockets.Remove(userSocket);
            // isUserConnected = false;
        }



        private static void CommandList(Socket userSocket)
        {
            Console.WriteLine("[Server]: Sending list of processes");

            string msg = Common.Utilities.PackProcessInfoForSending(processes);
            Console.WriteLine(msg);
            byte[] toSend = Encoding.UTF8.GetBytes(msg);
            _ = userSocket.Send(toSend);
            Console.WriteLine("[Server]: Done sending");
        }



        private static void CommandSpawn(Socket userSocket, string[] parameters)
        {
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
            double _totalCpuUsage = GetTotalCpuUsage();
            double _totalMemoryUsage = GetTotalMemoryUsage();
            if (_totalCpuUsage + cpu > 1.0 || _totalMemoryUsage + ram > 1.0)
            {
                msg = $"Error: Could not spawn a new process. At the moment, CPU usage is at {_totalCpuUsage * 100}%, and Memory usage is at {_totalMemoryUsage * 100}%";
            }
            else
            {
                msg = "Success: Spawned a process and added to the queue";

                var newProcess = new Process(name, execTime, prio, cpu, ram);
                lock (mutex)
                {
                    processes.Add(newProcess);
                    Monitor.Pulse(mutex);
                }

                double cpuUsage = GetTotalCpuUsage();
                double ramUsage = GetTotalMemoryUsage();

                if (cpuUsage > maxCpuUsage)
                    maxCpuUsage = cpuUsage;

                if (ramUsage > maxRamUsage)
                    maxRamUsage = ramUsage;

                
                if (shortestProcess == null || newProcess.ExecutionTime < shortestProcess.ExecutionTime)
                    shortestProcess = newProcess;
            }

            Console.WriteLine($"[Server]: {msg}");
            byte[] toSend = Encoding.UTF8.GetBytes(msg);
            _ = userSocket.Send(toSend);
        }



        private static void CommandSched(Socket userSocket, string[] parameters)
        {
            if (parameters.Length != 1)
            {
                Console.WriteLine($"[Server]: `spawn` command expects 1 argument, but {parameters.Length} is given");
                return;
            }

            // Validate parameters
            
            string _schedMode = parameters[0];
            string msg;
            if (!_schedMode.Equals("roundrobin") && !_schedMode.Equals("shortestfirst"))
            {
                msg = $"Error: Could not set a scheduling mode because invalid \"{_schedMode}\" is given";
            }
            else
            {
                msg = $"Success: Mode is set to \"{_schedMode}\"";
                lock (mutex)
                {
                    schedMode = _schedMode.Equals("roundrobin") ? SchedulerMode.ROUND_ROBIN : SchedulerMode.SHORTEST_FIRST;
                    Monitor.Pulse(mutex);
                }
            }

            Console.WriteLine($"[Server]: {msg}");
            byte[] toSend = Encoding.UTF8.GetBytes(msg);
            _ = userSocket.Send(toSend);
        }



        private static void CommandStart(Socket userSocket)
        {
            string msg;
            if (schedulerRunning == true)
            {
                msg = $"Success: Scheduler was already started";
            }
            else
            {
                msg = $"Success: Scheduler is now started";
                lock (mutex)
                {
                    schedulerRunning = true;
                    Monitor.Pulse(mutex);
                }
            }

            Console.WriteLine($"[Server]: {msg}");
            byte[] toSend = Encoding.UTF8.GetBytes(msg);
            _ = userSocket.Send(toSend);
        }



        private static void CommandStop(Socket userSocket)
        {
            string msg;
            if (schedulerRunning == false)
            {
                msg = $"Success: Scheduler was already stopped";
            }
            else
            {
                msg = $"Success: Scheduler is now stopped";
                lock (mutex)
                {
                    schedulerRunning = false;
                    Monitor.Pulse(mutex);
                }
            }

            Console.WriteLine($"[Server]: {msg}");
            byte[] toSend = Encoding.UTF8.GetBytes(msg);
            _ = userSocket.Send(toSend);
        }



        private static void CommandTerminate()
        {
            Console.WriteLine("[Server]: Exiting main loop");
            isRunning = false;
        }



        private static void CommandNotFound(Socket socket, string command)
        {
            string _msg = $"Error: Command \'{command}\' is not found";
            Console.WriteLine($"[Server]: {_msg}");
            if (socket.ProtocolType == ProtocolType.Udp)
                loginSocket.SendTo(Encoding.UTF8.GetBytes(_msg), senderEp);
            else
                socket.Send(Encoding.UTF8.GetBytes(_msg));
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



        private static void SchedulerEngine()
        {
            Console.WriteLine("[SCHEDULER]: Hello, World!");

            int _processIndex = -1;
            while (engineRunning)
            {
                Console.WriteLine($"[SCHEDULER]: In queue: {processes.Count}");
                lock (mutex)
                {
                    while (schedulerRunning == false || processes.Count <= 0)
                        Monitor.Wait(mutex);

                    if (schedMode == SchedulerMode.ROUND_ROBIN)
                        _processIndex = GetNextForRoundRobin(_processIndex);
                    else if (schedMode == SchedulerMode.SHORTEST_FIRST) 
                        _processIndex = GetNextForShortestFirst();
                }

                Process info = processes[_processIndex];
                string modeName = schedMode == SchedulerMode.ROUND_ROBIN ? "Round Robin" : "Shortest First";
                Console.WriteLine($"[SCHEDULER]: Scheduler Mode: {modeName}");
                Console.WriteLine($"[SCHEDULER]: Doing work of process with: Name = {info.Name}, Execution Time = {info.ExecutionTime}, Priority = {info.Priority}, CPU Usage = {info.CpuUsage}, Memory Usage = {info.MemoryUsage}");

                if (schedMode == SchedulerMode.ROUND_ROBIN)
                    DoRoundRobin(_processIndex);
                else if (schedMode == SchedulerMode.SHORTEST_FIRST) 
                    DoShortestFirst(_processIndex);
            }
        }



        private static int GetNextForShortestFirst()
        {
            int bestIndex = 0;
            int bestTime = processes[0].ExecutionTime;

            for (int i = 1; i < processes.Count; i++)
            {
                if (processes[i].ExecutionTime < bestTime)
                {
                    bestTime = processes[i].ExecutionTime;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }



        private static void DoShortestFirst(int processIndex)
        {
            Process p;
            lock (mutex)
            {
                p = processes[processIndex];
            }

            Thread.Sleep(p.ExecutionTime * 1000);

            Console.WriteLine($"[SCHEDULER]: The process with name \'{processes[processIndex].Name}\' has finished. Removing from queue");
            lock (mutex)
            {
                processes.RemoveAt(processIndex);
            }
        }



        private static int GetNextForRoundRobin(int processIndex)
        {
            return (processIndex + 1) % processes.Count;
        }



        private static void DoRoundRobin(int processIndex)
        {
            int execTime = processes[processIndex].ExecutionTime;
            int amountToSleep = int.Min(QUANT, execTime);
            Thread.Sleep(amountToSleep * 1_000);
            if (execTime < QUANT)
            {
                Console.WriteLine($"[SCHEDULER]: The process with name \'{processes[processIndex].Name}\' has finished. Removing from queue");
                lock (mutex)
                {
                    processes.RemoveAt(processIndex);
                }
            }
            else
            {
                Console.WriteLine($"[SCHEDULER]: The process with name \'{processes[processIndex].Name}\' has done its turn. Now it\'s paused, waiting its turn again");
                lock (mutex)
                {
                    processes[processIndex].ExecutionTime -= amountToSleep;
                }
            }
        }
    }
}
