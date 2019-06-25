using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Noise
{
    public class TextureCreator : MonoBehaviour
    {
        [Range(2, 512)] public int   resolution = 256;
        public                 float frequency  = 10f;

        public Noise.MethodType type;

        [Range(1, 3)]   public int   dimensions  = 3;
        [Range(1, 8)]   public int   octaves     = 1;
        [Range(1f, 4f)] public float lacunarity  = 2f;
        [Range(0f, 1f)] public float persistence = 0.5f;

        public Gradient coloring;

        private Texture2D texture;

        private void OnEnable()
        {
            if (texture == null) {
                texture = new Texture2D(resolution, resolution, TextureFormat.RGB24, true)
                {
                    name       = "Procedural Texture",
                    wrapMode   = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Trilinear,
                    anisoLevel = 9
                };
            }

            GetComponent<MeshRenderer>().material.mainTexture = texture;
            FillTexture();
        }

        private void Update()
        {
            if (transform.hasChanged) {
                transform.hasChanged = false;
                FillTexture();
            }
        }

        public void FillTexture()
        {
            if (texture.width != resolution) {
                texture.Resize(resolution, resolution);
            }

            Vector3 point00 = transform.TransformPoint(new Vector3(-0.5f, -0.5f));
            Vector3 point10 = transform.TransformPoint(new Vector3(0.5f, -0.5f));
            Vector3 point01 = transform.TransformPoint(new Vector3(-0.5f, 0.5f));
            Vector3 point11 = transform.TransformPoint(new Vector3(0.5f, 0.5f));

            var colors = new List<double>();
            float stepSize = 1f / resolution;
            var   method   = Noise.noiseMethods[(int) type][dimensions - 1];
            for (int y = 0; y < resolution; y++) {
                Vector3 point0 = Vector3.Lerp(point00, point01, (y + 0.5f) * stepSize);
                Vector3 point1 = Vector3.Lerp(point10, point11, (y + 0.5f) * stepSize);
                for (int x = 0; x < resolution; x++) {
                    Vector3 point  = Vector3.Lerp(point0, point1, (x + 0.5f) * stepSize);
                    float   sample = Noise.Sum(method, point, frequency, octaves, lacunarity, persistence).value;
                    if (type != Noise.MethodType.Value) {
                        sample = sample * 0.5f + 0.5f;
                    }

                    texture.SetPixel(x, y, coloring.Evaluate(sample));
                    colors.Add(sample);
                }
            }

            texture.Apply();
            Debug.Log("Texture min:  " + colors.Min());
        }
    }
}
