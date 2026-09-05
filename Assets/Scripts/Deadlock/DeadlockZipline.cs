using System.Collections.Generic;
using UnityEngine;

namespace Deadlock
{
	/// <summary>
	/// A straight cable the character can ride, like Deadlock's lane ziplines.
	/// Press the interact key near one to attach; jump or crouch to drop off.
	/// </summary>
	public class DeadlockZipline : MonoBehaviour
	{
		public static readonly List<DeadlockZipline> All = new List<DeadlockZipline>();

		[Tooltip("Cable endpoints in world space. Set by hand or by the level builder.")]
		public Vector3 startPoint = new Vector3(0f, 6f, 0f);
		public Vector3 endPoint = new Vector3(0f, 6f, 30f);
		public bool drawCable = true;
		public float cableWidth = 0.08f;

		public Vector3 Start => startPoint;
		public Vector3 End => endPoint;
		public float Length => Vector3.Distance(startPoint, endPoint);
		public Vector3 Direction => (endPoint - startPoint).normalized;

		void OnEnable()
		{
			All.Add(this);
			if (drawCable) BuildCable();
		}

		void OnDisable() => All.Remove(this);

		public Vector3 Sample(float t) => Vector3.Lerp(startPoint, endPoint, Mathf.Clamp01(t));

		/// <summary>Normalized position of the point on the cable closest to a world position.</summary>
		public float Project(Vector3 worldPosition)
		{
			Vector3 axis = endPoint - startPoint;
			float lengthSq = axis.sqrMagnitude;
			if (lengthSq < 0.0001f) return 0f;
			return Mathf.Clamp01(Vector3.Dot(worldPosition - startPoint, axis) / lengthSq);
		}

		public static DeadlockZipline FindNearest(Vector3 worldPosition, float maxDistance, out float t)
		{
			DeadlockZipline best = null;
			float bestDistance = maxDistance;
			t = 0f;

			foreach (var line in All)
			{
				float lt = line.Project(worldPosition);
				float distance = Vector3.Distance(line.Sample(lt), worldPosition);
				if (distance < bestDistance)
				{
					bestDistance = distance;
					best = line;
					t = lt;
				}
			}

			return best;
		}

		void BuildCable()
		{
			var lr = GetComponent<LineRenderer>();
			if (lr == null) lr = gameObject.AddComponent<LineRenderer>();
			lr.useWorldSpace = true;
			lr.positionCount = 2;
			lr.SetPosition(0, startPoint);
			lr.SetPosition(1, endPoint);
			lr.startWidth = lr.endWidth = cableWidth;
			if (lr.sharedMaterial == null)
			{
				var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
				if (shader != null) lr.material = new Material(shader) { color = new Color(1f, 0.75f, 0.2f) };
			}
		}

		void OnDrawGizmos()
		{
			Gizmos.color = new Color(1f, 0.75f, 0.2f);
			Gizmos.DrawLine(startPoint, endPoint);
			Gizmos.DrawWireSphere(startPoint, 0.2f);
			Gizmos.DrawWireSphere(endPoint, 0.2f);
		}
	}
}
