using Common;
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


        private static int serverPort = 64_000;
        private static EndPoint senderEp = new IPEndPoint(IPAddress.Any, 0);
        private static IPEndPoint loginEp = new IPEndPoint(IPAddress.Any, 50_001);
        private static IPEndPoint userEp = new IPEndPoint(IPAddress.Any, serverPort);

        private static bool isRunning = true;
        private static bool isUserConnected = false;
        private static double cpuUsage = 0.0,
                              ramUsage = 0.0;
        private static byte[]  receiveBuffer = new byte[1024];


        public static void Main(string[] args)
        {
            // Initialize available users
            registeredUsers.Add("bale32", "qwertyzaz");
            registeredUsers.Add("mile", "12341234");
            registeredUsers.Add("admin", "admin123");

            Console.WriteLine("[Server]: Hello, World!");

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



        public static void CommandLogin(string[] parameters)
        {
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

            serverSocket.Bind(userEp);
            serverSocket.Listen();
            userSocket = serverSocket.Accept();
            isUserConnected = true;

            //listening.Close();
            //
        }



        public static void CommandNotFound(string command)
        {
            string _msg = $"Error: Command \'{command}\' is not found";
            Console.WriteLine(_msg);
            loginSocket.SendTo(Encoding.UTF8.GetBytes(_msg), senderEp);
        }
    }
}
