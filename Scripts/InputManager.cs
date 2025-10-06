using Godot;
using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;

public partial class InputManager : Node
{
    public NetDataWriter CreateInputPacket(Queue<InputData> _inputQueue)
    {
        if (_inputQueue.Count == 0)
        {
            return null;
        }

        NetDataWriter writer = new NetDataWriter();

        writer.Put((byte)MessageType.PlayerInput);
        writer.Put((byte)_inputQueue.Count);

        while (_inputQueue.Count > 0)
        {
            InputData input = _inputQueue.Dequeue();
            input.Serialize(writer);
        }

        return writer;
    }
}