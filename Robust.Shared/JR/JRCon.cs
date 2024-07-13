using System.Net.Sockets;
using System.Net;
using System;
using System.Collections.Generic;
using System.Threading;

#pragma warning disable CS8625
#pragma warning disable CS8602
#pragma warning disable CS8603
#pragma warning disable CS8618
#pragma warning disable CS8600

namespace SlayerTK
{
    public enum JRType
    {
        SERVER_AUTH = 0,
        SERVERDATA_EXECCOMMAND,
        SERVERDATA_MESSAGE,
        SERVERDATA_MESSAGE_END,
        SERVERDATA_AUTOCOMPLETE_REQUEST,
        SERVDATA_AUTOCOMPLETE,
        SERVERDATA_AUTOCOMPLETE_EXTRA,
        SERVERDATA_AUTOCOMPLETE_REMOVE
    }

    public class JRMessage
    {
        public static JRMessage Create(string str, int id, JRType type, int extra)
        {
            JRMessage msg = new JRMessage();
            msg._message = str;
            msg._type = type;
            msg._extra = extra;
            msg._id = id;
            return msg;
        }

        public static JRMessage Create(byte[] buffer, int bufferLength, out int consumed)
        {
            JRMessage msg = new JRMessage();
            short size;
            int id;
            short type_;
            JRType type;
            int extra;
            string msgStr;
            consumed = 0;

            if (bufferLength < 12)
                return null;

            size = BitConverter.ToInt16(buffer, 0);
            size = IPAddress.NetworkToHostOrder(size);

            if (size + 12 > bufferLength)
                return null;

            id = BitConverter.ToInt32(buffer, 2);
            type_ = BitConverter.ToInt16(buffer, 6);
            extra = BitConverter.ToInt32(buffer, 8);


            id = IPAddress.NetworkToHostOrder(id);
            type_ = IPAddress.NetworkToHostOrder(type_);
            extra = IPAddress.NetworkToHostOrder(extra);

            type = (JRType)type_;

            msgStr = System.Text.Encoding.UTF8.GetString(buffer, 12, size);

            msg._message = msgStr;
            msg._type = type;
            msg._extra = extra;
            msg._id = id;

            consumed = 12 + msgStr.Length;

            return msg;
        }

        public string Message()
        {
            return _message;
        }

        public JRType Type()
        {
            return _type;
        }

        public int ID()
        {
            return _id;
        }

        public int Extra()
        {
            return _extra;
        }

        public byte[] ToBytes()
        {
            byte[] data = new byte[12 + _message.Length];
            short size = IPAddress.HostToNetworkOrder((short)_message.Length);
            int id = IPAddress.HostToNetworkOrder(_id);
            short type = IPAddress.HostToNetworkOrder((short)_type);
            int extra = IPAddress.HostToNetworkOrder(_extra);

            Array.Copy(BitConverter.GetBytes(size), 0, data, 0, 2);
            Array.Copy(BitConverter.GetBytes(id), 0, data, 2, 4);
            Array.Copy(BitConverter.GetBytes(type), 0, data, 6, 2);
            Array.Copy(BitConverter.GetBytes(extra), 0, data, 8, 4);
            Array.Copy(System.Text.Encoding.ASCII.GetBytes(_message), 0, data, 12, Math.Min(JRCon.JRCON_MAX_MSG, _message.Length));

            return data;
        }

        private string _message = "";
        private JRType _type;
        private int _id;
        private int _extra;
    }

    public enum JRStatus
    {
        SHUTTING_DOWN = -1,
        SHUTDOWN = 0,
        AWAITING_CONNECTION,
        CONNECTED,

    }

    public class JRCallbackArgs : EventArgs
    {
        public JRCallbackArgs(string message)
        {
            Message = message;
        }
        public readonly string Message;
    }

    public class JRAutoArgs : EventArgs
    {
        public JRAutoArgs(string name, string extra)
        {
            Name = name;
            Extra = extra;
        }
        public readonly string Name;
        public readonly string Extra;
        public List<string> Values = new List<string>();
        public int OutAt;
    }

    public class JRAutoRequest : EventArgs
    {
        public List<string> Values = new List<string>();
    }



    public static class JRCon
    {

