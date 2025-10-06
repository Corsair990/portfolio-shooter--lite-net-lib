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
    ChatMessage = 4,
    LoadNewScene =5
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

public struct WorldState : INetSerializable
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

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(playerID);
        writer.Put(posX);
        writer.Put(posY);
        writer.Put(posZ);
        writer.Put(rot);
        writer.Put(pitch);
        writer.Put(input);
        writer.Put(health);
        writer.Put(powerups);
        writer.Put(animation);
        writer.Put(tick);
    }

    public void Deserialize(NetDataReader reader)
    {
        playerID = reader.GetByte();
        posX = reader.GetUShort();
        posY = reader.GetUShort();
        posZ = reader.GetUShort();
        rot = reader.GetUShort();
        pitch = reader.GetSByte();
        input = reader.GetByte();
        health = reader.GetByte();
        powerups = reader.GetByte();
        animation = reader.GetByte();
        tick = reader.GetUShort();
    }

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

public struct SpawnData
{
    public byte playerID;
    public Vector3 pos;
}

public partial class NetworkMessages : Node
{
    public static NetDataWriter PlayerInput(InputData _inputs)
    { 
        NetDataWriter writer = new NetDataWriter();
        writer.Put((byte)MessageType.PlayerInput);
        return writer;
    }

    public static NetDataWriter PlayerSpawn(byte _playerId, Vector3 _position)
    {
        NetDataWriter writer = new NetDataWriter();
        writer.Put((byte)MessageType.PlayerSpawn);
        writer.Put((_playerId));
        writer.Put((ushort)(_position.X));
        writer.Put((ushort)(_position.Y));
        writer.Put((ushort)(_position.Z));
        return writer;
    }

    public static SpawnData ReadPlayerSpawn(NetDataReader _reader)
    {
        byte playerId = _reader.GetByte();
        float x = _reader.GetUShort();
        float y = _reader.GetUShort();
        float z = _reader.GetUShort();
        Vector3 position = new Vector3(x, y, z);

        GD.Print($"Read PlayerSpawn: playerId:{playerId}, position:{position}");
        return new SpawnData { playerID = playerId, pos = position };
    }

    public static NetDataWriter PlayerDespawn(byte _playerId)
    {
        NetDataWriter writer = new NetDataWriter();
        writer.Put((byte)MessageType.PlayerDespawn);
        writer.Put(_playerId);
        return writer;
    }

    public static byte ReadPlayerDespawn(NetDataReader _reader)
    {
        return _reader.GetByte();
    }

    public static NetDataWriter WriteWorldStateBatch(WorldState[] states)
    {
        NetDataWriter writer = new NetDataWriter();
        writer.Put((byte)MessageType.WorldState);

        writer.Put((byte)states.Length);

        // Loop and serialize each 16-byte struct
        foreach (var state in states)
        {
            state.Serialize(writer);
        }

        return writer;
    }

    public static NetDataWriter WorldState(WorldState _worldState)
    {
        NetDataWriter writer = new NetDataWriter();
        writer.Put((byte)MessageType.WorldState);
        return writer;
    }

    public static WorldState ReadWorldState(NetDataReader _reader)
    {
        byte playerId = _reader.GetByte();
        ushort x = _reader.GetUShort();
        ushort y = _reader.GetUShort();
        ushort z = _reader.GetUShort();
        ushort rot = _reader.GetUShort(); // 65535f * 360f;
        sbyte pitch = _reader.GetSByte(); // 127f * 90f;
        byte input = _reader.GetByte();
        byte health = _reader.GetByte();
        byte powerups = _reader.GetByte();
        byte animation = _reader.GetByte();
        ushort tick = _reader.GetUShort();

        return new WorldState { playerID = playerId, posX = x, posY = y, rot = rot, pitch = pitch, input = input, health = health, powerups = powerups, animation = animation, tick = tick  };
    }

    public static WorldState[] ReadWorldStateBatch(NetDataReader _reader)
    {
        byte count = _reader.GetByte();
        WorldState[] states = new WorldState[count];

        for (int i = 0; i < count; i++)
        {
            WorldState state = new WorldState();
            state.Deserialize(_reader);
            states[i] = state;
        }

        return states;
    }

    public static NetDataWriter ChatMessage(byte _playerId, string _message)
    {
        NetDataWriter writer = new NetDataWriter();
        writer.Put((byte)MessageType.ChatMessage);
        writer.Put(_playerId);
        writer.Put(_message);
        return writer;
    }

    public static String ReadChatMessage(NetDataReader _reader)
    {
        byte playerId = _reader.GetByte();
        string message = _reader.GetString();
        GD.Print($"{playerId}: {message}");
        return message;
    }

}
