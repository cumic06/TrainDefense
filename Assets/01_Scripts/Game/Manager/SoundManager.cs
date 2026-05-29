using Cumic;
using Cumic.Events;
using System.Collections.Generic;
using TrainDefense.Game.Datas;
using UnityEngine;

namespace TrainDefense.Game
{
    public class SoundManager : Singleton<SoundManager>
    {
        private enum SfxDuplicatePolicy
        {
            Allow,
            RestartExisting,
            IgnoreNew,
        }

        #region Variable

        #region Fields
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

        [Header("SFX Behavior")]
        [SerializeField]
        private SfxDuplicatePolicy sfxDuplicatePolicy = SfxDuplicatePolicy.RestartExisting;

        public float BGMVolume => bgmVolume;
        public float SFXVolume => sfxVolume;
        #endregion

        public bool IsBgmMuted { get; private set; }
        public bool IsSfxMuted { get; private set; }

        // 옵션 Mute와 독립된 일시 억제 플래그 (삼중택일 등 UI 중 SFX 차단용)
        private bool _isSfxSuppressed = false;
        private bool IsSfxSilenced => IsSfxMuted || _isSfxSuppressed;

        private readonly Stack<AudioSource> _sfxPool = new();
        private readonly List<AudioSource> _activeSfxSources = new();
        private readonly Dictionary<AudioSource, float> _sfxBaseVolumes = new();
        private SoundDB _soundDB;
        private float _currentBgmBaseVolume = 1f;

        private SoundDB _GetSoundDB()
        {
            if (_soundDB == null)
            {
                if (DatabaseManager.Instance != null && DatabaseManager.Instance.GetDB() != null)
                {
                    _soundDB = DatabaseManager.Instance.GetDB().SoundDB;
                    if (_soundDB != null)
                    {
                        _soundDB.Initialize();
                    }
                }
            }
            return _soundDB;
        }

        #endregion

        private void Start()
        {
            if (sfxSoundSourcePrefab != null)
            {
                ConfigureSfxSourceDefaults(sfxSoundSourcePrefab);
                ResetSfxSource(sfxSoundSourcePrefab);
                sfxSoundSourcePrefab.gameObject.SetActive(false);
            }

            if (sfxRoot == null || (sfxSoundSourcePrefab != null && sfxRoot == sfxSoundSourcePrefab.transform))
            {
                sfxRoot = new GameObject("SFX_Root").transform;
                sfxRoot.SetParent(transform);
                sfxRoot.localPosition = Vector3.zero;
                sfxRoot.localRotation = Quaternion.identity;
                sfxRoot.localScale = Vector3.one;
            }

            LoadOptions();
        }

        private void LoadOptions()
        {
            var option = UserOptionDataParser.Load();
            bgmVolume = Mathf.Clamp01(option.BgmVolume);
            sfxVolume = Mathf.Clamp01(option.SfxVolume);
            IsBgmMuted = option.IsBgmMuted;
            IsSfxMuted = option.IsSfxMuted;

            if (bgmSoundSource != null)
            {
                bgmSoundSource.volume = _currentBgmBaseVolume * bgmVolume;
                bgmSoundSource.mute = IsBgmMuted;
            }
        }

        private void SaveOptions()
        {
            var option = (UserDataManager.Instance != null && UserDataManager.Instance.UserOptionData != null)
                ? UserDataManager.Instance.UserOptionData
                : UserOptionDataParser.Load();

            option.BgmVolume = bgmVolume;
            option.SfxVolume = sfxVolume;
            option.IsBgmMuted = IsBgmMuted;
            option.IsSfxMuted = IsSfxMuted;
            UserOptionDataParser.Save(option);
        }

        public void PlayBGMOnInit()
        {
            PlayBGM(SoundType.BGM_Lobby);
        }

        #region BGM Management

        public void PlayBGM(SoundType type)
        {
            var db = _GetSoundDB();
            if (db == null)
                return;
            var data = db.GetSoundData(type);
            if (data != null)
                PlayBGM(data);
        }

