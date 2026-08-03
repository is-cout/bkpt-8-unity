using UnityEngine;
using UnityEngine.Events;

[System.Serializable] public class OnValueChangedEvent : UnityEvent<float> { }

public class AutomaticSlider : MonoBehaviour
{
    [SerializeField] private bool autoReverse = false, smoothstep = false;
    [SerializeField, Min(0.01f)] private float duration = 1f;
    [SerializeField] private OnValueChangedEvent onValueChanged = default;

    private float value;

    public bool Reversed { get; set; }
    public bool AutoReverse
    {
        get => autoReverse;
        set => autoReverse = value;
    }

    private float SmoothedValue => 3f * value * value - 2f * value * value * value;

    private void FixedUpdate()
    {
        float delta = Time.deltaTime / duration;

        if (Reversed)
        {
            value -= delta;
            if (value <= 0f)
            {
                if (AutoReverse)
                {
                    value = Mathf.Min(1f, -value);
                    Reversed = false;
                }
                else
                {
                    value = 0f;
                    enabled = false;
                }
            }
        }
        else
        {
            value += delta;
            if (value >= 1f)
            {
                if (AutoReverse)
                {
                    value = Mathf.Max(0f, 2f - value);
                    Reversed = true;
                }
                else
                {
                    value = 1f;
                    enabled = false;
                }
            }
        }

        onValueChanged.Invoke(smoothstep ? SmoothedValue : value);
    }
}