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

namespace Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public Socket client;


        private readonly string default_client_username = "client";
        private readonly string default_hostIP = "127.0.0.1";
        private readonly int default_port = 20001;

        private string client_id;
        private string client_username;
        private string hostIP = "";
        public int port = 20001;

        MsgJsonFormatObj msgJsonObj;

        public MainWindow()
        {
            InitializeComponent();
            SettingTable.Visibility = Visibility.Hidden;

            btn_leave_room.IsEnabled = false;
            txtbox_sending_msg.IsReadOnly = true;
            btn_sending.IsEnabled = false;
        }
        private void JoinRoom(object sender, RoutedEventArgs e)
        {
            btn_join_room.IsEnabled = false;

            name.Text = default_client_username;
            server_ip.Text = default_hostIP;
            server_port.Text = default_port.ToString();

            SettingTable.Visibility = Visibility.Visible;
        }
        private void CancelSetting(object sender, RoutedEventArgs e)
        {
            btn_join_room.IsEnabled = true;
            SettingTable.Visibility = Visibility.Hidden;
        }
        private void InputBoxOnblur(object sender, RoutedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            switch (textBox.Name.ToString())
            {
                case "name":
                    name.Text = (name.Text == null || name.Text.Equals("")) ? default_client_username : name.Text;
                    break;
                case "server_ip":
                    server_ip.Text = (server_ip.Text == null || server_ip.Text.Equals("")) ? default_hostIP : server_ip.Text;
                    break;
                case "server_port":
                    server_port.Text = (server_port.Text == null || server_port.Text.Equals("")) ? default_port.ToString() : server_port.Text;
                    break;
            }
        }

        private void Connect(object sender, RoutedEventArgs e)
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
                hostIP = server_ip.Text;
                port = Int32.Parse(server_port.Text);
                client_username = name.Text;

                //connect to socket server
                IPAddress ip = IPAddress.Parse(hostIP);
                //socket()
                client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                //connect()
                try
                {
                    client.Connect(new IPEndPoint(ip, port));
                    if (client.Connected)
                    {
                        client_id = System.Guid.NewGuid().ToString();
                        
                        msgJsonObj = new MsgJsonFormatObj(client_id, client_username, "");
                        msgJsonObj.IsAlive = true;
                        SendingMsg(msgJsonObj);
                        Thread receiveThread = new Thread(ReceiveMessage);
                        receiveThread.Start();
                        CloseSettingWindow();
                    }
                }
                catch (SocketException ex)
                {
                    if(ex.SocketErrorCode == SocketError.ConnectionRefused)
                    {
                        System.Windows.MessageBox.Show("Room not exists");
                    }
                    else
                    {
                        System.Windows.MessageBox.Show(ex.Message);
                    }
                   
                }
            }
        }

        private void LeaveRoom(object sender, RoutedEventArgs e)
        {
            ResetRoom();
            try
            {
                msgJsonObj.IsAlive = false;
                SendingMsg(msgJsonObj);
                txtbox_receive_msg.Dispatcher.BeginInvoke(
            new Action(() => { txtbox_receive_msg.Text = ""; }), null);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
        private void ResetRoom()
        {
            btn_leave_room.Dispatcher.BeginInvoke(
                         new Action(() => { btn_leave_room.IsEnabled = false;  }), null);
            txtbox_sending_msg.Dispatcher.BeginInvoke(
                        new Action(() => { txtbox_sending_msg.IsReadOnly = true; }), null);
            btn_sending.Dispatcher.BeginInvoke(
                        new Action(() => { btn_sending.IsEnabled = false; }), null);
            btn_join_room.Dispatcher.BeginInvoke(
                        new Action(() => { btn_join_room.IsEnabled = true; }), null);

            RecordsView.Dispatcher.BeginInvoke(
             new Action(() => { RecordsView.ItemsSource = new List<string>(); }), null);
        }

        //send()
        private void Send(object sender, RoutedEventArgs e)
        {
            String text = txtbox_sending_msg.Text;
            try
            {
                //send message to server
                SendingMsg(text + "\n");
                txtbox_sending_msg.Text = "";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                txtbox_receive_msg.Text += "send Fail\n";
            }
        }
        private void CloseSettingWindow()
        {
            btn_join_room.IsEnabled = false;
            btn_leave_room.IsEnabled = true;
            txtbox_sending_msg.IsReadOnly = false;
            btn_sending.IsEnabled = true;
            SettingTable.Visibility = Visibility.Hidden;
        }

        private void SendingMsg(string msgStr)
        {
            msgJsonObj.Msg_body = msgStr;
            SendingMsg(msgJsonObj);
        }

        private void SendingMsg(MsgJsonFormatObj msg)
        {
            string jsonData = JsonConvert.SerializeObject(msg);
            byte[] dataBytes = Encoding.Default.GetBytes(jsonData);
            if (client.Connected)
            {
                client.Send(dataBytes);
            }
        }


        //receive()
        public void ReceiveMessage()
        {
            while (true)
            {
                try
                {
                    if (!ShowMsg())
                    {
                        break;
                    }
                }
                catch (Exception ex)
                {
                    //exception close()
                    System.Windows.MessageBox.Show("Error : " + ex.Message);
                    client.Shutdown(SocketShutdown.Both);
                    client.Close();
                    ResetRoom();
                    break;
                }
            }
            msgJsonObj.IsAlive = false;
            ResetRoom();
            if (client.Connected)
            {
                client.Shutdown(SocketShutdown.Both);
                client.Close();
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private bool ShowMsg()
        {
            byte[] result = new byte[1024];
            //receive message from client
            int receive_num = client.Receive(result);
            String recStr = Encoding.ASCII.GetString(result, 0, receive_num);
            MsgJsonFormatObj hostMsg = JsonConvert.DeserializeObject<MsgJsonFormatObj>(Encoding.ASCII.GetString(result, 0, receive_num));
            if (hostMsg != null && hostMsg.IsAlive)
            {
                if (hostMsg.MemberList != null && hostMsg.MemberList.Count > 0)
                {
                    RecordsView.Dispatcher.BeginInvoke(
                        new Action(() => { RecordsView.ItemsSource = hostMsg.MemberList; }), null);
                }
                txtbox_receive_msg.Dispatcher.BeginInvoke(
                           new Action(() => { txtbox_receive_msg.Text += hostMsg.Msg_body; }), null);
                if (hostMsg.Id.Equals(""))
                {
                    return false;
                }
                return true;
            }
            else
            {
                msgJsonObj.IsAlive = false;
                SendingMsg(msgJsonObj);
                return false;
            }

        }
    }
}
