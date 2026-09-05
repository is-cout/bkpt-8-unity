using UnityEngine;

namespace Deadlock
{
	public enum MoveState
	{
		Ground,
		Air,
		Slide,
		Mantle,
		Zipline
	}

	/// <summary>
	/// Deadlock-flavoured movement: Source-style acceleration and friction with a
	/// stamina economy on top (dash, dash-jump, air jump, wall jump), plus sliding,
	/// auto-mantling and ziplines.
	/// </summary>
	[DefaultExecutionOrder(0)]
	[RequireComponent(typeof(DeadlockMotor))]
	[RequireComponent(typeof(DeadlockInput))]
	public class DeadlockCharacter : MonoBehaviour
	{
		[Header("References")]
		public DeadlockCamera view;

		[Header("Ground movement")]
		public float walkSpeed = 7f;
		[Tooltip("Extra speed once auto-sprint kicks in, like Deadlock's out-of-combat sprint.")]
		public float sprintBonus = 2f;
		public float sprintChargeTime = 2.5f;
		public float crouchSpeed = 3.6f;
		[Tooltip("Source-style acceleration multiplier (accel * wishSpeed * dt).")]
		public float groundAccel = 14f;
		public float groundFriction = 8f;
		public float stopSpeed = 1.5f;

		[Header("Air movement")]
		public float airAccel = 22f;
		[Tooltip("Air acceleration is capped to this projected speed (enables air-strafing).")]
		public float airSpeedCap = 1.6f;
		[Tooltip("Extra, forgiving steering on top of the Source air model.")]
		public float airDrift = 5f;
		public float gravity = 30f;
		public float fallGravityMultiplier = 1.25f;
		public float terminalVelocity = 60f;

		[Header("Jump")]
		public float jumpHeight = 1.6f;
		public float coyoteTime = 0.12f;
		public int maxAirJumps = 1;
		public int airJumpCost = 1;
		[Tooltip("How much an air jump can redirect existing horizontal momentum.")]
		[Range(0f, 1f)] public float airJumpRedirect = 0.6f;

		[Header("Dash")]
		public float dashSpeed = 16f;
		public float dashDuration = 0.22f;
		public float dashCooldown = 0.35f;
		public int dashCost = 1;
		public bool allowAirDash = true;
		[Tooltip("Jumping inside this window after a dash turns it into a long dash-jump.")]
		public float dashJumpWindow = 0.45f;
		public float dashJumpSpeed = 15f;
		public float dashJumpHeight = 2.0f;

		[Header("Crouch & slide")]
		public float crouchHeight = 1.15f;
		public float heightLerpSpeed = 12f;
		public float slideMinSpeed = 5.5f;
		public float slideBoost = 2.5f;
		public float slideFriction = 1.1f;
		public float slideSlopeAccel = 28f;
		public float slideMaxSpeed = 22f;
		public float slideEndSpeed = 3.2f;
		public float slideSteer = 3f;

		[Header("Wall jump")]
		public bool enableWallJump = true;
		public float wallCheckDistance = 0.35f;
		public float wallJumpHeight = 1.7f;
		public float wallJumpPush = 8f;
		public int wallJumpCost = 1;

		[Header("Mantle")]
		public bool enableMantle = true;
		public bool autoMantle = true;
		public float mantleMinHeight = 0.35f;
		public float mantleMaxHeight = 2.0f;
		public float mantleReach = 0.7f;
		public float mantleDuration = 0.32f;

		[Header("Zipline")]
		public float ziplineAttachRange = 3f;
		public float ziplineSpeed = 20f;
		public float ziplineAccel = 25f;
		public float ziplineHangDistance = 0.35f;
		public float ziplineDetachBoost = 4f;

		[Header("Visual")]
		public bool createDebugVisual = true;

