using UnityEngine;

namespace Deadlock
{
	/// <summary>
	/// Kinematic capsule solver. Owns nothing but collision: sweeps a virtual capsule
	/// through the world, slides along surfaces (Source-style ClipVelocity), steps up
	/// small obstacles and keeps the capsule glued to walkable ground.
	/// The capsule origin is at the character's feet.
	/// </summary>
	public class DeadlockMotor : MonoBehaviour
	{
		[Header("Capsule")]
		public float radius = 0.4f;
		public float standingHeight = 1.8f;
		public float skinWidth = 0.02f;

		[Header("Collision")]
		public LayerMask collisionMask = ~0;
		public float maxSlopeAngle = 50f;
		public float stepHeight = 0.4f;
		public float groundSnapDistance = 0.35f;

		const int MaxSlideIterations = 5;
		const float MinMoveDistance = 0.0002f;

		readonly Collider[] overlaps = new Collider[8];

		public float Height { get; private set; }
		public bool Grounded { get; private set; }
		public bool WasGroundedLastMove { get; private set; }
		public Vector3 GroundNormal { get; private set; } = Vector3.up;
		public Collider GroundCollider { get; private set; }
		/// <summary>Normal of the last steep surface we bumped into this move, or zero.</summary>
		public Vector3 LastWallNormal { get; private set; }
		public Collider LastWallCollider { get; private set; }

		CapsuleCollider probeCollider;
		Transform probeTransform;

		void Awake()
		{
			Height = standingHeight;
			GroundNormal = Vector3.up;
		}

		public void SetHeight(float height)
		{
			Height = Mathf.Max(height, radius * 2f + 0.01f);
		}

		public bool IsWalkable(Vector3 normal) => Vector3.Angle(normal, Vector3.up) <= maxSlopeAngle;

		#region Capsule helpers

		void CapsulePoints(Vector3 feet, float height, out Vector3 bottom, out Vector3 top)
		{
			bottom = feet + Vector3.up * radius;
			top = feet + Vector3.up * (height - radius);
		}

		bool CapsuleCast(Vector3 feet, Vector3 dir, float distance, out RaycastHit hit)
		{
			CapsulePoints(feet, Height, out var bottom, out var top);
			return Physics.CapsuleCast(bottom, top, radius, dir, out hit, distance,
				collisionMask, QueryTriggerInteraction.Ignore);
		}

		/// <summary>True when a capsule of the given height would fit at this position.</summary>
		public bool FitsAt(Vector3 feet, float height)
		{
			var bottom = feet + Vector3.up * (radius + skinWidth);
			var top = feet + Vector3.up * (height - radius - skinWidth);
			return !Physics.CheckCapsule(bottom, top, radius, collisionMask, QueryTriggerInteraction.Ignore);
		}

		public bool CastFromCapsule(Vector3 feet, Vector3 dir, float distance, out RaycastHit hit)
		{
			return CapsuleCast(feet, dir, distance, out hit);
		}

		#endregion

		/// <summary>Source's ClipVelocity: removes the component of v that enters the plane.</summary>
		public static Vector3 ClipVelocity(Vector3 velocity, Vector3 normal, float overbounce = 1f)
		{
			float backoff = Vector3.Dot(velocity, normal) * overbounce;
			return velocity - normal * backoff;
		}

		#region Move

		/// <summary>
		/// Sweeps the capsule by velocity * dt, resolving collisions.
		/// Returns the velocity left after sliding.
		/// </summary>
		public Vector3 Move(Vector3 velocity, float dt, bool snapToGround)
		{
			WasGroundedLastMove = Grounded;
			LastWallNormal = Vector3.zero;
			LastWallCollider = null;

			Vector3 position = Depenetrate(transform.position);

			// Walking a slope should not bleed speed: reorient the velocity along the
			// ground plane while keeping its magnitude.
			if (Grounded && snapToGround)
			{
				float speed = velocity.magnitude;
				if (speed > 0.001f)
				{
					Vector3 along = ClipVelocity(velocity, GroundNormal);
					if (along.sqrMagnitude > 0.0001f) velocity = along.normalized * speed;
				}
			}

			Vector3 delta = velocity * dt;
			bool wasGrounded = Grounded;

			Vector3 slidPos = position;
			Vector3 slidVel = velocity;
			bool blocked = CollideAndSlide(ref slidPos, delta, ref slidVel);

			// If a wall stopped us while walking, try to step over it.
			if (blocked && wasGrounded && stepHeight > 0f &&
				TryStepUp(position, delta, velocity, slidPos, out var steppedPos, out var steppedVel))
			{
				slidPos = steppedPos;
				slidVel = steppedVel;
			}

			position = Depenetrate(slidPos);
			velocity = slidVel;
			ProbeGround(ref position, ref velocity, snapToGround, wasGrounded);

			transform.position = position;
			return velocity;
		}

