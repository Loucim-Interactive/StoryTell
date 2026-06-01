using System.Collections;
using System.Collections.Generic;
using FirstPersonControllerSystem.Scripts.ControllerSys;
using InmersionSystem.Ambient.Configs;
using UnityEngine;

namespace InmersionSystem.Ambient.Effects {
    public class FootstepsEffect : AmbientEffect<FootstepsConfig>
    {
        [Header("References")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private PlayerMotorScript playerMotor; // CHORE: REFACTOR AND ELIMINATE DEPENDENCY
        [SerializeField] private Transform groundCheckOrigin;

        [Header("Ground Detection")]
        [SerializeField] private float raycastDistance = 1.1f;
        [SerializeField] private LayerMask groundLayers;

        private Coroutine _cycle;
        private bool _isSprinting;

        // Physic Material name → ESurfaceType mapping
        private static readonly Dictionary<string, InmersiveElements.ESurfaceType> MaterialSurfaceMap = new()
        {
            { "Rocks",    InmersiveElements.ESurfaceType.Rocks    },
            { "Grass",    InmersiveElements.ESurfaceType.Grass    },
            { "Mud",      InmersiveElements.ESurfaceType.Mud      },
            { "Snow",     InmersiveElements.ESurfaceType.Snow     },
            { "Concrete", InmersiveElements.ESurfaceType.Concrete },
            { "Metal",    InmersiveElements.ESurfaceType.Metal    },
            { "Wood",     InmersiveElements.ESurfaceType.Wood     },
        };

        private void Start() {
            OnEffectStart();
        }
        
        public override void OnEffectStart()
        {
            if (config == null) return;
            _cycle = StartCoroutine(FootstepCycleRoutine());
        }

        public override void OnEffectStop()
        {
            if (_cycle != null) StopCoroutine(_cycle);
        }

        private IEnumerator FootstepCycleRoutine()
        {
            while (true)
            {
                // Only play when grounded and moving
                if (playerMotor.IsGrounded && playerMotor.IsMoving) {
                    var surface = DetectSurface();
                    PlayFootstep(surface);
                }

                var interval = _isSprinting 
                    ? config.sprintStepInterval 
                    : config.walkStepInterval;

                yield return new WaitForSeconds(interval);
            }
        }

        private InmersiveElements.ESurfaceType DetectSurface()
        {
            var origin = groundCheckOrigin 
                ? groundCheckOrigin.position 
                : transform.position;
            
            if (!Physics.Raycast(origin, Vector3.down, out var hit, raycastDistance, groundLayers)) return InmersiveElements.ESurfaceType.Grass; // default fallback
            var physicMaterial = hit.collider.sharedMaterial;
            if (!physicMaterial) return InmersiveElements.ESurfaceType.Grass;

            // Strip "(Instance)" suffix Unity sometimes adds
            var matName = physicMaterial.name.Replace(" (Instance)", "");
            return MaterialSurfaceMap.GetValueOrDefault(matName, InmersiveElements.ESurfaceType.Grass);
        }

        private void PlayFootstep(InmersiveElements.ESurfaceType surface)
        {
            foreach (var set in config.surfaces)
            {
                if (set.surface != surface) continue;
                if (set.clips == null || set.clips.Length == 0) return;

                var clip = set.clips[Random.Range(0, set.clips.Length)];
                audioSource.PlayOneShot(clip, set.volume);
                return;
            }

            // No matching surface set found — silent
        }
    }
}