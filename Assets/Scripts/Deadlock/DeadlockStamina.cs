using UnityEngine;

namespace Deadlock
{
	/// <summary>
	/// Deadlock-style stamina: a small pool of discrete charges, each one refilling
	/// on its own timer. Dash, air jump and wall jump all draw from it.
	/// </summary>
	public class DeadlockStamina : MonoBehaviour
	{
		[Tooltip("Number of stamina charges.")]
		public int maxCharges = 3;
		[Tooltip("Seconds to regenerate a single charge.")]
		public float rechargeTime = 5f;
		[Tooltip("Delay before regeneration starts after spending a charge.")]
		public float rechargeDelay = 0.5f;

		public int Charges { get; private set; }
		/// <summary>0..1 progress of the charge currently regenerating.</summary>
		public float PartialCharge { get; private set; }

		float delayTimer;

		void Awake()
		{
			Charges = maxCharges;
		}

		public bool Has(int amount = 1) => Charges >= amount;

		public bool TrySpend(int amount = 1)
		{
			if (Charges < amount) return false;
			Charges -= amount;
			PartialCharge = 0f;
			delayTimer = rechargeDelay;
			return true;
		}

		public void RefillAll()
		{
			Charges = maxCharges;
			PartialCharge = 0f;
		}

		void Update()
		{
			if (Charges >= maxCharges)
			{
				PartialCharge = 0f;
				return;
			}

			if (delayTimer > 0f)
			{
				delayTimer -= Time.deltaTime;
				return;
			}

			PartialCharge += Time.deltaTime / Mathf.Max(0.01f, rechargeTime);
			if (PartialCharge >= 1f)
			{
				PartialCharge = 0f;
				Charges = Mathf.Min(maxCharges, Charges + 1);
			}
		}
	}
}
