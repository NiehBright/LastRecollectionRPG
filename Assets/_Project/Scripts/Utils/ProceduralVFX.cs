using UnityEngine;

namespace RPG.Combat
{
    public class ProceduralVFX : MonoBehaviour
    {
        public static ProceduralVFX Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Tạo hiệu ứng hạt dựa trên thuộc tính của kỹ năng tại vị trí mục tiêu.
        /// </summary>
        public void SpawnVFX(SkillData skill, Vector3 position)
        {
            ElementType element = ElementType.Physical;
            
            // Lấy hệ của skill (ta có thể lấy màu hoặc tự suy diễn thuộc tính dựa trên skillColor)
            // Nhằm đơn giản, ta sẽ lấy Element dựa trên thuộc tính của Active Character hoặc tự đoán qua skillId
            if (CombatManager.Instance != null && CombatManager.Instance.activeCharacter != null)
            {
                element = CombatManager.Instance.activeCharacter.characterData.element;
            }

            GameObject vfxGO = new GameObject($"VFX_{skill.skillName}");
            vfxGO.transform.position = position;

            ParticleSystem ps = vfxGO.AddComponent<ParticleSystem>();
            ps.Stop(); // Dừng hệ thống hạt trước khi cấu hình
            ConfigureParticleSystem(ps, element, skill.skillColor);

            // Tự hủy sau khi hiệu ứng kết thúc (2.5 giây)
            Destroy(vfxGO, 2.5f);
        }

        /// <summary>
        /// Tạo hiệu ứng khi nhân vật kích hoạt phòng thủ (Guard).
        /// </summary>
        public void SpawnGuardVFX(Vector3 position)
        {
            GameObject vfxGO = new GameObject("VFX_Guard");
            vfxGO.transform.position = position + Vector3.up * 1f;

            ParticleSystem ps = vfxGO.AddComponent<ParticleSystem>();
            ps.Stop(); // Dừng hệ thống hạt trước khi cấu hình
            
            var main = ps.main;
            main.duration = 0.5f;
            main.loop = false;
            main.startLifetime = 0.5f;
            main.startSpeed = 0.2f;
            main.startSize = 0.4f;
            main.startColor = new Color(0.5f, 0.7f, 1.0f, 0.8f); // Màu xanh dương nhạt bảo vệ
            main.maxParticles = 50;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 40) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.8f;
            
            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;

            // Chạy hiệu ứng
            ParticleSystemRenderer rend = ps.GetComponent<ParticleSystemRenderer>();
            if (rend != null)
            {
                rend.material = GetOrCreateParticleMaterial();
            }

            ps.Play();

            Destroy(vfxGO, 1.0f);
        }

        private void ConfigureParticleSystem(ParticleSystem ps, ElementType element, Color customColor)
        {
            var main = ps.main;
            main.duration = 1.0f;
            main.loop = false;
            main.startLifetime = 0.8f;
            main.startSpeed = 5.0f;
            main.startSize = 0.3f;
            main.maxParticles = 100;

            var emission = ps.emission;
            emission.rateOverTime = 0f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.1f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;

            Gradient grad = new Gradient();

            switch (element)
            {
                case ElementType.Fire:
                    main.startLifetime = 0.6f;
                    main.startSpeed = 8f;
                    main.startSize = 0.4f;
                    emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 60) });
                    
                    grad.SetKeys(
                        new GradientColorKey[] { new GradientColorKey(Color.red, 0f), new GradientColorKey(Color.yellow, 0.5f), new GradientColorKey(Color.black, 1.0f) },
                        new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0f), new GradientAlphaKey(1.0f, 0.7f), new GradientAlphaKey(0f, 1.0f) }
                    );
                    break;

                case ElementType.Ice:
                    main.startLifetime = 1.0f;
                    main.startSpeed = 3f;
                    main.startSize = 0.25f;
                    emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 40) });
                    shape.shapeType = ParticleSystemShapeType.Cone;
                    shape.angle = 25f;

                    grad.SetKeys(
                        new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.cyan, 0.4f), new GradientColorKey(new Color(0.1f, 0.4f, 1.0f), 1.0f) },
                        new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0f), new GradientAlphaKey(1.0f, 0.8f), new GradientAlphaKey(0f, 1.0f) }
                    );
                    break;

                case ElementType.Lightning:
                    main.startLifetime = 0.4f;
                    main.startSpeed = 12f;
                    main.startSize = 0.15f;
                    emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 80) });
                    
                    grad.SetKeys(
                        new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.yellow, 0.3f), new GradientColorKey(new Color(0.8f, 0.2f, 1.0f), 1.0f) }, // Vàng tím
                        new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0f), new GradientAlphaKey(1.0f, 0.5f), new GradientAlphaKey(0f, 1.0f) }
                    );
                    break;

                case ElementType.Nature:
                    main.startLifetime = 1.2f;
                    main.startSpeed = 2f;
                    main.startSize = 0.3f;
                    emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 35) });
                    
                    // Bay hướng lên trên
                    var velocity = ps.velocityOverLifetime;
                    velocity.enabled = true;
                    velocity.x = new ParticleSystem.MinMaxCurve(0f);
                    velocity.y = new ParticleSystem.MinMaxCurve(2.0f);
                    velocity.z = new ParticleSystem.MinMaxCurve(0f);

                    grad.SetKeys(
                        new GradientColorKey[] { new GradientColorKey(new Color(0.2f, 0.8f, 0.3f), 0f), new GradientColorKey(Color.green, 0.6f), new GradientColorKey(Color.yellow, 1.0f) },
                        new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0f), new GradientAlphaKey(1.0f, 0.7f), new GradientAlphaKey(0f, 1.0f) }
                    );
                    break;

                case ElementType.Physical:
                default:
                    main.startLifetime = 0.5f;
                    main.startSpeed = 10f;
                    main.startSize = 0.2f;
                    emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 50) });
                    
                    grad.SetKeys(
                        new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.gray, 0.5f), new GradientColorKey(Color.darkGray, 1.0f) },
                        new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0f), new GradientAlphaKey(1.0f, 0.6f), new GradientAlphaKey(0f, 1.0f) }
                    );
                    break;
            }

            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);

            // Chạy Particle System
            ParticleSystemRenderer rend = ps.GetComponent<ParticleSystemRenderer>();
            if (rend != null)
            {
                rend.material = GetOrCreateParticleMaterial();
            }

            ps.Play();
        }

        private Material GetOrCreateParticleMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            
            return new Material(shader);
        }
    }
}
