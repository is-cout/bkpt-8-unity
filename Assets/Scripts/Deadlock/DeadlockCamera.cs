using UnityEngine;

namespace Deadlock
{
	/// <summary>
	/// Third person over-the-shoulder camera. It owns the yaw/pitch of the aim, so the
	/// character simply faces the camera's yaw the way Deadlock heroes do.
	/// </summary>
	[DefaultExecutionOrder(100)]
	[RequireComponent(typeof(Camera))]
	public class DeadlockCamera : MonoBehaviour
	{
		[Header("Target")]
		public Transform target;
		[Tooltip("Height of the pivot above the character's feet.")]
		public float pivotHeight = 1.45f;
		[Tooltip("Sideways/vertical offset of the shoulder view.")]
		public Vector2 shoulderOffset = new Vector2(0.65f, 0.05f);
		public float distance = 4.0f;

		[Header("Look")]
		public float minPitch = -60f;
		public float maxPitch = 75f;
		public float pivotSmoothing = 12f;

		[Header("Collision")]
		public LayerMask collisionMask = ~0;
		public float collisionRadius = 0.25f;
		public float collisionPadding = 0.15f;

		[Header("Feel")]
		public float baseFov = 65f;
		public float maxExtraFov = 14f;
		[Tooltip("Speed at which the extra FOV is fully applied.")]
		public float fovSpeedRange = 16f;
		public float fovSmoothing = 6f;
		[Tooltip("Camera roll applied when strafing, in degrees.")]
		public float strafeRoll = 1.5f;
		public float rollSmoothing = 8f;

		public float Yaw { get; private set; }
		public float Pitch { get; private set; }
		public Quaternion YawRotation => Quaternion.Euler(0f, Yaw, 0f);
		public Vector3 Forward => YawRotation * Vector3.forward;
		public Vector3 Right => YawRotation * Vector3.right;

		DeadlockInput input;
		DeadlockCharacter character;
		Camera cam;
		float smoothPivotY;
		float currentRoll;
		bool initialized;

		void Awake()
		{
			cam = GetComponent<Camera>();
			if (target == null)
			{
				character = FindAnyObjectByType<DeadlockCharacter>();
				if (character != null) target = character.transform;
			}
			else
			{
				character = target.GetComponent<DeadlockCharacter>();
			}

			if (target != null) input = target.GetComponent<DeadlockInput>();
			if (input == null) input = FindAnyObjectByType<DeadlockInput>();

			Yaw = transform.eulerAngles.y;
			Pitch = 15f;
			if (cam != null) cam.fieldOfView = baseFov;
		}

		void LateUpdate()
		{
			if (target == null) return;

			if (input != null)
			{
				Yaw += input.LookDelta.x;
				Pitch = Mathf.Clamp(Pitch - input.LookDelta.y, minPitch, maxPitch);
			}

			float pivotY = target.position.y + pivotHeight;
			if (!initialized)
			{
				smoothPivotY = pivotY;
				initialized = true;
			}
			else
			{
				// Smoothing only the vertical axis keeps steps and crouches from popping
				// while horizontal motion stays perfectly responsive.
				smoothPivotY = Mathf.Lerp(smoothPivotY, pivotY, 1f - Mathf.Exp(-pivotSmoothing * Time.deltaTime));
			}

			Vector3 pivot = new Vector3(target.position.x, smoothPivotY, target.position.z);
			Quaternion rotation = Quaternion.Euler(Pitch, Yaw, 0f);
			Vector3 shoulder = rotation * new Vector3(shoulderOffset.x, shoulderOffset.y, 0f);
			Vector3 origin = pivot + shoulder;
			Vector3 desired = origin - rotation * Vector3.forward * distance;

			// Pull the camera in when something is between it and the character.
			Vector3 toCamera = desired - origin;
			float wanted = toCamera.magnitude;
			if (wanted > 0.001f && Physics.SphereCast(origin, collisionRadius, toCamera / wanted,
				out var hit, wanted, collisionMask, QueryTriggerInteraction.Ignore))
			{
				desired = origin + toCamera / wanted * Mathf.Max(0f, hit.distance - collisionPadding);
			}

			float targetRoll = 0f;
			float targetFov = baseFov;
			if (character != null)
			{
				targetRoll = -character.StrafeSign * strafeRoll;
				float speed = character.HorizontalSpeed;
				targetFov = baseFov + maxExtraFov * Mathf.Clamp01(speed / Mathf.Max(1f, fovSpeedRange));
			}

			currentRoll = Mathf.Lerp(currentRoll, targetRoll, 1f - Mathf.Exp(-rollSmoothing * Time.deltaTime));
			if (cam != null)
			{
				cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, 1f - Mathf.Exp(-fovSmoothing * Time.deltaTime));
			}

			transform.SetPositionAndRotation(desired, Quaternion.Euler(Pitch, Yaw, currentRoll));
		}
	}
}
