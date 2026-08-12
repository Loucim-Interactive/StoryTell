using UnityEngine;
using UnityEngine.InputSystem;
using System;

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
                if (Input.GetKeyDown(navigateChoicesForwardKey))
                    direction++;

                if (Input.GetKeyDown(navigateChoicesBackwardKey))
                    direction--;
            }

            if (direction == 0)
                return;

            _previousChosenIndex = _currentChosenIndex;
            _currentChosenIndex += direction;

            ClampChoices();
            SelectionChanged?.Invoke(_currentChosenIndex);
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
