﻿using System.Dynamic;
using System.Threading.Tasks;

namespace SignalR.Hubs {
    public class ClientAgent : DynamicObject, IClientAgent, IGroupManager {
        private readonly IConnection _connection;
        private readonly string _hubName;

        public ClientAgent(IConnection connection, string hubName) {
            _connection = connection;
            _hubName = hubName;
        }

        public dynamic this[string key] {
            get {
                return new SignalAgent(_connection, key, _hubName);
            }
        }

        public Task Invoke(string method, params object[] args) {
            return Invoke(_connection, method, _hubName, method, args);
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result) {
            result = this[binder.Name];
            return true;
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result) {
            result = Invoke(binder.Name, args);
            return true;
        }

        public static Task Invoke(IConnection connection, string signal, string hubName, string method, object[] args) {
            signal = hubName + "." + signal;

            var invocation = new {
                Hub = hubName,
                Method = method,
                Args = args
            };

            return connection.Broadcast(signal, invocation);
        }

        public Task AddToGroup(string clientId, string groupName) {
            groupName = _hubName + "." + groupName;
           return  SendCommand(clientId, CommandType.AddToGroup, groupName);
        }

        public Task RemoveFromGroup(string clientId, string groupName) {
            groupName = _hubName + "." + groupName;
            return SendCommand(clientId, CommandType.RemoveFromGroup, groupName);
        }

        private Task SendCommand(string clientId, CommandType commandType, object commandValue) {
            string signal = _hubName + "." + clientId + "." + PersistentConnection.SignalrCommand;

            var groupCommand = new SignalCommand {
                Type = commandType,
                Value = commandValue
            };

            return _connection.Broadcast(signal, groupCommand);
        }
    }
}
