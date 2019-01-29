using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace StressTest
{
    class StressTester
    {
        public static readonly string Neo = @"c56f33fc6ecfcd0c225c4ab356fee59390af8560be0e930faebe74a6daff7c9b";
        public static readonly string transMigrateCommand = @"transmigrate";
        public static readonly string newTransMigrateCommand = @"newTransMigrate";
        public static readonly string smashWalletCommand = @"smashWallet";
        public static readonly string countTransactionsCommand = @"countTransactions";
        public static readonly string getBlockCountCommand = @"getblockcount";
        public static readonly string preHeatCommand = @"preheat";
        public static readonly string recorderFile = @"result.csv";
        public static readonly string configFileURL = @"config.ini";
        public static readonly int maxSmashCount = 2000;
        public static readonly int maxTxPerThread = 1000;
        private static object locker = new object();
        public static int txCount = 0;
        public static int port = 0;
        public static int port2 = 0;
        public static string testeeURL = null;
        public static string commanderIP;
        public static int successfulTx = 0;
        public static int currentHeight = 0;
        public static int threadNum = 0;
        public static int successfulThreadNum = 0;
        public static double sleepInterval = 0;

        public static bool readConfig()
        {
            if (!File.Exists(configFileURL)) return false;
            StreamReader reader = new StreamReader(configFileURL);
            testeeURL = reader.ReadLine();
            if (testeeURL == null || testeeURL == "" || reader.EndOfStream) return false;
            commanderIP = reader.ReadLine();
            if (commanderIP == null || commanderIP == "" || reader.EndOfStream) return false;
            txCount = int.Parse(reader.ReadLine());
            if (txCount <= 0 || reader.EndOfStream) return false;
            threadNum = (txCount - 1) / maxTxPerThread + 1;
            port = int.Parse(reader.ReadLine());
            if (port <= 0 || reader.EndOfStream) return false;
            port2 = int.Parse(reader.ReadLine());
            if (port2 <= 0 || reader.EndOfStream) return false;
            sleepInterval = double.Parse(reader.ReadLine());
            if (sleepInterval < 0) return false;
            reader.Close();
            return true;
        }

        public static string CreateCommand(string command, List<string> parameters)
        {
            string result = testeeURL + @"/?jsonrpc=2.0&method=" + command + @"&params=[";
            if (parameters.Count != 0)
            {
                foreach (string parameter in parameters)
                {
                    result += "\"" + parameter + "\",";
                }
                result = result.Substring(0, result.Length - 1);
            }
            result += @"]&id=1";
            return result;
        }

        public static int SmashWalletStep(int count)
        {
            if (count <= 0) return -1;
            HttpWebRequest webReq = (HttpWebRequest)WebRequest.Create(new Uri(CreateCommand(smashWalletCommand, new List<string> { Neo, count.ToString() })));
            webReq.Method = "GET";
            webReq.ContentType = "application/x-www-form-urlencoded";
            webReq.Timeout = 600000;
            webReq.ContentLength = 0;
            HttpWebResponse response = (HttpWebResponse)webReq.GetResponse();
            StreamReader sr = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
            string ret = sr.ReadToEnd();
            sr.Close();
            response.Close();
            ReturnedResult deserializedResult = JsonConvert.DeserializeObject<ReturnedResult>(ret);
            if (deserializedResult.result.Equals("Success")) return 0;
            else if (deserializedResult.result.Equals("Enough accounts already exist.")) return -2;
            else return -3;
        }

        public static bool SmashWallet (int count)
        {
            if (count <= 0) return false;
            Console.WriteLine("Smashing wallet...");
            int currentCount = Math.Min(count, maxSmashCount);
            while (true)
            {
                int result = SmashWalletStep(currentCount);
                if (result == 0 || result == -2)
                {
                    Console.WriteLine("Current Accounts: " + currentCount);
                    int nextCount = Math.Min(currentCount + maxSmashCount, count);
                    if (currentCount == nextCount) break;
                    currentCount = nextCount;
                }
                else
                {
                    Console.WriteLine("Error in smashing wallet.");
                    return false;
                }
                Thread.Sleep(20000);
            }
            Console.WriteLine("Wallet smashed successfully.");
            return true;
        }

        public static string TransMigrate(int start, int count, int rounds)
        {
            HttpWebRequest webReq = (HttpWebRequest)WebRequest.Create(new Uri(CreateCommand(transMigrateCommand, new List<string> { Neo, start.ToString(), count.ToString(), rounds.ToString()})));
            webReq.Method = "GET";
            webReq.ContentType = "application/x-www-form-urlencoded";
            webReq.Timeout = 6000000;
            webReq.ContentLength = 0;

            HttpWebResponse response = (HttpWebResponse)webReq.GetResponse();
            StreamReader sr = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
            string ret = sr.ReadToEnd();
            sr.Close();
            response.Close();
            ReturnedResult deserializedResult = JsonConvert.DeserializeObject<ReturnedResult>(ret);
            lock (locker)
            {
                successfulThreadNum++;
            }
            return deserializedResult.result;
        }

        public static string NewTransMigrate(int count)
        {
            HttpWebRequest webReq = (HttpWebRequest)WebRequest.Create(new Uri(CreateCommand(newTransMigrateCommand, new List<string> {sleepInterval.ToString(), txCount.ToString()})));
            webReq.Method = "GET";
            webReq.ContentType = "application/x-www-form-urlencoded";
            webReq.Timeout = 6000000;
            webReq.ContentLength = 0;

            HttpWebResponse response = (HttpWebResponse)webReq.GetResponse();
            StreamReader sr = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
            string ret = sr.ReadToEnd();
            sr.Close();
            response.Close();
            ReturnedResult deserializedResult = JsonConvert.DeserializeObject<ReturnedResult>(ret);
            lock (locker)
            {
                successfulThreadNum++;
            }
            Console.WriteLine(deserializedResult.result);
            return deserializedResult.result;
        }

        public static int CountTransactions(int start)
        {
            HttpWebRequest webReq = (HttpWebRequest)WebRequest.Create(new Uri(CreateCommand(countTransactionsCommand, new List<string> { start.ToString() })));
            webReq.Method = "GET";
            webReq.ContentType = "application/x-www-form-urlencoded";
            webReq.Timeout = 6000000;
            webReq.ContentLength = 0;

            HttpWebResponse response = (HttpWebResponse)webReq.GetResponse();
            StreamReader sr = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
            string ret = sr.ReadToEnd();
            sr.Close();
            response.Close();
            ReturnedResult deserializedResult = JsonConvert.DeserializeObject<ReturnedResult>(ret);
            return int.Parse(deserializedResult.result);
        }

        public static int GetBlockHeight()
        {
            HttpWebRequest webReq = (HttpWebRequest)WebRequest.Create(new Uri(CreateCommand(getBlockCountCommand, new List<string> { })));
            webReq.Method = "GET";
            webReq.ContentType = "application/x-www-form-urlencoded";
            webReq.Timeout = 6000000;
            webReq.ContentLength = 0;

            HttpWebResponse response = (HttpWebResponse)webReq.GetResponse();
            StreamReader sr = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
            string ret = sr.ReadToEnd();
            sr.Close();
            response.Close();
            ReturnedResult deserializedResult = JsonConvert.DeserializeObject<ReturnedResult>(ret);
            int result = int.Parse(deserializedResult.result);
            return result <= 0 ? -1 : result - 1;
        }

        public static int Preheat()
        {
            HttpWebRequest webReq = (HttpWebRequest)WebRequest.Create(new Uri(CreateCommand(preHeatCommand, new List<string> {})));
            webReq.Method = "GET";
            webReq.ContentType = "application/x-www-form-urlencoded";
            webReq.Timeout = 6000000;
            webReq.ContentLength = 0;

            HttpWebResponse response = (HttpWebResponse)webReq.GetResponse();
            StreamReader sr = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
            string ret = sr.ReadToEnd();
            sr.Close();
            response.Close();
            ReturnedResult deserializedResult = JsonConvert.DeserializeObject<ReturnedResult>(ret);
            return int.Parse(deserializedResult.result);
        }

        public static void writeLineCSVFile(List<string> inputs, ref StreamWriter writer)
        {
            if (inputs.Count == 0) return;
            string result = "";
            foreach (string input in inputs)
            {
                result += input + ",";
            }
            writer.WriteLine(result);
        }

        public static string testRound(int count, int rounds)
        {
            if (count <=  0) return null;
            StreamWriter recorder = null;
            if (!File.Exists(recorderFile))
            {
                recorder = new StreamWriter(recorderFile, true);
                writeLineCSVFile(new List<string> { "Planned Transactions", "Successful Transactions", "Seconds Consumed"}, ref recorder);
                recorder.Flush();
                recorder.Close();
            }
            recorder = new StreamWriter(recorderFile, true);
            Console.WriteLine("Test started.");
            Console.WriteLine("Planned Transactions: " + count);

            Console.WriteLine("Sending transactions...");
            DateTime startTime = DateTime.Now;
            for (int i = 0; i < threadNum; i++)
            {
                ThreadTransMigrateClass threadTransMigrateTemplate = new ThreadTransMigrateClass(i * maxTxPerThread, Math.Min(maxTxPerThread, txCount - i * maxTxPerThread), rounds);
                Thread thread = new Thread(new ThreadStart(threadTransMigrateTemplate.ThreadTransMigrate));
                thread.IsBackground = true;
                thread.Start();
            }
            while (successfulThreadNum != threadNum) { }
            DateTime endTime = DateTime.Now;
            double secondsComsumed = (endTime - startTime).TotalSeconds;
            Console.WriteLine("Sent successfully.");
            Console.WriteLine("Successful Transactions: " + successfulTx);
            Console.WriteLine("Seconds consumed: " + secondsComsumed);

            writeLineCSVFile(new List<string> { count.ToString(), successfulTx.ToString(), secondsComsumed.ToString() }, ref recorder);
            Console.WriteLine("Test round finished.");

            recorder.Flush();
            recorder.Close();
            return count.ToString() + "," + successfulTx.ToString() + "," + startTime.ToBinary().ToString() + "," + endTime.ToBinary().ToString() + "," + currentHeight.ToString();


            /*int a = -1, b = -2, c = -3, round = 1;
            while (a != b || b != c || round < 10)
            {
                Thread.Sleep(20000);
                Console.WriteLine("Round " + round + " ...");
                c = b;
                b = a;
                a = CountTransactions(startHeight);
                Console.WriteLine("Current OnChain Transactions: " + (a - accumulateTxCount));
                if (a >= accumulateTxCount + successfulTx) break;
                round++;
            }*/
        }

        public static string testRound2(int count, int rounds)
        {
            if (count <= 0) return null;
            StreamWriter recorder = null;
            if (!File.Exists(recorderFile))
            {
                recorder = new StreamWriter(recorderFile, true);
                writeLineCSVFile(new List<string> { "Planned Transactions", "Successful Transactions", "Seconds Consumed" }, ref recorder);
                recorder.Flush();
                recorder.Close();
            }
            recorder = new StreamWriter(recorderFile, true);
            Console.WriteLine("Test started.");
            Console.WriteLine("Planned Transactions: " + count * rounds);

            Console.WriteLine("Sending transactions...");
            DateTime startTime = DateTime.Now;
            string[] transamigrateResult = TransMigrate(0, txCount, rounds).Split(new char[] { ':' });
            successfulTx = int.Parse(transamigrateResult[0]);
            int receivedHeight = int.Parse(transamigrateResult[1]);
            currentHeight = Math.Max(currentHeight, receivedHeight);
            DateTime endTime = DateTime.Now;
            double secondsComsumed = (endTime - startTime).TotalSeconds;
            Console.WriteLine("Sent successfully.");
            Console.WriteLine("Successful Transactions: " + successfulTx);
            Console.WriteLine("Seconds consumed: " + secondsComsumed);

            writeLineCSVFile(new List<string> { (count * rounds).ToString(), successfulTx.ToString(), secondsComsumed.ToString() }, ref recorder);
            Console.WriteLine("Test round finished.");

            recorder.Flush();
            recorder.Close();
            return (count * rounds).ToString() + "," + successfulTx.ToString() + "," + startTime.ToBinary().ToString() + "," + endTime.ToBinary().ToString() + "," + currentHeight.ToString();
        }

        public static string testRound3(int count)
        {
            if (count <= 0) return null;
            StreamWriter recorder = null;
            if (!File.Exists(recorderFile))
            {
                recorder = new StreamWriter(recorderFile, true);
                writeLineCSVFile(new List<string> { "Planned Transactions", "Successful Transactions", "Seconds Consumed" }, ref recorder);
                recorder.Flush();
                recorder.Close();
            }
            recorder = new StreamWriter(recorderFile, true);
            Console.WriteLine("Test started.");
            Console.WriteLine("Planned TPS: " + 1000 / sleepInterval);

            Console.WriteLine("Sending transactions...");
            DateTime startTime = DateTime.Now;
            string[] transamigrateResult = NewTransMigrate(txCount).Split(new char[] { ':' });
            successfulTx = int.Parse(transamigrateResult[0]);
            int receivedHeight = int.Parse(transamigrateResult[1]);
            currentHeight = Math.Max(currentHeight, receivedHeight);
            DateTime endTime = DateTime.Now;
            double secondsComsumed = (endTime - startTime).TotalSeconds;
            Console.WriteLine("Sent successfully.");
            Console.WriteLine("Successful Transactions: " + successfulTx);
            Console.WriteLine("Seconds consumed: " + secondsComsumed);

            writeLineCSVFile(new List<string> { count.ToString(), successfulTx.ToString(), secondsComsumed.ToString() }, ref recorder);
            Console.WriteLine("Test round finished.");

            recorder.Flush();
            recorder.Close();
            return count.ToString() + "," + successfulTx.ToString() + "," + startTime.ToBinary().ToString() + "," + endTime.ToBinary().ToString() + "," + currentHeight.ToString();
        }

        /// <summary>
        /// 监听连接
        /// </summary>
        /// <param name="o"></param>
        static void Listen(object o)
        {
            var serverSocket = o as Socket;
            while (true)
            {
                //等待连接并且创建一个负责通讯的socket
                var send = serverSocket.Accept();
                //获取链接的IP地址
                var sendIpoint = send.RemoteEndPoint.ToString();
                Console.WriteLine($"{sendIpoint}Connection");
                //开启一个新线程不停接收消息
                Thread thread = new Thread(Receive);
                thread.IsBackground = true;
                thread.Start(send);
            }
        }

        /// <summary>
        /// 接收消息
        /// </summary>
        /// <param name="o"></param>
        static void Receive(object o)
        {
            var send = o as Socket;
            while (true)
            {
                //获取发送过来的消息容器
                byte[] buffer = new byte[1024 * 1024 * 2];
                var effective = send.Receive(buffer);
                //有效字节为0则跳过
                if (effective == 0)
                {
                    break;
                }
                var str = Encoding.UTF8.GetString(buffer, 0, effective);
                Console.WriteLine("Command Received.");
                //多线程
                //var buffers = Encoding.UTF8.GetBytes(testRound(txCount, int.Parse(str)));

                //单线程
                //var buffers = Encoding.UTF8.GetBytes(testRound2(txCount, int.Parse(str)));
                //send.Send(buffers);

                //优化交易
                var buffers = Encoding.UTF8.GetBytes(testRound3(txCount));
                send.Send(buffers);
            }
        }

        public static string GetLocalIP()
        {
            try
            {
                string HostName = Dns.GetHostName(); //得到主机名
                IPHostEntry IpEntry = Dns.GetHostEntry(HostName);
                for (int i = 0; i < IpEntry.AddressList.Length; i++)
                {
                    //从IP地址列表中筛选出IPv4类型的IP地址
                    //AddressFamily.InterNetwork表示此IP为IPv4,
                    //AddressFamily.InterNetworkV6表示此地址为IPv6类型
                    if (IpEntry.AddressList[i].AddressFamily == AddressFamily.InterNetwork)
                    {
                        string result = IpEntry.AddressList[i].ToString();
                        if (result[result.Length - 1] == '1' && result[result.Length - 2] == '.') continue;
                        return result;
                    }
                }
                return "";
            }
            catch (Exception ex)
            {
                return "";
            }
        }

        public static void Main(string[] args)
        {
            if (!readConfig())
            {
                Console.WriteLine("Config.ini invalid");
                return;
            }
            //if (!SmashWallet(txCount)) return;
            Console.WriteLine("Preheating...");
            //int preheat = Preheat();
            //Console.WriteLine("Preheated accounts: " + preheat);
            Socket serverSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            IPAddress ip = IPAddress.Any;
            IPEndPoint point = new IPEndPoint(ip, port);
            //socket绑定监听地址
            serverSocket.Bind(point);
            Console.WriteLine("Listen Success");
            //设置同时连接个数
            serverSocket.Listen(3);

            //利用线程后台执行监听,否则程序会假死
            Thread thread = new Thread(Listen);
            thread.IsBackground = true;
            thread.Start(serverSocket);

            Socket socketClient = new Socket(SocketType.Stream, ProtocolType.Tcp);
            ip = IPAddress.Parse(commanderIP);
            point = new IPEndPoint(ip, port2);
            //进行连接
            socketClient.Connect(point);
            Console.WriteLine("Requiring command...");

            var buffer = Encoding.UTF8.GetBytes(GetLocalIP());
            var temp = socketClient.Send(buffer);

            Console.WriteLine("Input \"exit\" to finish test.");
            while (!Console.ReadLine().ToLower().Equals("exit"))
            {
            }

            Console.ReadKey();
        }

        class ReturnedResult
        {
            public string jsonrpc = null;
            public string id = null;
            public string result = null;
        }

        class ThreadTransMigrateClass
        {
            private int start = 0;
            private int count = 0;
            private int rounds = 0;

            public ThreadTransMigrateClass(int start, int count, int rounds)
            {
                this.start = start;
                this.count = count;
                this.rounds = rounds;
            }

            public void ThreadTransMigrate()
            {
                string[] transamigrateResult = TransMigrate(start, count, rounds).Split(new char[] { ':' });
                successfulTx += int.Parse(transamigrateResult[0]);
                int receivedHeight = int.Parse(transamigrateResult[1]);
                currentHeight = Math.Max(currentHeight, receivedHeight);
            }
        }
    }
}
