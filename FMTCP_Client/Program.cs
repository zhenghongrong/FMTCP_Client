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
    public enum Value_Type { AI = 1, AO, AR, DI, DO, DR, VA, VD, VT }

    class Program
    {
        static Socket tcpClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        static IPAddress ipaddress = IPAddress.Parse("127.0.0.1");
        static EndPoint point = new IPEndPoint(ipaddress, 60000);

        #region 主逻辑
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("程序启动中，请稍等......");
                tcpClient.Connect(point);

                while (true)
                {
                    //Console.WriteLine(Get_Value("VT", "CODE"));
                    //Console.WriteLine(Write_Value("DR", "DR1", "0"));
                    //Write_Value("AR", "CODE", "0.388888");
                    //Thread.Sleep(1000);

                    //string l = RedVarValue("VT.学习机_1ECU条码").Trim('|');
                    //string K = RedVarValue("AR.AR2").Trim('|');
                    //string k = RedVarValue("VT.CODE|VT.VT1|AR.AR2|DR.DR1|VT.CODE");



                    Console.WriteLine("The result is:" + RedVarValue("DR.DR1"));
                    //Console.ReadKey();
                    //Console.WriteLine("The result is:"+WriteVarValues("VT.CODE|VT.VT1|VA.VA1|AR.AR1","T|T|T|T"));
                    //Console.ReadKey();
                    
                    //Console.WriteLine("The result is:" + ReadVarNames("VT"));
                    //Console.ReadKey();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("程序运行异常，请重启！\n\r" + e.ToString());
                Console.ReadKey();
            }
        }
        #endregion

        #region 命令方式读取变量
        static string RedVarValue(string name)
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

            byte[] new_v = new byte[1024];
            byte[] old_v = new byte[1024];
            byte[] V = new byte[] { };
            int j = 0;

            while (V != new_v)
            {
                if (bytecomp(new_v,old_v))
                {
                    if (j >= 5)
                        V = new_v;
                    if(j<5)
                    {
                        new_v = Send(send);
                        j++;
                    }
                }
                else
                {
                    j = 0;
                    new_v = Send(send);
                    old_v = new_v;
                }
            }

            byte[] re = new byte[V.Length];

            for (int i = 0; i < V.Length - 7; i++)
                re[i] = V[i + 7];

            string K = Encoding.Default.GetString(re);
            K = K.Substring(K.IndexOf("|"), K.LastIndexOf("|"));
            K = K.Trim('\0');
            return K;
        }
        #endregion

        #region 命令方式写入变量
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
            K = K.Trim('\0');
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
            K = K.Trim('\0');
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
            try
            {
                byte[] receive = new byte[1024];

                tcpClient.Send(Command);//发送传递进来的数组
                tcpClient.Receive(receive);//这里传递一个byte数组，实际上这个receive数组用来接收数据
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