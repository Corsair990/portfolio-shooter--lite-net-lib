using Godot;
using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Net;
using System.Net.Sockets;

public partial class NetworkManager : Node, INetEventListener
{
    private NetManager netMan;
    [Export] private bool isServer = false;
    [Export] private string address = "127.0.0.1";
    [Export] private int port = 9050;
    [Export] private byte tickRate = 60; // 60 ticks per second

    public int currentTick = 0;

    [Export] private PackedScene serverPlayerScene;
    [Export] private PackedScene clientPlayerScene;

    public override void _Ready()
    {
        if (isServer)
        {
            StartServer();
        }
    }

    public override void _PhysicsProcess(double _delta)
    {
        if (isServer && netMan != null && netMan.IsRunning)
        {
            netMan.PollEvents();
            currentTick++;
            ProcessClientInputs();
        }
    }

    public void StartServer()
    {
        netMan = new NetManager(this);
        netMan.Start(port);
        GD.Print($"Server started on port {port}...");
    }

    public void StartClient()
    {
        netMan = new NetManager(this);
        netMan.Start();
        netMan.Connect(address, port, "");
        GD.Print($"Client started and connecting to server {address}:{port}...");
    }

    public void OnConnectionRequest(ConnectionRequest _request)
    {
        
    }

    public void OnNetworkError(IPEndPoint _endPoint, SocketError _socketError)
    {
        GD.PrintErr($"Network error: {_socketError} at {_endPoint}");
    }

    public void OnNetworkLatencyUpdate(NetPeer _peer, int _latency)
    {
        
    }

    public void OnNetworkReceive(NetPeer _peer, NetPacketReader _reader, byte _channelNumber, DeliveryMethod _deliveryMethod)
    {
        byte type = _reader.GetByte();

        if ((MessageType)type == MessageType.PlayerInput)
        {
            byte count = _reader.GetByte();

            for (int i = 0; i < count; i++)
            {
                InputData input = new InputData();
                input.Deserialize(_reader);

                GD.Print($"Received input from peer {_peer.Id}: PackedInput:{input.packedInput}, Tick:{input.tick}");
            }
        }
    }

    public void OnNetworkReceiveUnconnected(IPEndPoint _remoteEndPoint, NetPacketReader _reader, UnconnectedMessageType _messageType)
    {
        
    }

    public void OnPeerConnected(NetPeer _peer)
    {
        GD.Print($"Peer connected: {_peer.Id}");
    }

    public void OnPeerDisconnected(NetPeer _peer, DisconnectInfo _disconnectInfo)
    {
        GD.Print($"Peer disconnected: {_peer.Id}, Reason: {_disconnectInfo.Reason}");
    }

    private void ProcessClientInputs()
    {
        
    }

    private void ProcessPhysics()
    { 
    
    }

    private void SendWorldState()
    {
        // ollect data for the WorldState (16 bytes per player).
        // Iterate through all peers connected to the server.
        foreach (NetPeer peer in netMan.ConnectedPeerList)
        {
            // Generate the array of WorldState structs here.
            // NetDataWriter writer = NetworkMessages.CreateWorldSnapshot(allPlayerStates);
            // peer.Send(writer, DeliveryMethod.Unreliable);
        }
    }
}