		// --- runtime state -------------------------------------------------------
		public MoveState State { get; private set; } = MoveState.Air;
		public Vector3 Velocity => velocity;
		public float HorizontalSpeed => new Vector2(velocity.x, velocity.z).magnitude;
		public bool Grounded => motor.Grounded;
		public bool IsSprinting => sprinting;
		public bool IsCrouching => crouching;
		public bool IsDashing => dashTimer > 0f;
		public int AirJumpsLeft => maxAirJumps - airJumpsUsed;
		public float StrafeSign { get; private set; }
		public DeadlockStamina Stamina => stamina;

		DeadlockMotor motor;
		DeadlockInput input;
		DeadlockStamina stamina;

		Vector3 velocity;
		Vector3 wishDir;
		float wishSpeed;

		bool sprinting;
		float sprintCharge;
		bool crouching;
		float targetHeight;

		float lastGroundedTime = -99f;
		float lastJumpTime = -99f;
		int airJumpsUsed;

		float dashTimer;
		float dashCooldownTimer;
		float lastDashTime = -99f;
		Vector3 dashDir;

		Collider lastWallJumpCollider;

		Vector3 mantleStart, mantleTarget;
		float mantleTimer;

		DeadlockZipline zipline;
		float ziplineT;
		float ziplineSpeedCurrent;
		int ziplineDirection = 1;

		float JumpSpeed => Mathf.Sqrt(2f * gravity * Mathf.Max(0.01f, jumpHeight));
		float DashJumpSpeed => Mathf.Sqrt(2f * gravity * Mathf.Max(0.01f, dashJumpHeight));
		float WallJumpSpeed => Mathf.Sqrt(2f * gravity * Mathf.Max(0.01f, wallJumpHeight));

		void Awake()
		{
			motor = GetComponent<DeadlockMotor>();
			input = GetComponent<DeadlockInput>();
			stamina = GetComponent<DeadlockStamina>();
			if (stamina == null) stamina = gameObject.AddComponent<DeadlockStamina>();
			if (view == null) view = FindAnyObjectByType<DeadlockCamera>();
			targetHeight = motor.standingHeight;
			visualBody = transform.Find("Visual");
			if (visualBody == null && createDebugVisual) BuildDebugVisual();
		}

		void Update()
		{
			float dt = Mathf.Min(Time.deltaTime, 1f / 30f);
			if (dt <= 0f) return;

			switch (State)
			{
				case MoveState.Mantle:
					TickMantle(dt);
					return;
				case MoveState.Zipline:
					TickZipline(dt);
					return;
			}

			TickTimers(dt);
			FaceView();
			ReadWish();
			TickHeight(dt);
			TickCrouchAndSlide(dt);
			TickJump(dt);
			TickDash(dt);
			ApplyAcceleration(dt);
			ApplyGravity(dt);
			MoveAndCollide(dt);
			PostMove(dt);
		}

		#region Frame steps

		void TickTimers(float dt)
		{
			if (motor.Grounded) lastGroundedTime = Time.time;
			if (dashTimer > 0f) dashTimer -= dt;
			if (dashCooldownTimer > 0f) dashCooldownTimer -= dt;
		}

		void FaceView()
		{
			if (view == null) return;
			transform.rotation = view.YawRotation;
		}

		void ReadWish()
		{
			Vector2 move = input.Move;
			StrafeSign = Mathf.Abs(move.x) > 0.1f ? Mathf.Sign(move.x) : 0f;

			Vector3 forward = view != null ? view.Forward : transform.forward;
			Vector3 right = view != null ? view.Right : transform.right;
			Vector3 dir = forward * move.y + right * move.x;
			dir.y = 0f;

			wishDir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.zero;
			wishSpeed = CurrentMaxSpeed() * Mathf.Clamp01(move.magnitude);

			// Auto-sprint: keep moving and the hero winds up to sprint speed.
			bool movingForward = move.y > 0.1f && HorizontalSpeed > walkSpeed * 0.5f;
			if (motor.Grounded && movingForward && !crouching && State != MoveState.Slide)
				sprintCharge = Mathf.Min(sprintCharge + Time.deltaTime, sprintChargeTime);
			else if (!movingForward)
				sprintCharge = Mathf.Max(0f, sprintCharge - Time.deltaTime * 2f);

			sprinting = sprintCharge >= sprintChargeTime;
		}

