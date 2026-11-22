using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GrillaPurgatory.Game
{
    /// <summary>
    /// The conveyor belt that spits out flashlights. Dispense() (the red button calls it) puts a new flashlight on the start
    /// of the belt; it rides along and slides off the end into the tray. If you grab it on the way, it's yours. There
    /// are never more than a handful lying about: the oldest one you aren't holding disappears.
    /// </summary>
    public class ConveyorDispenser : MonoBehaviour
    {
        public Flashlight flashlightPrefab;
        public Transform start, end;
        public Renderer belt;                 // its texture scrolls while it runs
        public float beltSpeed = 0.55f;
        public int maxFlashlights = 5;

        readonly List<Flashlight> spawned = new List<Flashlight>();
        Material beltMaterial;
        AudioSource audioSource;
        float runningUntil, scroll;

        static AudioClip motorClip;

        void Awake()
        {
            if (belt != null) beltMaterial = belt.material;
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f;
            audioSource.maxDistance = 25f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            if (motorClip == null) motorClip = MakeMotorClip();
        }

        void Update()
        {
            if (beltMaterial == null || Time.time > runningUntil) return;
            scroll = Mathf.Repeat(scroll + beltSpeed * Time.deltaTime / 0.5f, 1f);   // the stripes repeat every half metre
            beltMaterial.SetTextureOffset("_BaseMap", new Vector2(0f, -scroll));
        }

        public void Dispense()
        {
            if (flashlightPrefab == null || start == null || end == null) return;
            StartCoroutine(Carry());
        }

        IEnumerator Carry()
        {
            // keep the number of flashlights down
            spawned.RemoveAll(f => f == null);
            while (spawned.Count >= maxFlashlights)
            {
                var oldest = spawned.Find(f => !f.IsHeld);
                if (oldest == null) break;
                spawned.Remove(oldest);
                Destroy(oldest.gameObject);
            }

            Vector3 along = end.position - start.position;
            float distance = along.magnitude;
            Vector3 direction = along / distance;

            var light = Instantiate(flashlightPrefab, start.position, Quaternion.LookRotation(direction, Vector3.up));
            light.SetCarried(true);
            spawned.Add(light);

            float travelTime = distance / beltSpeed;
            runningUntil = Time.time + travelTime + 0.5f;
            audioSource.PlayOneShot(motorClip, 0.9f);

            float travelled = 0f;
            while (travelled < distance && light != null && !light.IsHeld)
            {
                float step = beltSpeed * Time.deltaTime;
                light.transform.position += direction * step;
                travelled += step;
                yield return null;
            }

            // off the end of the belt: it drops into the tray (unless you already took it)
            if (light != null && !light.IsHeld)
            {
                light.SetCarried(false);
                light.Body.linearVelocity = direction * 0.6f;
            }
        }

        // A clunk, then a motor hum, synthesised
        static AudioClip MakeMotorClip()
        {
            const int rate = 22050;
            int n = (int)(rate * 1.6f);
            var data = new float[n];
            var rng = new System.Random(4);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                float clunk = Mathf.Sin(2f * Mathf.PI * (120f - 60f * t) * t) * Mathf.Exp(-t * 18f) * 0.9f;
                float hum = (Mathf.Sin(2f * Mathf.PI * 70f * t) + 0.5f * Mathf.Sin(2f * Mathf.PI * 141f * t)) * 0.22f
                          * Mathf.Clamp01(t * 4f) * Mathf.Clamp01((1.6f - t) * 3f);
                float rattle = ((float)rng.NextDouble() * 2f - 1f) * 0.05f * Mathf.Clamp01(t * 4f);
                data[i] = Mathf.Clamp(clunk + hum + rattle, -1f, 1f);
            }
            var clip = AudioClip.Create("ConveyorMotor", n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
