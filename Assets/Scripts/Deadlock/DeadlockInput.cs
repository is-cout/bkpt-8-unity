using UnityEngine;
using UnityEngine.InputSystem;

namespace Deadlock
{
	/// <summary>
	/// Raw input for the character. Polls the Input System devices directly so this
	/// controller has no dependency on any action asset.
	/// Buffered presses (jump, dash) are consumed by the character.
	/// </summary>
	[DefaultExecutionOrder(-100)]
	public class DeadlockInput : MonoBehaviour
	{
		[Header("Look")]
		public float mouseSensitivity = 0.12f;
		public bool lockCursor = true;

		[Header("Buffering")]
		public float bufferTime = 0.15f;

		public Vector2 Move { get; private set; }
		public Vector2 LookDelta { get; private set; }
		public bool JumpHeld { get; private set; }
		public bool CrouchHeld { get; private set; }
		public bool InteractPressed { get; private set; }

		float jumpBufferedAt = -99f;
		float dashBufferedAt = -99f;

		public bool JumpBuffered => Time.time - jumpBufferedAt <= bufferTime;
		public bool DashBuffered => Time.time - dashBufferedAt <= bufferTime;

		public void ConsumeJump() => jumpBufferedAt = -99f;
		public void ConsumeDash() => dashBufferedAt = -99f;

		void OnEnable()
		{
			if (lockCursor) SetCursorLocked(true);
		}

		void OnDisable()
		{
			SetCursorLocked(false);
		}

		public static void SetCursorLocked(bool locked)
		{
			Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
			Cursor.visible = !locked;
		}

		void Update()
		{
			var kb = Keyboard.current;
			var mouse = Mouse.current;

			Vector2 move = Vector2.zero;
			bool jump = false, dash = false;
			InteractPressed = false;

			if (kb != null)
			{
				if (kb.wKey.isPressed || kb.upArrowKey.isPressed) move.y += 1f;
				if (kb.sKey.isPressed || kb.downArrowKey.isPressed) move.y -= 1f;
				if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) move.x += 1f;
				if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) move.x -= 1f;

				JumpHeld = kb.spaceKey.isPressed;
				jump = kb.spaceKey.wasPressedThisFrame;
				CrouchHeld = kb.ctrlKey.isPressed || kb.cKey.isPressed;
				dash = kb.leftShiftKey.wasPressedThisFrame;
				InteractPressed = kb.eKey.wasPressedThisFrame;

				if (kb.escapeKey.wasPressedThisFrame) SetCursorLocked(false);
			}

			if (mouse != null)
			{
				LookDelta = mouse.delta.ReadValue() * mouseSensitivity;
				if (mouse.rightButton.wasPressedThisFrame) dash = true; // alternative dash bind
				if (mouse.leftButton.wasPressedThisFrame && lockCursor) SetCursorLocked(true);
			}
			else
			{
				LookDelta = Vector2.zero;
			}

			if (Cursor.lockState != CursorLockMode.Locked) LookDelta = Vector2.zero;

			Move = move.sqrMagnitude > 1f ? move.normalized : move;
			if (jump) jumpBufferedAt = Time.time;
			if (dash) dashBufferedAt = Time.time;
		}
	}
}
