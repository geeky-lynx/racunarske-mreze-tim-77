using Common;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Server
{
    public class Server
    {
        public static void Main(string[] args)
        {
            double  cpuUsage = 0.0,
                    ramUsage = 0.0;
            byte[]  receiveBuffer = new byte[1024];

            List<User> registeredUsers = new List<User>();
            registeredUsers.Add(new User("mile", "12341234"));
            registeredUsers.Add(new User("admin", "admin123"));

            Console.WriteLine("[Server]: Hello, World!");

            //Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Socket loginSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint serverEp = new IPEndPoint(IPAddress.Any, 50_001);
            loginSocket.Bind(serverEp);

            Console.WriteLine($"Server is listening on: {serverEp}");

            EndPoint senderEp = new IPEndPoint(IPAddress.Any, 0);



            try
            {
                int byteCount = loginSocket.ReceiveFrom(receiveBuffer, ref senderEp);
                string message = Encoding.UTF8.GetString(receiveBuffer, 0, byteCount);
                Console.WriteLine($"Received message (length = {byteCount}) from {senderEp}.");
                Console.WriteLine($"Message: {message}");

                //byte[] sendBuffer = new byte[1024];
                string[] parts = message.Split(' ');
                if (parts[0].ToLower().CompareTo("login") == 0)
                {
                    // Is user registered
                    int target = -1;
                    for (int index = 0; index < registeredUsers.Count; index++)
                        if (registeredUsers[index].Name.Equals(parts[1]))
                        {
                            target = index;
                            break;
                        }

                    if (target == -1)
                    {
                        string msg = $"Error: No user registered as: {parts[1]}";
                        Console.WriteLine(msg);
                        int sentCount = loginSocket.SendTo(Encoding.UTF8.GetBytes(msg), senderEp);
                    }
                    else
                    {
                        if (!registeredUsers[target].Password.Equals(parts[2]))
                        {
                            string msg = "Error: Incorrect password";
                            Console.WriteLine(msg);
                            int sentCount = loginSocket.SendTo(Encoding.UTF8.GetBytes(msg), senderEp);
                        }
                        else
                        {
                            string msg = $"Success: TcpSocket={64000}";
                            Console.WriteLine(msg);
                            int sentCount = loginSocket.SendTo(Encoding.UTF8.GetBytes(msg), senderEp);
                        }
                    }
                }

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
    }
}
