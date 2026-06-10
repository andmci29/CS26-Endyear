using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class FloatingMusicNote : MonoBehaviour
{
    [Header("Movement Bounds (Canvas Pixels)")]
    [Tooltip("The local Y coordinate where the note teleports back to the bottom.")]
    public float minY = -600f;
    [Tooltip("The local Y coordinate where the note triggers its teleport loop.")]
    public float maxY = 600f;

    [Header("Speed Settings")]
    public float minSpeed = 40f;
    public float maxSpeed = 90f;

    [Header("Tilt/Rotation Settings")]
    [Tooltip("Minimum rotation speed (negative values tilt counter-clockwise).")]
    public float minRotationSpeed = -30f;
    [Tooltip("Maximum rotation speed (positive values tilt clockwise).")]
    public float maxRotationSpeed = 30f;

    [Header("Optional Variety")]
    [Tooltip("If greater than 0, the note will slightly shift left/right randomly whenever it teleports back down.")]
    public float horizontalSpawnRange = 50f;

    private RectTransform rectTransform;
    private float currentSpeed;
    private float currentRotationSpeed;
    private float initialX;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        initialX = rectTransform.anchoredPosition.x;

        // Establish initial unique speeds for this note
        RandomizeNoteCharacteristics();

        // Scatter notes vertically at the start so they don't all rise in a single uniform line
        Vector2 spawnPos = rectTransform.anchoredPosition;
        spawnPos.y = Random.Range(minY, maxY);
        rectTransform.anchoredPosition = spawnPos;
    }

    void Update()
    {
        // 1. Travel Upwards
        Vector2 position = rectTransform.anchoredPosition;
        position.y += currentSpeed * Time.deltaTime;
        rectTransform.anchoredPosition = position;

        // 2. Apply Gentle Tilt/Rotation
        rectTransform.Rotate(0f, 0f, currentRotationSpeed * Time.deltaTime);

        // 3. Teleport Loop Check
        if (rectTransform.anchoredPosition.y >= maxY)
        {
            TeleportToBottom();
        }
    }

    void TeleportToBottom()
    {
        // Reroll characteristics so it behaves like a totally different note on its next pass
        RandomizeNoteCharacteristics();

        Vector2 resetPosition = rectTransform.anchoredPosition;
        resetPosition.y = minY;

        // Apply slight horizontal variance if configured
        if (horizontalSpawnRange > 0f)
        {
            resetPosition.x = initialX + Random.Range(-horizontalSpawnRange, horizontalSpawnRange);
        }

        rectTransform.anchoredPosition = resetPosition;
    }

    void RandomizeNoteCharacteristics()
    {
        currentSpeed = Random.Range(minSpeed, maxSpeed);
        currentRotationSpeed = Random.Range(minRotationSpeed, maxRotationSpeed);
    }
}