		float CurrentMaxSpeed()
		{
			if (State == MoveState.Slide) return slideMaxSpeed;
			if (crouching) return crouchSpeed;
			return walkSpeed + (sprinting ? sprintBonus : 0f);
		}

		void TickHeight(float dt)
		{
			float h = Mathf.Lerp(motor.Height, targetHeight, 1f - Mathf.Exp(-heightLerpSpeed * dt));
			if (Mathf.Abs(h - targetHeight) < 0.01f) h = targetHeight;
			motor.SetHeight(h);
		}

		void TickCrouchAndSlide(float dt)
		{
			bool wantsCrouch = input.CrouchHeld;

			if (State == MoveState.Slide)
			{
				bool stillFast = HorizontalSpeed > slideEndSpeed;
				if (!wantsCrouch || !stillFast || !motor.Grounded)
				{
					if (!wantsCrouch && CanStandUp()) EndCrouch();
					State = motor.Grounded ? MoveState.Ground : MoveState.Air;
				}
				else
				{
					return;
				}
			}

			if (wantsCrouch)
			{
				bool canSlide = motor.Grounded && !crouching && HorizontalSpeed >= slideMinSpeed;
				StartCrouch();
				if (canSlide) StartSlide();
			}
			else if (crouching && CanStandUp())
			{
				EndCrouch();
			}
		}

		void StartCrouch()
		{
			crouching = true;
			targetHeight = crouchHeight;
		}

		void EndCrouch()
		{
			crouching = false;
			targetHeight = motor.standingHeight;
		}

		bool CanStandUp() => motor.FitsAt(transform.position, motor.standingHeight);

		void StartSlide()
		{
			State = MoveState.Slide;
			Vector3 flat = new Vector3(velocity.x, 0f, velocity.z);
			if (flat.sqrMagnitude > 0.001f)
				velocity += flat.normalized * slideBoost;
			sprintCharge = 0f;
		}

		/// <summary>Replaces the walking acceleration while sliding.</summary>
		void SlidePhysics(float dt)
		{
			// Downhill gravity component keeps ramps fast, friction is much lower than walking.
			Vector3 slopeDir = Vector3.ProjectOnPlane(Vector3.down, motor.GroundNormal);
			if (slopeDir.sqrMagnitude > 0.0001f)
				velocity += slopeDir.normalized * (slideSlopeAccel * slopeDir.magnitude * dt);

			ApplyFriction(slideFriction, dt);

			// A little steering, but nothing that lets you accelerate by wiggling.
			if (wishDir.sqrMagnitude > 0.001f)
			{
				Vector3 flat = new Vector3(velocity.x, 0f, velocity.z);
				float speed = flat.magnitude;
				if (speed > 0.01f)
				{
					Vector3 steered = Vector3.RotateTowards(flat.normalized, wishDir,
						slideSteer * Mathf.Deg2Rad * 10f * dt, 0f);
					flat = steered * speed;
					velocity = new Vector3(flat.x, velocity.y, flat.z);
				}
			}

			ClampHorizontalSpeed(slideMaxSpeed);
		}

		void TickJump(float dt)
		{
			if (!input.JumpBuffered) return;

			bool grounded = motor.Grounded || Time.time - lastGroundedTime <= coyoteTime;

			// Mantling beats every other jump: aiming at a ledge and pressing jump climbs it.
			if (enableMantle && TryMantle())
			{
				input.ConsumeJump();
				return;
			}

			if (grounded && Time.time - lastJumpTime > 0.1f)
			{
				input.ConsumeJump();
				if (Time.time - lastDashTime <= dashJumpWindow) DashJump();
				else NormalJump();
				return;
			}

			if (enableWallJump && TryWallJump())
			{
				input.ConsumeJump();
				return;
			}

			if (airJumpsUsed < maxAirJumps && stamina.Has(airJumpCost))
			{
				input.ConsumeJump();
				AirJump();
			}
		}

