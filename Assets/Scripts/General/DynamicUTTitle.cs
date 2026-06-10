using UnityEngine;

public class DynamicUITitle : MonoBehaviour
{
    private RectTransform rectTransform;
    private Vector2 startPos;
    private Vector3 startScale;
    private Vector3 startRotation;

    [Header("Hover Effect")]
    [Tooltip("How fast the image bobs up and down.")]
    public float hoverSpeed = 2f;
    [Tooltip("How high and low the image travels in pixels.")]
    public float hoverHeight = 15f;

    [Header("Breathing Effect")]
    [Tooltip("How fast the image scales in and out.")]
    public float pulseSpeed = 1.5f;
    [Tooltip("How much larger/smaller the image gets (0.05 is 5%).")]
    public float pulseAmount = 0.05f;

    [Header("Tilt Effect")]
    [Tooltip("How fast the image rocks back and forth.")]
    public float tiltSpeed = 1.2f;
    [Tooltip("The maximum degrees the image will tilt left and right.")]
    public float tiltAngle = 2.5f; // Kept very low for a "super slight" effect

    private void Start()
    {
        // Grab the UI RectTransform and save its exact starting placement
        rectTransform = GetComponent<RectTransform>();
        startPos = rectTransform.anchoredPosition;
        startScale = rectTransform.localScale;
        startRotation = rectTransform.localEulerAngles;
    }

    private void Update()
    {
        // Smooth waves between -1 and 1 based on the game's time
        float hoverWave = Mathf.Sin(Time.time * hoverSpeed) * hoverHeight;
        float pulseWave = Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;

        // Using Cosine here offsets the rhythm from the Sine waves for a more natural float
        float tiltWave = Mathf.Cos(Time.time * tiltSpeed) * tiltAngle;

        // Apply the hover to the Y axis
        rectTransform.anchoredPosition = new Vector2(startPos.x, startPos.y + hoverWave);

        // Apply the pulse to the scale uniformly
        rectTransform.localScale = startScale + new Vector3(pulseWave, pulseWave, 0f);

        // Apply the tilt to the Z axis rotation (the axis that spins 2D UI elements)
        rectTransform.localEulerAngles = startRotation + new Vector3(0f, 0f, tiltWave);
    }
}