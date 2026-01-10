namespace Dypsloom.CuteBoars.Script
{
    using System;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// A component that rotates objects using a slider as input.
    /// </summary>
    public class RotationSlider : MonoBehaviour
    {
        [Tooltip("The slider with values -1 to 1")]
        [SerializeField] protected Slider m_Slider;
        [Tooltip("The max rotation speed when the slider is 1")]
        [SerializeField] protected float m_RotationSpeed;
        [Tooltip("The objects to rotate")]
        [SerializeField] protected Transform[] m_Objects;

        private Quaternion[] m_RotationsAtStart;

        /// <summary>
        /// Cache the start rotations.
        /// </summary>
        private void Awake()
        {
            m_RotationsAtStart = new Quaternion[m_Objects.Length];
            for (int i = 0; i < m_Objects.Length; i++) {
                if(m_Objects[i] == null){continue;}
                m_RotationsAtStart[i] = m_Objects[i].localRotation;
            }
        }

        /// <summary>
        /// Compute every frame.
        /// </summary>
        private void Update()
        {
            for (int i = 0; i < m_Objects.Length; i++) {
                var obj = m_Objects[i];
                if(obj == null){continue;}
                
                obj.Rotate(Vector3.up,m_RotationSpeed*Time.deltaTime*m_Slider.value);
            }
        }
        
        /// <summary>
        /// Reset the rotation to the start rotation.
        /// </summary>
        public void ResetRotation()
        {
            for (int i = 0; i < m_Objects.Length; i++) {
                var obj = m_Objects[i];
                if(obj == null){continue;}
                
                obj.localRotation = m_RotationsAtStart[i];
            }

            m_Slider.value = 0;
        }
    }
}