		void NormalJump()
		{
			if (State == MoveState.Slide) State = MoveState.Air;
			velocity.y = JumpSpeed;
			LeaveGround();
		}

		void DashJump()
		{
			Vector3 dir = dashDir.sqrMagnitude > 0.001f ? dashDir : transform.forward;
			Vector3 flat = new Vector3(velocity.x, 0f, velocity.z);
			float speed = Mathf.Max(flat.magnitude, dashJumpSpeed);
			velocity = dir * speed;
			velocity.y = DashJumpSpeed;
			dashTimer = 0f;
			lastDashTime = -99f;
			LeaveGround();
		}

		void AirJump()
		{
			if (!stamina.TrySpend(airJumpCost)) return;
			airJumpsUsed++;

			Vector3 flat = new Vector3(velocity.x, 0f, velocity.z);
			if (wishDir.sqrMagnitude > 0.001f)
			{
				float speed = Mathf.Max(flat.magnitude, walkSpeed);
				Vector3 redirected = Vector3.Lerp(flat.normalized == Vector3.zero ? wishDir : flat.normalized,
					wishDir, airJumpRedirect).normalized;
				flat = redirected * speed;
			}
			velocity = new Vector3(flat.x, JumpSpeed, flat.z);
			lastJumpTime = Time.time;
		}

		bool TryWallJump()
		{
			if (motor.Grounded) return false;
			if (!stamina.Has(wallJumpCost)) return false;
			if (!FindWall(out var normal, out var wall)) return false;
			if (wall == lastWallJumpCollider) return false;
			if (!stamina.TrySpend(wallJumpCost)) return false;

			lastWallJumpCollider = wall;
			airJumpsUsed = 0; // walls refresh the air jump, chaining stays stamina-limited

			Vector3 flat = new Vector3(velocity.x, 0f, velocity.z);
			flat = DeadlockMotor.ClipVelocity(flat, normal); // kill the speed going into the wall
			Vector3 push = normal * wallJumpPush;
			Vector3 steer = wishDir.sqrMagnitude > 0.001f ? wishDir * walkSpeed * 0.5f : Vector3.zero;

			velocity = flat + push + steer;
			velocity.y = WallJumpSpeed;
			lastJumpTime = Time.time;
			return true;
		}

		bool FindWall(out Vector3 normal, out Collider wall)
		{
			if (motor.LastWallNormal != Vector3.zero)
			{
				normal = motor.LastWallNormal;
				wall = motor.LastWallCollider;
				return true;
			}

			normal = Vector3.zero;
			wall = null;
			float best = float.MaxValue;

			Vector3[] dirs =
			{
				wishDir.sqrMagnitude > 0.001f ? wishDir : transform.forward,
				transform.forward, -transform.forward, transform.right, -transform.right
			};

			foreach (var dir in dirs)
			{
				if (dir.sqrMagnitude < 0.001f) continue;
				if (motor.CastFromCapsule(transform.position, dir, wallCheckDistance, out var hit) &&
					!motor.IsWalkable(hit.normal) && Mathf.Abs(Vector3.Dot(hit.normal, Vector3.up)) < 0.4f &&
					hit.distance < best)
				{
					best = hit.distance;
					normal = hit.normal;
					wall = hit.collider;
				}
			}

			return wall != null;
		}

