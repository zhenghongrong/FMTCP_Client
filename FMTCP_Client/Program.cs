﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;


namespace FMTCP_Client
{
    class Program
    {
        static Socket tcpClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        static IPAddress ipaddress = IPAddress.Parse("192.168.43.203");
        static EndPoint point = new IPEndPoint(ipaddress, 5002);

        #region 主逻辑
        static void Main(string[] args)
        {
            tcpClient.Connect(point);
            int k = 0;

            while (true)
            {
                //Console.WriteLine(Get_Value("VT", "学习机_1ECU条码"));
                Console.WriteLine(Write_Value("VA", "VA1", ".3"));
                k++;
                Thread.Sleep(1000);
            }
        }
        #endregion

        #region 写入变量内容
        static string Write_Value(string type, string name, string value)
        {
            int value_command_type = 0;//装变量类型的关键数据
            byte[] write_value_feedback = new byte[4];//装SCADA返回的数据
            string return_out;//返回给调用者

            switch (type)
            {
                case "AO": value_command_type = 2; break;
                case "AR": value_command_type = 3; break;
                case "DO": value_command_type = 5; break;
                case "DR": value_command_type = 6; break;
                case "VA": value_command_type = 7; break;
                case "VD": value_command_type = 8; break;
                case "VT": value_command_type = 9; break;
                default: break;
            }

            byte[] index = Get_Index(type, name);//获取变量索引
            byte[] write_command_init = new byte[] { 62, 42, 39, 19, (byte)value_command_type, index[0], index[1], index[2], index[3] };//发送命令的初始化指令

            try
            {
                #region 数值类型
                if (value_command_type == 2 || value_command_type == 3 || value_command_type == 7)
                {
                    #region double类型
                    if (value.IndexOf('.') > 0)
                    {
                        byte[] write_values_command = new byte[18];//写值初始化指令

                        double value_string_to_double = double.Parse(value);

                        for (int i = 0; i < write_command_init.Length; i++)
                        {
                            write_values_command[i] = write_command_init[i];
                        }

                        write_values_command[9] = 8;//发送double类型，【9】=8。

                        byte[] value_to_byte = BitConverter.GetBytes(value_string_to_double);//将double类型数值转化为数组
                        for (int i = 0; i < value_to_byte.Length; i++)
                            write_values_command[10 + i] = value_to_byte[i];//改写发送到SCADA的命令

                        write_value_feedback = Send(write_values_command);//获取SCADA返回的数据
                    }
                    #endregion

                    #region int类型
                    else
                    {
                        byte[] write_values_command = new byte[14];//写值初始化指令

                        int value_string_to_int = int.Parse(value);

                        for (int i = 0; i < write_command_init.Length; i++)
                        {
                            write_values_command[i] = write_command_init[i];
                        }

                        write_values_command[9] = 4;//发送int类型，【9】=4。

                        byte[] value_to_byte = BitConverter.GetBytes(value_string_to_int);//将double类型数值转化为数组
                        for (int i = 0; i < value_to_byte.Length; i++)
                            write_values_command[10 + i] = value_to_byte[i];//改写发送到SCADA的命令

                        write_value_feedback = Send(write_values_command);//获取SCADA返回的数据
                    }
                    #endregion
                }
                #endregion

                #region 布尔类型
                else if (value_command_type == 5 || value_command_type == 6 || value_command_type == 8)
                {
                    byte[] write_values_command = new byte[11];//写值初始化指令

                    int value_string_to_int = int.Parse(value);

                    for (int i = 0; i < write_command_init.Length; i++)
                    {
                        write_values_command[i] = write_command_init[i];
                    }

                    write_values_command[9] = 1;//发送bool类型，【9】=1。
                    write_values_command[10] = (byte)int.Parse(value);

                    write_value_feedback = Send(write_values_command);//获取SCADA返回的数据
                }
                #endregion

                #region 文本类型
                else if (value_command_type == 9)
                {
                    byte[] write_values_command = new byte[200];//写值初始化指令

                    for (int i = 0; i < write_command_init.Length; i++)
                    {
                        write_values_command[i] = write_command_init[i];
                    }

                    byte[] value_to_byte = Encoding.Default.GetBytes(value);//将string类型内容转化为数组

                    write_values_command[9] = (byte)value_to_byte.Length;//发送string类型，【9】=内容的长度

                    for (int i = 0; i < value_to_byte.Length; i++)
                        write_values_command[10 + i] = value_to_byte[i];//改写发送到SCADA的命令

                    write_value_feedback = Send(write_values_command);//获取SCADA返回的数据
                }
                #endregion

                if (write_value_feedback[4] == 0)
                {
                    return_out = name + "=" + value + ":写入成功!";
                }
                else if (value_command_type == 9 && write_value_feedback[4] == 5)
                {
                    return_out = "写入失败：变量【" + name + "】" + "原值为【" + value + "】";
                }
                else
                {
                    return_out = "写入失败，错误代码：" + write_value_feedback[4].ToString();
                }
            }
            catch
            {
                return_out = "string 转 数值异常，请检查！";
            }

            return return_out;
        }

