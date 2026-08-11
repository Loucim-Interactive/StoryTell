using System;
using Systems.DecisionSystem;
using Systems.DecisionSystem.UI;
using Systems.EventSystem.Scripts;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace Systems.WalkieSystem.Scripts
{
    public class WalkieTalkieScript : MonoBehaviour
    {
        [Header("Walkie Talkie Settings")]
        [SerializeField] private KeyCode _walkieTalkieKey = KeyCode.Q;
        [SerializeField] private InputActionReference _walkieTalkieSubmit;
        [SerializeField] private bool _toggleWalkie = false;
        [SerializeField] private float _enterWalkieCooldown = 0.7f;

        [Header("Walkie Talkie Refs")]
        [SerializeField] private WalkieInteractionMachine _stateMachine;
        [SerializeField] private WalkieTimer walkieChoiceTimer;
        [SerializeField] private GameObject walkieTalkie;

        private bool _walkieTalkieVisible;

        private void Awake()
        {
            _stateMachine = GetComponent<WalkieInteractionMachine>();

            if (!_stateMachine)
                _stateMachine = gameObject.AddComponent<WalkieInteractionMachine>();

            _walkieTalkieVisible = false;
            StoreWalkie();
        }

        private void OnEnable()
        {
            GameEventBus.Subscribe<WalkieDecisionAsset>(
                GameplayEvents.WalkieTalkieTrigger,
                HandleTrigger
            );
        }

        private void OnDisable()
        {
            GameEventBus.Unsubscribe<WalkieDecisionAsset>(
                GameplayEvents.WalkieTalkieTrigger,
                HandleTrigger
            );
        }

        private void HandleTrigger(WalkieDecisionAsset asset)
        {
            _stateMachine.SwitchState(
                WalkieInteractionMachine.WalkieInteractionStates.Awaiting
            );
        }

        private void Update()
        {
            if (_walkieTalkieVisible) {
                if (_stateMachine.IsAwaiting) {
                    _stateMachine.SwitchState(WalkieInteractionMachine.WalkieInteractionStates.Choosing);
                }
            }
            HandleInput();
        }

        private void HandleInput()
        {
            if (_toggleWalkie)
            {
                // Toggle mode:
                // Press once -> open.
                // Press again -> close.
                if (!Input.GetKeyDown(_walkieTalkieKey))
                    return;

                if (_walkieTalkieVisible)
                    ExitWalkie();
                else
                    EnterWalkie();

                return;
            }

            // Hold mode:
            // Key down -> open.
            // Key released -> close.
            if (Input.GetKeyDown(_walkieTalkieKey))
                EnterWalkie();

            if (Input.GetKeyUp(_walkieTalkieKey))
                ExitWalkie();
        }

        private void EnterWalkie()
        {
            if (_walkieTalkieVisible)
                return;

            _walkieTalkieVisible = true;
            TakeOutWalkie();
        }

        private void ExitWalkie()
        {
            if (!_walkieTalkieVisible)
                return;

            _walkieTalkieVisible = false;

            _stateMachine.SwitchState(
                WalkieInteractionMachine.WalkieInteractionStates.Finished
            );

            StoreWalkie();
        }

        private void TakeOutWalkie()
        {
            walkieTalkie.SetActive(true);
        }

        private void StoreWalkie()
        {
            walkieTalkie.SetActive(false);
        }
    }
}