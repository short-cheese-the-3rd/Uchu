using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using RakDotNet;
using RakDotNet.IO;
using Uchu.Core;
using Uchu.World.Parsers;

namespace Uchu.World
{
    using GameMessageHandlerMap = Dictionary<ushort, List<Handler>>;
    
    public class WorldServer : Server
    {
        private readonly GameMessageHandlerMap _gameMessageHandlerMap;

        private readonly List<Zone> _zones = new List<Zone>();
        
        private readonly ZoneParser _parser;
        
        public WorldServer(int port, string password = "3.25 ND1") : base(port, password)
        {
            _gameMessageHandlerMap = new GameMessageHandlerMap();

            _parser = new ZoneParser(Resources);

            OnGameMessage += HandleGameMessage;
        }

        public async Task<Zone> GetZone(ZoneId zoneId)
        {
            if (_zones.Any(z => z.ZoneInfo.ZoneId == (uint) zoneId))
                return _zones.First(z => z.ZoneInfo.ZoneId == (uint) zoneId);
            
            var info = await _parser.ParseAsync(ZoneParser.Zones[zoneId]);

            // Create new Zone
            var zone = new Zone(info, this);
            _zones.Add(zone);
            zone.Initialize();

            return _zones.First(z => z.ZoneInfo.ZoneId == (uint) zoneId);
        }
        
        protected override void RegisterAssembly(Assembly assembly)
        {
            var groups = assembly.GetTypes().Where(c => c.IsSubclassOf(typeof(HandlerGroup)));

            foreach (var group in groups)
            {
                var instance = (HandlerGroup) Activator.CreateInstance(group);
                instance.Server = this;
                
                foreach (var method in group.GetMethods().Where(m => !m.IsStatic && !m.IsAbstract))
                {
                    var attr = method.GetCustomAttribute<PacketHandlerAttribute>();
                    if (attr == null) continue;

                    var parameters = method.GetParameters();
                    if (parameters.Length == 0 ||
                        !typeof(IPacket).IsAssignableFrom(parameters[0].ParameterType)) continue;
                    var packet = (IPacket) Activator.CreateInstance(parameters[0].ParameterType);

                    if (typeof(IGameMessage).IsAssignableFrom(parameters[0].ParameterType))
                    {
                        continue;
                    }

                    var remoteConnectionType = attr.RemoteConnectionType ?? packet.RemoteConnectionType;
                    var packetId = attr.PacketId ?? packet.PacketId;

                    if (!HandlerMap.ContainsKey(remoteConnectionType))
                        HandlerMap[remoteConnectionType] = new Dictionary<uint, Handler>();

                    var handlers = HandlerMap[remoteConnectionType];
                    
                    Logger.Debug(!handlers.ContainsKey(packetId) ? $"Registered handler for packet {packet}" : $"Handler for packet {packet} overwritten");
                    
                    handlers[packetId] = new Handler
                    {
                        Group = instance,
                        Info = method,
                        Packet = packet,
                        RunTask = attr.RunTask
                    };
                }
            }
        }

        private void HandleGameMessage(long objectId, ushort messageId, BitReader reader, IPEndPoint endPoint)
        {
            if (!_gameMessageHandlerMap.TryGetValue(messageId, out var messageHandler))
            {
                Logger.Warning($"No handler registered for GameMessage (0x{messageId:x})!");
                        
                return;
            }
                    
            Logger.Debug($"Received {messageHandler[0].Packet.GetType().FullName}");

            foreach (var handler in messageHandler)
            {
                reader.BaseStream.Position = 18;

                //((IGameMessage) handler.Packet).ObjectId = objectId;

                try
                {
                    reader.Read(handler.Packet);
                    // TODO: Invoke handler
                    Logger.Debug($"Invoked handler for GameMessage {messageHandler[0].Packet.GetType().FullName}");
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                    throw;
                }
            }
        }
    }
}