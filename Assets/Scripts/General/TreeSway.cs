using UnityEngine;

public class BackgroundSway : MonoBehaviour
{
    [Header("Sway Settings")]
    [Tooltip("Maximum angle (in degrees) the tree will tilt to either side.")]
    public float swayMagnitude = 1.5f;

    [Tooltip("How fast the tree sways back and forth.")]
    public float swaySpeed = 1.2f;

    [Header("Chaos (Randomness)")]
    [Tooltip("Adds slight speed variations so every tree doesn't look identical.")]
    public float speedVariation = 0.2f;

    // Private tracking variables
    private float baseZRotation;
    private float randomOffset;
    private float uniqueSpeed;

    void Start()
    {
        // Remember the tree's starting rotation so we don't snap it to 0
        baseZRotation = transform.localEulerAngles.z;

        // Generate a random starting point in the sine wave (0 to 360 degrees)
        randomOffset = Random.Range(0f, 100f);

        // Give this specific tree a slightly unique speed modifier
        uniqueSpeed = swaySpeed + Random.Range(-speedVariation, speedVariation);
    }

    void Update()
    {
        // Calculate the current sway using a sine wave over time
        // Mathf.Sin returns a smooth curve between -1 and 1
        float swayAngle = Mathf.Sin(Time.time * uniqueSpeed + randomOffset) * swayMagnitude;

        // Apply the sway to the Z-axis (tilting left and right relative to the camera)
        transform.localRotation = Quaternion.Euler(
            transform.localEulerAngles.x,
            transform.localEulerAngles.y,
            baseZRotation + swayAngle
        );
    }
}