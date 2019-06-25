using UnityEngine;

namespace Noise.Surface
{
    [RequireComponent(typeof(ParticleSystem))]
    public class SurfaceFlow : MonoBehaviour
    {
        public SurfaceCreator surface;
        public float flowStrength;

        private ParticleSystem            system;
        private ParticleSystem.Particle[] particles;

        private void LateUpdate()
        {
            if (system == null) {
                system = GetComponent<ParticleSystem>();
            }

            if (particles == null || particles.Length < system.main.maxParticles) {
                particles = new ParticleSystem.Particle[system.main.maxParticles];
            }

            int particleCount = system.GetParticles(particles);
            PositionParticles();
            system.SetParticles(particles, particleCount);
        }

        private void PositionParticles()
        {
            var q         = Quaternion.Euler(surface.rotation);
            var qInv      = Quaternion.Inverse(q);
            var method    = Noise.noiseMethods[(int) surface.type][surface.dimensions - 1];
            var amplitude = surface.damping ? surface.strength / surface.frequency : surface.strength;
            for (int i = 0; i < particles.Length; i++) {
                var position = particles[i].position;
                var point    = q * new Vector3(position.x, position.z) + surface.offset;
                var sample = Noise.Sum(method,
                                       point,
                                       surface.frequency,
                                       surface.octaves,
                                       surface.lacunarity,
                                       surface.persistence);

                sample                =  surface.type == Noise.MethodType.Value ? (sample - 0.5f) : (sample * 0.5f);
                sample                *= amplitude;
                sample.derivative     =  qInv * sample.derivative;

                Vector3 curl = new Vector3(sample.derivative.y, 0f, -sample.derivative.x);
                position += curl * Time.deltaTime * flowStrength;
                position.y            =  sample.value + system.startSize;
                particles[i].position =  position;
            }
        }
    }
}
