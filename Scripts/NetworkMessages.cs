using Godot;
using System;
using LiteNetLib;
using LiteNetLib.Utils;

public enum MessageType : byte
{
    PlayerInput = 0,
    PlayerSpawn = 1,
    PlayerDespawn = 2,
    WorldState = 3,
    ChatMessage = 4
}

public struct InputData : INetSerializable
{
    public byte packedInput; // Using a byte to represent up to 8 inputs with bitwise operations
    public ushort tick; // Using a ushort for tick count to save space

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(packedInput);
        writer.Put(tick);
    }

    public void Deserialize(NetDataReader reader)
    {
        packedInput = reader.GetByte();
        tick = reader.GetUShort();
    }
}

public struct WorldState
{
    // We'll use a byte for the player ID, allowing up to 255 players
    public byte playerID;

    // Cant send a vector directly so we split it into its components and use ushorts to optimize network data
    public ushort posX;
    public ushort posY;
    public ushort posZ;

    // We'll use a ushort for rotations. We can represent 0-360 degrees with a precision of 0.0055 degrees
    public ushort rot;

    // We use a sbyte to represent pitch from -90 to +90 degrees
    public sbyte pitch;

    // We use a a single byte to represent up to 8 inputs using bitwise operations
    public byte input;

    // We use a single byte to represent Health from 0 to 100
    public byte health;

    // We use a single byte to represent up to 8 status effects using bitwise operations
    public byte powerups;

    // We use a single byte to represent up to 255 animation states
    public byte animation;

    // We use a ushort to represent the tick count for synchronization
    // Rounds will be precise for about 18 minutes at 60 ticks per second
    // We'll keeps rounds short to about 5 minutes to avoid this being an issue
    public ushort tick;

    /*
     * Our world state packet is 16 bytes per player at 60hz server tick this results in about 960 bytes/sec.
     * At 4 players this is about 31 kbps for the server or 3.84 KBps. This setup would run on dial-up.
     * At 16 players this is about 124 kbps for the server or 15.5 KBps.
    */

    public WorldState(byte _playerID, ushort _posX, ushort _posY, ushort _posZ, ushort _rot, sbyte _pitch, byte _input, byte _health, byte _powerups, byte _animation, ushort _tick)
    {
        playerID = _playerID;
        posX = _posX;
        posY = _posY;
        posZ = _posZ;
        rot = _rot;
        pitch = _pitch;
        input = _input;
        health = _health;
        powerups = _powerups;
        animation = _animation;
        tick = _tick;
    }
}

public partial class NetworkMessages : Node
{
    public static NetDataWriter PlayerInput(InputData _inputs)
    { 
        NetDataWriter writer = new NetDataWriter();
        writer.Put((byte)MessageType.PlayerInput);
        return writer;
    }

    public static NetDataWriter PlayerSpawn(int _playerId, Vector3I _position)
    {
        NetDataWriter writer = new NetDataWriter();
        writer.Put((byte)MessageType.PlayerSpawn);
        writer.Put((byte)(_playerId));
        writer.Put((ushort)(_position.X));
        writer.Put((ushort)(_position.Y));
        writer.Put((ushort)(_position.Z));
        return writer;
    }

    public static NetDataReader ReadPlayerSpawn(NetDataReader _reader)
    {
        int playerId = _reader.GetUShort();
        int x = _reader.GetUShort();
        int y = _reader.GetUShort();
        int z = _reader.GetUShort();
        Vector3I position = new Vector3I(x, y, z);
        GD.Print($"Read PlayerSpawn: playerId:{playerId}, position:{position}");
        return _reader;
    }

    public static NetDataWriter PlayerDespawn(int _playerId)
    {
        NetDataWriter writer = new NetDataWriter();
        writer.Put((byte)MessageType.PlayerDespawn);
        writer.Put((byte)_playerId);
        return writer;
    }

    public static NetDataWriter WorldState(WorldState _worldState)
    {
        NetDataWriter writer = new NetDataWriter();
        writer.Put((byte)MessageType.WorldState);
        return writer;
    }

    public static NetDataReader ReadWorldState(NetDataReader _reader)
    {
        int playerId = _reader.GetUShort();
        int x = _reader.GetUShort();
        int y = _reader.GetUShort();
        int z = _reader.GetUShort();
        float rot = _reader.GetUShort() / 65535f * 360f;
        float pitch = _reader.GetSByte() / 127f * 90f;
        byte input = _reader.GetByte();
        byte health = _reader.GetByte();
        byte powerups = _reader.GetByte();
        byte animation = _reader.GetByte();
        ushort tick = _reader.GetUShort();

        return _reader;
    }

    public static NetDataWriter ChatMessage(int _playerId, string _message)
    {
        NetDataWriter writer = new NetDataWriter();
        writer.Put((byte)MessageType.ChatMessage);
        writer.Put((byte)_playerId);
        writer.Put(_message);
        return writer;
    }

    public static NetDataReader ReadChatMessage(NetDataReader _reader)
    {
        int playerId = _reader.GetUShort();
        string message = _reader.GetString();
        GD.Print($"{playerId}: {message}");
        return _reader;
    }

}
