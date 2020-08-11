using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Sockets;
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
        MsgJsonFormatObj hostMsgBox=null;
        private readonly string default_hostName = "host";
        private readonly string default_hostIP = "127.0.0.1";
        private readonly int default_port = 20001;

        private string hostIP = "";
        private string hostID = "";
        private string hostName = "";
        private int port = 0;

        public MainWindow()
        {
            InitializeComponent();
            CreateSettingTable.Visibility = Visibility.Hidden;
        }

        private void CreatRoom(object sender, RoutedEventArgs e)
        {
            btn_create_room.IsEnabled = false;
            btn_room_shutdown.IsEnabled = false;
            txtbox_sending_msg.IsReadOnly = true;
            btn_sending.IsEnabled = false;

            server_name.Text = default_hostName;
            server_ip.Text = default_hostIP;
            server_port.Text = default_port.ToString();

            CreateSettingTable.Visibility = Visibility.Visible;
        }

        private void CancelSetting(object sender, RoutedEventArgs e)
        {
            CloseSettingWindow();
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
                hostName = server_port.Text;
                port = Int32.Parse(server_port.Text);
                try
                {
                    Start();
                    CloseSettingWindow();
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show("Error : "+ ex.Message);
                }
            }

        }

        private void CloseSettingWindow()
        {
            btn_create_room.IsEnabled = true;
            btn_room_shutdown.IsEnabled = true;
            txtbox_sending_msg.IsReadOnly = false;
            btn_sending.IsEnabled = true;
            CreateSettingTable.Visibility = Visibility.Hidden;
        }
      


        //start a socket server
        private void Start()
        {
            if (server == null)
            {
                txtbox_receive_msg.Text = "Server Start";
                IPAddress ip = IPAddress.Parse(hostIP);
                //socket()
                server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                //bind()
                server.Bind(new IPEndPoint(ip, port));
                //listen()
                server.Listen(10);
                hostMsgBox = new MsgJsonFormatObj(hostID, hostName, "");

                Thread thread = new Thread(Listen);
                thread.Start();
            }
        }

        //listen to socket client
        private void Listen()
        {
            while (true)
            {
                //accept()
                Socket client = server.Accept();

                
                Thread receive = new Thread(ReceiveMsg);
                
                receive.Start(client);
            }
        }

        //receive client message and send to client
        public void ReceiveMsg(object client)
        {
            Socket connection = (Socket)client;
            IPAddress clientIP = (connection.RemoteEndPoint as IPEndPoint).Address;

            byte[] result = new byte[1024];
            //receive message from client
            int receive_num = connection.Receive(result);
            MsgJsonFormatObj msg = JsonConvert.DeserializeObject<MsgJsonFormatObj>(Encoding.ASCII.GetString(result, 0, receive_num));

            txtbox_receive_msg.Dispatcher.BeginInvoke(
                new Action(() => { txtbox_receive_msg.Text += "\n" + msg.Cient_username + " connect\n"; }), null);

            //send welcome message to client
            SendingMsg(connection,"Welcome " + msg.Cient_username + "\n");
            
            while (true)
            {
                try
                {
                    ShowMsg(connection);
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
        }

        private void ShowMsg(Socket connection)

        {
            byte[] result = new byte[1024];
            //receive message from client
            int receive_num = connection.Receive(result);
            MsgJsonFormatObj msg = JsonConvert.DeserializeObject<MsgJsonFormatObj>(Encoding.ASCII.GetString(result, 0, receive_num));

           /* String receive_str = Encoding.ASCII.GetString(result, 0, receive_num);*/

            if (receive_num > 0)
            {
                //resend message to client
                hostMsgBox.Msg_body = msg.Cient_username + " : " + msg.Msg_body;

                /* connection.Send(Encoding.ASCII.GetBytes("You send: " + msg.Msg_body));*/
                SendingMsg(connection);

                txtbox_receive_msg.Dispatcher.BeginInvoke(
                    new Action(() => { txtbox_receive_msg.Text += hostMsgBox.Msg_body; }), null);
            }

        }

        private void SendingMsg(Socket connection)
        {
            string jsonData = JsonConvert.SerializeObject(hostMsgBox);
            byte[] dataBytes = Encoding.Default.GetBytes(jsonData);
            connection.Send(dataBytes);
        }
        private void SendingMsg(Socket connection, string msg)
        {
            hostMsgBox.Msg_body = msg;
            SendingMsg(connection);
        }

        //close() when close window
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            Environment.Exit(0);
        }

       
    }
}
