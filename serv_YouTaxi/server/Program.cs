using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Data.SqlClient;

namespace YouTaxi_server
{
    public class Server
    {
        const int ECHO_PORT = 8081;
        public static string local_IP = "192.168.1.149";
        //public static string local_IP = "192.168.137.156";
        public static SqlConnection conn = new SqlConnection();
        public static string str_connect_DB = "";
        public static bool connect_DB = false;        
        //public static int nClients = 0;
        //public static SqlDataReader sql_read = null;
        //public static int N = 0;
        //public static int N_authorized = 0;

        public static void Main(string[] arg)
        {
            //переменная в которой формируется строка подключения к БД
            str_connect_DB = "";
            try
            {
                str_connect_DB = "Data Source = DESKTOP-KASPER; Initial Catalog = taxi;";
                //аутентификация вин
                str_connect_DB += "Integrated Security = True";
                //засылаем в переменную подключения сформированную строку
                conn.ConnectionString = str_connect_DB;
                //выполняем подключение
                conn.Open();
                Console.WriteLine("Подключился к БД");
                connect_DB = true;
            }
            catch
            {
                connect_DB = false;
                Console.WriteLine("Ошибка подключения к БД");
                Console.ReadLine();
            }

            if (connect_DB == true)
                try
                {
                    //Console.WriteLine("Укажите IP сервера");
                    //local_IP = Console.ReadLine();
                    // Связываем сервер с локальным портом
                    TcpListener clientListener = new TcpListener(IPAddress.Parse(local_IP), ECHO_PORT);
                    // Начинаем слушать
                    clientListener.Start();

                    Console.WriteLine("Ожидание подключения...");

                    while (true)
                    {
                        //Даем согласие на соединение
                        TcpClient client = clientListener.AcceptTcpClient();
                        work s = new work();
                        s.client = client;
                        ThreadStart threadDelegate = new ThreadStart(s.Work_with_user);
                        Thread clientThread = new Thread(threadDelegate);
                        clientThread.Start();
                    }
                    clientListener.Stop();
                }
                catch (Exception exp)
                {
                    Console.WriteLine("Ошибка: " + exp);
                }
            else
            {
                Console.WriteLine("Не удалось подключится к БД, дальнейшая работа сервера не возможна");
            }
        }
    }

    public class work
    {
        public TcpClient client;

        public void Work_with_user()
        {
            ClientYouTaxi cHandler = new ClientYouTaxi();

            //Передаем значение объекту ClientHandler
            cHandler.clientSocket = client;
            //Server.N++;
            Console.WriteLine("Кто то подключился к серверу");
            //Создаем новый поток для клиента
            cHandler.RunClient();
        }
    }
}
