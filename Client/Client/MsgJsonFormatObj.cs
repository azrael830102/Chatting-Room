using System;
using System.Collections.Generic;
using System.Text;

namespace Client
{
    class MsgJsonFormatObj
    {
        private string cient_id;
        private string cient_username;
        private string msg_body;

        public MsgJsonFormatObj(string id,string username, string msg)
        {
            cient_id = id;
            cient_username = username;
            msg_body = msg;
        }

        public string Cient_id { get => cient_id; set => cient_id = value; }
        public string Cient_username { get => cient_username; set => cient_username = value; }
        public string Msg_body { get => msg_body; set => msg_body = value; }
    }
}
