using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Generic playground utility. Drives a single UI button that cycles through every scene listed
/// in Build Settings, wrapping around at the end. Used by the "Same playground Tests" comparison
/// scenes so each movement system can be tried back to back.
/// </summary>
public class SceneCycler : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Text label;
    [SerializeField] private KeyCode cycleKey = KeyCode.F7;

    private void Awake()
    {
        // The comparison scenes each ship their own EventSystem, but keep a fallback so the
        // prefab still works if dropped into a scene without one.
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }
    }

    private void Start()
    {
        if (button != null)
        {
            button.onClick.AddListener(LoadNext);
        }
        RefreshLabel();
    }

    private void Update()
    {
        // The UI button can't be clicked while a controller locks/hides the cursor, so also
        // expose the cycle on a hotkey. Check both input backends so it works regardless of the
        // project's Active Input Handling setting.
        if (HotkeyPressed())
        {
            LoadNext();
        }
    }

    private bool HotkeyPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current[Key.F7].wasPressedThisFrame)
        {
            return true;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(cycleKey))
        {
            return true;
        }
#endif
        return false;
    }

    private void RefreshLabel()
    {
        if (label == null)
        {
            return;
        }

        Scene active = SceneManager.GetActiveScene();
        int index = active.buildIndex;
        int count = SceneManager.sceneCountInBuildSettings;
        label.text = index < 0
            ? active.name + "  (not in build)"
            : string.Format("{0}   ({1}/{2})   ▶ Next", active.name, index + 1, count);
    }

    /// <summary>Load the next scene in Build Settings, wrapping to the first at the end.</summary>
    public void LoadNext()
    {
        int count = SceneManager.sceneCountInBuildSettings;
        if (count == 0)
        {
            Debug.LogWarning("SceneCycler: no scenes in Build Settings.");
            return;
        }

        int index = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene((index + 1 + count) % count);
    }
}
