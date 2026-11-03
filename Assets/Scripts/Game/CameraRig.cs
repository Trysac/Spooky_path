using UnityEngine;

namespace SpookyPath.Game
{
    /// <summary>
    /// A high three-quarter camera that trails the explorer, looks slightly ahead of where they
    /// walk and shakes on heavy hits. North (away from the camera) is "up" on the screen.
    /// </summary>
    public sealed class CameraRig : MonoBehaviour
    {
        [System.NonSerialized] public Vector3 Offset = new Vector3(0f, 10.5f, -13f);
        [System.NonSerialized] public float Smoothing = 6f;

        Transform target;
        Vector3 focus;
        Vector3 lead;
        float shake;

        public void Follow(Transform newTarget, bool snap)
        {
            target = newTarget;
            if (snap && target != null)
            {
                focus = target.position;
                lead = Vector3.zero;
                Place();
            }
        }

        /// <summary>Frames a fixed point (title screen fly-over, screenshots).</summary>
        public void LookAtPoint(Vector3 point, Vector3 offset)
        {
            target = null;
            focus = point;
            transform.position = point + offset;
            transform.rotation = Quaternion.LookRotation(point - transform.position, Vector3.up);
        }

        public void Shake(float amount) => shake = Mathf.Max(shake, amount);

        public void SetLead(Vector3 velocity) => lead = Vector3.Lerp(lead, Vector3.ClampMagnitude(velocity * 0.35f, 2.5f), Time.unscaledDeltaTime * 2f);

        void LateUpdate()
        {
            if (target == null)
            {
                return;
            }
            focus = Vector3.Lerp(focus, target.position + lead, 1f - Mathf.Exp(-Smoothing * Time.unscaledDeltaTime));
            Place();
        }

        void Place()
        {
            // Levels lead north, so the camera shows a little more of what lies ahead.
            Vector3 anchor = new Vector3(focus.x, Mathf.Max(focus.y, -1f), focus.z + 2.5f);
            Vector3 jitter = Vector3.zero;
            if (shake > 0f)
            {
                jitter = Random.insideUnitSphere * shake * 0.4f;
                shake = Mathf.Max(0f, shake - Time.unscaledDeltaTime * 2.5f);
            }
            transform.position = anchor + Offset + jitter;
            transform.rotation = Quaternion.LookRotation(anchor + Vector3.up * 1f - (anchor + Offset), Vector3.up);
        }
    }
}
