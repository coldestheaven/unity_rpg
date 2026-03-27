using UnityEngine;

namespace Managers
{
    public class AudioManager : Framework.Base.SingletonMonoBehaviour<AudioManager>
    {
        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("Settings")]
        [SerializeField] private float musicVolume = 0.8f;
        [SerializeField] private float sfxVolume = 0.8f;

        public float MusicVolume
        {
            get => musicVolume;
            set
            {
                musicVolume = value;
                musicSource.volume = musicVolume;
            }
        }

        public float SfxVolume
        {
            get => sfxVolume;
            set
            {
                sfxVolume = value;
                sfxSource.volume = sfxVolume;
            }
        }

        protected override void Awake()
        {
            base.Awake();

            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.loop = true;
            }
            musicSource.volume = musicVolume;

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
            }
            sfxSource.volume = sfxVolume;
        }

        public void PlayMusic(AudioClip clip)
        {
            if (clip == null) return;

            if (musicSource.clip == clip && musicSource.isPlaying) return;

            musicSource.clip = clip;
            musicSource.Play();
        }

        public void PlaySFX(AudioClip clip)
        {
            if (clip == null) return;

            sfxSource.PlayOneShot(clip);
        }

        public void PlaySFX(AudioClip clip, float volumeScale)
        {
            if (clip == null) return;

            sfxSource.PlayOneShot(clip, volumeScale);
        }

        public void StopMusic()
        {
            musicSource.Stop();
        }

        public void PauseMusic()
        {
            musicSource.Pause();
        }

        public void ResumeMusic()
        {
            musicSource.UnPause();
        }
    }
}
