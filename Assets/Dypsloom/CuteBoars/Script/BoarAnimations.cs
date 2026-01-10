using UnityEngine;

namespace Dypsloom.CuteBoars.Script
{
    using System;
    using System.Collections;

    /// <summary>
    /// The boar animations controls animations and effects.
    /// </summary>
    public class BoarAnimations : MonoBehaviour
    {
        [Tooltip("The boar Animator")]
        [SerializeField] protected Animator m_Animator;
        [Tooltip("The body to hide when dead")]
        [SerializeField] protected GameObject m_Body;
        [Tooltip("The VFX when running.")]
        [SerializeField] protected GameObject m_RunEffect;
        [Tooltip("The VFX when sleeping.")]
        [SerializeField] protected GameObject m_SleepEffect;
        [Tooltip("The VFX when dead, to hide the model swap")]
        [SerializeField] protected GameObject m_DeathEffect;
        [Tooltip("The dead body model swap")]
        [SerializeField] protected GameObject m_DeadBody;

        private static readonly int s_Idle = Animator.StringToHash("Idle");
        private static readonly int s_Walk = Animator.StringToHash("Walk");
        private static readonly int s_Run = Animator.StringToHash("Run");
        private static readonly int s_Sleep = Animator.StringToHash("Sleep");
        private static readonly int s_Attack = Animator.StringToHash("Attack");
        private static readonly int s_Damaged = Animator.StringToHash("Damaged");
        private static readonly int s_Death = Animator.StringToHash("Death");
        private static readonly int s_Eat = Animator.StringToHash("Eat");

        /// <summary>
        /// Check that the boar is set up correctly.
        /// </summary>
        private void Awake()
        {
            if (m_Animator == null) {
                m_Animator = GetComponent<Animator>();
            }

            if (m_Body == null) {
                Debug.LogError("The Body field on the boar is not set.");
            }
            if (m_RunEffect == null) {
                Debug.LogError("The Run effect field on the boar is not set.");
            }
            if (m_SleepEffect == null) {
                Debug.LogError("The Sleep effect field on the boar is not set.");
            }
            if (m_DeathEffect == null) {
                Debug.LogError("The Death effect field on the boar is not set.");
            }
            if (m_DeadBody == null) {
                Debug.LogError("The Dead Body field on the boar is not set.");
            }

            ReturnToDefault();
        }

        /// <summary>
        /// Reset the boar to default.
        /// </summary>
        public void ReturnToDefault()
        {
            m_Body.SetActive(true);
            m_RunEffect.SetActive(false);
            m_DeathEffect.SetActive(false);
            m_DeadBody.SetActive(false);
            m_SleepEffect.SetActive(false);
        }

        public void PlayIdle()
        {
            SetAnimTriggerForAll(s_Idle);
        }
        
        public void PlayWalk()
        {
            SetAnimTriggerForAll(s_Walk);
        }
        
        public void PlayRun()
        {
            SetAnimTriggerForAll(s_Run);
            m_RunEffect.SetActive(true);
        }
        
        public void PlaySleep()
        {
            SetAnimTriggerForAll(s_Sleep);
            m_SleepEffect.SetActive(true);
        }
        
        public void PlayAttack()
        {
            SetAnimTriggerForAll(s_Attack);
        }
        
        public void PlayEat()
        {
            SetAnimTriggerForAll(s_Eat);
        }
        
        public void PlayDamaged()
        {
            SetAnimTriggerForAll(s_Damaged);
        }
        
        public void PlayDeath()
        {
            SetAnimTriggerForAll(s_Death);
        }

        /// <summary>
        /// Function called by the animation event.
        /// </summary>
        public void DeathEffect()
        {
            m_DeathEffect.SetActive(true);
        }
        
        /// <summary>
        /// Function called by the animation event.
        /// </summary>
        public void DeathModelSwap()
        {
            m_DeadBody.SetActive(true);
            m_Body.SetActive(false);
        }

        /// <summary>
        /// Set the anim trigger for all the animators.
        /// </summary>
        /// <param name="animHash">The anim hash</param>
        public void SetAnimTriggerForAll(int animHash)
        {
            m_Animator.SetTrigger(animHash);
        }
    }
}