        public void PlayBGM(string id)
        {
            var db = _GetSoundDB();
            if (db == null)
                return;
            var data = db.GetSoundData(id);
            if (data != null)
                PlayBGM(data);
        }

        public void PlayBGMByClipId(string clipId)
        {
            var db = _GetSoundDB();
            if (db == null)
                return;
            var data = db.GetSoundDataByClipId(clipId);
            if (data != null)
                PlayBGM(data);
        }

        public void PlayBGM(SoundData data)
        {
            if (bgmSoundSource == null)
                return;
            if (data == null || data.Clip == null)
                return;

            _currentBgmBaseVolume = data.Volume;
            bgmSoundSource.clip = data.Clip;
            bgmSoundSource.volume = _currentBgmBaseVolume * bgmVolume;
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
                bgmSoundSource.volume = _currentBgmBaseVolume * bgmVolume;
            }
            SaveOptions();
        }

        public void MuteBGM(bool mute)
        {
            IsBgmMuted = mute;
            if (bgmSoundSource != null)
            {
                bgmSoundSource.mute = IsBgmMuted;
            }
            SaveOptions();
        }

        public void MuteBGM()
        {
            IsBgmMuted = !IsBgmMuted;
            if (bgmSoundSource != null)
            {
                bgmSoundSource.mute = IsBgmMuted;
            }
            SaveOptions();
        }

        #endregion

        #region SFX Management

        public void PlaySFX(SoundType type, bool isLoop = false, bool ignoreSuppress = false)
        {
            var db = _GetSoundDB();
            if (db == null)
                return;
            var data = db.GetSoundData(type);
            if (data != null)
                PlaySFX(data, isLoop, ignoreSuppress);
        }

        public void PlaySFX(string id, bool isLoop = false)
        {
            var db = _GetSoundDB();
            if (db == null)
                return;
            var data = db.GetSoundData(id);
            if (data != null)
                PlaySFX(data, isLoop);
        }

        public void PlaySFXByClipId(string clipId, bool isLoop = false)
        {
            var db = _GetSoundDB();
            if (db == null)
                return;
            var data = db.GetSoundDataByClipId(clipId);
            if (data != null)
                PlaySFX(data, isLoop);
        }

        public void PlaySFX(SoundData data, bool isLoop, bool ignoreSuppress = false)
        {
            if (data == null || data.Clip == null)
                return;

            // 옵션 음소거는 항상 존중하되, 일시 억제(Pause 등)는 ignoreSuppress로 우회 가능
            bool silenced = IsSfxMuted || (_isSfxSuppressed && !ignoreSuppress);
            if (silenced)
                return;

            var existing = FindActiveSfxSourceByClip(data.Clip);
            if (existing != null)
            {
                if (sfxDuplicatePolicy == SfxDuplicatePolicy.IgnoreNew)
                    return;

                if (sfxDuplicatePolicy == SfxDuplicatePolicy.RestartExisting)
                {
                    ApplySfxSettings(existing, data, ignoreSuppress);
                    existing.Stop();
                    existing.Play();
                    return;
                }
            }

            AudioSource source = GetSfxSource();
            ApplySfxSettings(source, data, ignoreSuppress);
            source.loop = isLoop;
            source.Play();

            _activeSfxSources.Add(source);
        }

        public void StopSFX(SoundType type)
        {
            var db = _GetSoundDB();
            if (db == null)
                return;
            var data = db.GetSoundData(type);
            if (data != null)
                StopSFX(data);
        }

        public void StopSFX(SoundData data)
        {
            if (data == null || data.Clip == null)
                return;

            var existing = FindActiveSfxSourceByClip(data.Clip);
            if (existing != null)
            {
                existing.Stop();
            }
        }

        private void ApplySfxSettings(AudioSource source, SoundData data, bool ignoreSuppress = false)
        {
            ConfigureSfxSourceDefaults(source);
            source.clip = data.Clip;
            _sfxBaseVolumes[source] = data.Volume;
            source.volume = data.Volume * sfxVolume;
            source.mute = ignoreSuppress ? IsSfxMuted : IsSfxSilenced;
        }

