using System;
using System.Data;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Data.SqlClient;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Collections.Generic;
using SMSC_API;

namespace YouTaxi_server
{
    public class ClientYouTaxi
    {
        public TcpClient clientSocket;
        public double price;
        public bool authorized = false;

        //инфа о пользователе
        public string name;
        public string patronymic;
        public int city;
        public string number_user;
        public string SMS_pas;

        //инфа об автомобиле
        public string color;
        public string brand;
        public string number_auto;

        //метод для распознования запроса          
        //str - полученая от клиента строка в формате JSON
        public int GetRequest(NetworkStream writerStream, StreamReader readerStream, Request request)
        {

            switch (request.who)
            {
                case ("n"):
                    {
                        switch (request.command)
                        {
                            case ("newPerson"):
                                {
                                    byte[] dataWrite;
                                    Response send = new Response();
                                    string json = "";
                                    try
                                    {
                                        //проверяем есть ли такой номер уже в системе
                                        string str_conn = "select * from dbo.пользователи where номер_телефона = @number";
                                        SqlCommand cmd = new SqlCommand(str_conn, Server.conn);

                                        //создаем параметры для запроса
                                        SqlParameter SqlParam_number = new SqlParameter("@number", SqlDbType.Decimal);
                                        SqlParam_number.Precision = 11;
                                        SqlParam_number.Scale = 0;
                                        SqlParam_number.Value = request.parameters[0];
                                        number_user = request.parameters[0];
                                        //добавляем параметры к запросу
                                        cmd.Parameters.Add(SqlParam_number);

                                        //выполняем запрос и если в результате строк не найдено то
                                        if (cmd.ExecuteNonQuery() == -1)
                                        {
                                            //проверяем прислал ли нам пользователь смс пароль на подтверждение
                                            bool exist_smspas = false;
                                            try
                                            {
                                                if (request.parameters[2] == "") exist_smspas = false;
                                                else exist_smspas = true;
                                            }
                                            catch (Exception e)
                                            {
                                                exist_smspas = false;
                                            }

                                            if (!exist_smspas)//если пароля нет
                                            {
                                                Console.WriteLine("Новый " + request.parameters[0] + " запросил СМС_пароль " + DateTime.Now.ToString());

                                                //Генерируем пароль
                                                Random r = new Random();
                                                int int_sms_pas = r.Next(10000, 99999);
                                                SMS_pas = Convert.ToString(int_sms_pas);

                                                //хешируем пароль
                                                MD5 MD5_cod = new MD5CryptoServiceProvider();
                                                byte[] hash = MD5_cod.ComputeHash(Encoding.UTF8.GetBytes(SMS_pas));

                                                SMSC smsc = new SMSC();
                                                SMS_pas = "Ваш пароль: " + SMS_pas;
                                                string[] smsc_rez = smsc.send_sms(number_user, SMS_pas , 0); // ЗАМЕНИТЬ СВОЙ НОМЕР НА НОМЕР ПРОФИЛЯ!!!!!!!!1

                                                send.status = "OK";
                                                send.cod = "2";
                                                send.argument = new List<string>();
                                                send.argument.Add(SMS_pas); //!!!!!!!!!!!Убрать СМС
                                                json = JsonConvert.SerializeObject(send);
                                                json += "\r\n";
                                                dataWrite = Encoding.UTF8.GetBytes(json);
                                                writerStream.Write(dataWrite, 0, dataWrite.Length);

                                                //сохраняем хеш пароля
                                                SMS_pas = "";
                                                foreach (byte a in hash) SMS_pas += a.ToString("x");

                                                return 2;
                                            }
                                            else
                                                if (request.parameters[2] == SMS_pas.ToString())
                                            {
                                                try
                                                {
                                                    str_conn = "INSERT INTO пользователи(номер_телефона, id_города, активирован, пароль_кеш) " +
                                                               "values(@number, @city, 'true', @pas)";
                                                    cmd = new SqlCommand(str_conn, Server.conn);

                                                    //создаем параметры для запроса
                                                    SqlParameter SqlParam_new_number = new SqlParameter("@number", SqlDbType.Decimal);
                                                    SqlParam_new_number.Value = request.parameters[0];
                                                    SqlParameter SqlParam_city = new SqlParameter("@city", SqlDbType.Int);
                                                    SqlParam_city.Value = request.parameters[1];
                                                    city = Convert.ToInt32(request.parameters[1]);

                                                    //Повтороно хешируем присланный пароль так как в БД хранится двукратнохешированный пароль
                                                    MD5 MD5_cod = new MD5CryptoServiceProvider();
                                                    byte[] hash = MD5_cod.ComputeHash(Encoding.UTF8.GetBytes(request.parameters[2]));
                                                    request.parameters[2] = "";
                                                    foreach (byte a in hash)
                                                    {
                                                        request.parameters[2] += a.ToString("x");
                                                    }
                                                    SqlParameter SqlParam_new_pas = new SqlParameter("@pas", SqlDbType.NChar);
                                                    SqlParam_new_pas.Value = request.parameters[2];

                                                    //добавляем параметры к запросу
                                                    cmd.Parameters.Add(SqlParam_new_number);
                                                    cmd.Parameters.Add(SqlParam_city);
                                                    cmd.Parameters.Add(SqlParam_new_pas);

                                                    //выполнение запроса
                                                    if (cmd.ExecuteNonQuery() == -1) //если строк нет значит пользователь не был добавлен
                                                    {
                                                        send.status = "ERROR BD";
                                                        send.cod = "201";
                                                        send.argument = new List<string>();
                                                        json = JsonConvert.SerializeObject(send);
                                                        json += "\r\n";
                                                        dataWrite = Encoding.UTF8.GetBytes(json);
                                                        writerStream.Write(dataWrite, 0, dataWrite.Length);
                                                        return 201;
                                                    }
                                                }
                                                catch (Exception e)
                                                {
                                                    send.status = "ERROR BD";
                                                    send.cod = "201";
                                                    send.argument = new List<string>();
                                                    json = JsonConvert.SerializeObject(send);
                                                    json += "\r\n";
                                                    dataWrite = Encoding.UTF8.GetBytes(json);
                                                    writerStream.Write(dataWrite, 0, dataWrite.Length);
                                                    return 201;
                                                }

                                                send.status = "OK";
                                                send.cod = "3";
                                                send.argument = new List<string>();
                                                json = JsonConvert.SerializeObject(send);
                                                json += "\r\n";
                                                dataWrite = Encoding.UTF8.GetBytes(json);
                                                writerStream.Write(dataWrite, 0, dataWrite.Length);
                                                //if (!authorized) Server.N_authorized++;
                                                //Server.N_authorized++;
                                                authorized = true;
                                                Console.WriteLine("Новый " + request.parameters[0] + " зарегистрировался в систему " + DateTime.Now.ToString());
                                                return 3;
                                            }
                                            else
                                            {
                                                send.status = "ERROR";
                                                send.cod = "103";
                                                send.argument = new List<string>();
                                                json = JsonConvert.SerializeObject(send);
                                                json += "\r\n";
                                                dataWrite = Encoding.UTF8.GetBytes(json);
                                                writerStream.Write(dataWrite, 0, dataWrite.Length);
                                                return 103;
                                            }
                                        }
                                        else
                                        {
                                            send.status = "ERROR NUMBER BUSY";
                                            send.cod = "104";
                                            send.argument = new List<string>();
                                            json = JsonConvert.SerializeObject(send);
                                            json += "\r\n";
                                            dataWrite = Encoding.UTF8.GetBytes(json);
                                            writerStream.Write(dataWrite, 0, dataWrite.Length);
                                            return 104;
                                        }
                                    }
                                    catch
                                    {
                                        send.status = "ERROR BD";
                                        send.cod = "201";
                                        send.argument = new List<string>();
                                        json = JsonConvert.SerializeObject(send);
                                        json += "\r\n";
                                        dataWrite = Encoding.UTF8.GetBytes(json);
                                        writerStream.Write(dataWrite, 0, dataWrite.Length);
                                        return 201;
                                    }
                                    break;
                                }
                            case ("getNewPas"):
                                {
                                    byte[] dataWrite;
                                    Response send = new Response();
                                    string json = "";
                                    try
                                    {
                                        //проверяем есть ли такой номер в системе
                                        string str_conn = "select номер_телефона from dbo.пользователи where номер_телефона = @number";
                                        SqlCommand cmd = new SqlCommand(str_conn, Server.conn);

                                        //создаем параметры для запроса
                                        SqlParameter SqlParam_number = new SqlParameter("@number", SqlDbType.Decimal);
                                        SqlParam_number.Precision = 11;
                                        SqlParam_number.Scale = 0;
                                        SqlParam_number.Value = request.parameters[0];
                                        number_user = request.parameters[0];

                                        //добавляем параметры к запросу
                                        cmd.Parameters.Add(SqlParam_number);

                                        //выполняем запрос и если в результате строки найдены
                                        var an = cmd.ExecuteScalar();
                                        if (an != null)
                                        {
                                            //проверяем прислал ли нам пользователь смс пароль на подтверждение
                                            bool exist_smspas = false;
                                            try
                                            {
                                                if (request.parameters[1] == "") exist_smspas = false;
                                                else exist_smspas = true;
                                            }
                                            catch (Exception e)
                                            {
                                                exist_smspas = false;
                                            }

                                            if (!exist_smspas)//если пароля нет
                                            {
                                                Console.WriteLine(request.parameters[0] + " пытается востановить пароль " + DateTime.Now.ToString());

                                                //Генерируем пароль
                                                Random r = new Random();
                                                int int_sms_pas = r.Next(10000, 99999);
                                                SMS_pas = Convert.ToString(int_sms_pas);

                                                //хешируем пароль
                                                MD5 MD5_cod = new MD5CryptoServiceProvider();
                                                byte[] hash = MD5_cod.ComputeHash(Encoding.UTF8.GetBytes(SMS_pas));

                                                SMSC smsc = new SMSC();
                                                SMS_pas = "Ваш пароль: " + SMS_pas;
                                                string[] smsc_rez = smsc.send_sms("89681703956", SMS_pas , 0); // ЗАМЕНИТЬ СВОЙ НОМЕР НА НОМЕР ПРОФИЛЯ!!!!!!!!1

                                                send.status = "OK";
                                                send.cod = "15";
                                                send.argument = new List<string>();
                                                send.argument.Add(SMS_pas);//!!!!!Убрать СМС
                                                json = JsonConvert.SerializeObject(send);
                                                json += "\r\n";
                                                dataWrite = Encoding.UTF8.GetBytes(json);
                                                writerStream.Write(dataWrite, 0, dataWrite.Length);

                                                //сохраняем хеш пароля
                                                SMS_pas = "";
                                                foreach (byte a in hash) SMS_pas += a.ToString("x");

                                                return 15;
                                            }
                                            else
                                            if (request.parameters[1] == SMS_pas.ToString())
                                            {
                                                try
                                                {
                                                    str_conn = "UPDATE пользователи SET пароль_кеш = @pas WHERE номер_телефона = @number";
                                                    cmd = new SqlCommand(str_conn, Server.conn);

                                                    //создаем параметры для запроса
                                                    SqlParameter SqlParam_new_number = new SqlParameter("@number", SqlDbType.Decimal);
                                                    SqlParam_new_number.Value = request.parameters[0];

                                                    //Повтороно хешируем присланный пароль так как в БД хранится двукратнохешированный пароль
                                                    MD5 MD5_cod = new MD5CryptoServiceProvider();
                                                    byte[] hash = MD5_cod.ComputeHash(Encoding.UTF8.GetBytes(request.parameters[1]));
                                                    request.parameters[1] = "";
                                                    foreach (byte a in hash)
                                                    {
                                                        request.parameters[1] += a.ToString("x");
                                                    }
                                                    SqlParameter SqlParam_new_pas = new SqlParameter("@pas", SqlDbType.NChar);
                                                    SqlParam_new_pas.Value = request.parameters[1];

                                                    //добавляем параметры к запросу
                                                    cmd.Parameters.Add(SqlParam_new_number);
                                                    cmd.Parameters.Add(SqlParam_new_pas);

                                                    //выполнение запроса
                                                    if (cmd.ExecuteNonQuery() == -1) //если строк нет значит пароль не был изменен
                                                    {
                                                        send.status = "ERROR BD";
                                                        send.cod = "201";
                                                        send.argument = new List<string>();
                                                        json = JsonConvert.SerializeObject(send);
                                                        json += "\r\n";
                                                        dataWrite = Encoding.UTF8.GetBytes(json);
                                                        writerStream.Write(dataWrite, 0, dataWrite.Length);
                                                        return 201;
                                                    }
                                                }
                                                catch (Exception e)
                                                {
                                                    send.status = "ERROR BD";
                                                    send.cod = "201";
                                                    send.argument = new List<string>();
                                                    json = JsonConvert.SerializeObject(send);
                                                    json += "\r\n";
                                                    dataWrite = Encoding.UTF8.GetBytes(json);
                                                    writerStream.Write(dataWrite, 0, dataWrite.Length);
                                                    Console.WriteLine(e);
                                                    return 201;
                                                }
                                                send.status = "OK";
                                                send.cod = "16";
                                                send.argument = new List<string>();
                                                json = JsonConvert.SerializeObject(send);
                                                json += "\r\n";
                                                dataWrite = Encoding.UTF8.GetBytes(json);
                                                writerStream.Write(dataWrite, 0, dataWrite.Length);
                                                Console.WriteLine("" + request.parameters[0] + " полкучил новый пароль " + DateTime.Now.ToString());
                                                return 16;
                                            }
                                            else
                                            {
                                                send.status = "ERROR";
                                                send.cod = "127";
                                                json = JsonConvert.SerializeObject(send);
                                                dataWrite = Encoding.UTF8.GetBytes(json);
                                                writerStream.Write(dataWrite, 0, dataWrite.Length);
                                                return 127;
                                            }
                                        }
                                        else
                                        {
                                            send.status = "ERROR";
                                            send.cod = "128";
                                            send.argument = new List<string>();
                                            json = JsonConvert.SerializeObject(send);
                                            json += "\r\n";
                                            dataWrite = Encoding.UTF8.GetBytes(json);
                                            writerStream.Write(dataWrite, 0, dataWrite.Length);
                                            return 128;
                                        }
                                    }
                                    catch
                                    {
                                        send.status = "ERROR BD";
                                        send.cod = "201";
                                        send.argument = new List<string>();
                                        json = JsonConvert.SerializeObject(send);
                                        json += "\r\n";
                                        dataWrite = Encoding.UTF8.GetBytes(json);
                                        writerStream.Write(dataWrite, 0, dataWrite.Length);
                                        return 201;
                                    }
                                    break;
                                }
                            case ("setOption"):
                                {
                                    byte[] dataWrite;
                                    Response send = new Response();
                                    string json = "";
                                    try
                                    {
                                        SqlCommand cmd = new SqlCommand();
                                        if (request.parameters.Count == 3)
                                        {
                                            cmd = new SqlCommand("n_setOption_v1", Server.conn);
                                        }
                                        else if (request.parameters.Count == 7)
                                        {
                                            cmd = new SqlCommand("n_setOption_v2", Server.conn);

                                            SqlParameter SqlParam_state_number = new SqlParameter("@state_number", SqlDbType.NChar);
                                            SqlParam_state_number.Value = request.parameters[3];
                                            cmd.Parameters.Add(SqlParam_state_number);

                                            SqlParameter SqlParam_data_burn_auto = new SqlParameter("@data_burn_auto", SqlDbType.Int);
                                            SqlParam_data_burn_auto.Value = request.parameters[4];
                                            cmd.Parameters.Add(SqlParam_data_burn_auto);

                                            SqlParameter SqlParam_сolor = new SqlParameter("@сolor", SqlDbType.NChar);
                                            SqlParam_сolor.Value = request.parameters[5];
                                            cmd.Parameters.Add(SqlParam_сolor);

                                            SqlParameter SqlParam_brand_auto = new SqlParameter("@brand_auto", SqlDbType.NChar);
                                            SqlParam_brand_auto.Value = request.parameters[6];
                                            cmd.Parameters.Add(SqlParam_brand_auto);
                                        }
                                        else
                                        {
                                            send.status = "ERROR";
                                            send.cod = "134";
                                            send.argument = new List<string>();
                                            json = JsonConvert.SerializeObject(send);
                                            json += "\r\n";
                                            dataWrite = Encoding.UTF8.GetBytes(json);
                                            writerStream.Write(dataWrite, 0, dataWrite.Length);
                                            return 134;
                                        }

                                        cmd.CommandType = CommandType.StoredProcedure;

                                        SqlParameter SqlParam_Res = new SqlParameter("@res", SqlDbType.Int);
                                        SqlParam_Res.Value = 201;
                                        cmd.Parameters.Add(SqlParam_Res).Direction = ParameterDirection.InputOutput;

                                        SqlParameter SqlParam_number = new SqlParameter("@number", SqlDbType.Decimal);
                                        SqlParam_number.Value = number_user;
                                        cmd.Parameters.Add(SqlParam_number);

                                        SqlParameter SqlParam_surname = new SqlParameter("@surname", SqlDbType.NChar);
                                        SqlParam_surname.Value = request.parameters[0];
                                        cmd.Parameters.Add(SqlParam_surname);

                                        SqlParameter SqlParam_name = new SqlParameter("@name", SqlDbType.NChar);
                                        SqlParam_name.Value = request.parameters[1];
                                        cmd.Parameters.Add(SqlParam_name);

                                        SqlParameter SqlParam_id_city = new SqlParameter("@id_city", SqlDbType.Int);
                                        SqlParam_id_city.Value = request.parameters[2];
                                        cmd.Parameters.Add(SqlParam_id_city);

                                        cmd.ExecuteNonQuery();
                                        string result = Convert.ToString(cmd.Parameters["@res"].Value);

                                        if (result == "18") send.status = "OK";
                                        else send.status = "ERROR";
                                        send.cod = result;
                                        send.argument = new List<string>();
                                        json = JsonConvert.SerializeObject(send);
                                        json += "\r\n";
                                        dataWrite = Encoding.UTF8.GetBytes(json);
                                        writerStream.Write(dataWrite, 0, dataWrite.Length);
                                        Console.WriteLine("Пользователь " + number_user + " " + DateTime.Now.ToString() + " обновил информацию о себе " + result);
                                        return Convert.ToInt32(result);
                                    }
                                    catch (Exception e)
                                    {                                 
                                        send.status = "ERROR BD";
                                        send.cod = "201";
                                        send.argument = new List<string>();
                                        json = JsonConvert.SerializeObject(send);
                                        json += "\r\n";
                                        dataWrite = Encoding.UTF8.GetBytes(json); writerStream.Write(dataWrite, 0, dataWrite.Length);
                                        Console.WriteLine(e);
                                        return 201;
                                    }
                                    break;
                                }
                            case ("exit"):
                                {
                                    try
                                    {
                                        clientSocket.Close();
                                        return 4;
                                    }
                                    catch { return 126;}
                                    break;
                                }
                            default: {return 105; break; }
                        }
                        break;
                    }
                case ("p"):
                    {
                        switch (request.command)
                        {
                            case ("hello"):
                                {
                                    byte[] dataWrite;
                                    Response send = new Response();
                                    string json = "";
                                    try
                                    {
                                        //Повтороно хешируем присланный пароль так как в БД хранится двукратнохешированный пароль
                                        MD5 MD5_cod = new MD5CryptoServiceProvider();
                                        byte[] hash = MD5_cod.ComputeHash(Encoding.UTF8.GetBytes(request.parameters[1]));
                                        string pas = "";
                                        foreach (byte a in hash) pas += a.ToString("x");

                                        string str_conn = "select активирован from dbo.пользователи where номер_телефона = @number and пароль_кеш = @pas";
                                        SqlCommand cmd = new SqlCommand(str_conn, Server.conn);

                                        //создаем параметры для запроса
                                        SqlParameter SqlParam_number = new SqlParameter("@number", SqlDbType.Decimal);
                                        SqlParam_number.Value = request.parameters[0];
                                        SqlParameter SqlParam_pas = new SqlParameter("@pas", SqlDbType.NChar);
                                        SqlParam_pas.Value = pas;

                                        //добавляем параметры к запросу
                                        cmd.Parameters.Add(SqlParam_number);
                                        cmd.Parameters.Add(SqlParam_pas);

                                        //если в результате запроса есть строки то 
                                        if (cmd.ExecuteScalar() != null)
                                        {
                                            //считываем id_пользователя
                                            number_user = request.parameters[0];
                                            bool activate = Convert.ToBoolean(cmd.ExecuteScalar());

                                            if (activate == true)
                                            {
                                                //Считываем город
                                                str_conn = "SELECT id_города " +
                                                           "FROM[Taxi].[dbo].[пользователи] " +
                                                           "where пользователи.номер_телефона=@number";
                                                cmd = new SqlCommand(str_conn, Server.conn);

                                                //создаем параметры для запроса
                                                SqlParam_number = new SqlParameter("@number", SqlDbType.Decimal);
                                                SqlParam_number.Value = number_user;

                                                //добавляем параметры к запросу
                                                cmd.Parameters.Add(SqlParam_number);

                                                //выполнение запроса
                                                city = Convert.ToInt32(cmd.ExecuteScalar());


                                                send.status = "OK";
                                                send.cod = "1";
                                                send.argument = new List<string>();
                                                send.argument.Add(Convert.ToString(city));
                                                json = JsonConvert.SerializeObject(send);
                                                json += "\r\n";
                                                dataWrite = Encoding.UTF8.GetBytes(json);
                                                writerStream.Write(dataWrite, 0, dataWrite.Length);
                                                //if (!authorized) Server.N_authorized++;
                                                //authorized = true;
                                                Console.WriteLine("Подключился " + number_user + " " + DateTime.Now.ToString());
                                                return 1;
                                            }
                                            else
                                            {
                                                send.status = "ERROR";
                                                send.cod = "102";
                                                send.argument = new List<string>();
                                                json = JsonConvert.SerializeObject(send);
                                                json += "\r\n";
                                                dataWrite = Encoding.UTF8.GetBytes(json);
                                                writerStream.Write(dataWrite, 0, dataWrite.Length);
                                                return 102;
                                            }
                                        }
                                        else
                                        {
                                            send.status = "ERROR"; 
                                            send.cod = "101";
                                            send.argument = new List<string>();
                                            json = JsonConvert.SerializeObject(send);
                                            json += "\r\n";
                                            dataWrite = Encoding.UTF8.GetBytes(json);
                                            writerStream.Write(dataWrite, 0, dataWrite.Length);
                                            return 101;
                                        }
                                    }
                                    catch
                                    {
                                        send.status = "ERROR BD";
                                        send.cod = "201";
                                        send.argument = new List<string>();
                                        json = JsonConvert.SerializeObject(send);
                                        json += "\r\n";
                                        dataWrite = Encoding.UTF8.GetBytes(json);
                                        writerStream.Write(dataWrite, 0, dataWrite.Length);
                                        return 201;
                                    }
                                    break;
                                }
                            case ("newOrder"): //Не надежные SQL запросы решить внедрением хранимых процедур
                                {
                                    byte[] dataWrite;
                                    Response send = new Response();
                                    string json = "";
                                    string id_comp = "";
                                    try
                                    {
                                        //Рассчет цены
                                        //Рассчет длины пути
                                        string str_conn = "select город from dbo.города where id_города = @city";
                                        SqlCommand cmd = new SqlCommand(str_conn, Server.conn);

                                        //создаем параметры для запроса
                                        SqlParameter SqlParam_city = new SqlParameter("@city", SqlDbType.NChar);
                                        SqlParam_city.Value = city;

                                        //добавляем параметры к запросу
                                        cmd.Parameters.Add(SqlParam_city);
                                        string name_city = Convert.ToString(cmd.ExecuteScalar());//выполнили запрос и сохранили результат в переменной
                                        string text_dep = name_city + " " + request.parameters[1];
                                        string text_arr = name_city + " " + request.parameters[2];
                                        string site = "https://maps.googleapis.com/maps/api/distancematrix/json?units=imperial&origins=" + text_dep + "&destinations=" + text_arr + "&key=AIzaSyCraPc_A9hC65AQ2GjVBBxtZwvWMUGUPqc";
                                        System.Net.WebClient web = new System.Net.WebClient();
                                        web.Encoding = Encoding.UTF8;
                                        json = web.DownloadString(site);

                                        GoogleDistance distance = JsonConvert.DeserializeObject<GoogleDistance>(json);

                                        double yardage = 0;
                                        if (distance.status == "OK")
                                        {
                                            if(distance.rows[0].elements[0].status== "OK")
                                                yardage = Math.Round(Convert.ToDouble(distance.rows[0].elements[0].distance.value) / 1000, 1);
                                            else
                                            {
                                                send.status = "ERROR";
                                                send.cod = "110";
                                                send.argument = new List<string>();
                                                json = JsonConvert.SerializeObject(send);
                                                json += "\r\n";
                                                dataWrite = Encoding.UTF8.GetBytes(json);
                                                writerStream.Write(dataWrite, 0, dataWrite.Length);
                                                return 110;
                                            }
                                        }
                                        else
                                        {
                                            send.status = "ERROR";
                                            send.cod = "137";
                                            send.argument = new List<string>();
                                            json = JsonConvert.SerializeObject(send);
                                            json += "\r\n";
                                            dataWrite = Encoding.UTF8.GetBytes(json);
                                            writerStream.Write(dataWrite, 0, dataWrite.Length);
                                            return 137;
                                        }

                                        str_conn = "select TOP(1) * from dbo.тарифы where id_города = @city ORDER BY дата desc";
                                        cmd = new SqlCommand(str_conn, Server.conn);

                                        //создаем параметры для запроса
                                        SqlParameter SqlParam_city1 = new SqlParameter("@city", SqlDbType.NChar);
                                        SqlParam_city1.Value = city;
                                        cmd.Parameters.Add(SqlParam_city1);

                                        //выполнение запроса
                                        SqlDataReader sql_read = null;
                                        sql_read = cmd.ExecuteReader();

                                        //получение тарифов для расчета стоимости поездки
                                        int km1, km2, km_b, inaction, constant;//за 1км, за второй км, за последующие км, за ожидание, за вызов
                                        if (sql_read.HasRows)
                                        {
                                            sql_read.Read();
                                            {
                                                km1 = Convert.ToInt32(sql_read.GetValue(2));
                                                km2 = Convert.ToInt32(sql_read.GetValue(3));
                                                km_b = Convert.ToInt32(sql_read.GetValue(4));
                                                inaction = Convert.ToInt32(sql_read.GetValue(5));
                                                constant = Convert.ToInt32(sql_read.GetValue(6));
                                            }
                                            
                                            price = constant;
                                            if (yardage > 2) price += km1+km2+(yardage - 2)*km_b;
                                            else if (yardage > 1) price += km1+(yardage - 1)*km2;
                                            else if (yardage > 0) price += km1;

                                            if (Convert.ToBoolean(request.parameters[5])) price = price / 10 * 7;
                                        }

                                        // закрываем reader
                                        if (sql_read != null)
                                        {
                                            sql_read.Close();
                                        }

                                        string move = "";
                                        if (request.parameters[0] == "ok")
                                        {
                                            if (Convert.ToBoolean(request.parameters[5]))
                                            {
                                                SqlDataReader sql_read_companion = null;
                                                try
                                                {
                                                    move = "новый с попутчиком";

                                                    str_conn = "select s.id_заказа, дата_создания, адрес_отправления, адрес_прибытия, стоимость, попутчик, id_заказа_попутчика, id_города " +
                                                               "from заказы_в_работе inner join (select id_заказа,max(дата_время) time from заказ_параметр group by id_заказа) s " +
                                                               "on заказы_в_работе.дата_время = s.time " +
                                                               "where попутчик=1 and id_заказа_попутчика is null and id_города=@city";
                                                    cmd = new SqlCommand(str_conn, Server.conn);

                                                    //создаем параметры для запроса
                                                    SqlParameter SqlParam_city2 = new SqlParameter("@city", SqlDbType.NChar);
                                                    SqlParam_city2.Value = city;
                                                    cmd.Parameters.Add(SqlParam_city2);

                                                    //выполнение запроса
                                                    sql_read_companion = null;
                                                    sql_read_companion = cmd.ExecuteReader();

                                                    if (sql_read_companion.HasRows)
                                                    {
                                                        while(sql_read_companion.Read())
                                                        {
                                                            //добавляем параметры к запросу
                                                            string text_address1 = name_city + " " + request.parameters[1];
                                                            string text_address2 = name_city + " " + Convert.ToString(sql_read_companion.GetValue(2));
                                                            site = "https://maps.googleapis.com/maps/api/distancematrix/json?units=imperial&origins=" + text_address1 + "&destinations=" + text_address2 + "&key=AIzaSyCraPc_A9hC65AQ2GjVBBxtZwvWMUGUPqc";
                                                            web.Encoding = Encoding.UTF8;
                                                            json = web.DownloadString(site);
                                                            GoogleDistance distance1 = JsonConvert.DeserializeObject<GoogleDistance>(json);
                                                            double yardage2=3;
                                                            if (distance1.rows[0].elements[0].status == "OK")
                                                                yardage2 = Math.Round(Convert.ToDouble(distance1.rows[0].elements[0].distance.value) / 1000, 1);

                                                            if(yardage2<2) { id_comp = Convert.ToString(sql_read_companion.GetValue(0)); break; }
                                                        }
                                                    }
                                                }
                                                catch (Exception exc){ }
                                                if (sql_read_companion != null)
                                                {
                                                    sql_read_companion.Close();
                                                }

                                            }
                                            else move = "новый";
                                        }
                                        else
                                        if (request.parameters[0] == "info")
                                        {
                                            move = "инфо";
                                        }
                                        else
                                        {
                                            send.status = "ERROR";
                                            send.cod = "111";
                                            send.argument = new List<string>();
                                            json = JsonConvert.SerializeObject(send);
                                            json += "\r\n";
                                            dataWrite = Encoding.UTF8.GetBytes(json);
                                            writerStream.Write(dataWrite, 0, dataWrite.Length);
                                            return 111;
                                        }

                                        //запрос на добавление нового заказа
                                        SqlParameter SqlParam_id_comp_ord;
                                        if (id_comp == "")
                                        {
                                            str_conn = "INSERT INTO заказы(номер_телефона, дата_создания, комментарий, попутчик)"
                                                              + "values(@number, GETDATE(), @comment, @companion)"
                                                              + "select @@IDENTITY";
                                        }
                                        else
                                        {
                                            str_conn = "INSERT INTO заказы(номер_телефона, дата_создания, комментарий, попутчик, id_заказа_попутчика)"
                                                              + "values(@number, GETDATE(), @comment, @companion, @id_comp_ord ) "
                                                              + "select @@IDENTITY";
                                        }
                                        cmd = new SqlCommand(str_conn, Server.conn);

                                        if (id_comp != "")
                                        {
                                            SqlParam_id_comp_ord = new SqlParameter("@id_comp_ord", SqlDbType.Int);
                                            SqlParam_id_comp_ord.Value = Convert.ToInt32(id_comp);
                                            cmd.Parameters.Add(SqlParam_id_comp_ord);
                                        }

                                        //создаем параметры для запроса
                                        SqlParameter SqlParam_number = new SqlParameter("@number", SqlDbType.Decimal);
                                        SqlParam_number.Value = number_user;
                                        cmd.Parameters.Add(SqlParam_number);
                                        SqlParameter SqlParam_comment = new SqlParameter("@comment", SqlDbType.NChar);
                                        SqlParam_comment.Value = request.parameters[4];
                                        cmd.Parameters.Add(SqlParam_comment);
                                        SqlParameter SqlParam_companion = new SqlParameter("@companion", SqlDbType.Bit);
                                        SqlParam_companion.Value = Convert.ToBoolean(request.parameters[5]);
                                        cmd.Parameters.Add(SqlParam_companion);

                                        //выполнение запроса
                                        //ПОЛУЧЕНИЕ ID_заказа для дальнейшей работы с ним
                                        int id = Convert.ToInt32(cmd.ExecuteScalar());

                                        if(id_comp!="")
                                        {
                                            str_conn = str_conn = "UPDATE заказы SET id_заказа_попутчика =  @id_order WHERE id_заказа_попутчика = @id_comp_ord1";
                                            SqlParameter SqlParam_id_comp_ord1 = new SqlParameter("@id_comp_ord1", SqlDbType.Int);
                                            SqlParam_id_comp_ord1.Value = Convert.ToInt32(id_comp);
                                            cmd.Parameters.Add(SqlParam_id_comp_ord1);

                                            SqlParameter SqlParam_id1 = new SqlParameter("@id_order", SqlDbType.Int);
                                            SqlParam_id1.Value = Convert.ToInt32(id);
                                            cmd.Parameters.Add(SqlParam_id1);

                                            if (cmd.ExecuteNonQuery()!=-1)
                                            { }
                                        }

                                        str_conn = "INSERT INTO заказ_параметр(id_заказа, дата_время, адрес_отправления, адрес_прибытия, стоимость, количество_пассажиров, действие, id_города) "
                                                            + "values(@id_order, GETDATE(), @dep, @arr, @price, @num, @move, @id_city)";
                                        cmd = new SqlCommand(str_conn, Server.conn);

                                        //создаем параметры для запроса
                                        SqlParameter SqlParameter_id_order = new SqlParameter("@id_order", SqlDbType.Int);
                                        SqlParameter_id_order.Value = id;
                                        SqlParameter SqlParameter_dep = new SqlParameter("@dep", SqlDbType.NChar);
                                        SqlParameter_dep.Value = request.parameters[1];
                                        SqlParameter SqlParameter_arr = new SqlParameter("@arr", SqlDbType.NChar);
                                        SqlParameter_arr.Value = request.parameters[2];
                                        SqlParameter SqlParameter_price = new SqlParameter("@price", SqlDbType.Int);
                                        SqlParameter_price.Value = price;
                                        SqlParameter SqlParameter_num = new SqlParameter("@num", SqlDbType.Int);
                                        SqlParameter_num.Value = request.parameters[3];
                                        SqlParameter SqlParameter_move = new SqlParameter("@move", SqlDbType.NChar);
                                        SqlParameter_move.Value = move;
                                        SqlParameter SqlParameter_id_city = new SqlParameter("@id_city", SqlDbType.Int);
                                        SqlParameter_id_city.Value = city;

                                        //добавляем параметры к запросу
                                        cmd.Parameters.Add(SqlParameter_id_order);
                                        cmd.Parameters.Add(SqlParameter_dep);
                                        cmd.Parameters.Add(SqlParameter_arr);
                                        cmd.Parameters.Add(SqlParameter_price);
                                        cmd.Parameters.Add(SqlParameter_num);
                                        cmd.Parameters.Add(SqlParameter_move);
                                        cmd.Parameters.Add(SqlParameter_id_city);

                                        //выполнение запроса
                                        if (cmd.ExecuteNonQuery() == -1)
                                        {
                                            send.status = "ERROR BD";
                                            send.cod = "201";
                                            send.argument = new List<string>();
                                            json = JsonConvert.SerializeObject(send);
                                            json += "\r\n";
                                            dataWrite = Encoding.UTF8.GetBytes(json);
                                            writerStream.Write(dataWrite, 0, dataWrite.Length);
                                            Console.WriteLine("Error: строки для создания заказа не были добавлены");
                                            return 201;
                                        }

                                        if (request.parameters[0] == "info")
                                        {
                                            str_conn = "UPDATE заказы SET дата_закрытия =  GETDATE() WHERE id_заказа = @id_order";
                                            cmd = new SqlCommand(str_conn, Server.conn);

                                            //создаем параметры для запроса
                                            SqlParameter SqlParameter_id_order_1 = new SqlParameter("@id_order", SqlDbType.Int);
                                            SqlParameter_id_order_1.Value = id;

                                            //добавляем параметры к запросу
                                            cmd.Parameters.Add(SqlParameter_id_order_1);

                                            //выполнение запроса
                                            cmd.ExecuteNonQuery();
                                        }

                                        if (request.parameters[0] == "ok")
                                        {
                                            send.status = "OK";
                                            send.cod = "6";
                                            send.argument = new List<string>();
                                            send.argument.Add(Convert.ToString(id));
                                            send.argument.Add(Convert.ToString(price));
                                            send.argument.Add(Convert.ToString(yardage));
                                            json = JsonConvert.SerializeObject(send);
                                            json += "\r\n";
                                            dataWrite = Encoding.UTF8.GetBytes(json);
                                            writerStream.Write(dataWrite, 0, dataWrite.Length);
                                            Console.WriteLine("Новый заказ от пользователя" + number_user + " " + DateTime.Now.ToString());
                                            return 6;
                                        }
                                        else
                                        {
                                            send.status = "OK";
                                            send.cod = "13";
                                            send.argument = new List<string>();
                                            send.argument.Add(Convert.ToString(id));
                                            send.argument.Add(Convert.ToString(price));
                                            send.argument.Add(Convert.ToString(yardage));
                                            json = JsonConvert.SerializeObject(send);
                                            json += "\r\n";
                                            dataWrite = Encoding.UTF8.GetBytes(json);
                                            writerStream.Write(dataWrite, 0, dataWrite.Length);
                                            Console.WriteLine("запрос цены от пользователя" + number_user + " " + DateTime.Now.ToString());
                                            return 13;
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        send.status = "ERROR BD";
                                        send.cod = "201";
                                        send.argument = new List<string>();
                                        json = JsonConvert.SerializeObject(send);
                                        json += "\r\n";
                                        dataWrite = Encoding.UTF8.GetBytes(json);
                                        writerStream.Write(dataWrite, 0, dataWrite.Length);
                                        Console.WriteLine(e);
                                        return 201;
                                    }
                                    break;
                                }
                            case ("changeOrder"): //ПРОВЕРИТЬ РАБОТУ!!!!!!!!!!!!!!!!!!1
                                {
                                    byte[] dataWrite;
                                    Response send = new Response();
                                    string json = "";
                                    try
                                    {
                                        int id = Convert.ToInt32(request.parameters[0]);
                                        string move = "измененный"; //измененный значит что последние действие было изменение заказ пассажиром
                                        
                                        //Рассчет цены
                                        //Рассчет длины пути
                                        string str_conn = "select город from dbo.города where id_города = @city";
                                        SqlCommand cmd = new SqlCommand(str_conn, Server.conn);

                                        //создаем параметры для запроса
                                        SqlParameter SqlParam_city = new SqlParameter("@city", SqlDbType.NChar);
                                        SqlParam_city.Value = city;

                                        //добавляем параметры к запросу
                                        cmd.Parameters.Add(SqlParam_city);
                                        string name_city = Convert.ToString(cmd.ExecuteScalar());//выполнили запрос и сохранили результат в переменной
                                        string text_dep = name_city + " " + request.parameters[1];
                                        string text_arr = name_city + " " + request.parameters[2];
                                        string site = "https://maps.googleapis.com/maps/api/distancematrix/json?units=imperial&origins=" + text_dep + "&destinations=" + text_arr + "&key=AIzaSyCraPc_A9hC65AQ2GjVBBxtZwvWMUGUPqc";
                                        System.Net.WebClient web = new System.Net.WebClient();
                                        web.Encoding = Encoding.UTF8;
                                        json = web.DownloadString(site);

                                        GoogleDistance distance = JsonConvert.DeserializeObject<GoogleDistance>(json);

                                        double yardage = 0;
                                        if (distance.status == "OK")
                                        {
                                            if (distance.rows[0].elements[0].status == "OK")
                                                yardage = Math.Round(Convert.ToDouble(distance.rows[0].elements[0].distance.value) / 1000, 1);
                                            else
                                            {
                                                send.status = "ERROR";
                                                send.cod = "110";
                                                send.argument = new List<string>();
                                                json = JsonConvert.SerializeObject(send);
                                                json += "\r\n";
                                                dataWrite = Encoding.UTF8.GetBytes(json);
                                                writerStream.Write(dataWrite, 0, dataWrite.Length);
                                                return 110;
                                            }
                                        }
                                        else
                                        {
                                            send.status = "ERROR";
                                            send.cod = "137";
                                            send.argument = new List<string>();
                                            json = JsonConvert.SerializeObject(send);
                                            json += "\r\n";
                                            dataWrite = Encoding.UTF8.GetBytes(json);
                                            writerStream.Write(dataWrite, 0, dataWrite.Length);
                                            return 137;
                                        }

                                        str_conn = "select TOP(1) * from dbo.тарифы where id_города = @city ORDER BY дата desc";
                                        cmd = new SqlCommand(str_conn, Server.conn);

                                        //создаем параметры для запроса
                                        SqlParameter SqlParam_city1 = new SqlParameter("@city", SqlDbType.NChar);
                                        SqlParam_city1.Value = city;

                                        //добавляем параметры к запросу
                                        cmd.Parameters.Add(SqlParam_city1);

                                        //выполнение запроса
                                        SqlDataReader sql_read = cmd.ExecuteReader();

                                        //ПОЛУЧЕНИЕ тарифов для расчета стоимости поездки
                                        int km1, km2, km_b, inaction, constant;//за 1км, за второй км, за последующие км, за ожидание, за вызов
                                        if (sql_read.HasRows)
                                        {
                                            sql_read.Read();
                                            {
                                                km1 = Convert.ToInt32(sql_read.GetValue(2));
                                                km2 = Convert.ToInt32(sql_read.GetValue(3));
                                                km_b = Convert.ToInt32(sql_read.GetValue(4));
                                                inaction = Convert.ToInt32(sql_read.GetValue(5));
                                                constant = Convert.ToInt32(sql_read.GetValue(6));
                                            }

                                            price = constant;
                                            if (yardage > 2) price += km1 + km2 + (yardage - 2) * km_b;
                                            else if (yardage > 1) price += km1 + (yardage - 1) * km2;
                                            else if (yardage > 0) price += km1;
                                        }

                                        // закрываем reader
                                        if (sql_read != null)
                                        {
                                            sql_read.Close();
                                        }

                                        str_conn = "INSERT INTO заказ_параметр(id_заказа, дата_время, адрес_отправления, адрес_прибытия, стоимость, количество_пассажиров, действие)"
                                                            + "values(@id_order, GETDATE(), @dep, @arr, @price, @num, @move)";
                                        cmd = new SqlCommand(str_conn, Server.conn);

                                        //создаем параметры для запроса
                                        SqlParameter param_id_order = new SqlParameter("@id_order", SqlDbType.Int);
                                        param_id_order.Value = id;
                                        SqlParameter param_dep = new SqlParameter("@dep", SqlDbType.NChar);
                                        param_dep.Value = request.parameters[1];
                                        SqlParameter param_arr = new SqlParameter("@arr", SqlDbType.NChar);
                                        param_arr.Value = request.parameters[2];
                                        SqlParameter param_price = new SqlParameter("@price", SqlDbType.Int);
                                        param_price.Value = price;
                                        SqlParameter param_num = new SqlParameter("@num", SqlDbType.Int);
                                        param_num.Value = request.parameters[3];
                                        SqlParameter param_move = new SqlParameter("@move", SqlDbType.NChar);
                                        param_move.Value = move;

                                        //добавляем параметры к запросу
                                        cmd.Parameters.Add(param_id_order);
                                        cmd.Parameters.Add(param_dep);
                                        cmd.Parameters.Add(param_arr);
                                        cmd.Parameters.Add(param_price);
                                        cmd.Parameters.Add(param_num);
                                        cmd.Parameters.Add(param_move);

                                        //выполнение запроса
                                        if (cmd.ExecuteNonQuery() == -1)
                                        {
                                            send.status = "ERROR";
                                            send.cod = "106";
                                            send.argument = new List<string>();
                                            json = JsonConvert.SerializeObject(send);
                                            json += "\r\n";
                                            dataWrite = Encoding.UTF8.GetBytes(json);
                                            writerStream.Write(dataWrite, 0, dataWrite.Length);
                                            Console.WriteLine("Error: строки для создания заказа не были добавлены возможно заказа с таким номером нет");
                                            return 106;
                                        }
                                        else
                                        {
                                            send.status = "OK";
                                            send.cod = "5";
                                            send.argument = new List<string>();
                                            send.argument.Add(Convert.ToString(id));
                                            send.argument.Add(Convert.ToString(price));
                                            send.argument.Add(Convert.ToString(yardage));
                                            json = JsonConvert.SerializeObject(send);
                                            json += "\r\n";
                                            dataWrite = Encoding.UTF8.GetBytes(json);
                                            writerStream.Write(dataWrite, 0, dataWrite.Length);
                                            Console.WriteLine("Новый заказ от пользователя" + number_user + " " + DateTime.Now.ToString());
                                            return 5;
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        send.status = "ERROR BD";
                                        send.cod = "201";
                                        send.argument = new List<string>();
                                        json = JsonConvert.SerializeObject(send);
                                        json += "\r\n";
                                        dataWrite = Encoding.UTF8.GetBytes(json);
                                        writerStream.Write(dataWrite, 0, dataWrite.Length);
                                        Console.WriteLine(e);
                                        return 201;
                                    }
                                    break;
                                }
                            case ("killOrder"):
                                {
                                    byte[] dataWrite;
                                    Response send = new Response();
                                    string json = "";
                                    try
                                    {
                                        //проверяем есть ли такой номер уже в системе
                                        string str_conn = "select id_заказа from dbo.заказы_в_работе where номер_телефона = @number and id_заказа=@id_order";
                                        SqlCommand cmd = new SqlCommand(str_conn, Server.conn);

                                        //создаем параметры для запроса
                                        SqlParameter SqlParam_number = new SqlParameter("@number", SqlDbType.Decimal);
                                        SqlParam_number.Precision = 11;
                                        SqlParam_number.Scale = 0;
                                        SqlParam_number.Value = number_user;
                                        SqlParameter SqlParam_id_order = new SqlParameter("@id_order", SqlDbType.Int);
                                        SqlParam_id_order.Value = request.parameters[0];

                                        //добавляем параметры к запросу
                                        cmd.Parameters.Add(SqlParam_number);
                                        cmd.Parameters.Add(SqlParam_id_order);

                                        //если в результате есть строки
                                        if (cmd.ExecuteScalar()!=null)
                                        {
                                            str_conn = "UPDATE заказы SET дата_закрытия =  GETDATE() WHERE id_заказа = @id_order; " +
                                                       "INSERT INTO заказ_параметр(id_заказа, дата_время, действие)" +
                                                       "values(@id_order, GETDATE(), @move)";
                                            cmd = new SqlCommand(str_conn, Server.conn);

                                            //создаем параметры для запроса
                                            SqlParameter SqlParameter_id_order = new SqlParameter("@id_order", SqlDbType.Int);
                                            SqlParameter_id_order.Value = request.parameters[0];
                                            SqlParameter SqlParameter_move = new SqlParameter("@move", SqlDbType.NChar);
                                            SqlParameter_move.Value = "отмена заказчиком";

                                            //добавляем параметры к запросу
                                            cmd.Parameters.Add(SqlParameter_id_order);
                                            cmd.Parameters.Add(SqlParameter_move);

                                            //выполнение запроса
                                            if (cmd.ExecuteNonQuery() != -1)
                                            {
                                                send.status = "OK";
                                                send.cod = "7";
                                                json = JsonConvert.SerializeObject(send);
                                                json += "\r\n";
                                                dataWrite = Encoding.UTF8.GetBytes(json);
                                                writerStream.Write(dataWrite, 0, dataWrite.Length);
                                                return 7;
                                            }
                                            else
                                            {
                                                send.status = "ERROR BD";
                                                send.cod = "201";
                                                send.argument = new List<string>();
                                                json = JsonConvert.SerializeObject(send);
                                                json += "\r\n";
                                                dataWrite = Encoding.UTF8.GetBytes(json);
                                                writerStream.Write(dataWrite, 0, dataWrite.Length);
                                                Console.WriteLine("Error: Заказа от" + number_user + " не получилось изменить " + DateTime.Now.ToString());
                                                return 201;
                                            }
                                        }
                                        else
                                        {
                                            send.status = "ERROR";
                                            send.cod = "114";
                                            send.argument = new List<string>();
                                            json = JsonConvert.SerializeObject(send);
                                            json += "\r\n";
                                            dataWrite = Encoding.UTF8.GetBytes(json);
                                            writerStream.Write(dataWrite, 0, dataWrite.Length);
                                            return 114;
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        send.status = "ERROR BD";
                                        send.cod = "201";
                                        send.argument = new List<string>();
                                        json = JsonConvert.SerializeObject(send);
                                        json += "\r\n";
                                        dataWrite = Encoding.UTF8.GetBytes(json);
                                        writerStream.Write(dataWrite, 0, dataWrite.Length);
                                        Console.WriteLine(e);
                                        return 201;
                                    }        
                                    break;
                                }
                            case ("getStateOrder"):
                                {
                                    string orders = "";
                                    byte[] dataWrite;
                                    Response send = new Response();
                                    send.argument = new List<string>();
                                    string json = "";
                                    try
                                    {
                                        string str_conn = "select s.id_заказа, дата_создания, адрес_отправления, адрес_прибытия, стоимость from заказы_в_работе inner join (select id_заказа,max(дата_время) time from заказ_параметр group by id_заказа) s " +
                                                          "on заказы_в_работе.дата_время = s.time where номер_телефона = @number";
                                        SqlCommand cmd = new SqlCommand(str_conn, Server.conn);

                                        //создаем параметры для запроса
                                        SqlParameter SqlParam_number = new SqlParameter("@number", SqlDbType.Decimal);
                                        SqlParam_number.Value = number_user;

                                        //добавляем параметры к запросу
                                        cmd.Parameters.Add(SqlParam_number);

                                        SqlDataReader sql_read = null;

                                        //выполнение запроса
                                        sql_read = cmd.ExecuteReader();

                                        //если в результате запроса есть строки то 
                                        if (sql_read.HasRows)
                                        {
                                            //считываем параметры заказов
                                            while (sql_read.Read())
                                            {
                                                Order o = new Order();
                                                o.id = Convert.ToString(sql_read.GetValue(0));
                                                o.date = Convert.ToString(sql_read.GetValue(1));
                                                o.dep = Convert.ToString(sql_read.GetValue(2));
                                                o.arr = Convert.ToString(sql_read.GetValue(3));
                                                o.price = Convert.ToString(sql_read.GetValue(4));
                                                json = JsonConvert.SerializeObject(o);
                                                send.argument.Add(json);
                                            }
                                        }

                                        // закрываем reader
                                        if (sql_read != null)
                                        {
                                            sql_read.Close();
                                        }


                                        send.status = "OK";
                                        send.cod = "8";   
                                        json = JsonConvert.SerializeObject(send);
                                        json += "\r\n";
                                        dataWrite = Encoding.UTF8.GetBytes(json);
                                        writerStream.Write(dataWrite, 0, dataWrite.Length);
                                        Console.WriteLine("Пользователь " + number_user + " " + DateTime.Now.ToString() + " запросил список СВОИХ заказов");
                                        return 8;
                                    }
                                    catch(Exception e)
                                    {
                                        send.status = "ERROR BD";
                                        send.cod = "201";
                                        send.argument = new List<string>();
                                        json = JsonConvert.SerializeObject(send);
                                        json += "\r\n";
                                        dataWrite = Encoding.UTF8.GetBytes(json);
                                        writerStream.Write(dataWrite, 0, dataWrite.Length);
                                        Console.WriteLine(e);
                                        return 201;
                                    }
                                    break;
                                }
                            case ("getHistory"):
                                {
                                    string orders = "";
                                    byte[] dataWrite;
                                    Response send = new Response();
                                    send.argument = new List<string>();
                                    string json = "";
                                    try
                                    {
                                        string str_conn = "select * from (заказы inner join заказ_параметр on заказы.id_заказа=заказ_параметр.id_заказа), "+
                                                          "(select id_заказа, max(дата_время) time from заказ_параметр group by id_заказа) t "+
                                                          "where дата_закрытия is not null and действие<>'инфо' and t.id_заказа = заказы.id_заказа "+
                                                          "and номер_телефона = @number and дата_время = time and адрес_отправления is not null";
                                        SqlCommand cmd = new SqlCommand(str_conn, Server.conn);

                                        //создаем параметры для запроса
                                        SqlParameter SqlParam_number = new SqlParameter("@number", SqlDbType.Decimal);
                                        SqlParam_number.Value = number_user;

                                        //добавляем параметры к запросу
                                        cmd.Parameters.Add(SqlParam_number);

                                        SqlDataReader sql_read = null;

                                        //выполнение запроса
                                        sql_read = cmd.ExecuteReader();

                                        //если в результате запроса есть строки то 
                                        if (sql_read.HasRows)
                                        {
                                            //считываем параметры заказов
                                            while (sql_read.Read())
                                            {
                                                send.status = "OK";
                                                send.cod = "19";

                                                Order o = new Order();
                                                o.id = Convert.ToString(sql_read.GetValue(0));
                                                o.dep = Convert.ToString(sql_read.GetValue(8));
                                                o.arr = Convert.ToString(sql_read.GetValue(9));
                                                o.date = Convert.ToString(sql_read.GetValue(2));
                                                o.price = Convert.ToString(sql_read.GetValue(10));
                                                json = JsonConvert.SerializeObject(o);
                                                send.argument.Add(json);
                                            }
                                        }
                                        else
                                        {
                                            send.status = "OK";
                                            send.cod = "20";
                                        }

                                        // закрываем reader
                                        if (sql_read != null)
                                        {
                                            sql_read.Close();
                                        }

                                        json = JsonConvert.SerializeObject(send);
                                        json += "\r\n";
                                        dataWrite = Encoding.UTF8.GetBytes(json);
                                        writerStream.Write(dataWrite, 0, dataWrite.Length);
                                        Console.WriteLine("Пользователь " + number_user + " " + DateTime.Now.ToString() + " запросил историю заказов");
                                        return Convert.ToInt32(send.cod);
                                    }
                                    catch (Exception e)
                                    {
                                        send.status = "ERROR BD";
                                        send.cod = "201";
                                        send.argument = new List<string>();
                                        json = JsonConvert.SerializeObject(send);
                                        json += "\r\n";
                                        dataWrite = Encoding.UTF8.GetBytes(json);
                                        writerStream.Write(dataWrite, 0, dataWrite.Length);
                                        Console.WriteLine(e);
                                        return 201;
                                    }
                                    break;
                                }
                            default: { Console.WriteLine("неизвестная просьба в запросе: "); return 115; break; }
                        }
                        break;
                    }
                case ("d"):
                    {
                        switch (request.command)
                        {
                            case ("hello"):
                                {
                                    byte[] dataWrite;
                                    Response send = new Response();
                                    string json = "";
                                    try
                                    {
                                        //Повтороно хешируем присланный пароль так как в БД хранится двукратнохешированный пароль
                                        MD5 MD5_cod = new MD5CryptoServiceProvider();
                                        byte[] hash = MD5_cod.ComputeHash(Encoding.UTF8.GetBytes(request.parameters[1]));
                                        string pas = "";
                                        foreach (byte a in hash) pas += a.ToString("x");


                                        SqlCommand cmd = new SqlCommand("d_hello", Server.conn);
                                        cmd.CommandType = CommandType.StoredProcedure;

                                        SqlParameter SqlParam_res = new SqlParameter("@res", SqlDbType.Int);
                                        SqlParam_res.Value = 201;
                                        cmd.Parameters.Add(SqlParam_res).Direction = ParameterDirection.InputOutput;
                                        SqlParameter SqlParam_number = new SqlParameter("@number_d", SqlDbType.Decimal);
                                        SqlParam_number.Precision = 11;
                                        SqlParam_number.Scale = 0;
                                        SqlParam_number.Value = request.parameters[0];
                                        cmd.Parameters.Add(SqlParam_number);
                                        SqlParameter SqlParam_pas = new SqlParameter("@pas", SqlDbType.NChar);
                                        SqlParam_pas.Value = pas;
                                        cmd.Parameters.Add(SqlParam_pas);

                                        cmd.ExecuteNonQuery();
                                        string result = Convert.ToString(cmd.Parameters["@res"].Value);

                                        if(result=="17")
                                        {
                                            SqlDataReader sql_read = null;
                                            try
                                            {
                                                string str_conn = "select * from dbo.водители where номер_водителя = @number";
                                                cmd = new SqlCommand(str_conn, Server.conn);

                                                //создаем параметры для запроса
                                                SqlParameter SqlParam_number1 = new SqlParameter("@number", SqlDbType.Decimal);
                                                SqlParam_number1.Precision = 11;
                                                SqlParam_number1.Scale = 0;
                                                SqlParam_number1.Value = request.parameters[0];
                                                cmd.Parameters.Add(SqlParam_number1);

                                                //выполнение запроса
                                                
                                                sql_read = cmd.ExecuteReader();

                                                if (sql_read.HasRows)
                                                {
                                                    sql_read.Read();
                                                    {
                                                        number_user = Convert.ToString(sql_read.GetValue(0)).Trim();
                                                        name = Convert.ToString(sql_read.GetValue(1)).Trim();
                                                        patronymic = Convert.ToString(sql_read.GetValue(2)).Trim();
                                                        color = Convert.ToString(sql_read.GetValue(3)).Trim();
                                                        brand = Convert.ToString(sql_read.GetValue(4)).Trim();
                                                        number_auto = Convert.ToString(sql_read.GetValue(6)).Trim();
                                                        city = Convert.ToInt32(sql_read.GetValue(7));
                                                    }
                                                }

                                                // закрываем reader
                                                if (sql_read != null)
                                                {
                                                    sql_read.Close();
                                                }
                                            }
                                            catch (Exception e) //ВСТАВИТЬ ОБРАБОТКУ ОШИБКИ!!!!
                                            {
                                                result = "201";
                                                // закрываем reader
                                                if (sql_read != null)
                                                {
                                                    sql_read.Close();
                                                }
                                            }
                                        }

                                        send.status = "";
                                        send.cod = result;
                                        send.argument = new List<string>();
                                        send.argument.Add(name);
                                        send.argument.Add(patronymic);
                                        send.argument.Add(color);
                                        send.argument.Add(brand);
                                        send.argument.Add(number_auto);
                                        send.argument.Add(Convert.ToString(city));
                                        json = JsonConvert.SerializeObject(send);
                                        json += "\r\n";
                                        dataWrite = Encoding.UTF8.GetBytes(json);
                                        writerStream.Write(dataWrite, 0, dataWrite.Length);
                                        Console.WriteLine("Водитель " + number_user + " " + DateTime.Now.ToString() + " попытался войти в ситему, ему вернулся код " + result);
                                        return Convert.ToInt32(result);
                                    }
                                    catch (Exception e)
                                    {
                                        send.status = "ERROR BD";
                                        send.cod = "201";
                                        send.argument = new List<string>();
                                        json = JsonConvert.SerializeObject(send);
                                        json += "\r\n";
                                        dataWrite = Encoding.UTF8.GetBytes(json);
                                        writerStream.Write(dataWrite, 0, dataWrite.Length);
                                        Console.WriteLine(e);
                                        return 201;
                                    }
                                    break;
                                }
                            case ("getOrder")://!!!!!!!
                                {
                                    string orders = "";
                                    byte[] dataWrite;
                                    Response send = new Response();
                                    send.argument = new List<string>();
                                    string json = "";
                                    try
                                    {
                                        string str_conn = "select * from заказы_в_работе inner join (select id_заказа,max(дата_время) time from заказ_параметр group by id_заказа) s " +
                                                          "on заказы_в_работе.дата_время = s.time where номер_телефона_водителя is null and id_города=@city and (id_заказа_попутчика is not null or (попутчик is null or попутчик=0))";
                                        SqlCommand cmd = new SqlCommand(str_conn, Server.conn);

                                        //создаем параметры для запроса
                                        SqlParameter SqlParam_city = new SqlParameter("@city", SqlDbType.Int);
                                        SqlParam_city.Value = city;

                                        //добавляем параметры к запросу
                                        cmd.Parameters.Add(SqlParam_city);

                                        SqlDataReader sql_read = null;

                                        //выполнение запроса
                                        sql_read = cmd.ExecuteReader();

                                        //если в результате запроса есть строки то 
                                        if (sql_read.HasRows)
                                        {
                                            while (sql_read.Read())
                                            {
                                                send.status = "OK";
                                                send.cod = "9";

                                                Order o = new Order();
                                                o.id = Convert.ToString(sql_read.GetValue(0));
                                                o.dep = Convert.ToString(sql_read.GetValue(2));
                                                o.arr = Convert.ToString(sql_read.GetValue(3));
                                                o.date = Convert.ToString(sql_read.GetValue(1));
                                                o.price = Convert.ToString(sql_read.GetValue(6));
                                                json = JsonConvert.SerializeObject(o);
                                                send.argument.Add(json);
                                            }
                                        }

                                        // закрываем reader
                                        if (sql_read != null)
                                        {
                                            sql_read.Close();
                                        }

                                        json = JsonConvert.SerializeObject(send);
                                        json += "\r\n";
                                        dataWrite = Encoding.UTF8.GetBytes(json);
                                        writerStream.Write(dataWrite, 0, dataWrite.Length);
                                        Console.WriteLine("Водитель " + number_user + " " + DateTime.Now.ToString() + " запросил список СВОИХ заказов");
                                        return 9;
                                    }
                                    catch (Exception e)
                                    {
                                        send.status = "ERROR BD";
                                        send.cod = "201";
                                        json = JsonConvert.SerializeObject(send);
                                        json += "\r\n";
                                        dataWrite = Encoding.UTF8.GetBytes(json);
                                        writerStream.Write(dataWrite, 0, dataWrite.Length);
                                        Console.WriteLine(e);
                                        return 201;
                                    }
                                    break;
                                }
                            case ("takeOrder"):
                                {
                                    byte[] dataWrite;
                                    Response send = new Response();
                                    string json = "";
                                    try
                                    {
                                        SqlCommand cmd = new SqlCommand("takeOrder", Server.conn);
                                        cmd.CommandType = CommandType.StoredProcedure;

                                        SqlParameter SqlParam_Res = new SqlParameter("@res", SqlDbType.Int);
                                        SqlParam_Res.Value = 201;
                                        cmd.Parameters.Add(SqlParam_Res).Direction = ParameterDirection.InputOutput;
                                        SqlParameter SqlParam_id_order = new SqlParameter("@id_order", SqlDbType.Int);
                                        SqlParam_id_order.Value = request.parameters[0];
                                        cmd.Parameters.Add(SqlParam_id_order);
                                        SqlParameter SqlParam_number_d = new SqlParameter("@number_d", SqlDbType.Decimal);
                                        SqlParam_number_d.Value = number_user;
                                        cmd.Parameters.Add(SqlParam_number_d);

                                        cmd.ExecuteNonQuery();                       
                                        string result = Convert.ToString(cmd.Parameters["@res"].Value);

                                        if(result == "12")
                                        {
                                            cmd = new SqlCommand("select номер_телефона from заказы where id_заказа=@id_order", Server.conn);

                                            SqlParameter SqlParam_id_order1 = new SqlParameter("@id_order", SqlDbType.Int);
                                            SqlParam_id_order1.Value = request.parameters[0];
                                            cmd.Parameters.Add(SqlParam_id_order1);

                                            string num_passenger = Convert.ToString(cmd.ExecuteScalar());
                                            SMSC smsc = new SMSC();
                                            SMS_pas = "Скоро к вам подъедит "+name +" "+ patronymic +" на "+ color + " " + brand + " c номером: " + number_auto;
                                            string[] smsc_rez = smsc.send_sms(num_passenger, SMS_pas , 0); // ЗАМЕНИТЬ СВОЙ НОМЕР НА НОМЕР ПРОФИЛЯ!!!!!!!!1

                                            send.status = "OK";
                                            send.cod = result;
                                            send.argument = new List<string>();
                                            json = JsonConvert.SerializeObject(send);
                                            json += "\r\n";
                                            dataWrite = Encoding.UTF8.GetBytes(json);
                                            writerStream.Write(dataWrite, 0, dataWrite.Length);
                                            Console.WriteLine("Водитель " + number_user + " " + DateTime.Now.ToString() + " принял заказ и получил ответ " + result);
                                            return Convert.ToInt32(result);
                                        }
                                        send.status = "ERROR"; 
                                        send.cod = result;
                                        send.argument = new List<string>();
                                        json = JsonConvert.SerializeObject(send);
                                        json += "\r\n";
                                        dataWrite = Encoding.UTF8.GetBytes(json);
                                        writerStream.Write(dataWrite, 0, dataWrite.Length);
                                        Console.WriteLine("Водитель " + number_user + " " + DateTime.Now.ToString() + " принял заказ и получил ответ "+ result);
                                        return Convert.ToInt32(result);
                                    }
                                    catch (Exception e)
                                    {
                                        send.status = "ERROR BD";
                                        send.cod = "201";
                                        send.argument = new List<string>();
                                        json = JsonConvert.SerializeObject(send);
                                        json += "\r\n";
                                        dataWrite = Encoding.UTF8.GetBytes(json);
                                        writerStream.Write(dataWrite, 0, dataWrite.Length);
                                        Console.WriteLine(e);
                                        return 201;
                                    }
                                    break;
                                }
                            case ("killOrder"):
                                {
                                    byte[] dataWrite;
                                    Response send = new Response();
                                    string json = "";
                                    try
                                    {
                                        SqlCommand cmd = new SqlCommand("killOrder", Server.conn);
                                        cmd.CommandType = CommandType.StoredProcedure;

                                        SqlParameter SqlParam_Res = new SqlParameter("@res", SqlDbType.Int);
                                        SqlParam_Res.Value = 201;
                                        cmd.Parameters.Add(SqlParam_Res).Direction = ParameterDirection.InputOutput;
                                        SqlParameter SqlParam_id_order = new SqlParameter("@id_order", SqlDbType.Int);
                                        SqlParam_id_order.Value = request.parameters[0];
                                        cmd.Parameters.Add(SqlParam_id_order);
                                        SqlParameter SqlParam_number_d = new SqlParameter("@number_d", SqlDbType.Decimal);
                                        SqlParam_number_d.Value = number_user;
                                        cmd.Parameters.Add(SqlParam_number_d);

                                        cmd.ExecuteNonQuery();
                                        string result = Convert.ToString(cmd.Parameters["@res"].Value);

                                        if (result == "14") send.status = "OK";
                                        else send.status = "ERROR";
                                        send.cod = result;
                                        send.argument = new List<string>();
                                        json = JsonConvert.SerializeObject(send);
                                        json += "\r\n";
                                        dataWrite = Encoding.UTF8.GetBytes(json);
                                        writerStream.Write(dataWrite, 0, dataWrite.Length);
                                        Console.WriteLine("Водитель " + number_user + " " + DateTime.Now.ToString() + " отказался от заказа и получил ответ " + result);
                                        return Convert.ToInt32(result);
                                    }
                                    catch (Exception e)
                                    {
                                        send.status = "ERROR BD";
                                        send.cod = "201";
                                        send.argument = new List<string>();
                                        json = JsonConvert.SerializeObject(send);
                                        json += "\r\n";
                                        dataWrite = Encoding.UTF8.GetBytes(json);
                                        writerStream.Write(dataWrite, 0, dataWrite.Length);
                                        Console.WriteLine(e);
                                        return 201;
                                    }
                                    break;
                                }
                            case ("changeOrder")://ПРОВЕРИТЬ РАБОТОСПОСОБНОСТЬ!!!!
                                {
                                    byte[] dataWrite;
                                    Response send = new Response();
                                    string json = "";
                                    try
                                    {
                                        SqlCommand cmd = new SqlCommand("changeOrder", Server.conn);
                                        cmd.CommandType = CommandType.StoredProcedure;

                                        SqlParameter SqlParam_Res = new SqlParameter("@res", SqlDbType.Int);
                                        SqlParam_Res.Value = 201;
                                        cmd.Parameters.Add(SqlParam_Res).Direction = ParameterDirection.InputOutput;
                                        SqlParameter SqlParam_id_order = new SqlParameter("@id_order", SqlDbType.Int);
                                        SqlParam_id_order.Value = request.parameters[0];
                                        cmd.Parameters.Add(SqlParam_id_order);
                                        SqlParameter SqlParam_number_d = new SqlParameter("@number_d", SqlDbType.Decimal);
                                        SqlParam_number_d.Value = number_user;
                                        cmd.Parameters.Add(SqlParam_number_d);
                                        SqlParameter SqlParam_state = new SqlParameter("@state", SqlDbType.NChar);
                                        SqlParam_state.Value = request.parameters[1];
                                        cmd.Parameters.Add(SqlParam_state);

                                        cmd.ExecuteNonQuery();
                                        string result = Convert.ToString(cmd.Parameters["@res"].Value);

                                        if (result == "11") send.status = "OK";
                                        else send.status = "ERROR";
                                        send.cod = result;
                                        send.argument = new List<string>();
                                        json = JsonConvert.SerializeObject(send);
                                        json += "\r\n";
                                        dataWrite = Encoding.UTF8.GetBytes(json);
                                        writerStream.Write(dataWrite, 0, dataWrite.Length);
                                        Console.WriteLine("Водитель " + number_user + " " + DateTime.Now.ToString() + " изменил состояние заказа " + result);
                                        return Convert.ToInt32(result);
                                    }
                                    catch (Exception e)
                                    {
                                        send.status = "ERROR BD";
                                        send.cod = "201";
                                        send.argument = new List<string>();
                                        json = JsonConvert.SerializeObject(send);
                                        json += "\r\n";
                                        dataWrite = Encoding.UTF8.GetBytes(json);
                                        writerStream.Write(dataWrite, 0, dataWrite.Length);
                                        Console.WriteLine(e);
                                        return 201;
                                    }
                                    break;
                                }
                            case ("getState"):
                                {
                                    string orders = "";
                                    byte[] dataWrite;
                                    Response send = new Response();
                                    send.argument = new List<string>();
                                    string json = "";
                                    try
                                    {
                                        string str_conn = "select s.id_заказа, дата_создания, адрес_отправления, адрес_прибытия, стоимость, действие, номер_телефона_водителя "+
                                                          "from заказы_в_работе "+
                                                          "inner join (select id_заказа,max(дата_время) time from заказ_параметр group by id_заказа) s "+
                                                          "on заказы_в_работе.дата_время = s.time where номер_телефона_водителя = @number";
                                        SqlCommand cmd = new SqlCommand(str_conn, Server.conn);

                                        //создаем параметры для запроса
                                        SqlParameter SqlParam_number = new SqlParameter("@number", SqlDbType.Decimal);
                                        SqlParam_number.Value = number_user;

                                        //добавляем параметры к запросу
                                        cmd.Parameters.Add(SqlParam_number);

                                        SqlDataReader sql_read = null;

                                        //выполнение запроса
                                        sql_read = cmd.ExecuteReader();

                                        //если в результате запроса есть строки то 
                                        if (sql_read.HasRows)
                                        {
                                            //считываем параметры заказов
                                            while (sql_read.Read())
                                            {
                                                Order o = new Order();
                                                o.id = Convert.ToString(sql_read.GetValue(0));
                                                o.date = Convert.ToString(sql_read.GetValue(1));
                                                o.dep = Convert.ToString(sql_read.GetValue(2));
                                                o.arr = Convert.ToString(sql_read.GetValue(3));
                                                o.price = Convert.ToString(sql_read.GetValue(4));
                                                o.move = Convert.ToString(sql_read.GetValue(5));
                                                json = JsonConvert.SerializeObject(o);
                                                send.argument.Add(json);
                                            }
                                        }

                                        // закрываем reader
                                        if (sql_read != null)
                                        {
                                            sql_read.Close();
                                        }

                                        send.status = "OK";
                                        send.cod = "10";
                                        json = JsonConvert.SerializeObject(send);
                                        json += "\r\n";
                                        dataWrite = Encoding.UTF8.GetBytes(json);
                                        writerStream.Write(dataWrite, 0, dataWrite.Length);
                                        Console.WriteLine("Водитель " + number_user + " " + DateTime.Now.ToString() + " запросил список СВОИХ заказов");
                                        return 10;
                                    }
                                    catch (Exception e)
                                    {
                                        send.status = "ERROR BD";
                                        send.cod = "201";
                                        send.argument = new List<string>();
                                        json = JsonConvert.SerializeObject(send);
                                        json += "\r\n";
                                        dataWrite = Encoding.UTF8.GetBytes(json);
                                        writerStream.Write(dataWrite, 0, dataWrite.Length);
                                        Console.WriteLine(e);
                                        return 201;
                                    }
                                    break;
                                }
                            default: { Console.WriteLine("неизвестная просьба в запросе: " ); return 202; break; }
                        }
                        break;
                    }
                default: { Console.WriteLine("неизвестный отправитель запроса: "); return 203; break; }
            }
        }

        //метод для работы с клиентом
        public void RunClient()
        {
            string returnData = "";
            StreamReader readerStream;
            NetworkStream writerStream;

            try
            {
                // Создаем классы потоков
                readerStream = new StreamReader(clientSocket.GetStream());
                writerStream = clientSocket.GetStream();

                while (true)
                {
                    if (returnData != null)
                    {
                        try
                        {
                            returnData = readerStream.ReadLine();
                            Request request = JsonConvert.DeserializeObject<Request>(returnData);
                            GetRequest(writerStream, readerStream, request);
                        }
                        catch (Exception exc) { clientSocket.Close(); break; }
                    }
                }
            }
            catch (IOException)
            {
                clientSocket.Close();
                //Server.N--;
                //if (authorized == true) Server.N_authorized--;
                Console.WriteLine("Кто-то разорвал соединение с сервером");
            }
            catch (System.ObjectDisposedException)
            {
                clientSocket.Close();
                //Server.N--;
                //if (authorized == true) Server.N_authorized--;
                Console.WriteLine("Кто-то разорвал соединение с сервером");
            }
        }
    }
}
