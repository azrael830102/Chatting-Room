using System;
using System.Collections.Generic;
using System.Text;

namespace Client
{
    class MsgJsonFormatObj
    {
        private string _id;
        private string _username;
        private string msg_body;
        private bool _isAlive;
        List<string> _memberList = new List<string>();

        public MsgJsonFormatObj(string id, string username, string msg)
        {
            _id = id;
            _username = username;
            msg_body = msg;
            _isAlive = true;
        }

        public string Id { get => _id; set => _id = value; }
        public string Username { get => _username; set => _username = value; }
        public string Msg_body { get => msg_body; set => msg_body = value; }
        public bool IsAlive { get => _isAlive; set => _isAlive = value; }
        public List<string> MemberList { get => _memberList; set => _memberList = value; }
    }
}