        private AudioSource FindActiveSfxSourceByClip(AudioClip clip)
        {
            if (clip == null)
                return null;

            for (int i = _activeSfxSources.Count - 1; i >= 0; i--)
            {
                var source = _activeSfxSources[i];
                if (source == null)
                {
                    _activeSfxSources.RemoveAt(i);
                    continue;
                }

                if (source.isPlaying && source.clip == clip)
                    return source;
            }

            return null;
        }

        private AudioSource GetSfxSource()
        {
            AudioSource source;
            if (_sfxPool.Count > 0)
            {
                source = _sfxPool.Pop();
            }
            else
            {
                source = CreateSfxSource();
            }

            ResetSfxSource(source);
            ConfigureSfxSourceDefaults(source);
            source.gameObject.SetActive(true);

            return source;
        }

        private AudioSource CreateSfxSource()
        {
            AudioSource source;
            if (sfxSoundSourcePrefab != null)
            {
                source = Instantiate(sfxSoundSourcePrefab, sfxRoot);
            }
            else
            {
                GameObject go = new GameObject("SFXSource");
                go.transform.SetParent(sfxRoot);
                source = go.AddComponent<AudioSource>();
            }

            ConfigureSfxSourceDefaults(source);
            return source;
        }

        public void SetSFXVolume(float volume)
        {
            var previous = sfxVolume;
            sfxVolume = Mathf.Clamp01(volume);

            if (Mathf.Approximately(previous, sfxVolume))
            {
                SaveOptions();
                return;
            }

            foreach (var source in _activeSfxSources)
            {
                if (source == null)
                    continue;

                var baseVolume = _sfxBaseVolumes.TryGetValue(source, out var v) ? v : 1f;
                source.volume = baseVolume * sfxVolume;
            }

            foreach (var source in _sfxPool)
            {
                if (source == null)
                    continue;
                var baseVolume = _sfxBaseVolumes.TryGetValue(source, out var v) ? v : 1f;
                source.volume = baseVolume * sfxVolume;
            }

            SaveOptions();
        }

        public void MuteSFX(bool mute)
        {
            IsSfxMuted = mute;
            foreach (var source in _activeSfxSources)
            {
                source.mute = IsSfxMuted;
            }
            SaveOptions();
        }

        public void MuteSFX()
        {
            IsSfxMuted = !IsSfxMuted;
            foreach (var source in _activeSfxSources)
            {
                source.mute = IsSfxMuted;
            }
            SaveOptions();
        }

        // 옵션 Mute 상태와 무관하게 일시적으로 SFX를 차단/복원한다.
        // 옵션값을 덮어쓰지 않으므로 저장되지 않는다.
        public void SuppressSFX(bool suppress)
        {
            _isSfxSuppressed = suppress;
            foreach (var source in _activeSfxSources)
            {
                if (source == null)
                    continue;
                source.mute = IsSfxSilenced;
            }
        }

        private void Update()
        {
            // 재생이 끝난 SFX Source들을 풀로 반환
            for (int i = _activeSfxSources.Count - 1; i >= 0; i--)
            {
                var source = _activeSfxSources[i];
                if (source == null)
                {
                    _activeSfxSources.RemoveAt(i);
                    continue;
                }

                if (!source.isPlaying)
                {
                    ReturnSfxSource(source);
                    _activeSfxSources.RemoveAt(i);
                }
            }
        }

        private void ReturnSfxSource(AudioSource source)
        {
            if (source == null)
                return;

            ResetSfxSource(source);
            ConfigureSfxSourceDefaults(source);
            source.gameObject.SetActive(false);
            _sfxPool.Push(source);
            _sfxBaseVolumes[source] = 1f;
        }

        private void ConfigureSfxSourceDefaults(AudioSource source)
        {
            if (source == null)
                return;

            source.playOnAwake = false;
            source.loop = false;
            source.mute = IsSfxSilenced;
        }

        private void ResetSfxSource(AudioSource source)
        {
            if (source == null)
                return;

            if (source.isPlaying || source.clip != null)
            {
                source.Stop();
            }

            if (source.clip != null)
            {
                source.time = 0f;
                source.clip = null;
            }
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
