using System.Collections.Generic;
using Gahwit.Core;
using Gahwit.UI;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif
using UnityEngine;

namespace Gahwit.Boot
{
    // Plays cues by id (AudioCatalog) on pooled sources, with no allocation per play. Music crossfades and ducks
    // under the voice note. The sound switch mutes every effect group; the music switch mutes music and ambience.
    // Groups are volumes in code for now; a mixer asset can take over when a group needs its own effects.
    public sealed class AudioService : MonoBehaviour
    {
        const int EffectSources = 8;
        const float FadeSeconds = 1.5f, DuckedVolume = 0.25f, MusicVolume = 0.7f;

        readonly Dictionary<string, AudioCatalog.Cue> cues = new Dictionary<string, AudioCatalog.Cue>();
        readonly AudioSource[] effects = new AudioSource[EffectSources];
        readonly Dictionary<string, AudioSource> loops = new Dictionary<string, AudioSource>();
        AudioSource musicA, musicB;
        string musicId;
        int nextEffect;
        float musicFade = 1f, duck = 1f;
        bool ducked;

        public bool SoundOn { get; set; } = true;
        public bool MusicOn { get; set; } = true;
        public bool HapticsOn { get; set; } = true;

        static AudioService shared;

        // One for the whole run: it outlives restarts through the Boot scene, so music doesn't stop and start.
        public static AudioService Shared => shared != null ? shared : shared = Create();

        static AudioService Create()
        {
            var service = new GameObject("Audio").AddComponent<AudioService>();
            DontDestroyOnLoad(service.gameObject);
            service.Init();
            return service;
        }

        void Init()
        {
            var catalog = AudioCatalog.Load();
            if (catalog != null)
                foreach (var cue in catalog.Cues)
                    if (cue.Clip != null) cues[cue.Id] = cue;
            for (int i = 0; i < EffectSources; i++) effects[i] = NewSource(false);
            musicA = NewSource(true);
            musicB = NewSource(true);

            UiSound.Play = id => Play(id);
            UiSound.Loop = SetLoop;
            UiSound.Duck = on => ducked = on;
            UiSound.Buzz = Buzz;
        }

        AudioSource NewSource(bool loop)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0;
            return source;
        }

        public bool Has(string id) => cues.ContainsKey(id);

        public void Play(string id, float pitch = 1f)
        {
            if (!SoundOn || !cues.TryGetValue(id, out var cue)) return;
            var source = effects[nextEffect];
            nextEffect = (nextEffect + 1) % EffectSources;
            source.clip = cue.Clip;
            source.pitch = pitch;
            source.volume = 1f;
            source.Play();
        }

        // A sound that repeats while something is held, such as the pour memory's stream.
        void SetLoop(string id, bool on)
        {
            if (!loops.TryGetValue(id, out var source))
            {
                if (!on || !cues.TryGetValue(id, out var cue)) return;
                source = NewSource(true);
                source.clip = cue.Clip;
                loops[id] = source;
            }
            if (on && SoundOn && !source.isPlaying) source.Play();
            else if (!on) source.Stop();
        }

        // Crossfades to a music cue; the same cue keeps playing.
        public void PlayMusic(string id)
        {
            if (id == musicId || !cues.TryGetValue(id, out var cue)) return;
            musicId = id;
            (musicA, musicB) = (musicB, musicA);
            musicA.clip = cue.Clip;
            musicA.volume = 0;
            musicA.Play();
            musicFade = 0f;
        }

        public void Buzz(Haptic style)
        {
            if (!HapticsOn) return;
#if UNITY_IOS && !UNITY_EDITOR
            GahwitHaptic((int)style);
#endif
            // Android: a short vibration through the Vibrator service comes with the Android build (missing/23).
        }

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void GahwitHaptic(int style);
#endif

        void Update()
        {
            musicFade = Mathf.MoveTowards(musicFade, 1f, Time.unscaledDeltaTime / FadeSeconds);
            duck = Mathf.MoveTowards(duck, ducked ? DuckedVolume : 1f, Time.unscaledDeltaTime * 2f);
            float volume = MusicOn ? MusicVolume * duck : 0f;
            musicA.volume = volume * musicFade;
            musicB.volume = volume * (1f - musicFade);
            if (musicFade >= 1f && musicB.isPlaying) musicB.Stop();
            if (!SoundOn)
                foreach (var loop in loops.Values)
                    if (loop.isPlaying) loop.Stop();
        }
    }
}
