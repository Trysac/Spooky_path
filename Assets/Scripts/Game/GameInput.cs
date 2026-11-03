using UnityEngine;
using UnityEngine.InputSystem;

namespace SpookyPath.Game
{
    /// <summary>What the explorer was told to do this frame, from a device or from a script.</summary>
    public struct Commands
    {
        /// <summary>Movement on screen axes: x right, y up (away from the camera).</summary>
        public Vector2 Move;
        public bool Sprint;
        public bool Jump;
        public bool Strike;
        /// <summary>The spell slot pressed this frame (0-3), or -1.</summary>
        public int Spell;
        public bool Interact;

        public static Commands None => new Commands { Spell = -1 };
    }

    /// <summary>
    /// Keyboard + mouse and gamepad through the Input System. Keyboard: WASD or arrows move,
    /// Shift sprints, Space jumps, left click or J strikes, 1-4 cast, E interacts, Esc pauses.
    /// Gamepad: left stick moves, B (east) sprints, A (south) jumps, X (west) strikes,
    /// shoulders and triggers cast, Y (north) interacts, Start pauses.
    /// </summary>
    public sealed class GameInput
    {
        const float DeadZone = 0.25f;
        int stickStep;

        static Keyboard Keys => Keyboard.current;
        static Mouse Pointer => Mouse.current;
        static Gamepad Pad => Gamepad.current;

        /// <summary>True when the gamepad was the last device used (the HUD shows pad prompts then).</summary>
        public bool UsingGamepad { get; private set; }

        public Commands Read()
        {
            Commands commands = Commands.None;
            Vector2 move = Vector2.zero;
            if (Keys != null)
            {
                move.x += (Keys.dKey.isPressed || Keys.rightArrowKey.isPressed ? 1f : 0f) - (Keys.aKey.isPressed || Keys.leftArrowKey.isPressed ? 1f : 0f);
                move.y += (Keys.wKey.isPressed || Keys.upArrowKey.isPressed ? 1f : 0f) - (Keys.sKey.isPressed || Keys.downArrowKey.isPressed ? 1f : 0f);
                commands.Sprint |= Keys.leftShiftKey.isPressed || Keys.rightShiftKey.isPressed;
                commands.Jump |= Keys.spaceKey.wasPressedThisFrame;
                commands.Strike |= Keys.jKey.wasPressedThisFrame;
                commands.Interact |= Keys.eKey.wasPressedThisFrame;
                if (Keys.digit1Key.wasPressedThisFrame || Keys.numpad1Key.wasPressedThisFrame) commands.Spell = 0;
                if (Keys.digit2Key.wasPressedThisFrame || Keys.numpad2Key.wasPressedThisFrame) commands.Spell = 1;
                if (Keys.digit3Key.wasPressedThisFrame || Keys.numpad3Key.wasPressedThisFrame) commands.Spell = 2;
                if (Keys.digit4Key.wasPressedThisFrame || Keys.numpad4Key.wasPressedThisFrame) commands.Spell = 3;
                if (Keys.anyKey.wasPressedThisFrame)
                {
                    UsingGamepad = false;
                }
            }
            if (Pointer != null)
            {
                commands.Strike |= Pointer.leftButton.wasPressedThisFrame;
                if (Pointer.rightButton.wasPressedThisFrame)
                {
                    commands.Spell = 0;
                }
            }
            if (Pad != null)
            {
                Vector2 stick = Pad.leftStick.ReadValue();
                if (stick.magnitude > DeadZone)
                {
                    move += stick;
                    UsingGamepad = true;
                }
                move += Pad.dpad.ReadValue();
                commands.Sprint |= Pad.buttonEast.isPressed || Pad.leftStickButton.isPressed;
                commands.Jump |= Pad.buttonSouth.wasPressedThisFrame;
                commands.Strike |= Pad.buttonWest.wasPressedThisFrame;
                commands.Interact |= Pad.buttonNorth.wasPressedThisFrame;
                if (Pad.leftShoulder.wasPressedThisFrame) commands.Spell = 0;
                if (Pad.rightShoulder.wasPressedThisFrame) commands.Spell = 1;
                if (Pad.leftTrigger.wasPressedThisFrame) commands.Spell = 2;
                if (Pad.rightTrigger.wasPressedThisFrame) commands.Spell = 3;
                if (Pad.buttonSouth.wasPressedThisFrame || Pad.buttonWest.wasPressedThisFrame || Pad.startButton.wasPressedThisFrame)
                {
                    UsingGamepad = true;
                }
            }
            commands.Move = Vector2.ClampMagnitude(move, 1f);
            return commands;
        }

        public bool Pause =>
            (Keys != null && (Keys.escapeKey.wasPressedThisFrame || Keys.pKey.wasPressedThisFrame)) ||
            (Pad != null && Pad.startButton.wasPressedThisFrame);

        /// <summary>Activates the highlighted menu entry.</summary>
        public bool Confirm =>
            (Keys != null && (Keys.enterKey.wasPressedThisFrame || Keys.numpadEnterKey.wasPressedThisFrame || Keys.spaceKey.wasPressedThisFrame)) ||
            (Pad != null && Pad.buttonSouth.wasPressedThisFrame);

        /// <summary>Leaves a sub-menu.</summary>
        public bool Back =>
            (Keys != null && (Keys.escapeKey.wasPressedThisFrame || Keys.backspaceKey.wasPressedThisFrame)) ||
            (Pad != null && Pad.buttonEast.wasPressedThisFrame);

        /// <summary>-1 (up), +1 (down) or 0, from arrows, W/S, the d-pad or the stick, once per press.</summary>
        public int NavigateVertical()
        {
            int step = 0;
            if (Keys != null)
            {
                if (Keys.upArrowKey.wasPressedThisFrame || Keys.wKey.wasPressedThisFrame) step = -1;
                if (Keys.downArrowKey.wasPressedThisFrame || Keys.sKey.wasPressedThisFrame) step = 1;
            }
            if (Pad != null)
            {
                if (Pad.dpad.up.wasPressedThisFrame) step = -1;
                if (Pad.dpad.down.wasPressedThisFrame) step = 1;
                float y = Pad.leftStick.ReadValue().y;
                int sign = y > 0.6f ? -1 : y < -0.6f ? 1 : 0;
                if (sign != 0 && sign != stickStep)
                {
                    step = sign;
                }
                stickStep = sign;
            }
            return step;
        }

        /// <summary>-1, +1 or 0 from left/right, A/D or the d-pad, for sliders.</summary>
        public int NavigateHorizontal()
        {
            int step = 0;
            if (Keys != null)
            {
                if (Keys.leftArrowKey.wasPressedThisFrame || Keys.aKey.wasPressedThisFrame) step = -1;
                if (Keys.rightArrowKey.wasPressedThisFrame || Keys.dKey.wasPressedThisFrame) step = 1;
            }
            if (Pad != null)
            {
                if (Pad.dpad.left.wasPressedThisFrame) step = -1;
                if (Pad.dpad.right.wasPressedThisFrame) step = 1;
            }
            return step;
        }
    }
}
