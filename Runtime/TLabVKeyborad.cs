using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace TLab.VKeyborad
{
    [RequireComponent(typeof(AudioSource))]
    public class TLabVKeyborad : MonoBehaviour
    {
        [Header("Key Audio")]
        [SerializeField] private AudioClip m_keyStroke;

        [Header("Key BOX")]
        [SerializeField] private GameObject m_keyBOX;
        [SerializeField] private GameObject m_romajiBOX;
        [SerializeField] private GameObject m_symbolBOX;
        [SerializeField] private GameObject m_operatorBOX;

        [Header("Settings")]
        [SerializeField] private bool m_hideOnStart = false;

        [Header("Transform Anchor")]
        [SerializeField] private Transform m_anchor;

        [Header("Callback")]
        [SerializeField] private UnityEvent<bool> m_onVisibilityChanged;

#if UNITY_EDITOR
        [Header("Keyborad Visual (Editor Only)")]
        public Sprite m_keyImage;
        public Material m_keyMat;
        public Color m_keyImageColor = Color.white;

        public ColorBlock m_keyColorBlock;
#endif

        [SerializeField, HideInInspector]
        private InputFieldBase m_inputFieldBase;

        private bool m_mobile = false;
        private bool m_shift = false;
        private bool m_initialized = false;

        private float m_inertia = 0.0f;

        private AudioSource m_audioSource;

        private List<string> m_keyBuffer = new List<string>();
        private List<SKeyCode> m_sKeyBuffer = new List<SKeyCode>();

        private const float IMMEDIATELY = 0f;
        private const float INERTIA = 0.1f;

        public bool mobile
        {
            get
            {
                m_mobile = Platform.mobile;
                return m_mobile;
            }
        }

        public bool shift => m_shift;

        public bool initialized => m_initialized;

        public bool isVisible => m_keyBOX.activeSelf;

        public InputFieldBase inputFieldBase => m_inputFieldBase;

        private string THIS_NAME => "[ " + this.GetType() + "] ";

        public void SwitchInputField(InputFieldBase inputFieldBase) => m_inputFieldBase = inputFieldBase;

        public void OnKeyPress(string key) => m_keyBuffer.Add(key);

        public void OnSKeyPress(SKeyCode sKey) => m_sKeyBuffer.Add(sKey);

        public void SetTransform(Vector3 position, Vector3 target, Vector3 worldUp)
        {
            if (m_anchor == null)
                m_anchor = this.transform;

            m_anchor.position = position;

            m_anchor.LookAt(target, worldUp);
        }

        public void SetVisibility(bool active)
        {
            if (active == isVisible)
                return;

            m_keyBOX.SetActive(active);

            m_onVisibilityChanged.Invoke(active);
        }

        public void SwitchVisibility() => SetVisibility(!isVisible);

        public void Hide(bool active) => SetVisibility(!active);

        public void Show(bool active) => SetVisibility(active);

        public void SetUp()
        {
            if (m_initialized)
            {
                Debug.LogError(THIS_NAME + "keyborad has already been initialised");
                return;
            }

            m_mobile = Platform.mobile;

            if (m_mobile)
            {
                // Can refer to a parent hierarchy if it is inactive but active itself ?

                m_operatorBOX.SetActive(true);
                m_romajiBOX.SetActive(true);
                m_symbolBOX.SetActive(true);

                foreach (var key in KeyBase.Keys(m_keyBOX))
                    key.keyborad = this;

                m_operatorBOX.SetActive(true);
                m_romajiBOX.SetActive(true);
                m_symbolBOX.SetActive(false);
            }
            else
            {
                SetVisibility(false);
            }

            if (m_audioSource == null)
                m_audioSource = GetComponent<AudioSource>();

            m_initialized = true;
        }

        private void Start()
        {
            if (!m_initialized)
                SetUp();

            if (m_hideOnStart)
                SetVisibility(false);
        }

        private void Update()
        {
            if (!m_initialized)
            {
                return;
            }

            if (m_mobile)
            {
                foreach (SKeyCode sKey in m_sKeyBuffer)
                {
                    switch (sKey)
                    {
                        case SKeyCode.BACKSPACE:
                            m_inputFieldBase?.OnBackSpacePressed();
                            break;
                        case SKeyCode.RETURN:
                            m_inputFieldBase?.OnEnterPressed();
                            break;
                        case SKeyCode.SHIFT:
                            m_shift = !m_shift;

                            foreach (var key in KeyBase.Keys(m_keyBOX))
                                key.OnShift();

                            m_inputFieldBase?.OnShiftPressed();
                            break;
                        case SKeyCode.SPACE:
                            m_inputFieldBase?.OnSpacePressed();
                            break;
                        case SKeyCode.TAB:
                            m_inputFieldBase?.OnTabPressed();
                            break;
                        case SKeyCode.SYMBOL:
                            bool active = m_romajiBOX.activeSelf;
                            m_romajiBOX.SetActive(!active);
                            m_symbolBOX.SetActive(active);
                            m_inputFieldBase?.OnSymbolPressed();
                            break;
                    }

                    AudioHandler.ShotAudio(m_audioSource, m_keyStroke, IMMEDIATELY);
                }

                foreach (string key in m_keyBuffer)
                {
                    m_inputFieldBase?.OnKeyPressed(key);

                    AudioHandler.ShotAudio(m_audioSource, m_keyStroke, IMMEDIATELY);
                }

                m_sKeyBuffer.Clear();
                m_keyBuffer.Clear();
            }
            else
            {
                m_inertia += Time.deltaTime;

                if (Input.anyKey)
                {
                    string inputString = Input.inputString;

                    if (Input.GetKeyDown(KeyCode.Return))
                    {
                        m_inputFieldBase?.OnEnterPressed();
                    }
                    else if (Input.GetKeyDown(KeyCode.Tab))
                    {
                        m_inputFieldBase?.OnTabPressed();
                    }
                    else if (Input.GetKeyDown(KeyCode.Space))
                    {
                        m_inputFieldBase?.OnSpacePressed();
                    }
                    else if (Input.GetKey(KeyCode.Backspace) && m_inertia > INERTIA)
                    {
                        m_inputFieldBase?.OnBackSpacePressed();
                        m_inertia = 0.0f;
                    }
                    else if (inputString != "" && inputString != "")
                    {
                        m_inputFieldBase?.OnKeyPressed(inputString);
                    }
                    else if (Input.GetMouseButtonDown(1))
                    {
                        m_inputFieldBase?.OnKeyPressed(GUIUtility.systemCopyBuffer);
                    }
                }
            }
        }

#if UNITY_EDITOR
        public void Attach<T, K>(GameObject root) where T : Component where K : Component
        {
            var ks = root.GetComponentsInChildren<K>();
            foreach (K k in ks)
            {
                var t = k.GetComponent<T>();

                if (t == null)
                    k.gameObject.AddComponent<T>();
            }
        }

        public void SetUpKey()
        {
            m_romajiBOX.SetActive(true);
            m_symbolBOX.SetActive(true);
            m_operatorBOX.SetActive(true);

            Attach<Key, Button>(m_romajiBOX);
            Attach<Key, Button>(m_symbolBOX);
            Attach<SKey, Button>(m_operatorBOX);

            foreach (var key in KeyBase.Keys(m_keyBOX))
            {
                key.Setup();
                UnityEditor.EditorUtility.SetDirty(key);
            }

            m_romajiBOX.SetActive(true);
            m_symbolBOX.SetActive(false);
            m_operatorBOX.SetActive(true);
        }

        public void SetUpKeyVisual()
        {
            m_romajiBOX.SetActive(true);
            m_symbolBOX.SetActive(true);
            m_operatorBOX.SetActive(true);

            foreach (var key in KeyBase.Keys(m_keyBOX))
            {
                var button = key.GetComponent<Button>();
                button.colors = m_keyColorBlock;

                var image = key.GetComponent<Image>();
                image.color = m_keyImageColor;
                image.sprite = m_keyImage;
                image.material = m_keyMat;

                UnityEditor.EditorUtility.SetDirty(image);
                UnityEditor.EditorUtility.SetDirty(button);
            }

            m_romajiBOX.SetActive(true);
            m_symbolBOX.SetActive(false);
            m_operatorBOX.SetActive(true);
        }
#endif
    }
}
