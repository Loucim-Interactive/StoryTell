using System.Collections;

using UnityEngine;

namespace StoryTell.GameplayPath
{
    // An XZ polygon encloses the camp and the route, leaving room to explore nearby.
    public sealed class GameplayPathBoundary : MonoBehaviour
    {
        [SerializeField] private GameplayPathController controller;
        [SerializeField] private Vector2[] outline;
        [SerializeField] private BoxCollider[] barriers;
        [SerializeField, Min(1)] private float warningDistance = 5;
        private CharacterController character;

        private Vector3 lastSafe;
        private bool hasSafe, returning;
        private Coroutine turn;
        public Vector2[] Outline => outline;
        public BoxCollider[] Barriers => barriers;
        public bool Returning => returning;

        private void Start()
        {
            character = controller.Player.GetComponent<CharacterController>();

            foreach (var wall in barriers) wall.enabled = false;
            Evaluate();
        }
        private void LateUpdate() => Evaluate();
        public void Evaluate()
        {
            if (returning || controller.IsBusy || controller.Progress.Stage == PathStage.Complete) return;
            var position = controller.Player.position;
            bool inside = Contains(new Vector2(position.x, position.z), out int edge, out float distance);
            if (inside && distance > warningDistance + 2) { lastSafe = position; hasSafe = true; }
            if (!hasSafe || (inside && distance > warningDistance)) return;
            if (barriers[edge].enabled && inside) return;
            // A fallback for teleporting or crossing the edge during a long frame.
            if (!inside)
            {
                bool enabled = character.enabled; character.enabled = false; controller.Player.position = lastSafe; character.enabled = enabled;
            }
            if (!controller.BeginRouteReturn()) return;
            returning = true;
            controller.Notice(Reason(), 7);
            turn = StartCoroutine(ReturnInside(edge));
        }
        public bool Contains(Vector2 point, out int nearestEdge, out float distance)
        {
            bool inside = false; nearestEdge = 0; float minimum = float.PositiveInfinity;
            for (int i = 0, j = outline.Length - 1; i < outline.Length; j = i++)
            {
                Vector2 a = outline[j], b = outline[i], delta = b - a;
                Vector2 closest = a + delta * Mathf.Clamp01(Vector2.Dot(point - a, delta) / delta.sqrMagnitude);
                float sqr = (point - closest).sqrMagnitude;
                if (sqr < minimum) { minimum = sqr; nearestEdge = j; }
                if ((a.y > point.y) != (b.y > point.y) && point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x) inside = !inside;
            }
            distance = Mathf.Sqrt(minimum); return inside;
        }
        private string Reason() => controller.Progress.Stage switch
        {
            <= PathStage.ReturnEquipment => "Tengo que volver con Rubén y preparar mi equipo.",
            <= PathStage.ReturnLetter => "Rubén y los demás cuentan conmigo. Debo volver al campamento.",
            <= PathStage.CarryCrate => "Mi deber es llevar los suministros al puesto. No puedo desviarme.",
            _ => "Mis compañeros están en el frente. No puedo dejarlos solos."
        };
        private IEnumerator ReturnInside(int edge)
        {
            Vector3 direction = Vector3.ProjectOnPlane(lastSafe - controller.Player.position, Vector3.up);
            if (direction.sqrMagnitude < 0.1f) direction = -Vector3.ProjectOnPlane(controller.Player.forward, Vector3.up);
            direction.Normalize(); Quaternion from = controller.Player.rotation, to = Quaternion.LookRotation(direction);
            float elapsed = 0;
            try
            {
                while (elapsed < 1)
                {
                    elapsed += Time.unscaledDeltaTime;
                    controller.Player.rotation = Quaternion.Slerp(from, to, Mathf.SmoothStep(0, 1, elapsed));
                    if (character.enabled) character.Move((direction * 2 + Vector3.down * 2) * Time.unscaledDeltaTime);
                    yield return null;
                }
                barriers[edge].enabled = true;
                Physics.SyncTransforms();
            }
            finally { returning = false; controller.EndRouteReturn(); turn = null; }
        }
        private void OnDisable()
        {
            if (turn != null) StopCoroutine(turn);
            if (returning) { returning = false; controller.EndRouteReturn(); }
            turn = null;
        }
    }
}

