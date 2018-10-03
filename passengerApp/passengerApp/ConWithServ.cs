using System;
using System.Text;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Net.NetworkInformation;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Cheesesquare
{
    public class Request//��� JSON
    {
        public string who { get; set; }
        public string command { get; set; }
        public List<string> parameters { get; set; }
    }

    public class Response
    {
        public string status { get; set; }
        public string cod { get; set; }
        public List<string> argument { get; set; }
    }

    public class ConWithServ
    {
        public static TcpClient eClient;
        public static NetworkStream writerStream;
        public static StreamReader readerStream;
        static int ECHO_PORT = 8081;
        static string IP = "192.168.1.149";
        //static string IP = "192.168.137.156";
        static Response respError = new Response();

        //�������� ���� try catch ��� ���� ������� �������� ������
        public static Response hello(string number, string cod) //System.IO.IOException: "�� ������� ��������� ������ �� ������������� ����������: ��������� �� ����� ����-���������� ��������� ������������� �����������."
        {
            string dataToSend;
            Response response;

            MD5 MD5_cod = new MD5CryptoServiceProvider();
            byte[] hash = MD5_cod.ComputeHash(Encoding.UTF8.GetBytes(cod));
            cod = "";
            foreach (byte a in hash)
            {
                cod += a.ToString("x");
            }

            try
            {
                Request request = new Request();
                request.who = "d";
                request.command = "hello";
                request.parameters = new List<string>();
                request.parameters.Add(number);
                request.parameters.Add(cod);
                dataToSend = JsonConvert.SerializeObject(request);
                dataToSend += "\r\n";

                // ���������� ��� ������������ �� ������
                byte[] data = Encoding.UTF8.GetBytes(dataToSend);
                writerStream.Write(data, 0, data.Length);

                // �������� ����� �� �������
                string returnData = readerStream.ReadLine();
                response = JsonConvert.DeserializeObject<Response>(returnData);
            }
            catch (Exception e)
            {
                respError.cod = "201";
                respError.status = "ERROR BD";
                respError.argument = new List<string>();
                return respError;
            }
            return response;
        }

        public static Response newPerson(string number, string idcity, string cod)//���������� ������ ������������
        {
            string dataToSend;
            MD5 MD5_cod = new MD5CryptoServiceProvider();
            byte[] hash = MD5_cod.ComputeHash(Encoding.UTF8.GetBytes(cod));
            cod = "";
            foreach (byte a in hash)
            {
                cod += a.ToString("x");
            }

            Response response;
            try
            {
                Request request = new Request();
                request.who = "n";
                request.command = "newPerson";
                request.parameters = new List<string>();
                request.parameters.Add(number);
                request.parameters.Add(idcity);
                request.parameters.Add(cod);
                dataToSend = JsonConvert.SerializeObject(request);
                dataToSend += "\r\n";

                // ���������� ��� ������������ �� ������
                byte[] data = Encoding.UTF8.GetBytes(dataToSend);
                writerStream.Write(data, 0, data.Length);

                // �������� ����� �� �������
                string returnData = readerStream.ReadLine();
                response = JsonConvert.DeserializeObject<Response>(returnData);
            }
            catch (Exception e)
            {
                respError.cod = "201";
                respError.status = "ERROR BD";
                respError.argument = new List<string>();
                return respError;
            }
            return response;
        }

        public static Response newPerson(string number, string idcity)
        {
            string dataToSend;
            Response response;
            try
            {
                Request request = new Request();
                request.who = "n";
                request.command = "newPerson";
                request.parameters = new List<string>();
                request.parameters.Add(number);
                request.parameters.Add(idcity);
                dataToSend = JsonConvert.SerializeObject(request);
                dataToSend += "\r\n";

                // ���������� ��� ������������ �� ������
                byte[] data = Encoding.UTF8.GetBytes(dataToSend);
                writerStream.Write(data, 0, data.Length);

                // �������� ����� �� �������
                string returnData = readerStream.ReadLine();
                response = JsonConvert.DeserializeObject<Response>(returnData);
            }
            catch (Exception e)
            {
                respError.cod = "201";
                respError.status = "ERROR BD";
                respError.argument = new List<string>();
                return respError;
            }
            return response;
        }

        public static Response getNewPas(string number)//������ ������ ������
        {
            string dataToSend;
            Response response;
            try
            {
                Request request = new Request();
                request.who = "n";
                request.command = "getNewPas";
                request.parameters = new List<string>();
                request.parameters.Add(number);
                dataToSend = JsonConvert.SerializeObject(request);
                dataToSend += "\r\n";

                // ���������� ��� ������������ �� ������
                byte[] data = Encoding.UTF8.GetBytes(dataToSend);
                writerStream.Write(data, 0, data.Length);

                // �������� ����� �� �������
                string returnData = readerStream.ReadLine();
                response = JsonConvert.DeserializeObject<Response>(returnData);
            }
            catch (Exception e)
            {
                respError.cod = "201";
                respError.status = "ERROR BD";
                respError.argument = new List<string>();
                return respError;
            }
            return response;
        }

        public static Response getNewPas(string number, string cod)//�������� ������ �� �������������
        {
            string dataToSend;
            MD5 MD5_cod = new MD5CryptoServiceProvider();
            byte[] hash = MD5_cod.ComputeHash(Encoding.UTF8.GetBytes(cod));
            cod = "";
            foreach (byte a in hash)
            {
                cod += a.ToString("x");
            }
            Response response;
            try
            {
                Request request = new Request();
                request.who = "n";
                request.command = "getNewPas";
                request.parameters = new List<string>();
                request.parameters.Add(number);
                request.parameters.Add(cod);
                dataToSend = JsonConvert.SerializeObject(request);
                dataToSend += "\r\n";

                // ���������� ��� ������������ �� ������
                byte[] data = Encoding.UTF8.GetBytes(dataToSend);
                writerStream.Write(data, 0, data.Length);

                // �������� ����� �� �������
                string returnData = readerStream.ReadLine();
                response = JsonConvert.DeserializeObject<Response>(returnData);
            }
            catch (Exception e)
            {
                respError.cod = "201";
                respError.status = "ERROR BD";
                respError.argument = new List<string>();
                return respError;
            }
            return response;
        }

        public static Response getOrder() //��������� ������ ��������� �������
        {
            string dataToSend;
            Response response;
            try
            {
                Request request = new Request();
                request.who = "d";
                request.command = "getOrder";
                request.parameters = new List<string>();
                dataToSend = JsonConvert.SerializeObject(request);
                dataToSend += "\r\n";

                byte[] data = Encoding.UTF8.GetBytes(dataToSend);
                writerStream.Write(data, 0, data.Length);

                // �������� ����� �� �������
                string returnData = readerStream.ReadLine();
                response = JsonConvert.DeserializeObject<Response>(returnData);
            }
            catch (Exception e)
            {
                respError.cod = "201";
                respError.status = "ERROR BD";
                respError.argument = new List<string>();
                return respError;
            }
            return response;
        }

        public static Response getState()// �������� ��������� ������
        {
            string dataToSend;
            Response response;
            try
            {
                Request request = new Request();
                request.who = "d";
                request.command = "getState";
                dataToSend = JsonConvert.SerializeObject(request);
                dataToSend += "\r\n";

                byte[] data = Encoding.UTF8.GetBytes(dataToSend);
                writerStream.Write(data, 0, data.Length);

                // �������� ����� �� �������
                string returnData = readerStream.ReadLine();
                response = JsonConvert.DeserializeObject<Response>(returnData);
            }
            catch (Exception e)
            {
                respError.cod = "201";
                respError.status = "ERROR BD";
                respError.argument = new List<string>();
                return respError;
            }
            return response;
        }

        public static Response getHistory()// �������� ��������� ������
        {
            string dataToSend;
            Response response;
            try
            {
                Request request = new Request();
                request.who = "p";
                request.command = "getHistory";
                dataToSend = JsonConvert.SerializeObject(request);
                dataToSend += "\r\n";

                byte[] data = Encoding.UTF8.GetBytes(dataToSend);
                writerStream.Write(data, 0, data.Length);

                // �������� ����� �� �������
                string returnData = readerStream.ReadLine();
                response = JsonConvert.DeserializeObject<Response>(returnData);
            }
            catch (Exception e)
            {
                respError.cod = "201";
                respError.status = "ERROR BD";
                respError.argument = new List<string>();
                return respError;
            }
            return response;
        }

        public static Response killOrder(string id) //������ ������
        {
            string dataToSend;
            Response response;
            try
            {
                Request request = new Request();
                request.who = "d";
                request.command = "killOrder";
                request.parameters = new List<string>();
                request.parameters.Add(id);
                dataToSend = JsonConvert.SerializeObject(request);
                dataToSend += "\r\n";

                byte[] data = Encoding.UTF8.GetBytes(dataToSend);
                writerStream.Write(data, 0, data.Length);

                // �������� ����� �� �������
                string returnData = readerStream.ReadLine();
                response = JsonConvert.DeserializeObject<Response>(returnData);
            }
            catch (Exception e)
            {
                respError.cod = "201";
                respError.status = "ERROR BD";
                respError.argument = new List<string>();
                return respError;
            }
            return response;
        }

        public static Response takeOrder(string id) //�������� ������
        {
            string dataToSend;
            Response response;
            try
            {
                Request request = new Request();
                request.who = "d";
                request.command = "takeOrder";
                request.parameters = new List<string>();
                request.parameters.Add(id);
                dataToSend = JsonConvert.SerializeObject(request);
                dataToSend += "\r\n";

                byte[] data = Encoding.UTF8.GetBytes(dataToSend);
                writerStream.Write(data, 0, data.Length);

                // �������� ����� �� �������
                string returnData = readerStream.ReadLine();
                response = JsonConvert.DeserializeObject<Response>(returnData);
            }
            catch (Exception e)
            {
                respError.cod = "201";
                respError.status = "ERROR BD";
                respError.argument = new List<string>();
                return respError;
            }
            return response;
        }

        public static Response changeOrder(string id, string state) //�������� ������ ������
        {
            string dataToSend;
            Response response;
            try
            {
                Request request = new Request();
                request.who = "d";
                request.command = "changeOrder";
                request.parameters = new List<string>();
                request.parameters.Add(id);
                request.parameters.Add(state);
                dataToSend = JsonConvert.SerializeObject(request);
                dataToSend += "\r\n";

                byte[] data = Encoding.UTF8.GetBytes(dataToSend);
                writerStream.Write(data, 0, data.Length);

                // �������� ����� �� �������
                string returnData = readerStream.ReadLine();
                response = JsonConvert.DeserializeObject<Response>(returnData);
            }
            catch (Exception e)
            {
                respError.cod = "201";
                respError.status = "ERROR BD";
                respError.argument = new List<string>();
                return respError;
            }
            return response;
        }

        public static Response setOption(string surname, string name, string city, 
                                            string state_number, string data_burn_auto, string �olor, 
                                            string brand_auto)//�������� �������� ������� �� ������
        {
            string dataToSend;
            Response response;
            try
            {
                Request request = new Request();
                request.who = "n";
                request.command = "setOption";
                request.parameters = new List<string>();
                request.parameters.Add(surname);
                request.parameters.Add(name);
                request.parameters.Add(city);
                request.parameters.Add(state_number);
                request.parameters.Add(data_burn_auto);
                request.parameters.Add(�olor);
                request.parameters.Add(brand_auto);
                dataToSend = JsonConvert.SerializeObject(request);
                dataToSend += "\r\n";

                byte[] data = Encoding.UTF8.GetBytes(dataToSend);
                writerStream.Write(data, 0, data.Length);

                // �������� ����� �� �������
                string returnData = readerStream.ReadLine();
                response = JsonConvert.DeserializeObject<Response>(returnData);
            }
            catch (Exception e)
            {
                respError.cod = "201";
                respError.status = "ERROR BD";
                respError.argument = new List<string>();
                return respError;
            }
            return response;
        }

        public static void exit() //������ ������
        {
            string dataToSend;
            Request request = new Request();
            request.who = "n";
            request.command = "exit";
            request.parameters = new List<string>();
            dataToSend = JsonConvert.SerializeObject(request);
            //dataToSend = "n|exit";
            dataToSend += "\r\n";

            byte[] data = Encoding.UTF8.GetBytes(dataToSend);
            writerStream.Write(data, 0, data.Length);
        }

        public static bool Initialized_con()
        {
            // ������� ���������� � ��������
            try
            {
                Ping pingSender = new Ping();
                PingReply reply = pingSender.Send(IP);

                //if (reply.Status == IPStatus.Success)
                //{

                    eClient = new TcpClient(IP, ECHO_PORT);
                    // ������� ������ �������
                    writerStream = eClient.GetStream();
                    readerStream = new StreamReader(eClient.GetStream());
                    return true;
                //}
                //else return false;
            }
            catch (System.Net.Sockets.SocketException)
            {
                return false;
            }
            catch (Exception exp)
            {
                return false;
            }
        }

    }
}