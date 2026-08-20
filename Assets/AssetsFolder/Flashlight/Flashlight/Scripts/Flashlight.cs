using UnityEngine;

namespace Game.PlayerHandItem
{
    public class Flashlight : MonoBehaviour
    {
        // 常量
        private const float ClosedOffsetZ = 0.5f;
        private const float TransitionDuration = 0.25f;

        // Shader 属性 ID
        private static readonly int RangeId = Shader.PropertyToID("_Range");
        private static readonly int AlphaId = Shader.PropertyToID("_Alpha");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        // 组件引用
        [SerializeField] private Light flashlight;
        [SerializeField] private MeshRenderer volumetricLight;

        // 参数范围
        [SerializeField] private Vector2 outerSpotRange = new(20f, 120f);
        [SerializeField] private Vector2 lightRange = new(3f, 10f);
        [SerializeField] private Vector2 alphaRange = new(0.1f, 0.2f);

        // 控制
        [SerializeField] private float sensitivity = 0.05f;
        [SerializeField] private float startAngle = 0.3F;

        [field: SerializeField] public bool IsOpen { get; set; }

        // 状态缓存
        private float _normalizedSpotAngle;
        private float _lightIntensity;
        private Vector3 _startPosition;

        // 音效
        [SerializeField] private AudioClip openCloseClip;

#if XIYU
        [VContainer.Inject] private AudioSystem.IAudioManager _audioManager;
#else
        [SerializeField] private AudioSource sfxSource;
#endif

#if PRIMETWEEN
        private PrimeTween.Sequence _sequence;
#endif

        // 暂时保留但未使用的字段，若确定无用可移除
        [SerializeField] private bool controlActive;
        [SerializeField] private bool applyLightColor;

        private void Start()
        {
            // 初始化手电筒参数
            flashlight.spotAngle = Mathf.Clamp(flashlight.spotAngle, outerSpotRange.x, outerSpotRange.y);
            flashlight.range = Mathf.Clamp(flashlight.range, lightRange.x, lightRange.y);

            _normalizedSpotAngle = Mathf.InverseLerp(outerSpotRange.x, outerSpotRange.y, flashlight.spotAngle);
            _lightIntensity = flashlight.intensity;

            if (!IsOpen)
                flashlight.intensity = 0f;

            if (applyLightColor)
                volumetricLight.sharedMaterial.SetColor(BaseColorId, flashlight.color);

            _startPosition = transform.localPosition;
            transform.localPosition = IsOpen ? _startPosition : _startPosition - Vector3.forward * ClosedOffsetZ;
        }

        private void Update()
        {
            if (MiddleButtonWasPressed())
            {
                IsOpen = !IsOpen;
                ChangeLight(IsOpen);
            }

            if (!IsOpen)
            {
                volumetricLight.sharedMaterial.SetFloat(AlphaId, 0f);
                return;
            }

            UpdateLightByScroll();
        }

        private void UpdateLightByScroll()
        {
            var deltaY = GetMouseScrollDeltaY();

            // 光线越散，照射距离近；光线越聚，照射距离远
            var t = _normalizedSpotAngle = Mathf.Clamp01(_normalizedSpotAngle + deltaY * sensitivity);

            var spotAngle = Mathf.Lerp(outerSpotRange.x, outerSpotRange.y, t);
            flashlight.spotAngle = spotAngle;

            var lightLength = Mathf.Lerp(lightRange.x, lightRange.y, 1f - t);
            flashlight.range = lightLength;

            // 调整体积光缩放与材质参数
            var localScale = volumetricLight.transform.localScale;
            volumetricLight.transform.localScale = new Vector3(localScale.x, localScale.y, lightLength);

            volumetricLight.sharedMaterial.SetFloat(RangeId, startAngle + Mathf.Lerp(outerSpotRange.x, outerSpotRange.y, t) * 2f * Mathf.Deg2Rad);
            volumetricLight.sharedMaterial.SetFloat(AlphaId, Mathf.Lerp(alphaRange.x, alphaRange.y, 1f - t));
        }

        public void ChangeLight(bool isOpen)
        {
            var endPosition = isOpen ? _startPosition : _startPosition - Vector3.forward * ClosedOffsetZ;

#if PRIMETWEEN
            if (_sequence.isAlive)
                _sequence.Stop();

            PrimeTween.Sequence.Create()
                .Chain(PrimeTween.Tween.LocalPosition(transform, endPosition, TransitionDuration, PrimeTween.Ease.InOutBack))
                .ChainCallback(this, AudioSoundTrigger, warnIfTargetDestroyed: false)
                .Chain(PrimeTween.Tween.LightIntensity(flashlight, isOpen ? _lightIntensity : 0f, TransitionDuration));
#else
            AudioSoundTrigger();
            flashlight.intensity = isOpen ? _lightIntensity : 0f;
            transform.localPosition = endPosition;
#endif
        }

        private void AudioSoundTrigger()
        {
#if XIYU
            _audioManager.PlayClip2D(AudioSystem.SoundType.SFX, openCloseClip);
#else
            if (sfxSource == null)
                sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.spatialBlend = 0;
            sfxSource.PlayOneShot(openCloseClip);
#endif
        }

        private static void AudioSoundTrigger(Flashlight self) => self.AudioSoundTrigger();

        private static bool MiddleButtonWasPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return UnityEngine.InputSystem.Mouse.current.middleButton.wasPressedThisFrame;
#else
            return Input.GetMouseButtonDown(2);
#endif
        }

        private static float GetMouseScrollDeltaY()
        {
#if ENABLE_INPUT_SYSTEM
            return UnityEngine.InputSystem.Mouse.current.scroll.ReadValue().y;
#else
            return Input.GetAxis("Mouse ScrollWheel");
#endif
        }
    }
}