using System;
using UnityEngine;

namespace Cumic
{
    public static class UtilUI
    {
        #region RectTransform Size

        /// <summary>
        /// RectTransform의 너비를 설정합니다.
        /// </summary>
        public static void SetWidth(this RectTransform rectTransform, float width)
        {
            rectTransform.sizeDelta = new Vector2(width, rectTransform.sizeDelta.y);
        }

        /// <summary>
        /// RectTransform의 높이를 설정합니다.
        /// </summary>
        public static void SetHeight(this RectTransform rectTransform, float height)
        {
            rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, height);
        }

        #endregion

        #region RectTransform Offsets

        /// <summary>
        /// RectTransform의 Left 오프셋을 설정합니다.
        /// </summary>
        public static void SetLeft(this RectTransform rectTransform, float left)
        {
            rectTransform.offsetMin = new Vector2(left, rectTransform.offsetMin.y);
        }

        /// <summary>
        /// RectTransform의 Right 오프셋을 설정합니다.
        /// </summary>
        public static void SetRight(this RectTransform rectTransform, float right)
        {
            rectTransform.offsetMax = new Vector2(-right, rectTransform.offsetMax.y);
        }

        /// <summary>
        /// RectTransform의 Top 오프셋을 설정합니다.
        /// </summary>
        public static void SetTop(this RectTransform rectTransform, float top)
        {
            rectTransform.offsetMax = new Vector2(rectTransform.offsetMax.x, -top);
        }

        /// <summary>
        /// RectTransform의 Bottom 오프셋을 설정합니다.
        /// </summary>
        public static void SetBottom(this RectTransform rectTransform, float bottom)
        {
            rectTransform.offsetMin = new Vector2(rectTransform.offsetMin.x, bottom);
        }

        #endregion

        #region Time Formatting

        /// <summary>
        /// float 시간(초)을 "HH:MM:SS" 형식 문자열로 변환합니다.
        /// </summary>
        public static string FormatTime(float seconds)
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(seconds);
            return timeSpan.ToString(@"hh\:mm\:ss");
        }

        /// <summary>
        /// float 시간(초)을 "MM:SS" 형식 문자열로 변환합니다.
        /// </summary>
        public static string FormatTimeMinutes(float seconds)
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(seconds);
            return timeSpan.ToString(@"mm\:ss");
        }

        /// <summary>
        /// float 시간(초)을 사용자 정의 형식 문자열로 변환합니다.
        /// </summary>
        public static string FormatTime(float seconds, string format)
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(seconds);
            return timeSpan.ToString(format);
        }

        #endregion

        #region String Formatting

        /// <summary>
        /// 형식 문자열과 인수를 조합하여 문자열을 반환합니다.
        /// </summary>
        public static string Format(string format, params object[] args)
        {
            return string.Format(format, args);
        }

        #endregion
    }
}
