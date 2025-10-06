using Godot;
using LiteNetLib.Utils;
using System;

public partial class PlayerController : CharacterBody3D
{
    [Export] public float moveSpeed = 5.0f;
    [Export] public float rotationSpeed = 10.0f;
    [Export] public float gravity = 20.0f;

    public Vector3 velocity = Vector3.Zero;
    public Vector3 inputDirection = Vector3.Zero;
    public byte lastProcessedInput = 0; // The last input byte this controller processed

    public byte OwnerClientId = 0;

    public void ApplyInput(byte packedInput)
    {
        // Store the input byte for logging/debugging/reconciliation if needed
        lastProcessedInput = packedInput;

        float moveX = 0;
        float moveZ = 0;

        // Use the shared InputUtils to unpack the flags
        if (InputUtils.GetInputState(packedInput, InputUtils.InputMask.Forward))
        {
            moveZ -= 1;
        }
        if (InputUtils.GetInputState(packedInput, InputUtils.InputMask.Backward))
        {
            moveZ += 1;
        }
        if (InputUtils.GetInputState(packedInput, InputUtils.InputMask.StrafeLeft))
        {
            moveX -= 1;
        }
        if (InputUtils.GetInputState(packedInput, InputUtils.InputMask.StrafeRight))
        {
            moveX += 1;
        }

        inputDirection = new Vector3(moveX, 0, moveZ).Normalized();

        // Handle Actions (e.g., shooting, interacting)
        if (InputUtils.GetInputState(packedInput, InputUtils.InputMask.PrimaryFire))
        {
            // Server Run authoritative shooting logic
            // Client Run predictive shooting animation/sound
            GD.Print($"Player {Name} fired primary weapon.");
        }
        // ... Handle Reload, Interact flags similarly
        if (InputUtils.GetInputState(packedInput, InputUtils.InputMask.Reload))
        {
            GD.Print($"Player {Name} reloaded primary weapon.");
        }

    }

    public void AuthoritativeMove(float _delta)
    {
        // Apply Gravity (simplified, assuming we are on a floor)
        if (!IsOnFloor())
        {
            velocity.Y -= gravity * _delta;
        }

        // Calculate planar velocity based on the last applied input
        Vector3 planarVelocity = inputDirection * moveSpeed;

        // Preserve gravity/vertical velocity
        velocity.X = planarVelocity.X;
        velocity.Z = planarVelocity.Z;

        // Apply the velocity to the CharacterBody3D
        Velocity = Velocity;
        MoveAndSlide();
    }

}