        #endregion

        #region 获取变量内容
        static string Get_Value(string type, string name)
        {
            byte[] value_temp = new byte[] { };//接受变量内容数组
            int value_command_type = 0;//装变量类型的关键数据
            byte[] index_temp = Get_Index(type, name);//获得索引

            switch (type)
            {
                case "AI": value_command_type = 1; break;
                case "AO": value_command_type = 2; break;
                case "AR": value_command_type = 3; break;
                case "DI": value_command_type = 4; break;
                case "DO": value_command_type = 5; break;
                case "DR": value_command_type = 6; break;
                case "VA": value_command_type = 7; break;
                case "VD": value_command_type = 8; break;
                case "VT": value_command_type = 9; break;
                default: break;
            }

            if (index_temp[0] * 1 == -1)//索引如果是-1，则说明SCADA中没有该变量
            {
                return "【" + name + "】" + "变量不存在，请检查！";
            }
            else
            {
                byte[] value_Command = new byte[] { 62, 42, 39, 18, (byte)value_command_type, index_temp[0], index_temp[1], index_temp[2], index_temp[3] };//带入索引，得到获取变量数值的命令
                value_temp = Send(value_Command);//发送获取变量数值的命令，得到变量数值报文

                if (value_temp.Length <= 200)//如果超过200个字节，则说明返回的指令是错误的！
                {
                    return Trans_Values(value_command_type, value_temp);//返回获取到的变量内容
                }
                else
                {
                    return "有异常，请检查！";
                }
            }
        }
        #endregion

        #region 获取索引
        static byte[] Get_Index(string type, string name)
        {
            byte[] get_index_command_init = new byte[26] { 62, 42, 39, 17, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00 };//变量索引初始指令
            byte[] name_byte = new byte[] { };//装变量名称的数组
            byte[] index_command = new byte[] { };//装完整的索引命令
            byte[] index_temp = new byte[200];//接受索引数组
            byte[] index = new byte[4];//截取索引
            switch (type)//根据变量的类型初始化不同的索引指令
            {
                case "AI": get_index_command_init[4] = 1; break;
                case "AO": get_index_command_init[4] = 2; break;
                case "AR": get_index_command_init[4] = 3; break;
                case "DI": get_index_command_init[4] = 4; break;
                case "DO": get_index_command_init[4] = 5; break;
                case "DR": get_index_command_init[4] = 6; break;
                case "VA": get_index_command_init[4] = 7; break;
                case "VD": get_index_command_init[4] = 8; break;
                case "VT": get_index_command_init[4] = 9; break;
                default: break;
            }

            name_byte = Encoding.Default.GetBytes(name);//将变量名称转化为数组
            for (int i = 0; i < name_byte.Length; i++)
            {
                get_index_command_init[i + 5] = name_byte[i];//改写变量索引初始指令
            }
            index_command = get_index_command_init;//得到获取变量索引的命令

            index_temp = Send(index_command);//得到变量的索引

            for (int c = 0; c <= 3; c++)
                index[c] = index_temp[4 + c];
            return index;
        }
        #endregion

        #region 内容转换
        static string Trans_Values(int type, byte[] value_temp)
        {
            string value = "初始数值";
            byte[] text_temp = new byte[1024];

            if (type == 1 || type == 2 || type == 3 || type == 7)//模拟量类型的直接用这个转换就可以了
            {
                byte[] value_trans = new byte[] { value_temp[5], value_temp[6], value_temp[7], value_temp[8], value_temp[9], value_temp[10], value_temp[11], value_temp[12] };//截取报文
                value = BitConverter.ToDouble(value_trans, 0).ToString();
            }
            else if (type == 4 || type == 5 || type == 6 || type == 8)//开关量类型的直接用这个转换就可以了
            {
                value = value_temp[5].ToString();
            }
            else if (type == 9)//文本类型的直接用这个转换就可以了
            {
                try
                {
                    for (int i = 0; value_temp[i] != 0; i++)
                        text_temp[i] = value_temp[i + 5];
                    value = Encoding.Default.GetString(text_temp);
                }
                catch
                {
                    value = "有异常，请检查！";
                }
            }
            value = value.TrimEnd('\0');//总长度200个，去掉数值为\0的数组！
            return value;
        }
        #endregion

        #region SCADA的发送与接受
        static byte[] Send(byte[] Command)
        {
            try
            {
                byte[] receive = new byte[200];

                tcpClient.Send(Command);//发送传递进来的数组
                tcpClient.Receive(receive);//这里传递一个byte数组，实际上这个data数组用来接收数据
                return receive;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return Encoding.Default.GetBytes(ex.ToString());
            }
        }
        #endregion
    }
}