using UnityEngine;

namespace GrillaPurgatory.Game
{
    /// <summary>Brings the character on the loading screen to life: it bobs, glances about, and waves.</summary>
    public class CharacterShowcase : MonoBehaviour
    {
        public Transform head, leftFist, rightFist;

        Vector3 headRest;
        Vector3 bodyRest;

        void Awake()
        {
            headRest = head.localPosition;
            bodyRest = transform.position;
        }

        void Update()
        {
            float t = Time.time;

            // floats gently up and down (it has no legs to stand on)
            transform.position = bodyRest + Vector3.up * Mathf.Sin(t * 1.6f) * 0.04f;
            head.localRotation = Quaternion.Euler(Mathf.Sin(t * 0.9f) * 4f, Mathf.Sin(t * 0.55f) * 22f, Mathf.Sin(t * 0.7f) * 3f);

            // left fist rests by its side; the right one waves hello
            leftFist.position = transform.TransformPoint(new Vector3(-0.38f, 0.78f, 0.1f + Mathf.Sin(t * 1.6f) * 0.02f));
            float wave = Mathf.Sin(t * 5f) * 0.1f;
            rightFist.position = transform.TransformPoint(new Vector3(0.42f + wave, 1.5f, 0.18f));
        }
    }
}
