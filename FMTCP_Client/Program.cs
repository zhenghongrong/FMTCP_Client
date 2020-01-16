using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using System.Xml;


namespace FMTCP_Client
{
    class Program
    {
        static Socket tcpClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        static IPAddress ipaddress = IPAddress.Parse("172.30.33.120");
        //static IPAddress ipaddress = IPAddress.Parse("192.168.150.128");
        static EndPoint point = new IPEndPoint(ipaddress, 5002);

        static WebReference.SQL_ProcessCommon ws = new WebReference.SQL_ProcessCommon();//初始化接口

        #region 获取数值
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("程序启动中，请稍等......");
                tcpClient.Connect(point);

                //拼接OP90需要读取的变量的命令字符串
                string[] OP90_ARR = new string[] { "DI.1_OP90_M5000", "DI.1_OP90_M5017", "DI.1_OP90_M5024", };
                string OP90 = "";
                for (int i = 0; i < OP90_ARR.Length; i++)
                {
                    OP90 += OP90_ARR[i];
                    OP90 += "|";
                }
                OP90 = OP90.Substring(0, OP90.Length - 1);

                //拼接OP220需要读取的变量的命令字符串
                string[] OP220_ARR = new string[] { "DI.1_OP220_M5000", "DI.1_OP220_M5017", "DI.1_OP220_M5024" };
                string OP220 = "";
                for (int i = 0; i < OP220_ARR.Length; i++)
                {
                    OP220 += OP220_ARR[i];
                    OP220 += "|";
                }
                OP220 = OP220.Substring(0, OP220.Length - 1);

                //需要读取的变量的变量的命令字符串
                string OP260A = "AI.1_OP260A_V0";
                string OP260B = "AI.1_OP260B_V0";
                string OP290 = "AI.1_OP290_D1006";

                //存储读取回来的旧值
                string OP90_value_o = null;
                string OP220_value_o = null;
                string OP260A_value_o = null;
                string OP260B_value_o = null;
                string OP290_value_o = null;

