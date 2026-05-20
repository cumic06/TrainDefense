using Sirenix.OdinInspector;
using UnityEngine;
using Cumic.Events;
using TrainDefense.Game.Events;

namespace TrainDefense.Game.Manager
{
    public class GameOverEffectController : MonoBehaviour
    {
        #region Variables

        private bool _isAnimating;
        private float _elapsedTime;
        private float _duration;

        private static readonly int _intensityId        = Shader.PropertyToID("_Intensity");
        private static readonly int _colorTintId        = Shader.PropertyToID("_ColorTint");
        private static readonly int _grayscaleStrengthId = Shader.PropertyToID("_GrayscaleStrength");

        #endregion

        #region Fields

        [Required]
        [SerializeField]
        private Material material;

        [BoxGroup("Effect")]
        [SerializeField]
        [Range(0f, 1f)]
        private float grayscaleStrength = 1f;

        [BoxGroup("Effect")]
        [SerializeField]
        private Color colorTint = new Color(0.3f, 0.3f, 0.3f, 1f);

        #endregion

        #region LifeCycle

        private void Start()
        {
            GameOverEffectFeature.Material = material;
            GameOverEffectFeature.IsActive = false;
            _SubscribeEvents();
        }

        private void OnDestroy()
        {
            _UnsubscribeEvents();
            _ResetEffect();
        }

        private void Update()
        {
            if (!_isAnimating)
                return;

            _elapsedTime += Time.unscaledDeltaTime;

            float intensity = Mathf.Clamp01(_elapsedTime / _duration);
            material.SetFloat(_intensityId, intensity);
        }

        #endregion

        #region Sub/UnSub

        private void _SubscribeEvents()
        {
            GameEventSystem.Subscribe<GameOverStartEvent>(_OnGameOverStart);
        }

        private void _UnsubscribeEvents()
        {
            GameEventSystem.Unsubscribe<GameOverStartEvent>(_OnGameOverStart);
        }

        #endregion

        private void _OnGameOverStart(GameOverStartEvent e)
        {
            if (material == null)
                return;

            _duration = e.Duration;
            _elapsedTime = 0f;
            _isAnimating = true;

            material.SetFloat(_grayscaleStrengthId, grayscaleStrength);
            material.SetColor(_colorTintId, colorTint);
            material.SetFloat(_intensityId, 0f);

            GameOverEffectFeature.IsActive = true;
        }

        private void _ResetEffect()
        {
            _isAnimating = false;
            _elapsedTime = 0f;
            GameOverEffectFeature.IsActive = false;

            if (material == null)
                return;

            material.SetFloat(_intensityId, 0f);
        }
    }
}
