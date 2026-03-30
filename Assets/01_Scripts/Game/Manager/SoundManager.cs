using Cumic;
using System.Collections.Generic;
using System.Threading;
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

      private readonly Stack<AudioSource> _sfxPool = new();
      private readonly List<AudioSource> _activeSfxSources = new();
      private readonly Dictionary<AudioSource, float> _sfxBaseVolumes = new();
      private SoundDB _soundDB;

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
         if (sfxRoot == null)
         {
            sfxRoot = new GameObject("SFX_Root").transform;
            sfxRoot.SetParent(transform);
         }

         bgmVolume = bgmSoundSource.volume;
         sfxVolume = sfxSoundSourcePrefab.volume;
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

      public void MuteBGM()
      {
         IsBgmMuted = !IsBgmMuted;
         if (bgmSoundSource != null)
         {
            bgmSoundSource.mute = IsBgmMuted;
         }
      }

      #endregion

      #region SFX Management

      public void PlaySFX(SoundType type)
      {
         var db = _GetSoundDB();
         if (db == null)
            return;
         var data = db.GetSoundData(type);
         if (data != null)
            PlaySFX(data);
      }

      public void PlaySFX(string id)
      {
         var db = _GetSoundDB();
         if (db == null)
            return;
         var data = db.GetSoundData(id);
         if (data != null)
            PlaySFX(data);
      }

      public void PlaySFXByClipId(string clipId)
      {
         var db = _GetSoundDB();
         if (db == null)
            return;
         var data = db.GetSoundDataByClipId(clipId);
         if (data != null)
            PlaySFX(data);
      }

      public void PlaySFX(SoundData data)
      {
         if (data == null || data.Clip == null)
            return;
         if (IsSfxMuted)
            return;

         var existing = FindActiveSfxSourceByClip(data.Clip);
         if (existing != null)
         {
            if (sfxDuplicatePolicy == SfxDuplicatePolicy.IgnoreNew)
               return;

            if (sfxDuplicatePolicy == SfxDuplicatePolicy.RestartExisting)
            {
               ApplySfxSettings(existing, data);
               existing.Stop();
               existing.Play();
               return;
            }
         }

         AudioSource source = GetSfxSource();
         ApplySfxSettings(source, data);
         source.Play();

         _activeSfxSources.Add(source);
      }

      private void ApplySfxSettings(AudioSource source, SoundData data)
      {
         source.clip = data.Clip;
         _sfxBaseVolumes[source] = data.Volume;
         source.volume = data.Volume * sfxVolume;
         source.mute = IsSfxMuted;
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
         var previous = sfxVolume;
         sfxVolume = Mathf.Clamp01(volume);

         if (Mathf.Approximately(previous, sfxVolume))
            return;

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
      }

      public void MuteSFX(bool mute)
      {
         IsSfxMuted = mute;
         foreach (var source in _activeSfxSources)
         {
            source.mute = IsSfxMuted;
         }
      }

      public void MuteSFX()
      {
         IsSfxMuted = !IsSfxMuted;
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
         _sfxBaseVolumes[source] = 1f;
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
