using DialogueSystem.Scripts;
using Systems.DialogueSystem.Scripts;
using UnityEngine;

namespace Systems.FirstPersonControllerSystem.Scripts.ControllerSys
{
    [DisallowMultipleComponent]
    public class NPCAnimatorScript : MonoBehaviour
    {
        public enum AnimationState
        {
            Idle, Walking, Running, Crouching, CrouchWalking, ArmedIdle, ArmedRunning, Talking, Custom
        }

        [Header("References")]
        [SerializeField] private Animator animator;
        [SerializeField] private SpeakerScript speaker;
        [Tooltip("The NPC root whose movement drives walking/running. Defaults to this transform.")]
        [SerializeField] private Transform movementRoot;

        [Header("Automatic Animation")]
        [Min(0.001f)] [SerializeField] private float walkingSpeedThreshold = 0.05f;
        [Min(0.001f)] [SerializeField] private float runningSpeedThreshold = 2.5f;
        [SerializeField] private bool crouching;
        [SerializeField] private bool armed;
        [SerializeField] private bool grounded = true;
        [Tooltip("Use the talking animation while this NPC owns a conversation, including choices.")]
        [SerializeField] private bool animateConversations = true;
        [Min(0f)] [SerializeField] private float transitionDuration = 0.15f;

        [Header("Fixed Animation")]
        [Tooltip("Overrides movement and conversation animation. The chosen animation still plays normally.")]
        [SerializeField] private bool lockAnimation;
        [SerializeField] private AnimationState fixedAnimation = AnimationState.Crouching;
        [Tooltip("For Custom: full state path on layer 0, for example Base Layer.My Animation. Add the state to NPCAnimController first.")]
        [SerializeField] private string customStateName;

        private static readonly string[] StateNames =
        {
            "Base Layer.Soldado Rig|Idle", "Base Layer.Soldado Rig|Walking Cycle",
            "Base Layer.Soldado Rig|Running Cycle", "Base Layer.Soldado Rig|Crouch Idle",
            "Base Layer.Soldado Rig|Crouch Walking", "Base Layer.Soldado Rig|IdleArmed",
            "Base Layer.Soldado Rig|Running Cycle Armed", "Base Layer.Soldado Rig|Talking"
        };
        private static readonly string[] ParameterNames =
            { "isGrounded", "isWalking", "isRunning", "isCrouching", "isArmed", "isTalking" };
        private readonly int[] _parameterHashes = new int[ParameterNames.Length];
        private readonly bool[] _hasParameter = new bool[ParameterNames.Length];
        private RuntimeAnimatorController _controller;
        private Vector3 _previousPosition;
        private int _lastStateHash;
        private bool _hasState;

        public bool IsAnimationLocked => lockAnimation;
        public void SetCrouching(bool value) => crouching = value;
        public void SetArmed(bool value) => armed = value;
        public void SetGrounded(bool value) => grounded = value;
        public void LockAnimation(AnimationState state) { fixedAnimation = state; lockAnimation = true; }
        public void UnlockAnimation() => lockAnimation = false;
        public void LockCustomAnimation(string stateName)
        {
            customStateName = stateName;
            LockAnimation(AnimationState.Custom);
        }

        private void OnEnable()
        {
            if (!animator) animator = GetComponentInChildren<Animator>(true);
            if (!speaker) speaker = GetComponentInParent<SpeakerScript>();
            if (!movementRoot) movementRoot = transform;
            _previousPosition = movementRoot.position;
            _controller = null;
            _hasState = false;
            if (!animator) Debug.LogWarning("NPCAnimatorScript needs an Animator on this NPC or its children.", this);
        }

        private void OnValidate()
        {
            walkingSpeedThreshold = Mathf.Max(0.001f, walkingSpeedThreshold);
            runningSpeedThreshold = Mathf.Max(walkingSpeedThreshold, runningSpeedThreshold);
        }

        private void Update()
        {
            Vector3 displacement = movementRoot.position - _previousPosition;
            _previousPosition = movementRoot.position;
            displacement.y = 0f;
            float speed = Time.deltaTime > 0f ? displacement.magnitude / Time.deltaTime : 0f;
            if (!animator || !animator.isActiveAndEnabled || !animator.runtimeAnimatorController) return;
            if (_controller != animator.runtimeAnimatorController) CacheParameters();

            AnimationState state = lockAnimation ? fixedAnimation : ChooseAutomaticState(speed);
            SetParameter(0, grounded);
            SetParameter(1, state == AnimationState.Walking || state == AnimationState.CrouchWalking);
            SetParameter(2, state == AnimationState.Running || state == AnimationState.ArmedRunning);
            SetParameter(3, state == AnimationState.Crouching || state == AnimationState.CrouchWalking);
            SetParameter(4, state == AnimationState.ArmedIdle || state == AnimationState.ArmedRunning);
            SetParameter(5, state == AnimationState.Talking);

            string stateName = state == AnimationState.Custom ? customStateName : StateNames[(int)state];
            int hash = Animator.StringToHash(stateName ?? "");
            if (_hasState && hash == _lastStateHash) return;
            bool firstState = !_hasState;
            _lastStateHash = hash;
            _hasState = true;
            if (string.IsNullOrWhiteSpace(stateName) || !animator.HasState(0, hash))
            {
                Debug.LogWarning($"NPC Animator is missing state '{stateName}'. Assign NPCAnimController or add the custom state on layer 0.", this);
                return;
            }
            // NPCAnimController has no automatic transitions: this script owns state selection.
            // Enter the starting pose immediately, then blend only when the selection changes.
            if (firstState || transitionDuration <= 0f) animator.Play(hash, 0, 0f);
            else animator.CrossFadeInFixedTime(hash, transitionDuration, 0, 0f);
        }

        private AnimationState ChooseAutomaticState(float speed)
        {
            bool moving = speed > walkingSpeedThreshold;
            if (crouching) return moving ? AnimationState.CrouchWalking : AnimationState.Crouching;
            if (armed) return moving ? AnimationState.ArmedRunning : AnimationState.ArmedIdle;
            if (moving) return speed >= runningSpeedThreshold ? AnimationState.Running : AnimationState.Walking;
            var manager = DialogueManagerScript.Instance;
            return animateConversations && speaker && manager && manager.IsConversationOwnedBy(speaker)
                ? AnimationState.Talking : AnimationState.Idle;
        }

        private void CacheParameters()
        {
            _controller = animator.runtimeAnimatorController;
            _hasState = false;
            for (int i = 0; i < ParameterNames.Length; i++)
            {
                _parameterHashes[i] = Animator.StringToHash(ParameterNames[i]);
                _hasParameter[i] = false;
                foreach (var parameter in animator.parameters)
                    if (parameter.nameHash == _parameterHashes[i] && parameter.type == AnimatorControllerParameterType.Bool)
                        _hasParameter[i] = true;
            }
        }

        private void SetParameter(int index, bool value)
        {
            if (_hasParameter[index]) animator.SetBool(_parameterHashes[index], value);
        }
    }
}
