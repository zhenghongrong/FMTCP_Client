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
        static IPAddress ipaddress = IPAddress.Parse("10.0.0.101");
        static EndPoint point = new IPEndPoint(ipaddress, 5002);

        #region 主逻辑
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("程序启动中，请稍等......");
                tcpClient.Connect(point);

                while (true)
                {
                    Console.WriteLine("The result is:" + RedVarValue("VT.%TIME"));
                    //Console.ReadKey();
                    //Console.WriteLine("The result is:" + WriteVarValues("VT.CODE|VT.VT1|VA.VA1|AR.AR1", "T|yy|yy|yy"));
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
        /**********************
         * 返回数据说明：
         * {0}标识数据所在的设备断线
         * (none)标识SCADA中无此数据
         ***********************/
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

            #region 滤波，防止数据抖动造成影响，连续读取5次相同的数据才确定数据可靠
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
                    new_v = Send(send);
                    old_v = new_v;
                }
            }
            #endregion

            byte[] re = new byte[V.Length];

            for (int i = 0; i < V.Length - 7; i++)
                re[i] = V[i + 7];

            string K = Encoding.Default.GetString(re);
            K = K.Substring(K.IndexOf("|"), K.LastIndexOf("|"));
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
            K = K.Substring(K.IndexOf("|"), K.LastIndexOf("|"));
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
            K = K.Substring(K.IndexOf("|"), K.LastIndexOf("|"));
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