using UnityEngine;
using System.Collections.Generic;
using RTSLockstep.Managers.GameManagers;

// Unity only allows a single AudioClip to be added to a GameObject
// when our AudioElement is created we create a new GameObject
// for each sound clip passed in we create a GameObject with that sound clip attached
// needed for us to be able to play the sound clips and mess around with things like volume levels
// GameObjects are then attached to the main GameObject we created for the AudioElement
// main GameObject is then attached to the parent transform passed into the constructor
// If no transform is passed in we want to attach the GameObject to the organizer object in the scene, to make sure that our inspector does not become unnecessarily cluttered.

namespace RTSLockstep.LSResources.Audio
{
    public class AudioElement
    {
        #region Properties
        private GameObject element;
        private Dictionary<AudioClip, GameObject> soundObjects = new Dictionary<AudioClip, GameObject>();
        #endregion

        #region Public
        public AudioElement(List<AudioClip> sounds, List<float> volumes, string id, Transform parentTransform)
        {
            if (sounds == null || sounds.Count == 0 || volumes == null || volumes.Count == 0 || sounds.Count != volumes.Count)
            {
                return;
            }
            element = new GameObject("AudioElement_" + id);
            if (parentTransform)
            {
                element.transform.localPosition = parentTransform.localPosition;
                element.transform.parent = parentTransform;
            }
            else
            {
                //attach it to the organizer object (since we know there should be one present)
                //do so to keep the inspector cleaner - this saves making a sounds object
                //   GameObjectList list = MonoBehaviour.FindObjectOfType(typeof(GameObjectList)) as GameObjectList;

                if (GameResourceManager.OrganizerObject)
                {
                    element.transform.parent = GameResourceManager.OrganizerObject;
                }
            }
            Add(sounds, volumes);
        }

        public void Add(List<AudioClip> sounds, List<float> volumes)
        {
            for (int i = 0; i < sounds.Count; i++)
            {
                AudioClip sound = sounds[i];
                if (!sound)
                {
                    continue;
                }
                GameObject temp = new GameObject(sound.name);
                temp.AddComponent(typeof(AudioSource));
                temp.GetComponent<AudioSource>().clip = sound;
                temp.GetComponent<AudioSource>().volume = volumes[i];
                temp.transform.parent = element.transform;
                temp.transform.localPosition = element.transform.localPosition;
                soundObjects.Add(sound, temp);
            }
        }

        public void Play(AudioClip sound)
        {
            GameObject temp;
            if (soundObjects.TryGetValue(sound, out temp))
            {
                if (!temp.GetComponent<AudioSource>().isPlaying)
                {
                    temp.GetComponent<AudioSource>().Play();
                }
            }
        }

        public void Pause(AudioClip sound)
        {
            GameObject temp;
            if (soundObjects.TryGetValue(sound, out temp))
            {
                temp.GetComponent<AudioSource>().Pause();
            }
        }

        public void Stop(AudioClip sound)
        {
            GameObject temp;
            if (soundObjects.TryGetValue(sound, out temp))
            {
                temp.GetComponent<AudioSource>().Stop();
            }
        }

        public bool IsPlaying(AudioClip sound)
        {
            GameObject temp;
            if (soundObjects.TryGetValue(sound, out temp))
            {
                return temp.GetComponent<AudioSource>().isPlaying;
            }
            return false;
        }
        #endregion
    }
}