using System.Collections.Generic;
using UnityEngine;

namespace SpookyPath.Game
{
    /// <summary>Plays the synthesized music and effects with the volumes from the settings menu.</summary>
    public sealed class AudioDirector : MonoBehaviour
    {
        const int Voices = 12;

        readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        readonly AudioSource[] voices = new AudioSource[Voices];
        AudioSource music;
        int next;
        float musicVolume = 0.7f;
        float effectsVolume = 0.9f;

        public void Init(GameArt art)
        {
            foreach (AudioClip clip in art.Effects)
            {
                if (clip != null)
                {
                    clips[clip.name] = clip;
                }
            }
            music = gameObject.AddComponent<AudioSource>();
            music.loop = true;
            music.playOnAwake = false;
            for (int i = 0; i < Voices; i++)
            {
                voices[i] = gameObject.AddComponent<AudioSource>();
                voices[i].playOnAwake = false;
            }
        }

        public void SetVolumes(float musicLevel, float effectsLevel)
        {
            musicVolume = musicLevel;
            effectsVolume = effectsLevel;
            if (music != null)
            {
                music.volume = 0.55f * musicVolume;
            }
        }

        public void PlayMusic(AudioClip clip)
        {
            if (music == null || clip == null || music.clip == clip && music.isPlaying)
            {
                return;
            }
            music.clip = clip;
            music.volume = 0.55f * musicVolume;
            music.Play();
        }

        public void StopMusic()
        {
            if (music != null)
            {
                music.Stop();
            }
        }

        /// <summary>Plays an effect by name with a little pitch variation so repeats do not sound robotic.</summary>
        public void Play(string name, float volume = 1f, float pitchJitter = 0.06f)
        {
            if (!clips.TryGetValue(name, out AudioClip clip) || effectsVolume <= 0f)
            {
                return;
            }
            AudioSource voice = voices[next];
            next = (next + 1) % Voices;
            voice.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
            voice.PlayOneShot(clip, volume * effectsVolume);
        }

        public bool Has(string name) => clips.ContainsKey(name);
    }
}
