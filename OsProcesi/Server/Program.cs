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
        private static EndPoint senderEp = new IPEndPoint(IPAddress.Any, 0);



        public static void Main(string[] args)
        {
            double  cpuUsage = 0.0,
                    ramUsage = 0.0;
            byte[]  receiveBuffer = new byte[1024];

            registeredUsers.Add("mile", "12341234");
            registeredUsers.Add("admin", "admin123");

            Console.WriteLine("[Server]: Hello, World!");

            //Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint serverEp = new IPEndPoint(IPAddress.Any, 50_001);
            loginSocket.Bind(serverEp);

            Console.WriteLine($"Server is listening on: {serverEp}");




            try
            {
                int byteCount = loginSocket.ReceiveFrom(receiveBuffer, ref senderEp);
                string message = Encoding.UTF8.GetString(receiveBuffer, 0, byteCount);
                Console.WriteLine($"Received message (length = {byteCount}) from {senderEp}.");
                Console.WriteLine($"Message: {message}");

                //byte[] sendBuffer = new byte[1024];
                string[] parts = message.Split(' ');
                
                if (parts.Length < 1)
                {
                    Console.WriteLine("Error: Empty command is sent");
                }

                string commandName = parts[0].ToLower();
                string[] parameters = parts.Skip(1).ToArray();

                if (commandName.Equals("login"))
                    CommandLogin(parameters);

                //int byteCount2 = loginSocket.SendTo(sendBuffer, ref senderEp);
            }
            catch (SocketException exception)
            {
                Console.WriteLine("SocketException: Something happened...");
                Console.WriteLine(exception.ToString());
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
        }
    }
}