        public static int JRCON_MAX_PACKET = 4096;
        public static int JRCON_MAX_MSG = JRCON_MAX_PACKET - 12;
        static Socket _client = null;
        static Socket _server = null;
        static JRStatus _status = JRStatus.SHUTDOWN;

        static Thread _recvThread = null;
        static Thread _sendThread = null;
        static Thread _acceptThread = null;

        static string _password = "";
        static int _id = 0;

        private static byte[] _recvBuffer = new byte[4096];
        private static int _recvBufferLength = 0;

        private static List<JRMessage> _sendQueue = new List<JRMessage>();

        public static event EventHandler<JRCallbackArgs> Callback;
        public static event EventHandler<JRAutoArgs> AutocompleteCallback;
        public static event EventHandler<JRAutoRequest> AutorequestCallback;

        public static void Host(string password = "", short port = 27015)
        {
            if (_status >= JRStatus.AWAITING_CONNECTION)
                Disconnect();

            _server = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp);

            _server.Bind(new IPEndPoint(IPAddress.Any, port));
            _server.Listen(port);

            _server.ReceiveTimeout = 1;

            _status = JRStatus.AWAITING_CONNECTION;

            _recvThread = new Thread(_recv);
            _sendThread = new Thread(_send);
            _acceptThread = new Thread(_accept);

            _recvThread.Start();
            _sendThread.Start();
            _acceptThread.Start();
        }

        public static void Send(object value)
        {
            Send(value, new JRColor(255, 255, 255));
        }

        public static void Send(object value, JRColor color)
        {
            lock (_sendQueue)
            {
                string msg = value.ToString();
                int _id = NewID();
                int _extra = 0;
                _extra = color.R;
                _extra = (_extra << 8) + color.G;
                _extra = (_extra << 8) + color.B;
                _extra = (_extra << 8) + Convert.ToByte(msg.Length > JRCON_MAX_MSG);
                if (msg.Length > JRCON_MAX_MSG)
                {
                    for (int i = 0; i < msg.Length;)
                    {
                        string newMsg = msg.Substring(i, Math.Min(JRCON_MAX_MSG, msg.Length));

                        _sendQueue.Add(JRMessage.Create(newMsg,
                            _id, JRType.SERVERDATA_MESSAGE, _extra
                            ));
                        i += newMsg.Length;
                    }
                    _sendQueue.Add(JRMessage.Create("", _id, JRType.SERVERDATA_MESSAGE_END, 0));
                }
                else
                {
                    _sendQueue.Add(JRMessage.Create(msg, _id, JRType.SERVERDATA_MESSAGE, _extra));
                }
            }
        }

        public static void SendAutocomplete(string value)
        {
            SendMessage(JRMessage.Create(value, NewID(), JRType.SERVDATA_AUTOCOMPLETE, 0));
        }

        public static void Disconnect()
        {
            lock (_sendQueue)
            {
                _sendQueue.Clear();
                _client.Disconnect(false);
                _client = null;

                _status = JRStatus.SHUTDOWN;
                _recvBufferLength = 0;
            }
        }

        public static JRStatus Status()
        {
            return _status;
        }

        private static JRMessage GetMessage()
        {
            JRMessage msg = null;
            int got = 0;
            int consumed = 0;
            if (_recvBufferLength >= 12)
            {
                msg = JRMessage.Create(_recvBuffer, _recvBufferLength, out consumed);
                Array.Copy(_recvBuffer, consumed, _recvBuffer, 0, _recvBufferLength - consumed);
                if (msg != null)
                {
                    _recvBufferLength -= consumed;
                    return msg;
                }
            }
            if (_client.Available > 0)
                got = _client.Receive(_recvBuffer, _recvBufferLength, JRCON_MAX_PACKET - _recvBufferLength, SocketFlags.None);
            else
                return null;

            if (got + _recvBufferLength < 12)
                return null;

            _recvBufferLength += got;

            msg = JRMessage.Create(_recvBuffer, _recvBufferLength, out consumed);

            if (msg != null)
                Array.Copy(_recvBuffer, consumed, _recvBuffer, 0, _recvBufferLength - consumed);
            _recvBufferLength -= consumed;
            return msg;

        }

