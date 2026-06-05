using System.Collections.Generic;
using UnityEngine;

namespace RPG.Combat
{
    public class TurnQueue
    {
        private List<CombatCharacter> characters = new List<CombatCharacter>();
        private Dictionary<CombatCharacter, float> remainingAV = new Dictionary<CombatCharacter, float>();

        private const float BaseDistance = 10000f;

        public void AddCharacter(CombatCharacter character)
        {
            if (!characters.Contains(character))
            {
                characters.Add(character);
                float speed = character.GetModifiedSpeed();
                remainingAV[character] = BaseDistance / speed;
            }
        }

        public void RemoveCharacter(CombatCharacter character)
        {
            if (characters.Contains(character))
            {
                characters.Remove(character);
                remainingAV.Remove(character);
            }
        }

        public void Clear()
        {
            characters.Clear();
            remainingAV.Clear();
        }

        /// <summary>
        /// Tìm nhân vật tiếp theo hành động, đẩy thời gian trôi đi (giảm AV của các nhân vật khác).
        /// </summary>
        public CombatCharacter GetNextCharacter(out float elapsedAV)
        {
            elapsedAV = 0f;
            if (characters.Count == 0) return null;

            CombatCharacter nextChar = null;
            float minAV = float.MaxValue;

            // Tìm nhân vật có AV nhỏ nhất
            foreach (var character in characters)
            {
                if (character.isDead) continue;

                float av = remainingAV[character];
                if (av < minAV)
                {
                    minAV = av;
                    nextChar = character;
                }
            }

            if (nextChar != null)
            {
                elapsedAV = minAV;
                // Trừ đi minAV của tất cả nhân vật
                List<CombatCharacter> keys = new List<CombatCharacter>(remainingAV.Keys);
                foreach (var character in keys)
                {
                    remainingAV[character] -= minAV;
                    if (remainingAV[character] < 0f) remainingAV[character] = 0f;
                }
            }

            return nextChar;
        }

        /// <summary>
        /// Reset AV của nhân vật sau khi hoàn thành lượt đi của họ.
        /// </summary>
        public void ResetCharacterAV(CombatCharacter character)
        {
            if (remainingAV.ContainsKey(character))
            {
                float speed = character.GetModifiedSpeed();
                remainingAV[character] = BaseDistance / speed;
            }
        }

        /// <summary>
        /// Cập nhật AV khi Speed của nhân vật thay đổi (do buff/debuff).
        /// Công thức: NewAV = OldAV * (OldSpeed / NewSpeed)
        /// </summary>
        public void OnSpeedChanged(CombatCharacter character, float oldSpeed, float newSpeed)
        {
            if (remainingAV.ContainsKey(character))
            {
                float currentAV = remainingAV[character];
                if (oldSpeed <= 0) oldSpeed = 1f;
                if (newSpeed <= 0) newSpeed = 1f;

                float updatedAV = currentAV * (oldSpeed / newSpeed);
                remainingAV[character] = updatedAV;
            }
        }

        /// <summary>
        /// Trả về danh sách sắp xếp các nhân vật theo thứ tự hành động sắp tới (cho HUD).
        /// </summary>
        public List<CombatCharacter> GetSortedQueue()
        {
            List<CombatCharacter> sorted = new List<CombatCharacter>(characters);
            sorted.Sort((a, b) =>
            {
                float avA = remainingAV.ContainsKey(a) ? remainingAV[a] : float.MaxValue;
                float avB = remainingAV.ContainsKey(b) ? remainingAV[b] : float.MaxValue;
                return avA.CompareTo(avB);
            });
            return sorted;
        }

        public float GetRemainingAV(CombatCharacter character)
        {
            if (remainingAV.ContainsKey(character))
            {
                return remainingAV[character];
            }
            return 99999f;
        }
    }
}
