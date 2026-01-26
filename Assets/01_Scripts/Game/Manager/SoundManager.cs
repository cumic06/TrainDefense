using System.Collections.Generic;
using UnityEngine;
using Cumic;
using TrainDefense.Game.Datas;

namespace TrainDefense.Game
{
    public class SoundManager : Singleton<SoundManager>
    {
        [Header("Audio Sources")]
        [SerializeField]
        private AudioSource bgmSoundSource;
        [SerializeField]
        private AudioSource sfxSoundSourcePrefab;
        [SerializeField]
        private Transform sfxRoot;

        [Header("Settings")]
        [Range(0f, 1f)]
        [SerializeField]
        private float bgmVolume = 1f;
        [Range(0f, 1f)]
        [SerializeField]
        private float sfxVolume = 1f;

        private bool IsBgmMuted { get; set; }
        private bool IsSfxMuted { get; set; }

        private Stack<AudioSource> _sfxPool = new Stack<AudioSource>();
        private List<AudioSource> _activeSfxSources = new List<AudioSource>();
        private SoundDB _soundDB;

        protected override void Awake()
        {
            base.Awake();

            if (sfxRoot == null)
            {
                sfxRoot = new GameObject("SFX_Root").transform;
                sfxRoot.SetParent(transform);
            }

            // DatabaseManager를 통해 SoundDB 캐싱
            _soundDB = DatabaseManager.Instance.GetDB().SoundDB;
            if (_soundDB != null)
            {
                _soundDB.Initialize();
            }
        }

        #region BGM Management

        public void PlayBGM(SoundType type)
        {
            var data = _soundDB.GetSoundData(type);
            if (data != null) PlayBGM(data);
        }

        public void PlayBGM(string id)
        {
            var data = _soundDB.GetSoundData(id);
            if (data != null) PlayBGM(data);
        }

        public void PlayBGMByClipId(string clipId)
        {
            var data = _soundDB.GetSoundDataByClipId(clipId);
            if (data != null) PlayBGM(data);
        }

        public void PlayBGM(SoundData data)
        {
            if (bgmSoundSource == null) return;
            if (data == null || data.Clip == null) return;

            bgmSoundSource.clip = data.Clip;
            bgmSoundSource.volume = data.Volume * bgmVolume;
            bgmSoundSource.loop = true;
            bgmSoundSource.mute = IsBgmMuted;
            bgmSoundSource.Play();
        }

        public void StopBGM()
        {
            if (bgmSoundSource != null)
            {
                bgmSoundSource.Stop();
            }
        }

        public void PauseBGM()
        {
            if (bgmSoundSource != null)
            {
                bgmSoundSource.Pause();
            }
        }

        public void ResumeBGM()
        {
            if (bgmSoundSource != null)
            {
                bgmSoundSource.UnPause();
            }
        }

        public void SetBGMVolume(float volume)
        {
            bgmVolume = Mathf.Clamp01(volume);
            if (bgmSoundSource != null)
            {
                bgmSoundSource.volume = bgmVolume;
            }
        }

        public void MuteBGM(bool mute)
        {
            IsBgmMuted = mute;
            if (bgmSoundSource != null)
            {
                bgmSoundSource.mute = IsBgmMuted;
            }
        }

        #endregion

        #region SFX Management

        public void PlaySFX(SoundType type)
        {
            var data = _soundDB.GetSoundData(type);
            if (data != null) PlaySFX(data);
        }

        public void PlaySFX(string id)
        {
            var data = _soundDB.GetSoundData(id);
            if (data != null) PlaySFX(data);
        }

        public void PlaySFXByClipId(string clipId)
        {
            var data = _soundDB.GetSoundDataByClipId(clipId);
            if (data != null) PlaySFX(data);
        }

        public void PlaySFX(SoundData data)
        {
            if (data == null || data.Clip == null) return;
            if (IsSfxMuted) return;

            AudioSource source = GetSfxSource();
            source.clip = data.Clip;
            source.volume = data.Volume * sfxVolume;
            source.Play();

            _activeSfxSources.Add(source);
        }

        private AudioSource GetSfxSource()
        {
            AudioSource source;
            if (_sfxPool.Count > 0)
            {
                source = _sfxPool.Pop();
                source.gameObject.SetActive(true);
            }
            else
            {
                if (sfxSoundSourcePrefab != null)
                {
                    source = Instantiate(sfxSoundSourcePrefab, sfxRoot);
                }
                else
                {
                    GameObject go = new GameObject("SFXSource");
                    go.transform.SetParent(sfxRoot);
                    source = go.AddComponent<AudioSource>();
                    source.playOnAwake = false;
                }
            }
            return source;
        }

        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            foreach (var source in _activeSfxSources)
            {
                if (source.isPlaying)
                {
                    source.volume = sfxVolume;
                }
            }
        }

        public void MuteSFX(bool mute)
        {
            IsSfxMuted = mute;
            foreach (var source in _activeSfxSources)
            {
                source.mute = IsSfxMuted;
            }
        }

        private void Update()
        {
            // 재생이 끝난 SFX Source들을 풀로 반환
            for (int i = _activeSfxSources.Count - 1; i >= 0; i--)
            {
                if (!_activeSfxSources[i].isPlaying)
                {
                    ReturnSfxSource(_activeSfxSources[i]);
                    _activeSfxSources.RemoveAt(i);
                }
            }
        }

        private void ReturnSfxSource(AudioSource source)
        {
            source.clip = null;
            source.gameObject.SetActive(false);
            _sfxPool.Push(source);
        }

        public void StopAllSFX()
        {
            foreach (var source in _activeSfxSources)
            {
                source.Stop();
                ReturnSfxSource(source);
            }
            _activeSfxSources.Clear();
        }

        #endregion
    }
}