        private static bool SendMessage(JRMessage msg)
        {
            if (_client == null)
                return false;

            byte[] data = msg.ToBytes();
            if (_client.Send(data) != data.Length)
            {
                return false;
            }
            return true;
        }

        static int NewID()
        {
            return _id++;
        }

        private static void _accept()
        {
            while (_status >= JRStatus.AWAITING_CONNECTION)
            {
                if (_status == JRStatus.AWAITING_CONNECTION)
                    Thread.Sleep(50);

                try
                {
                    _client = _server.Accept();

                    JRMessage auth = GetMessage();
                    if (auth == null)
                    {
                        _client.Disconnect(false);
                        _client = null;
                        continue;
                    }

                    if (auth.Message() == _password)
                    {
                        JRMessage authRep = JRMessage.Create("", auth.ID(), JRType.SERVER_AUTH, 1);
                        SendMessage(authRep);
                        _status = JRStatus.CONNECTED;
                    }
                    else
                    {
                        SendMessage(JRMessage.Create("", -1, JRType.SERVER_AUTH, 0));
                        _client.Disconnect(false);
                        _client = null;
                        return;
                    }


                }
                catch (Exception e) { }
            }
        }

        private static void _recv()
        {
            while (_status >= JRStatus.AWAITING_CONNECTION)
            {
                if (_status != JRStatus.CONNECTED)
                {
                    Thread.Sleep(TimeSpan.FromMilliseconds(0.0005));
                    continue;
                }

                JRMessage msg = GetMessage();
                if (msg != null)
                {
                    switch (msg.Type())
                    {
                        case JRType.SERVERDATA_EXECCOMMAND:
                            if (Callback != null)
                                Callback.Invoke(null, new JRCallbackArgs(msg.Message()));
                            break;
                        case JRType.SERVERDATA_AUTOCOMPLETE_REQUEST:
                            {
                                int id = NewID();
                                JRAutoRequest req = new JRAutoRequest();
                                if (AutorequestCallback != null)
                                    AutorequestCallback.Invoke(null, req);

                                foreach (var val in req.Values)
                                    SendMessage(JRMessage.Create(val, id, JRType.SERVDATA_AUTOCOMPLETE, 0));

                                SendMessage(JRMessage.Create("", id, JRType.SERVERDATA_AUTOCOMPLETE_REQUEST, 0));
                            }
                            break;
                        case JRType.SERVERDATA_AUTOCOMPLETE_EXTRA:
                            {
                                JRAutoArgs args = new JRAutoArgs(
                                    msg.Message().Substring(0, msg.Extra()),
                                    msg.Message().Substring(msg.Extra() + 1)
                                    );
                                if (AutocompleteCallback != null)
                                    AutocompleteCallback.Invoke(null, args);
                                int id = NewID();
                                foreach (var v in args.Values)
                                {
                                    SendMessage(JRMessage.Create(
                                            v,
                                            id,
                                            JRType.SERVERDATA_AUTOCOMPLETE_EXTRA,
                                            0
                                        ));
                                }
                                int extra = 1;
                                ushort outat = (ushort)args.OutAt;
                                for (int i = 0; i < 16; i++)
                                {
                                    int value = outat >> i & 1;
                                    extra |= value << i + 1;
                                }
                                SendMessage(JRMessage.Create(
                                    "",
                                    id,
                                    JRType.SERVERDATA_AUTOCOMPLETE_EXTRA,
                                    extra
                                    ));
                            }
                            break;
                        case JRType.SERVDATA_AUTOCOMPLETE: break;
                    }
                }
            }
        }

        private static void _send()
        {
            while (_status >= JRStatus.AWAITING_CONNECTION)
            {
                if (_status != JRStatus.CONNECTED)
                {
                    Thread.Sleep(TimeSpan.FromMilliseconds(0.0005));
                    continue;
                }

                lock (_sendQueue)
                {
                    foreach (var msg in _sendQueue)
                    {
                        SendMessage(msg);
                    }
                    _sendQueue.Clear();
                }
            }
        }

    }

    public struct JRColor
    {
        public JRColor(byte r = 0, byte g = 0, byte b = 0)
        {
            R = r;
            G = g;
            B = b;
        }

        public byte R;
        public byte G;
        public byte B;
    }
}
