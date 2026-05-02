using UnityEngine;

namespace Cumic
{
    public static class UtilMath
    {
        #region Distance

        /// <summary>
        /// 두 Vector3 사이의 제곱 거리를 반환합니다. (sqrt 없이 빠른 비교용)
        /// </summary>
        public static float SqrDistance(this Vector3 vector, Vector3 target)
        {
            return (vector - target).sqrMagnitude;
        }

        #endregion

        #region Probability

        /// <summary>
        /// 주어진 확률(0~100%)로 성공 여부를 반환합니다.
        /// </summary>
        public static bool CheckProbability(float percent)
        {
            return Random.value * 100f < percent;
        }

        /// <summary>
        /// 주어진 확률(0~1)로 성공 여부를 반환합니다.
        /// </summary>
        public static bool CheckProbabilityNormalized(float probability)
        {
            return Random.value < probability;
        }

        /// <summary>
        /// 가중치 배열에서 가중치 비율에 따라 인덱스를 하나 선택합니다.
        /// 0 이하 가중치는 선택 대상에서 제외됩니다.
        /// </summary>
        public static int GetWeightedRandomIndex(float[] weights)
        {
            if (weights == null || weights.Length == 0)
            {
                return -1;
            }

            float totalWeight = 0f;
            for (int i = 0; i < weights.Length; i++)
            {
                if (weights[i] > 0f)
                {
                    totalWeight += weights[i];
                }
            }

            if (totalWeight <= 0f)
            {
                return -1;
            }

            float randomPoint = Random.Range(0f, totalWeight);
            float cumulativeWeight = 0f;

            for (int i = 0; i < weights.Length; i++)
            {
                if (weights[i] <= 0f)
                {
                    continue;
                }

                cumulativeWeight += weights[i];
                if (randomPoint < cumulativeWeight)
                {
                    return i;
                }
            }

            return weights.Length - 1;
        }

        #endregion

        #region Clamping

        /// <summary>
        /// 값을 0과 1 사이로 클램프합니다.
        /// </summary>
        public static float Clamp01(float value)
        {
            return Mathf.Clamp01(value);
        }

        /// <summary>
        /// 값을 min과 max 사이로 클램프합니다.
        /// </summary>
        public static float Clamp(float value, float min, float max)
        {
            return Mathf.Clamp(value, min, max);
        }

        /// <summary>
        /// 정수 값을 min과 max 사이로 클램프합니다.
        /// </summary>
        public static int Clamp(int value, int min, int max)
        {
            return Mathf.Clamp(value, min, max);
        }

        #endregion

        #region Remapping

        /// <summary>
        /// 값을 한 범위에서 다른 범위로 매핑합니다.
        /// </summary>
        public static float Remap(float value, float fromMin, float fromMax, float toMin, float toMax)
        {
            return toMin + (value - fromMin) * (toMax - toMin) / (fromMax - fromMin);
        }

        #endregion

        #region Accumulation

        // base * (작은 percent) 가 1 미만일 때 RoundToInt가 0으로 라운딩되어
        // 누적이 통째로 사라지는 것을 막기 위한 fractional accumulator.
        // accumulator에 delta를 더하고 0 방향 truncate한 정수만 반환,
        // 잔여 소수는 다음 호출까지 보존. 음수 delta(대칭 복원) 시에도 +/- 호출이 정확히 상쇄됨.
        public static int AccumulateIntDelta(ref float accumulator, float delta)
        {
            accumulator += delta;
            int intDelta = (int)accumulator;
            accumulator -= intDelta;
            return intDelta;
        }

        #endregion
    }
}