		void TickDash(float dt)
		{
			if (!input.DashBuffered) return;
			if (dashCooldownTimer > 0f) return;
			if (!motor.Grounded && !allowAirDash) return;
			if (!stamina.Has(dashCost)) return;

			input.ConsumeDash();
			if (!stamina.TrySpend(dashCost)) return;

			Vector3 dir = wishDir.sqrMagnitude > 0.001f ? wishDir : transform.forward;
			dashDir = dir;
			dashTimer = dashDuration;
			dashCooldownTimer = dashCooldown;
			lastDashTime = Time.time;

			velocity = new Vector3(dir.x, 0f, dir.z) * dashSpeed + Vector3.up * Mathf.Max(0f, velocity.y);
			if (State == MoveState.Slide) State = motor.Grounded ? MoveState.Ground : MoveState.Air;
			if (crouching && CanStandUp()) EndCrouch();
		}

		void ApplyAcceleration(float dt)
		{
			// A dash is a committed burst: no friction, no steering while it lasts.
			if (dashTimer > 0f) return;

			if (State == MoveState.Slide)
			{
				SlidePhysics(dt);
			}
			else if (motor.Grounded)
			{
				ApplyFriction(groundFriction, dt);
				Vector3 dir = wishDir;
				if (dir.sqrMagnitude > 0.001f)
					dir = Vector3.ProjectOnPlane(dir, motor.GroundNormal).normalized;
				Accelerate(dir, wishSpeed, groundAccel, dt);
			}
			else
			{
				Accelerate(wishDir, Mathf.Min(wishSpeed, airSpeedCap), airAccel, dt);
				ApplyAirDrift(dt);
			}
		}

		void ApplyFriction(float friction, float dt)
		{
			Vector3 flat = new Vector3(velocity.x, 0f, velocity.z);
			float speed = flat.magnitude;
			if (speed < 0.01f)
			{
				velocity.x = 0f;
				velocity.z = 0f;
				return;
			}

			float control = Mathf.Max(speed, stopSpeed);
			float drop = control * friction * dt;
			float newSpeed = Mathf.Max(0f, speed - drop) / speed;
			velocity.x *= newSpeed;
			velocity.z *= newSpeed;
		}

		void Accelerate(Vector3 dir, float targetSpeed, float accel, float dt)
		{
			if (dir.sqrMagnitude < 0.001f || targetSpeed <= 0f) return;
			float current = Vector3.Dot(velocity, dir);
			float add = targetSpeed - current;
			if (add <= 0f) return;
			float accelSpeed = Mathf.Min(accel * targetSpeed * dt, add);
			velocity += dir * accelSpeed;
		}

		/// <summary>Forgiving extra air steering that never raises the horizontal speed.</summary>
		void ApplyAirDrift(float dt)
		{
			if (wishDir.sqrMagnitude < 0.001f || airDrift <= 0f) return;

			Vector3 flat = new Vector3(velocity.x, 0f, velocity.z);
			float speed = flat.magnitude;
			if (speed < 0.01f) return;

			Vector3 steered = Vector3.RotateTowards(flat.normalized, wishDir, airDrift * Mathf.Deg2Rad * dt * 10f, 0f);
			flat = steered * speed;
			velocity.x = flat.x;
			velocity.z = flat.z;
		}

		void ApplyGravity(float dt)
		{
			if (motor.Grounded && velocity.y <= 0.01f) return;
			float g = gravity * (velocity.y < 0f ? fallGravityMultiplier : 1f);
			// Holding jump does not float you; releasing early does not cut the arc either,
			// matching Deadlock's fixed jump arc.
			velocity.y = Mathf.Max(velocity.y - g * dt, -terminalVelocity);
		}

		void MoveAndCollide(float dt)
		{
			bool snap = Time.time - lastJumpTime > 0.08f && velocity.y <= 0.01f;
			velocity = motor.Move(velocity, dt, snap);
		}

