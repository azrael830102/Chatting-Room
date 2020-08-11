using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;

namespace Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public Socket client;
        public String host = "127.0.0.1";
        public int port = 20001;

        private string client_id;
        private string client_username;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Connect(object sender, RoutedEventArgs e)
        {
            //connect to socket server
            IPAddress ip = IPAddress.Parse(host);
            //socket()
            client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //connect()
            client.Connect(new IPEndPoint(ip, port));

            client_id = System.Guid.NewGuid().ToString();

            MsgJsonFormatObj msg = new MsgJsonFormatObj(client_id, "user1", "test123");
            SendingMsg(msg);


            Thread receiveThread = new Thread(ReceiveMessage);
            receiveThread.Start();
        }

        private void SendingMsg(MsgJsonFormatObj msg)
        {
            string jsonData = JsonConvert.SerializeObject(msg);
            byte[] dataBytes = Encoding.Default.GetBytes(jsonData);
            client.Send(dataBytes);
        }

        //send()
        private void Send(object sender, RoutedEventArgs e)
        {
            String text = txtbox_msg.Text;
            try
            {
                //send message to server
                client.Send(Encoding.ASCII.GetBytes(text + "\n"));
                txtbox_msg.Text = "";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                txtbox_receive_msg.Text += "send Fail\n";
            }

        }

        //receive()
        public void ReceiveMessage()
        {
            while (true)
            {
                try
                {
                    ShowMsg();
                }
                catch (Exception ex)
                {
                    //exception close()
                    System.Windows.MessageBox.Show("Error : " + ex.Message);
                    client.Shutdown(SocketShutdown.Both);
                    client.Close();
                    break;
                }
            }

        }
        private void ShowMsg()
        {
            byte[] result = new byte[1024];
            //receive message from client
            int receive_num = client.Receive(result);
            String recStr = Encoding.ASCII.GetString(result, 0, receive_num);

            MsgJsonFormatObj msg = JsonConvert.DeserializeObject<MsgJsonFormatObj>(Encoding.ASCII.GetString(result, 0, receive_num));
            txtbox_receive_msg.Dispatcher.BeginInvoke(
                           new Action(() => { txtbox_receive_msg.Text += msg.Msg_body; }), null);
        }
    }
}
