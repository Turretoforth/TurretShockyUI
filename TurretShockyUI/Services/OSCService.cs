using System;
using VRChatOSCLib;

namespace TurretShocky.Services
{
    public class OSCService
    {
        private static OSCService? _currentInstance = null;
        private static VRChatOSC? _oscClient = new();
        private static int? _remotePort; // Default VRChat OSC port
        private static int? _localPort; // Port for receiving OSC messages

        private OSCService()
        {
        }

        public static void Initialize(int remotePort = 9000, int localPort = 9001)
        {
            _currentInstance = new OSCService();
            _oscClient = new VRChatOSC(remotePort);
            _remotePort = remotePort;
            _localPort = localPort;
        }
        public static void Destroy()
        {
            _oscClient?.Dispose();
            _oscClient = null;
            _currentInstance = null;
            _remotePort = null;
            _localPort = null;
        }

        private static void CheckInstance()
        {
            if (_currentInstance == null)
            {
                throw new InvalidOperationException("OSCService is not initialized. Call Initialize() first.");
            }
            if (_oscClient == null)
            {
                throw new InvalidOperationException("OSC client is not initialized. Call Initialize() first.");
            }
        }

        public static void SendParameter(string name, bool value)
        {
            CheckInstance();
            _oscClient.SendParameter(name, value);
        }
        public static void SendParameter(string name, int value)
        {
            CheckInstance();
            _oscClient.SendParameter(name, value);
        }
        public static void SendParameter(string name, float value)
        {
            CheckInstance();
            _oscClient.SendParameter(name, value);
        }

        public static void StartOSC(EventHandler<VRCMessage> onOscMessageReceived)
        {
            CheckInstance();
            _oscClient.Connect(_remotePort ?? 9000);
            _oscClient.OnMessage += onOscMessageReceived;
            _oscClient.Listen(_localPort ?? 9001);
        }

    }
}