		void PostMove(float dt)
		{
			if (motor.Grounded)
			{
				if (!motor.WasGroundedLastMove) OnLanded();
				if (State != MoveState.Slide) State = MoveState.Ground;
			}
			else
			{
				if (State != MoveState.Slide) State = MoveState.Air;
				if (enableMantle && autoMantle && velocity.y < 1f && wishDir.sqrMagnitude > 0.1f)
					TryMantle();
			}
		}

		void OnLanded()
		{
			airJumpsUsed = 0;
			lastWallJumpCollider = null;
			// Landing while holding crouch at speed rolls straight into a slide.
			if (input.CrouchHeld && HorizontalSpeed >= slideMinSpeed && State != MoveState.Slide)
			{
				StartCrouch();
				StartSlide();
			}
		}

		void LeaveGround()
		{
			lastJumpTime = Time.time;
			lastGroundedTime = -99f;
			State = MoveState.Air;
		}

		void ClampHorizontalSpeed(float max)
		{
			Vector3 flat = new Vector3(velocity.x, 0f, velocity.z);
			if (flat.magnitude > max)
			{
				flat = flat.normalized * max;
				velocity.x = flat.x;
				velocity.z = flat.z;
			}
		}

		#endregion

		#region Mantle

		bool TryMantle()
		{
			if (State == MoveState.Mantle) return false;

			if (motor.Grounded && wishDir.sqrMagnitude < 0.001f) return false;

			Vector3 dir = wishDir.sqrMagnitude > 0.001f ? wishDir : transform.forward;
			dir.y = 0f;
			if (dir.sqrMagnitude < 0.001f) return false;
			dir.Normalize();

			Vector3 feet = transform.position;
			float chest = Mathf.Min(mantleMaxHeight, motor.Height) * 0.5f;

			// There must be something wall-like in front of us.
			if (!Physics.Raycast(feet + Vector3.up * chest, dir, out var wallHit,
				motor.radius + mantleReach, motor.collisionMask, QueryTriggerInteraction.Ignore))
				return false;
			if (motor.IsWalkable(wallHit.normal)) return false;

			// And a walkable top surface within reach, with room for the capsule.
			Vector3 probe = feet + dir * (motor.radius + 0.25f) + Vector3.up * (mantleMaxHeight + 0.2f);
			if (!Physics.Raycast(probe, Vector3.down, out var topHit, mantleMaxHeight + 0.2f,
				motor.collisionMask, QueryTriggerInteraction.Ignore))
				return false;
			if (!motor.IsWalkable(topHit.normal)) return false;

			float rise = topHit.point.y - feet.y;
			if (rise < mantleMinHeight || rise > mantleMaxHeight) return false;

			Vector3 target = topHit.point + dir * 0.05f;
			if (!motor.FitsAt(target, motor.standingHeight)) return false;

			mantleStart = feet;
			mantleTarget = target;
			mantleTimer = 0f;
			State = MoveState.Mantle;
			velocity = Vector3.zero;
			if (crouching) EndCrouch();
			return true;
		}

