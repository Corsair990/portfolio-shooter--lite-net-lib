using Godot;
using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;

public partial class NetworkManager : Node, INetEventListener
{
    private NetManager netMan;
    [Export] private bool isServer = false;
    [Export] private string address = "127.0.0.1";
    [Export] private int port = 9050;
    [Export] private byte tickRate = 60; // 60 ticks per second

    public int currentTick = 0;

    [Export] private PackedScene levelScene;
    [Export] private PackedScene serverPlayerScene;
    [Export] private PackedScene clientPlayerScene;

    private Dictionary<byte, Node> connectedClients = new Dictionary<byte, Node>();
    private Dictionary<byte, Queue<InputData>> inputBuffers = new Dictionary<byte, Queue<InputData>>();

    private NetPeer serverPeer;
    private byte localPlayerId = 0;
    private InputManager inputManager;

    public override void _Ready()
    {
        if (isServer)
        {
            StartServer();
        }
        else
            StartClient();
    }

    public override void _PhysicsProcess(double _delta)
    {
        if (isServer && netMan != null && netMan.IsRunning)
        {
            netMan.PollEvents();
            currentTick++;
            ProcessClientInputs();
            RunGameSimulation(_delta);
            SendWorldState();
        }
    }

    public void StartServer()
    {
        netMan = new NetManager(this);
        netMan.Start(port);
        GD.Print($"Server started on port {port}...");

        GetTree().UnloadCurrentScene();
        GetTree().ChangeSceneToPacked(levelScene);
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
        _request.Accept();

        GD.Print("Connection request approved.");
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

        if (isServer && netMan != null && netMan.IsRunning)
        {
            switch ((MessageType)type)
            {

                case MessageType.PlayerInput:
                    {
                        if (!inputBuffers.ContainsKey((byte)_peer.Id))
                        {
                            inputBuffers.Add((byte)_peer.Id, new Queue<InputData>());
                        }

                        Queue<InputData> peerInputQueue = inputBuffers[(byte)_peer.Id];

                        byte count = _reader.GetByte();

                        for (int i = 0; i < count; i++)
                        {
                            InputData input = new InputData();
                            input.Deserialize(_reader);

                            peerInputQueue.Enqueue(input);
                            GD.Print($"Received input from peer:{_peer.Id} PackedInput:{input.packedInput}, Tick:{input.tick}");
                        }
                    }
                    break;
            }
        }
        else
        {
            switch ((MessageType)type) 
            {
                case MessageType.PlayerSpawn: 
                    {
                        SpawnData spawn = NetworkMessages.ReadPlayerSpawn(_reader);

                        Node newPlayer = clientPlayerScene.Instantiate();
                        newPlayer.Name = $"Player_{spawn.playerID}";

                        newPlayer.Set("position", spawn.pos);
                        GetTree().Root.AddChild(newPlayer);

                        if (serverPeer != null && spawn.playerID == (byte)serverPeer.Id)
                        {
                            GD.Print($"Local player spawned with ID: {spawn.playerID}");
                            localPlayerId = spawn.playerID;
                            newPlayer.GetNode<PlayerController>("Player").OwnerClientId = localPlayerId;
                        }

                        connectedClients.Add(spawn.playerID, newPlayer);
                    }
                    break;

                case MessageType.PlayerDespawn:
                    {
                        byte playerId = NetworkMessages.ReadPlayerDespawn(_reader);
                        if (connectedClients.TryGetValue(playerId, out Node playerNode))
                        {
                            playerNode.QueueFree();
                            connectedClients.Remove(playerId);
                            GD.Print($"Client despawned player ID: {playerId}");
                        }
                    }
                    break;

                case MessageType.WorldState:
                    {
                        // Read the entire batch of authoritative states.
                        WorldState[] states = NetworkMessages.ReadWorldStateBatch(_reader);

                        // Apply states to local entities
                        foreach (var state in states)
                        {
                            if (connectedClients.TryGetValue(state.playerID, out Node playerNode))
                            {
                                // TODO: Apply reconciliation and smoothing logic here.
                                // For now, just snap position.
                                playerNode.Set("position", new Vector3(state.posX, state.posY, state.posZ));
                            }
                        }
                    }
                    break;
            }
        }

        
    }

    public void OnNetworkReceiveUnconnected(IPEndPoint _remoteEndPoint, NetPacketReader _reader, UnconnectedMessageType _messageType)
    {
        
    }

    public void OnPeerConnected(NetPeer _peer)
    {
        GD.Print($"Peer connected: {_peer.Id}");

        if (isServer && netMan != null && netMan.IsRunning)
        {
            Node newPlayer = serverPlayerScene.Instantiate();
            GetTree().Root.AddChild(newPlayer);
            connectedClients.Add((byte)_peer.Id, newPlayer);

            // Tell all clients a new player has spawned.
            NetDataWriter writer = NetworkMessages.PlayerSpawn((byte)_peer.Id, Vector3.Zero);
            netMan.SendToAll(writer, DeliveryMethod.ReliableOrdered);

            // Tell the new player where all the existing players are.
            foreach (var client in connectedClients) 
            {
                if (client.Key != (byte)_peer.Id)

                writer = NetworkMessages.PlayerSpawn((byte)_peer.Id, Vector3.Zero);
            }
        }
    }

    public void OnPeerDisconnected(NetPeer _peer, DisconnectInfo _disconnectInfo)
    {
        GD.Print($"Peer disconnected: {_peer.Id}, Reason: {_disconnectInfo.Reason}");
    }

    private void ProcessClientInputs()
    {
        foreach (var kvp in inputBuffers)
        {
            int playerId = kvp.Key;
            Node playerNode = connectedClients[(byte)playerId];
            PlayerController playerController = playerNode.GetNode<PlayerController>("Player");

            Queue<InputData> inputQueue = kvp.Value;

            while (inputQueue.Count > 0)
            {
                InputData input = inputQueue.Dequeue();

                playerController.ApplyInput(input.packedInput);
            }
        }
    }

    private void RunGameSimulation(double _delta)
    {
        // Authoritatively move all players based on the inputs applied in ProcessClientInputs
        foreach (var kvp in connectedClients)
        {
            // kvp.Value is the Player Node
            PlayerController controller = kvp.Value.GetNode<PlayerController>("PlayerController");
            controller.AuthoritativeMove((float)_delta);
        }

        // Move projectiles, run world events, resolve collisions, etc.
    }

    private void SendWorldState()
    {
        
    }
}
