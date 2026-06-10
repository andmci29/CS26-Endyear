using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Assets.Plugins.ButtonSoundsEditor
{
    public class ButtonClickSound : MonoBehaviour
    {
        [Header("Audio Settings")]
        public AudioSource AudioSource;
        public AudioClip ClickSound;

        public void Awake()
        {
            // Safety Check: Automatically try to grab the AudioSource if left blank in Inspector
            if (AudioSource == null)
                AudioSource = GetComponent<AudioSource>();

            Button button = GetComponent<Button>();
            if (button != null)
            {
                // If it's a standard UI Button, use this (cleanest and best performance)
                button.onClick.AddListener(PlayClickSound);
            }
            else
            {
                // If it's NOT a button (like a generic Image or Panel), use the EventTrigger fallback
                EventTrigger eventTrigger = GetComponent<EventTrigger>();
                if (eventTrigger != null)
                {
                    // Use FirstOrDefault instead of SingleOrDefault to prevent crashes if multiple exist
                    EventTrigger.Entry clickEntry = eventTrigger.triggers.FirstOrDefault(_ => _.eventID == EventTriggerType.PointerClick);

                    // FIX: If the trigger entry doesn't exist yet, build it procedurally!
                    if (clickEntry == null)
                    {
                        clickEntry = new EventTrigger.Entry();
                        clickEntry.eventID = EventTriggerType.PointerClick;
                        eventTrigger.triggers.Add(clickEntry);
                    }

                    clickEntry.callback.AddListener(_ => PlayClickSound());
                }
            }
        }

        private void PlayClickSound()
        {
            // Safety Check: Prevent NullReference crashes if clips are missing
            if (AudioSource != null && ClickSound != null)
            {
                AudioSource.PlayOneShot(ClickSound);
            }
            else
            {
                Debug.LogWarning($"Missing AudioSource or AudioClip on {gameObject.name}", gameObject);
            }
        }
    }
}