		void TickMantle(float dt)
		{
			mantleTimer += dt;
			float t = Mathf.Clamp01(mantleTimer / Mathf.Max(0.01f, mantleDuration));

			// Up first, then forward: reads like a hand-over-ledge climb.
			float up = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.6f));
			float fwd = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.35f) / 0.65f));

			Vector3 pos = mantleStart;
			pos.y = Mathf.Lerp(mantleStart.y, mantleTarget.y, up);
			pos.x = Mathf.Lerp(mantleStart.x, mantleTarget.x, fwd);
			pos.z = Mathf.Lerp(mantleStart.z, mantleTarget.z, fwd);
			transform.position = pos;

			FaceView();

			if (t >= 1f)
			{
				State = MoveState.Ground;
				airJumpsUsed = 0;
				lastWallJumpCollider = null;
				velocity = transform.forward * Mathf.Min(walkSpeed * 0.5f, walkSpeed);
			}
		}

		#endregion

		#region Zipline

		void TryAttachZipline()
		{
			var line = DeadlockZipline.FindNearest(transform.position + Vector3.up * motor.Height, ziplineAttachRange, out float t);
			if (line == null) return;

			zipline = line;
			ziplineT = t;
			ziplineSpeedCurrent = Mathf.Max(HorizontalSpeed * 0.5f, 4f);

			Vector3 lineDir = line.Direction;
			float facing = Vector3.Dot(transform.forward, lineDir);
			ziplineDirection = facing >= 0f ? 1 : -1;

			State = MoveState.Zipline;
			velocity = Vector3.zero;
			if (crouching) EndCrouch();
		}

		void TickZipline(float dt)
		{
			if (zipline == null)
			{
				State = MoveState.Air;
				return;
			}

			FaceView();

			// Steering along the cable: W/S pick the travel direction.
			float forwardInput = input.Move.y;
			if (Mathf.Abs(forwardInput) > 0.1f)
			{
				float wanted = Vector3.Dot(view != null ? view.Forward : transform.forward, zipline.Direction) * forwardInput;
				if (Mathf.Abs(wanted) > 0.2f) ziplineDirection = wanted > 0f ? 1 : -1;
			}

			ziplineSpeedCurrent = Mathf.MoveTowards(ziplineSpeedCurrent, ziplineSpeed, ziplineAccel * dt);
			ziplineT += ziplineDirection * ziplineSpeedCurrent * dt / Mathf.Max(0.01f, zipline.Length);

			bool ended = ziplineT <= 0f || ziplineT >= 1f;
			ziplineT = Mathf.Clamp01(ziplineT);

			Vector3 point = zipline.Sample(ziplineT);
			transform.position = point - Vector3.up * (motor.Height + ziplineHangDistance);

			bool jumpedOff = input.JumpBuffered;
			bool detach = jumpedOff || input.CrouchHeld || input.InteractPressed;
			if (detach || ended)
			{
				input.ConsumeJump();
				velocity = zipline.Direction * ziplineDirection * ziplineSpeedCurrent;
				if (jumpedOff) velocity += Vector3.up * ziplineDetachBoost;
				zipline = null;
				State = MoveState.Air;
				airJumpsUsed = 0;
			}
		}

		void LateUpdate()
		{
			if (input.InteractPressed && State != MoveState.Zipline) TryAttachZipline();
			UpdateVisual();
		}

		#endregion

		void BuildDebugVisual()
		{
			var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
			body.name = "Visual";
			Destroy(body.GetComponent<Collider>());
			body.transform.SetParent(transform, false);
			body.transform.localPosition = new Vector3(0f, motor.standingHeight * 0.5f, 0f);
			body.transform.localScale = new Vector3(motor.radius * 2f, motor.standingHeight * 0.5f, motor.radius * 2f);
			body.GetComponent<Renderer>().material.color = new Color(0.85f, 0.45f, 0.15f);

			var nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
			nose.name = "Facing";
			Destroy(nose.GetComponent<Collider>());
			nose.transform.SetParent(transform, false);
			nose.transform.localPosition = new Vector3(0f, motor.standingHeight * 0.8f, motor.radius + 0.1f);
			nose.transform.localScale = new Vector3(0.18f, 0.18f, 0.3f);
			nose.GetComponent<Renderer>().material.color = new Color(0.95f, 0.85f, 0.3f);

			visualBody = body.transform;
		}

		Transform visualBody;

		/// <summary>Keeps the debug capsule matching the (possibly crouched) collision height.</summary>
		void UpdateVisual()
		{
			if (visualBody == null) return;
			visualBody.localPosition = new Vector3(0f, motor.Height * 0.5f, 0f);
			visualBody.localScale = new Vector3(motor.radius * 2f, motor.Height * 0.5f, motor.radius * 2f);
		}

		void OnValidate()
		{
			crouchHeight = Mathf.Max(crouchHeight, 0.6f);
		}
	}
}