		bool CollideAndSlide(ref Vector3 position, Vector3 delta, ref Vector3 velocity)
		{
			bool hitSomething = false;
			Vector3 firstNormal = Vector3.zero;
			int planeCount = 0;

			for (int i = 0; i < MaxSlideIterations; i++)
			{
				float distance = delta.magnitude;
				if (distance < MinMoveDistance) break;

				Vector3 dir = delta / distance;
				if (CapsuleCast(position, dir, distance + skinWidth, out var hit))
				{
					hitSomething = true;

					if (hit.distance > skinWidth)
					{
						float travel = hit.distance - skinWidth;
						position += dir * travel;
						delta -= dir * travel;
					}

					Vector3 normal = hit.normal;
					if (!IsWalkable(normal) && Mathf.Abs(Vector3.Dot(normal, Vector3.up)) < 0.5f)
					{
						LastWallNormal = normal;
						LastWallCollider = hit.collider;
					}

					if (planeCount == 0)
					{
						firstNormal = normal;
						planeCount = 1;
						delta = ClipVelocity(delta, normal);
						velocity = ClipVelocity(velocity, normal);
					}
					else
					{
						// Second plane: clip against it, and if that pushes us back into the
						// first one, slide along the crease instead of jittering between them.
						delta = ClipVelocity(delta, normal);
						velocity = ClipVelocity(velocity, normal);

						if (Vector3.Dot(delta, firstNormal) < 0f || Vector3.Dot(velocity, firstNormal) < 0f)
						{
							Vector3 crease = Vector3.Cross(firstNormal, normal);
							if (crease.sqrMagnitude < 0.0001f)
							{
								delta = Vector3.zero;
								velocity = Vector3.zero;
								break;
							}
							crease.Normalize();
							delta = crease * Vector3.Dot(delta, crease);
							velocity = crease * Vector3.Dot(velocity, crease);
						}
						firstNormal = normal;
					}
				}
				else
				{
					position += delta;
					break;
				}
			}

			return hitSomething;
		}

		bool TryStepUp(Vector3 startPos, Vector3 delta, Vector3 startVel, Vector3 flatResult,
			out Vector3 position, out Vector3 velocity)
		{
			position = flatResult;
			velocity = startVel;

			Vector3 horizontal = new Vector3(delta.x, 0f, delta.z);
			if (horizontal.sqrMagnitude < MinMoveDistance) return false;
			Vector3 horizontalDir = horizontal.normalized;

			// Up
			Vector3 up = startPos;
			float upDistance = stepHeight;
			if (CapsuleCast(up, Vector3.up, stepHeight + skinWidth, out var upHit))
				upDistance = Mathf.Max(0f, upHit.distance - skinWidth);
			if (upDistance < 0.05f) return false;
			up += Vector3.up * upDistance;

			// Forward
			Vector3 forward = up;
			Vector3 forwardVel = startVel;
			CollideAndSlide(ref forward, horizontal, ref forwardVel);

			// Did we actually get further than the plain slide did?
			Vector3 gained = new Vector3(forward.x - flatResult.x, 0f, forward.z - flatResult.z);
			if (Vector3.Dot(gained, horizontalDir) < 0.02f) return false;

			// Down
			if (!CapsuleCast(forward, Vector3.down, upDistance + skinWidth * 2f, out var downHit)) return false;
			if (!IsWalkable(downHit.normal)) return false;
			forward += Vector3.down * Mathf.Max(0f, downHit.distance - skinWidth);

			position = forward;
			velocity = new Vector3(forwardVel.x, startVel.y, forwardVel.z);
			return true;
		}

		void ProbeGround(ref Vector3 position, ref Vector3 velocity, bool snapToGround, bool wasGrounded)
		{
			Grounded = false;
			GroundCollider = null;

			float probe = snapToGround
				? (wasGrounded ? groundSnapDistance : skinWidth * 4f)
				: skinWidth * 2f;

			if (CapsuleCast(position, Vector3.down, probe + skinWidth, out var hit) && IsWalkable(hit.normal))
			{
				Grounded = true;
				GroundNormal = hit.normal;
				GroundCollider = hit.collider;
				if (snapToGround)
				{
					position += Vector3.down * Mathf.Max(0f, hit.distance - skinWidth);
					if (velocity.y < 0f) velocity = ClipVelocity(velocity, hit.normal);
				}
			}
			else
			{
				GroundNormal = Vector3.up;
			}
		}

		Vector3 Depenetrate(Vector3 position)
		{
			var self = GetProbeCollider();
			self.radius = radius;
			self.height = Mathf.Max(Height, radius * 2f);
			self.center = new Vector3(0f, self.height * 0.5f, 0f);

			for (int pass = 0; pass < 3; pass++)
			{
				CapsulePoints(position, Height, out var bottom, out var top);
				int count = Physics.OverlapCapsuleNonAlloc(bottom, top, radius, overlaps,
					collisionMask, QueryTriggerInteraction.Ignore);
				if (count == 0) break;

				bool resolved = false;
				for (int i = 0; i < count; i++)
				{
					var other = overlaps[i];
					if (other == null || other == self || other.transform.IsChildOf(transform)) continue;

					probeTransform.SetPositionAndRotation(position, Quaternion.identity);
					if (Physics.ComputePenetration(self, position, Quaternion.identity,
						other, other.transform.position, other.transform.rotation,
						out var dir, out var dist))
					{
						position += dir * (dist + skinWidth * 0.5f);
						resolved = true;
					}
				}
				if (!resolved) break;
			}
			return position;
		}

		CapsuleCollider GetProbeCollider()
		{
			if (probeCollider == null)
			{
				var go = new GameObject("~MotorProbe") { hideFlags = HideFlags.HideAndDontSave };
				go.layer = 2; // Ignore Raycast
				probeCollider = go.AddComponent<CapsuleCollider>();
				probeCollider.enabled = false;
				probeTransform = go.transform;
			}
			return probeCollider;
		}

		void OnDestroy()
		{
			if (probeCollider != null) Destroy(probeCollider.gameObject);
		}

		#endregion

		void OnDrawGizmosSelected()
		{
			float h = Application.isPlaying ? Height : standingHeight;
			Gizmos.color = Grounded ? Color.green : Color.cyan;
			Vector3 p = transform.position;
			Gizmos.DrawWireSphere(p + Vector3.up * radius, radius);
			Gizmos.DrawWireSphere(p + Vector3.up * (h - radius), radius);
		}
	}
}
