using UnityEngine;

namespace Dypsloom.CuteBoars.Script
{
    /// <summary>
    /// Component to showcase the animations.
    /// </summary>
    public class AnimationShowcase : MonoBehaviour
    {
        [Tooltip("Animators to showcase")]
        [SerializeField] protected BoarAnimations[] m_Boars;

        public void PlayIdle()
        {
            for (int i = 0; i < m_Boars.Length; i++) {
                if(m_Boars[i] == null){ continue; }
                m_Boars[i].ReturnToDefault();
                m_Boars[i].PlayIdle();
            }
        }
        
        public void PlayWalk()
        {
            for (int i = 0; i < m_Boars.Length; i++) {
                if(m_Boars[i] == null){ continue; }
                m_Boars[i].ReturnToDefault();
                m_Boars[i].PlayWalk();
            }
        }
        
        public void PlayRun()
        {
            for (int i = 0; i < m_Boars.Length; i++) {
                if(m_Boars[i] == null){ continue; }
                m_Boars[i].ReturnToDefault();
                m_Boars[i].PlayRun();
            }
        }
        
        public void PlaySleep()
        {
            for (int i = 0; i < m_Boars.Length; i++) {
                if(m_Boars[i] == null){ continue; }
                m_Boars[i].ReturnToDefault();
                m_Boars[i].PlaySleep();
            }
        }
        
        public void PlayAttack()
        {
            for (int i = 0; i < m_Boars.Length; i++) {
                if(m_Boars[i] == null){ continue; }
                m_Boars[i].ReturnToDefault();
                m_Boars[i].PlayAttack();
            }
        }
        
        public void PlayDamaged()
        {
            for (int i = 0; i < m_Boars.Length; i++) {
                if(m_Boars[i] == null){ continue; }
                m_Boars[i].ReturnToDefault();
                m_Boars[i].PlayDamaged();
            }
        }
        
        public void PlayDeath()
        {
            for (int i = 0; i < m_Boars.Length; i++) {
                if(m_Boars[i] == null){ continue; }
                m_Boars[i].ReturnToDefault();
                m_Boars[i].PlayDeath();
            }
        }
        
        public void PlayEat()
        {
            for (int i = 0; i < m_Boars.Length; i++) {
                if(m_Boars[i] == null){ continue; }
                m_Boars[i].ReturnToDefault();
                m_Boars[i].PlayEat();
            }
        }

        /// <summary>
        /// Set the anim trigger for all the animators.
        /// </summary>
        /// <param name="animHash">The anim hash</param>
        public void SetAnimTriggerForAll(int animHash)
        {
            for (int i = 0; i < m_Boars.Length; i++) {
                if(m_Boars[i] == null){ continue; }
                m_Boars[i].ReturnToDefault();
                m_Boars[i].PlayIdle();
            }
        }
    }
}
