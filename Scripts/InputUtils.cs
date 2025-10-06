using System;
using Godot;

public static class InputUtils
{
    // Define the masks using bit positions (powers of 2)
    public enum InputMask : byte
    {
        Forward = 1,      // Bit 0 (WASD: W / Up)
        Backward = 2,     // Bit 1 (WASD: S / Down)
        StrafeLeft = 4,   // Bit 2 (WASD: A)
        StrafeRight = 8,  // Bit 3 (WASD: D)
        Jump = 16,        // Bit 4
        PrimaryFire = 32, // Bit 5 (Mouse 1)
        Reload = 64,      // Bit 6 (R)
        Interact = 128    // Bit 7 (E)
    }

    // Packs current Input state into a byte.
    public static byte PackCurrentInput()
    {
        byte packed = 0;

        // Movement
        if (Input.IsActionPressed("move_forward")) packed |= (byte)InputMask.Forward;
        if (Input.IsActionPressed("move_backward")) packed |= (byte)InputMask.Backward;
        if (Input.IsActionPressed("move_left")) packed |= (byte)InputMask.StrafeLeft;
        if (Input.IsActionPressed("move_right")) packed |= (byte)InputMask.StrafeRight;

        // Actions
        if (Input.IsActionPressed("jump")) packed |= (byte)InputMask.Jump;
        if (Input.IsActionPressed("fire_primary")) packed |= (byte)InputMask.PrimaryFire;
        if (Input.IsActionPressed("reload")) packed |= (byte)InputMask.Reload;
        if (Input.IsActionPressed("interact")) packed |= (byte)InputMask.Interact;

        return packed;
    }

    // Returns the current input state.
    public static bool GetInputState(byte packedData, InputMask mask)
    {
        // Use the bitwise AND operator to check if the specific bit is set
        return (packedData & (byte)mask) != 0;
    }
}