                while (true)
                {
                    string OP90_value_n = ReadVarValue(OP90);
                    Thread.Sleep(1000);
                    if (OP90_value_o != OP90_value_n)
                    {
                        OP90_value_o = OP90_value_n;
                        Change("OP90", OP90_value_n);
                    }

                    string OP220_value_n = ReadVarValue(OP220);
                    Thread.Sleep(1000);
                    if (OP220_value_o != OP220_value_n)
                    {
                        OP220_value_o = OP220_value_n;
                        Change("OP220", OP220_value_n);
                    }

                    string OP260A_value_n = ReadVarValue(OP260A);
                    Thread.Sleep(1000);
                    if (OP260A_value_o != OP260A_value_n)
                    {
                        OP260A_value_o = OP260A_value_n;
                        Change("OP260A", OP260A_value_n);
                    }

                    string OP260B_value_n = ReadVarValue(OP260B);
                    Thread.Sleep(1000);
                    if (OP260B_value_o != OP260B_value_n)
                    {
                        OP260B_value_o = OP260B_value_n;
                        Change("OP260B", OP260B_value_n);
                    }

                    string OP290_value_n = ReadVarValue(OP290);
                    Thread.Sleep(1000);
                    if (OP290_value_o != OP290_value_n)
                    {
                        OP290_value_o = OP290_value_n;
                        Change("OP290", OP290_value_n);
                    }
                }
                #endregion
            }
            catch (Exception e)
            {
                Console.WriteLine("程序运行异常，请重启！\r\n" + e.ToString());
                Console.ReadKey();
            }
        }
        #region 逻辑处理
        static void Change(string st_no, string value)
        {
            if (st_no == "OP260A" || st_no == "OP260B")
            {
                switch (value)
                {
                    case "4": Interface_("20018", st_no, "2"); break;//关机
                    case "2": Interface_("20018", st_no, "3"); break;//运行
                    case "1": Interface_("20018", st_no, "4"); break;//空闲
                    case "0": Interface_("20018", st_no, "1"); break;//故障
                    default:break;
                }
            }

            if (st_no == "OP290")
            {
                switch (value)
                {
                    case "1": Interface_("20018", st_no, "3");break;//运行
                    case "2": Interface_("20018", st_no, "1");break;//故障
                    default:break;
                }
            }

            if (st_no == "OP90" || st_no == "OP220")
            {
                string[] value_arr = value.Split('|');

                if (value_arr[0] == "0" || value_arr[1] == "1")
                    Interface_("20018", st_no, "1");
                if (value_arr[0] == "1" && value_arr[1] == "0")
                {
                    if (value_arr[2] == "0")
                        Interface_("20018", st_no, "3");
                    if (value_arr[2] == "1")
                        Interface_("20018", st_no, "4");
                }
            }
        }
        #endregion

        #region 调用接口
        static void Interface_(string sc_prno, string st_no, string status)
        {
            string[] status_name = new string[] { "","故障", "关机", "运行", "空闲" };

            string XML = "";
            XML += "<UpDate><Head><TableName>mes_dev_status</TableName></Head><Body1><dev_status>";
            XML += status;
            XML += "</dev_status></Body1><Body2><sc_prno>";
            XML += sc_prno;
            XML += "</sc_prno><st_no>";
            XML += st_no;
            XML += "</st_no>";
            XML += "</Body2></UpDate>";
            string k = ws.Input(XML);
            Console.WriteLine(sc_prno + "线," + st_no + "状态变更为:" + status+"("+status_name[int.Parse(status)]+"):"+k);
        }
        #endregion

        #region 命令方式读取变量
        /**********************
         * 返回数据说明：
         * {0}标识数据所在的设备断线
         * (none)标识SCADA中无此数据
         ***********************/
        static string ReadVarValue(string name)
        {
            byte[] init = new byte[] { 62, 42, 07, 226 };//
            byte[] command_len = new byte[3] { 0, 0, 0 };//需要发送的命令的长度，长度最小为【ReadVarValues("")】的长度

            string command = "";
            command += "ReadVarValues(\"";
            command += name;
            command += "\")";
            
            command_len = GetComLen(command.Length);

            byte[] command_to_byte = Encoding.Default.GetBytes(command);

            byte[] send = new byte[init.Length + command_len.Length + command_to_byte.Length];
            
            Buffer.BlockCopy(init, 0, send, 0, init.Length);
            Buffer.BlockCopy(command_len, 0, send, init.Length, command_len.Length);
            Buffer.BlockCopy(command_to_byte, 0, send, init.Length + command_len.Length, command_to_byte.Length);

            #region 滤波，防止数据抖动造成影响，连续读取5次相同的数据才确定数据可靠
            byte[] new_v = new byte[1024];
            byte[] old_v = new byte[1024];
            byte[] V = new byte[] { };
            int j = 0;
            int k = 0;//防止程序进入死循环

            new_v = Send(send);

            while ((V != new_v) && (k < 100))
            {
                if (bytecomp(new_v, old_v))
                {
                    if (j >= 5)
                        V = new_v;
                    else
                    {
                        new_v = Send(send);
                        j++;
                        Thread.Sleep(10);
                    }
                }
                else
                {
                    j = 0;
                    Thread.Sleep(10);
                    new_v = Send(send);
                    old_v = new_v;
                    k++;
                }
            }
            if (k > 2)
                Console.WriteLine("总共发生{0}次数据不稳定！", k);
            #endregion
            byte[] re = new byte[V.Length];

            for (int i = 0; i < V.Length - 7; i++)
                re[i] = V[i + 7];

            string K = Encoding.Default.GetString(re);
            K = K.Trim('\0');
            K = K.Substring(4);
            K = K.Substring(0, K.Length - 1);

            return K;
        }
        #endregion

        #region 命令方式写入变量
        /*
         * 返回数据说明：
         * {0}标识数据所在的设备断线
         * (none)标识SCADA中无此数据
         * T表示写入成功
         * F表示写入失败（如果是VT类型的变量，两次写入的内容都是一样的，那么第二次一定会是F）
         */
        static string WriteVarValues(string name,string values)
        {
            byte[] init = new byte[] { 62, 42, 07, 226 };
            byte[] command_len = new byte[3] { 0, 0, 0 };//长度最小为【WriteVarValues("")】的长度

            string command = "";
            command += "WriteVarValues(\"";
            command += name;
            command += "\",\"";
            command += values;
            command += "\")";

            command_len = GetComLen(command.Length);

            byte[] command_to_byte = Encoding.Default.GetBytes(command);

            byte[] send = new byte[init.Length+command_len.Length+command_to_byte.Length];

            Buffer.BlockCopy(init, 0, send, 0, init.Length);
            Buffer.BlockCopy(command_len, 0, send, init.Length, command_len.Length);
            Buffer.BlockCopy(command_to_byte, 0, send, init.Length + command_len.Length, command_to_byte.Length);

            byte[] V = Send(send);
            byte[] re = new byte[V.Length];
            for (int i = 0; i < V.Length - 7; i++)
                re[i] = V[i + 7];

            string K = Encoding.Default.GetString(re);
            //K = K.Substring(K.IndexOf("|"), K.LastIndexOf("|"));
            return K;
        }
        #endregion

        #region 命令方式读取设备表
        /*
         * 返回数据说明：
         */
        static string ReadDevnoBytes(int devno, int start, int end)
        {
            byte[] init = new byte[] { 62, 42, 07, 226 };
            byte[] command_len = new byte[3] { 0, 0, 0 };//长度最小为【WriteVarValues("")】的长度

            string command = "";
            command += "ReadDevnoBytes(";
            command += devno;
            command += ",";
            command += start;
            command += ",";
            command += end;
            command += ")";

            command_len = GetComLen(command.Length);

            byte[] command_to_byte = Encoding.Default.GetBytes(command);

            byte[] send = new byte[init.Length + command_len.Length + command_to_byte.Length];

            Buffer.BlockCopy(init, 0, send, 0, init.Length);
            Buffer.BlockCopy(command_len, 0, send, init.Length, command_len.Length);
            Buffer.BlockCopy(command_to_byte, 0, send, init.Length + command_len.Length, command_to_byte.Length);

            #region 滤波防抖
            byte[] new_v = new byte[1024];
            byte[] old_v = new byte[1024];
            byte[] V = new byte[] { };
            int j = 0;
            int k = 0;//防止程序进入死循环
            
            while ((V != new_v) && (k < 100))
            {
                if (bytecomp(new_v, old_v))
                {
                    if (j >= 5)
                        V = new_v;
                    else
                    {
                        new_v = Send(send);
                        j++;
                        Thread.Sleep(10);
                    }
                }
                else
                {
                    j = 0;
                    Thread.Sleep(10);
                    new_v = Send(send);
                    old_v = new_v;
                    k++;
                }
            }
            if (k > 2)
                Console.WriteLine("总共发生{0}次数据不稳定！", k);
            #endregion

            byte[] re = new byte[V.Length];
            for (int i = 0; i < V.Length - 7; i++)
                re[i] = V[i + 7];

            string K = Encoding.Default.GetString(re);
            //K = K.Substring(K.IndexOf("|"), K.LastIndexOf("|"));
            return K;
        }
        #endregion

        #region 命令获取某类型的所有变量
        static string ReadVarNames(string type)
        {
            byte[] init = new byte[] { 62, 42, 07, 226 };
            byte[] command_len = new byte[3] { 0, 0, 0 };//长度最小为【WriteVarValues("")】的长度

            string command = "";
            command += "ReadVarNames(\"";
            command += type;
            command += "\")";

            command_len = GetComLen(command.Length);

            byte[] command_to_byte = Encoding.Default.GetBytes(command);

            byte[] send = new byte[init.Length + command_len.Length + command_to_byte.Length];

            Buffer.BlockCopy(init, 0, send, 0, init.Length);
            Buffer.BlockCopy(command_len, 0, send, init.Length, command_len.Length);
            Buffer.BlockCopy(command_to_byte, 0, send, init.Length + command_len.Length, command_to_byte.Length);

            byte[] V = Send(send);
            byte[] re = new byte[V.Length];
            for (int i = 0; i < V.Length - 7; i++)
                re[i] = V[i + 7];

            string K = Encoding.Default.GetString(re);
            //K = K.Substring(K.IndexOf("|"), K.LastIndexOf("|"));
            return K;
        }
        #endregion

        #region 获得命令长度数组
        static byte[] GetComLen(int command_len)
        {
            int len = command_len;
            byte[] hex = new byte[3];
            hex[2] = (byte)(len & 0xFF);
            hex[1] = (byte)((len >> 8) & 0xFF);
            hex[0] = (byte)((len >> 16) & 0xFF);
            return hex;
        }
        #endregion

        #region 数组对比
        static bool bytecomp(byte[] a, byte[] b)
        {
            if (a.Length == b.Length)
            {
                for (int i = 0; i < a.Length; i++)
                {
                    if (a[i] != b[i])
                        return false;
                }
                return true;
            }
            else
                return false;
        }
        #endregion

        #region SCADA的发送与接受
        static byte[] Send(byte[] Command)
        {
            byte[] Result = new byte[2048];
            try
            {
                byte[] receive = new byte[2048];
                tcpClient.Send(Command);//发送传递进来的数组
                tcpClient.Receive(Result);//这里传递一个byte数组，实际上这个receive数组用来接收数据
                
                return Result;
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