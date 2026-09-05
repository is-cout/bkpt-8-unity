using UnityEngine;

namespace Deadlock
{
	/// <summary>On-screen readout for tuning the movement, plus the control list.</summary>
	public class DeadlockDebugHud : MonoBehaviour
	{
		public DeadlockCharacter character;
		public bool showControls = true;

		GUIStyle style;
		float displayedSpeed;
		float peakSpeed;

		void Awake()
		{
			if (character == null) character = GetComponent<DeadlockCharacter>();
			if (character == null) character = FindAnyObjectByType<DeadlockCharacter>();
		}

		void Update()
		{
			if (character == null) return;
			displayedSpeed = Mathf.Lerp(displayedSpeed, character.HorizontalSpeed, 1f - Mathf.Exp(-15f * Time.deltaTime));
			peakSpeed = Mathf.Max(peakSpeed, character.HorizontalSpeed);
		}

		void OnGUI()
		{
			if (character == null) return;

			style ??= new GUIStyle(GUI.skin.label)
			{
				fontSize = 15,
				normal = { textColor = Color.white },
				richText = true
			};

			var stamina = character.Stamina;
			string charges = "";
			if (stamina != null)
			{
				for (int i = 0; i < stamina.maxCharges; i++)
					charges += i < stamina.Charges ? "<color=#57d9ff>[#]</color>" : "<color=#555555>[ ]</color>";
			}

			GUI.Box(new Rect(10, 10, 260, showControls ? 320 : 150), GUIContent.none);
			GUILayout.BeginArea(new Rect(20, 18, 240, 320));

			GUILayout.Label($"<b>{character.State}</b>{(character.IsSprinting ? "  <color=#ffd166>SPRINT</color>" : "")}{(character.IsDashing ? "  <color=#ff6b6b>DASH</color>" : "")}", style);
			GUILayout.Label($"Speed: <b>{displayedSpeed:0.0}</b> m/s   (peak {peakSpeed:0.0})", style);
			GUILayout.Label($"Vertical: {character.Velocity.y:0.0} m/s", style);
			GUILayout.Label($"Stamina: {charges}", style);
			GUILayout.Label($"Air jumps: {Mathf.Max(0, character.AirJumpsLeft)}", style);

			if (showControls)
			{
				GUILayout.Space(10);
				GUILayout.Label("<b>Controles</b>", style);
				GUILayout.Label("WASD  mover / air-strafe", style);
				GUILayout.Label("Mouse  olhar", style);
				GUILayout.Label("Space  pular / air jump / wall jump", style);
				GUILayout.Label("Shift (ou botao direito)  dash", style);
				GUILayout.Label("Dash + Space  dash-jump", style);
				GUILayout.Label("Ctrl / C  agachar, correndo = slide", style);
				GUILayout.Label("Space na borda  mantle", style);
				GUILayout.Label("E  entrar/sair da zipline", style);
				GUILayout.Label("Esc  liberar o mouse", style);
			}

			GUILayout.EndArea();

			if (peakSpeed > 0f && Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.R)
				peakSpeed = 0f;
		}
	}
}
