﻿using EmbedIO.WebSockets;
using Graphite;
using Serilog;
using SlopCrew.Common.Network;
using System.Collections.Concurrent;
using SlopCrew.Common;

namespace SlopCrew.Server;

public class SlopWebSocketModule : WebSocketModule {
    public Dictionary<IWebSocketContext, ConnectionState> Connections = new();

    public SlopWebSocketModule() : base("/", true) { }

    protected override Task OnClientConnectedAsync(IWebSocketContext context) {
        lock (this.Connections) {
            this.Connections[context] = new ConnectionState(context);
            Server.Instance.Metrics.UpdateConnections(this.Connections.Count);

            return Task.CompletedTask;
        }
    }

    protected override Task OnClientDisconnectedAsync(IWebSocketContext context) {
        lock (this.Connections) {
            var state = this.Connections[context];
            Server.Instance.UntrackConnection(state);
            this.Connections.Remove(context);
            Server.Instance.Metrics.UpdateConnections(this.Connections.Count);

            return Task.CompletedTask;
        }
    }

    protected override Task OnMessageReceivedAsync(
        IWebSocketContext context, byte[] buffer, IWebSocketReceiveResult result
    ) {
        lock (this.Connections) {
            if (this.Connections.TryGetValue(context, out var state)) {
                try {
                    var msg = NetworkPacket.Read(buffer);
                    state.HandlePacket(msg);
                } catch (Exception e) {
                    Log.Error(e, "Error while handling message");
                }
            }

            return Task.CompletedTask;
        }
    }

    public void SendToContext(IWebSocketContext context, NetworkPacket msg) {
        var serialized = msg.Serialize();
        if (this.Connections.TryGetValue(context, out var state)) {
            lock (state.SendLock) {
                this.SendAsync(context, serialized);
            }
        } else {
            this.SendAsync(context, serialized);
        }
    }

    public void SendToContext(IWebSocketContext context, byte[] msg) {
        if (this.Connections.TryGetValue(context, out var state)) {
            lock (state.SendLock) {
                this.SendAsync(context, msg);
            }
        } else {
            this.SendAsync(context, msg);
        }
    }

    public void BroadcastInStage(
        IWebSocketContext context,
        NetworkPacket msg
    ) {
        if (!this.Connections.TryGetValue(context, out var state)) return;
        var otherSessions = this.Connections.ToList()
                                .Where(s => s.Key.Id != context.Id)
                                .Where(s => s.Value.Player?.Stage == state.Player?.Stage)
                                .ToList();

        var serialized = msg.Serialize();
        foreach (var session in otherSessions) {
            lock (session.Value.SendLock) {
                this.SendAsync(session.Key, serialized);
            }
        }
    }
}
