using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;

namespace Systems.DecisionSystem.UI
{
    public class DecisionManagerScript : MonoBehaviour
    {
        [Header("Decision Manager Settings")]
        [SerializeField] private bool useActions;

        [SerializeField] private InputActionReference navigateChoicesForward;
        [SerializeField] private InputActionReference navigateChoicesBackwards;

        [SerializeField] private KeyCode navigateChoicesForwardKey = KeyCode.Alpha1;
        [SerializeField] private KeyCode navigateChoicesBackwardKey = KeyCode.Alpha2;
        [SerializeField] private InputActionReference submitChoice;
        [SerializeField] private KeyCode submitChoiceKey = KeyCode.Return;

        // An optional external decision session (e.g. NPC dialogue). Radio input
        // continues to use the existing selection API when no session owns it.
        private UnityEngine.Object _owner;
        private MonoBehaviour _presenter;
        private Action<int> _onSelected;
        private int _presentedFrame;
        private float _submitReadyAt;
        public bool HasOwner => _owner != null;
        public bool HasPresenter => _presenter && _presenter.isActiveAndEnabled;
        public IReadOnlyList<string> Options { get; private set; }
        public int RequestVersion { get; private set; }
        public bool HasRequest => HasOwner && Options != null;

        public void RegisterPresenter(MonoBehaviour presenter) => _presenter = presenter;

        public void UnregisterPresenter(MonoBehaviour presenter)
        {
            if (_presenter != presenter) return;
            _presenter = null;
            Release(_owner);
        }

        public bool TryAcquire(UnityEngine.Object owner)
        {
            if (!owner || HasOwner || !HasPresenter) return false;
            _owner = owner;
            SetChoosing(false);
            return true;
        }

        public bool IsOwnedBy(UnityEngine.Object owner) => owner && _owner == owner;

        public bool Present(UnityEngine.Object owner, IReadOnlyList<string> labels, Action<int> onSelected)
        {
            if (!IsOwnedBy(owner) || !HasPresenter || HasRequest || labels == null || labels.Count == 0)
                return false;

            Options = new List<string>(labels);
            _onSelected = onSelected;
            RequestVersion++;
            _presentedFrame = Time.frameCount;
            _submitReadyAt=Time.unscaledTime+.18f;
            SetAmountChoices(Options.Count);
            SetInitialChosen(0);
            SetChoosing(false); // The presenter enables input once the list is bound.
            return true;
        }

        public void Submit(int index)
        {
            if (!HasRequest || !_isChoosing || Time.frameCount <= _presentedFrame ||Time.unscaledTime<_submitReadyAt ||
                index < 0 || index >= Options.Count) return;

            Action<int> callback = _onSelected;
            ClearRequest(); // Prevent duplicate/re-entrant submission before gameplay callbacks.
            callback?.Invoke(index);
        }

        public void Release(UnityEngine.Object owner)
        {
            if (_owner != owner) return;
            ClearRequest();
            _owner = null;
        }

        private void ClearRequest()
        {
            Options = null;
            _onSelected = null;
            SetChoosing(false);
            SetAmountChoices(0);
        }

        private void OnDisable() => Release(_owner);

        private int _previousChosenIndex;
        private int _currentChosenIndex;
        private int _amountChoices;
        private bool _isChoosing;

        public int PreviousIndex => _previousChosenIndex;
        public int CurrentIndex => _currentChosenIndex;
        public event Action<int> SelectionChanged;

        public void SetInitialChosen(int index)
        {
            _currentChosenIndex = index;
            _previousChosenIndex = index;
            ClampChoices();
            SelectionChanged?.Invoke(_currentChosenIndex);
        }

        public void SetAmountChoices(int amount)
        {
            _amountChoices = Mathf.Max(0, amount);
            ClampChoices();
        }

        public void SetChoosing(bool choosing)
        {
            _isChoosing = choosing;
        }

        private void Update()
        {
            if (!_isChoosing) return;

            if (_amountChoices <= 0)
                return;

            int direction = 0;
            var keyboard=Keyboard.current;

            if (useActions)
            {
                if (navigateChoicesForward != null &&
                    navigateChoicesForward.action.triggered)
                {
                    direction++;
                }

                if (navigateChoicesBackwards != null &&
                    navigateChoicesBackwards.action.triggered)
                {
                    direction--;
                }
            }
            else
            {
                if (keyboard!=null&&(keyboard.tabKey.wasPressedThisFrame&&!keyboard.shiftKey.isPressed||keyboard.downArrowKey.wasPressedThisFrame))
                    direction++;

                if (keyboard!=null&&(keyboard.tabKey.wasPressedThisFrame&&keyboard.shiftKey.isPressed||keyboard.upArrowKey.wasPressedThisFrame))
                    direction--;
            }

            if (direction != 0)
            {
                _previousChosenIndex = _currentChosenIndex;
                _currentChosenIndex += direction;
                ClampChoices();
                SelectionChanged?.Invoke(_currentChosenIndex);
            }

            if (HasRequest && ((submitChoice != null && submitChoice.action.triggered) ||
                keyboard!=null&&(keyboard.spaceKey.wasPressedThisFrame||keyboard.enterKey.wasPressedThisFrame)))
                Submit(_currentChosenIndex);
        }

        private void ClampChoices()
        {
            if (_amountChoices <= 0)
            {
                _currentChosenIndex = 0;
                _previousChosenIndex = 0;
                return;
            }

            // Valid indexes are 0 -> amountChoices - 1

            if (_currentChosenIndex >= _amountChoices)
                _currentChosenIndex = 0;

            if (_currentChosenIndex < 0)
                _currentChosenIndex = _amountChoices - 1;
        }
    }
}
