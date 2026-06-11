using System;
using UnityEngine;

namespace RPG.Combat
{
    [CreateAssetMenu(fileName = "ElementWeaknessDatabase", menuName = "RPG/Combat/Element Weakness Database")]
    public class ElementWeaknessDatabase : ScriptableObject
    {
        [System.Serializable]
        public struct ElementRelation
        {
            public ElementType attacker;
            public ElementType defender;
            public float multiplier;
        }

        [Header("Danh sách tương khắc thuộc tính")]
        public ElementRelation[] relations;

        /// <summary>
        /// Lấy hệ số sát thương tương khắc giữa hệ tấn công và hệ phòng thủ.
        /// </summary>
        public float GetMultiplier(ElementType attacker, ElementType defender)
        {
            // Ether khắc tất cả nguyên tố
            if (attacker == ElementType.Ether)
            {
                return 1.5f;
            }
            // Không nguyên tố nào khắc được Ether
            if (defender == ElementType.Ether)
            {
                return 1.0f;
            }

            // Kiểm tra cấu hình trong asset trước
            if (relations != null && relations.Length > 0)
            {
                foreach (var relation in relations)
                {
                    if (relation.attacker == attacker && relation.defender == defender)
                    {
                        return relation.multiplier;
                    }
                }
            }

            // Nếu chưa được cấu hình, sử dụng ma trận mặc định từ GDD
            return GetDefaultMultiplier(attacker, defender);
        }

        private float GetDefaultMultiplier(ElementType attacker, ElementType defender)
        {
            // Physical không có weakness
            if (attacker == ElementType.Physical || defender == ElementType.Physical)
            {
                return 1.0f;
            }

            switch (defender)
            {
                case ElementType.Fire:
                    if (attacker == ElementType.Ice) return 1.5f;       // Fire yếu vs Ice
                    if (attacker == ElementType.Nature) return 0.9f;    // Fire yếu vs Nature (lấy 0.9x khi Nature đánh Fire)
                    break;

                case ElementType.Ice:
                    if (attacker == ElementType.Fire) return 1.5f;      // Ice yếu vs Fire
                    if (attacker == ElementType.Lightning) return 0.9f; // Ice yếu vs Lightning
                    break;

                case ElementType.Lightning:
                    if (attacker == ElementType.Nature) return 1.5f;    // Lightning yếu vs Nature
                    if (attacker == ElementType.Fire) return 0.9f;      // Lightning yếu vs Fire
                    break;

                case ElementType.Nature:
                    if (attacker == ElementType.Lightning) return 1.5f; // Nature yếu vs Lightning
                    if (attacker == ElementType.Ice) return 0.9f;       // Nature yếu vs Ice
                    break;
            }

            return 1.0f; // Mặc định
        }

        /// <summary>
        /// Khởi tạo dữ liệu mặc định cho database nếu cần.
        /// </summary>
        public void InitializeDefaults()
        {
            relations = new ElementRelation[]
            {
                // Fire Defender
                new ElementRelation { attacker = ElementType.Ice, defender = ElementType.Fire, multiplier = 1.5f },
                new ElementRelation { attacker = ElementType.Nature, defender = ElementType.Fire, multiplier = 0.9f },

                // Ice Defender
                new ElementRelation { attacker = ElementType.Fire, defender = ElementType.Ice, multiplier = 1.5f },
                new ElementRelation { attacker = ElementType.Lightning, defender = ElementType.Ice, multiplier = 0.9f },

                // Lightning Defender
                new ElementRelation { attacker = ElementType.Nature, defender = ElementType.Lightning, multiplier = 1.5f },
                new ElementRelation { attacker = ElementType.Fire, defender = ElementType.Lightning, multiplier = 0.9f },

                // Nature Defender
                new ElementRelation { attacker = ElementType.Lightning, defender = ElementType.Nature, multiplier = 1.5f },
                new ElementRelation { attacker = ElementType.Ice, defender = ElementType.Nature, multiplier = 0.9f },
            };
        }
    }
}
