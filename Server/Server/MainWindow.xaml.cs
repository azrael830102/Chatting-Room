using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace Server
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Socket server;
        private Dictionary<string, Socket> clientDict = new Dictionary<string, Socket>();
        private Thread serverThread;
        private MsgJsonFormatObj hostMsgBox = null;
        private readonly string default_hostName = "host";
        private readonly string default_hostIP = "127.0.0.1";
        private readonly string default_member_limit = "5";
        private readonly int default_port = 20001;

        private string hostIP = "";
        private string hostID = "";
        private string hostName = "";
        private int port = 20001;
        private int limit = 5;

        public MainWindow()
        {
            InitializeComponent();
            CreateSettingTable.Visibility = Visibility.Hidden;

            btn_room_shutdown.IsEnabled = false;
            txtbox_sending_msg.IsReadOnly = true;
            btn_sending.IsEnabled = false;
        }

        private void CreatRoom(object sender, RoutedEventArgs e)
        {
            btn_create_room.IsEnabled = false;

            server_name.Text = default_hostName;
            server_ip.Text = default_hostIP;
            member_limit.Text = default_member_limit;
            server_port.Text = default_port.ToString();

            CreateSettingTable.Visibility = Visibility.Visible;
        }

        private void CancelSetting(object sender, RoutedEventArgs e)
        {
            btn_create_room.IsEnabled = true;
            CreateSettingTable.Visibility = Visibility.Hidden;
        }

        private void InputBoxOnblur(object sender, RoutedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            switch (textBox.Name.ToString())
            {
                case "server_name":
                    server_name.Text = (server_name.Text == null || server_name.Text.Equals("")) ? default_hostName : server_name.Text;
                    break;
                case "server_ip":
                    server_ip.Text = (server_ip.Text == null || server_ip.Text.Equals("")) ? default_hostIP : server_ip.Text;
                    break;
                case "server_port":
                    server_port.Text = (server_port.Text == null || server_port.Text.Equals("")) ? default_port.ToString() : server_port.Text;
                    break;
                case "member_limit":
                    member_limit.Text = (member_limit.Text == null || member_limit.Text.Equals("")) ? default_member_limit : member_limit.Text;
                    break;
            }
        }

        private void SubmitSetting(object sender, RoutedEventArgs e)
        {
            string[] ip_format = server_ip.Text.Split(".");
            if (ip_format.Length != 4)
            {
                System.Windows.MessageBox.Show("IP format error.");
            }
            else if (server_port.Text.Length != 5)
            {
                System.Windows.MessageBox.Show("Port format error.");
            }
            else
            {
                hostID = System.Guid.NewGuid().ToString();
                hostIP = server_ip.Text;
                hostName = server_name.Text;
                port = Int32.Parse(server_port.Text);
                try
                {
                    Start();
                    CloseSettingWindow();
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show("Error : " + ex.Message);
                }
            }
        }

        private void Send(object sender, RoutedEventArgs e)
        {
            String text = hostMsgBox.Username + " : " + txtbox_sending_msg.Text + "\n";
            if (!txtbox_sending_msg.Text.Trim().Equals(""))
            {
                try
                {
                    //send message to server
                    foreach (KeyValuePair<string, Socket> item in clientDict)
                    {
                        if (item.Key != hostMsgBox.Id + "," + hostMsgBox.Username)
                        {
                            SendingMsg(item.Value, text);
                        }
                    }
                    txtbox_receive_msg.Dispatcher.BeginInvoke(
                      new Action(() => { txtbox_receive_msg.Text += text; }), null);
                    txtbox_sending_msg.Text = "";
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    txtbox_receive_msg.Text += "send Fail\n";
                }
            }
        }

        private void CloseRoom(object sender, RoutedEventArgs e)
        {
            try
            {
                hostMsgBox.IsAlive = false;
                hostMsgBox.Msg_body = "This Chatting room is closed\n";

                RefreshTheList();
                ShowMsg(hostMsgBox);

                hostMsgBox.IsAlive = false;
                btn_room_shutdown.IsEnabled = false;
                txtbox_sending_msg.IsReadOnly = true;
                btn_sending.IsEnabled = false;
                btn_create_room.IsEnabled = true;
                clientDict.Remove(hostMsgBox.Id + "," + hostMsgBox.Username);
                server.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                txtbox_receive_msg.Text += "send Fail\n";
            }
        }



        private void CloseSettingWindow()
        {
            btn_create_room.IsEnabled = false;
            btn_room_shutdown.IsEnabled = true;
            txtbox_sending_msg.IsReadOnly = false;
            btn_sending.IsEnabled = true;
            CreateSettingTable.Visibility = Visibility.Hidden;
        }



        //start a socket server
        private void Start()
        {
            if (serverThread != null)
            {
                Console.WriteLine(serverThread.IsAlive);
            }

            if (server == null)
            {
                txtbox_receive_msg.Text = "Start chatting!\n";
                IPAddress ip = IPAddress.Parse(hostIP);
                //socket()
                server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                //bind()
                server.Bind(new IPEndPoint(ip, port));
                //listen()
                server.Listen(10);
                clientDict.Clear();
                hostMsgBox = new MsgJsonFormatObj(hostID, hostName, "");
                hostMsgBox.IsAlive = true;
                limit = Int32.Parse(member_limit.Text);

                clientDict.Add(hostMsgBox.Id + "," + hostMsgBox.Username, server);
                RefreshTheList();

                serverThread = new Thread(Listen);
                serverThread.Start();
            }
        }

        //listen to socket client
        private void Listen()
        {

            while (true && hostMsgBox.IsAlive)
            {
                try
                {
                    //accept()
                    Socket client = server.Accept();
                    System.Threading.WaitCallback waitCallback = new WaitCallback(ReceiveMsg);
                    
                    if (clientDict.Count >= limit)
                    {
                        MsgJsonFormatObj jobj = new MsgJsonFormatObj("","", "Chatting room is full\n");
                        string jsonData = JsonConvert.SerializeObject(jobj);
                        byte[] dataBytes = Encoding.Default.GetBytes(jsonData);
                        if (client.Connected)
                        {
                            client.Send(dataBytes);
                        }
                    }
                    else
                    {
                        ThreadPool.QueueUserWorkItem(waitCallback, client);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("server is closed.");
                    server = null;
                }
            }
        }

        //receive client message and send to client
        public void ReceiveMsg(object client)
        {
            Socket connection = (Socket)client;
            byte[] result = new byte[1024];
            //receive message from client
            int receive_num = connection.Receive(result);
            Thread.Sleep(1000);
            string str = Encoding.ASCII.GetString(result, 0, receive_num);
            MsgJsonFormatObj msg = JsonConvert.DeserializeObject<MsgJsonFormatObj>(str);
            
            clientDict.Add(msg.Id + "," + msg.Username, connection);
            RefreshTheList();

            hostMsgBox.Msg_body = "Welcome " + msg.Username + " join us!\n";
            ShowMsg(hostMsgBox);

            //send welcome message to client
            SendingMsg(connection, "Hi " + msg.Username + "\n");

            while (true && hostMsgBox.IsAlive)
            {
                try
                {
                    receive_num = connection.Receive(result);
                    msg = JsonConvert.DeserializeObject<MsgJsonFormatObj>(Encoding.ASCII.GetString(result, 0, receive_num));
                    if (receive_num > 0)
                    {
                        if (msg.IsAlive)
                        {
                            string txt = msg.Username + " : " + msg.Msg_body;
                            msg.Msg_body = txt;
                            ShowMsg(msg);
                        }
                        else
                        {
                            clientDict.Remove(msg.Id + "," + msg.Username);
                            RefreshTheList();
                            msg.Msg_body = msg.Username + " is leave...\n";
                            ShowMsg(msg);
                            break;

                        }
                    }
                }
                catch (Exception e)
                {
                    //exception close()
                    Console.WriteLine(e);
                    connection.Shutdown(SocketShutdown.Both);
                    connection.Close();
                    break;
                }
            }
            try
            {
                if (msg != null)
                {
                    clientDict.Remove(msg.Id + "," + msg.Username);
                }
                RefreshTheList();

                connection.Shutdown(SocketShutdown.Both);
                connection.Close();
            }
            catch (Exception e)
            {
                if (msg != null)
                {
                    msg.Msg_body = msg.Username + " is leave...\n";
                    ShowMsg(msg);
                }
                Console.WriteLine(e);
            }
        }
        [MethodImpl(MethodImplOptions.Synchronized)]
        private void ShowMsg(MsgJsonFormatObj msg)
        {

            hostMsgBox.Msg_body = msg.Msg_body;

            foreach (KeyValuePair<string, Socket> item in clientDict)
            {
                if (item.Key != hostMsgBox.Id + "," + hostMsgBox.Username)
                {
                    SendingMsg(item.Value);
                }
            }
            txtbox_receive_msg.Dispatcher.BeginInvoke(
                new Action(() => { txtbox_receive_msg.Text += hostMsgBox.Msg_body; }), null);
        }

        private void SendingMsg(Socket connection)
        {
            string jsonData = JsonConvert.SerializeObject(hostMsgBox)+"\n";
            byte[] dataBytes = Encoding.Default.GetBytes(jsonData);
            if (connection.Connected) {
                connection.Send(dataBytes);
            }
        }
        private void SendingMsg(Socket connection, string msg)
        {
            hostMsgBox.Msg_body = msg;
            SendingMsg(connection);
        }

        private void RefreshTheList()
        {
            List<string> memberList = new List<string>();
            foreach (KeyValuePair<string, Socket> item in clientDict)
            {
                string[] id_name = item.Key.Split(",");
                if (item.Key.Equals(hostMsgBox.Id + "," + hostMsgBox.Username))
                {
                    memberList.Add(id_name[1] + "(" + hostIP+":"+port + ")");
                }
                else
                {
                    memberList.Add(id_name[1] + "(" + item.Value.RemoteEndPoint.ToString() + ")");
                }
               
            }
            hostMsgBox.MemberList = memberList;
            RecordsView.Dispatcher.BeginInvoke(
             new Action(() => { RecordsView.ItemsSource = memberList; }), null);
        }


        //close() when close window
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            Environment.Exit(0);
        }
    }
}
