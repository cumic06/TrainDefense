using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cumic.Excel
{
    /// <summary>
    /// Excel 데이터를 C# 타입으로 변환하는 유틸리티 클래스
    /// null/빈 셀 값을 안전하게 처리
    /// </summary>
    public static class ExcelDataConverter
    {
        /// <summary>
        /// Excel 셀 값을 정수로 변환합니다
        /// </summary>
        /// <param name="value">변환할 값</param>
        /// <param name="defaultValue">변환 실패 시 기본값</param>
        /// <returns>변환된 정수값</returns>
        public static int ConvertToInt(object value, int defaultValue = 0)
        {
            if (value == null) return defaultValue;

            if (value is int intValue) return intValue;
            if (value is double doubleValue) return (int)doubleValue;
            if (value is float floatValue) return (int)floatValue;
            if (value is string stringValue && int.TryParse(stringValue, out int parsedInt)) return parsedInt;

            Debug.LogWarning($"정수로 변환할 수 없는 값: {value} (타입: {value.GetType()})");
            return defaultValue;
        }

        /// <summary>
        /// Excel 셀 값을 부동소수점으로 변환합니다
        /// </summary>
        /// <param name="value">변환할 값</param>
        /// <param name="defaultValue">변환 실패 시 기본값</param>
        /// <returns>변환된 부동소수점값</returns>
        public static float ConvertToFloat(object value, float defaultValue = 0f)
        {
            if (value == null) return defaultValue;

            if (value is float floatValue) return floatValue;
            if (value is double doubleValue) return (float)doubleValue;
            if (value is int intValue) return intValue;
            if (value is string stringValue && float.TryParse(stringValue, out float parsedFloat)) return parsedFloat;

            Debug.LogWarning($"부동소수점으로 변환할 수 없는 값: {value} (타입: {value.GetType()})");
            return defaultValue;
        }

        /// <summary>
        /// Excel 셀 값을 문자열로 변환합니다
        /// </summary>
        /// <param name="value">변환할 값</param>
        /// <param name="defaultValue">변환 실패 시 기본값</param>
        /// <returns>변환된 문자열값</returns>
        public static string ConvertToString(object value, string defaultValue = "")
        {
            if (value == null) return defaultValue;

            return value.ToString();
        }

        /// <summary>
        /// Excel 셀 값을 불린으로 변환합니다
        /// </summary>
        /// <param name="value">변환할 값</param>
        /// <param name="defaultValue">변환 실패 시 기본값</param>
        /// <returns>변환된 불린값</returns>
        public static bool ConvertToBool(object value, bool defaultValue = false)
        {
            if (value == null) return defaultValue;

            if (value is bool boolValue) return boolValue;
            if (value is string stringValue)
            {
                if (bool.TryParse(stringValue, out bool parsedBool)) return parsedBool;
                
                // 문자열 기반 불린 변환 (대소문자 무시)
                var lowerString = stringValue.ToLower();
                return lowerString switch
                {
                    "true" or "1" or "yes" or "y" or "on" => true,
                    "false" or "0" or "no" or "n" or "off" => false,
                    _ => defaultValue
                };
            }
            if (value is int intValue) return intValue != 0;
            if (value is double doubleValue) return doubleValue != 0;

            Debug.LogWarning($"불린으로 변환할 수 없는 값: {value} (타입: {value.GetType()})");
            return defaultValue;
        }

        /// <summary>
        /// Excel 셀 값을 열거형으로 변환합니다
        /// </summary>
        /// <typeparam name="T">열거형 타입</typeparam>
        /// <param name="value">변환할 값</param>
        /// <param name="defaultValue">변환 실패 시 기본값</param>
        /// <returns>변환된 열거형값</returns>
        public static T ConvertToEnum<T>(object value, T defaultValue = default) where T : Enum
        {
            if (value == null) return defaultValue;

            if (value is T enumValue) return enumValue;
            if (value is string stringValue && Enum.TryParse(typeof(T), stringValue, true, out object parsedEnum)) 
                return (T)parsedEnum;
            if (value is int intValue && Enum.IsDefined(typeof(T), intValue)) 
                return (T)Enum.ToObject(typeof(T), intValue);

            Debug.LogWarning($"열거형 {typeof(T).Name}으로 변환할 수 없는 값: {value} (타입: {value.GetType()})");
            return defaultValue;
        }

        /// <summary>
        /// Excel 셀 값을 DateTime으로 변환합니다
        /// </summary>
        /// <param name="value">변환할 값</param>
        /// <param name="defaultValue">변환 실패 시 기본값</param>
        /// <returns>변환된 DateTime값</returns>
        public static DateTime ConvertToDateTime(object value, DateTime defaultValue = default)
        {
            if (value == null) return defaultValue;

            if (value is DateTime dateTimeValue) return dateTimeValue;
            if (value is string stringValue && DateTime.TryParse(stringValue, out DateTime parsedDateTime)) 
                return parsedDateTime;

            Debug.LogWarning($"DateTime으로 변환할 수 없는 값: {value} (타입: {value.GetType()})");
            return defaultValue;
        }

        /// <summary>
        /// Dictionary에서 특정 키의 값을 안전하게 가져옵니다
        /// </summary>
        /// <typeparam name="T">반환 타입</typeparam>
        /// <param name="rowData">행 데이터</param>
        /// <param name="key">키</param>
        /// <param name="defaultValue">기본값</param>
        /// <returns>변환된 값</returns>
        public static T GetValue<T>(Dictionary<string, object> rowData, string key, T defaultValue = default)
        {
            if (rowData == null || !rowData.ContainsKey(key))
            {
                Debug.LogWarning($"키 '{key}'를 찾을 수 없습니다.");
                return defaultValue;
            }

            var value = rowData[key];
            if (value == null) return defaultValue;

            if (value is T directValue) return directValue;

            // 타입 변환 시도
            return typeof(T) switch
            {
                Type t when t == typeof(int) => (T)(object)ConvertToInt(value),
                Type t when t == typeof(float) => (T)(object)ConvertToFloat(value),
                Type t when t == typeof(string) => (T)(object)ConvertToString(value),
                Type t when t == typeof(bool) => (T)(object)ConvertToBool(value),
                Type t when t == typeof(DateTime) => (T)(object)ConvertToDateTime(value),
                _ when typeof(T).IsEnum => (T)(object)ConvertToEnum(value, (Enum)Activator.CreateInstance(typeof(T))),
                _ => defaultValue
            };
        }

        /// <summary>
        /// Dictionary에서 특정 키의 값을 문자열로 가져옵니다
        /// </summary>
        public static string GetString(Dictionary<string, object> rowData, string key, string defaultValue = "")
        {
            return GetValue(rowData, key, defaultValue);
        }

        /// <summary>
        /// Dictionary에서 특정 키의 값을 정수로 가져옵니다
        /// </summary>
        public static int GetInt(Dictionary<string, object> rowData, string key, int defaultValue = 0)
        {
            return GetValue(rowData, key, defaultValue);
        }

        /// <summary>
        /// Dictionary에서 특정 키의 값을 부동소수점으로 가져옵니다
        /// </summary>
        public static float GetFloat(Dictionary<string, object> rowData, string key, float defaultValue = 0f)
        {
            return GetValue(rowData, key, defaultValue);
        }

        /// <summary>
        /// Dictionary에서 특정 키의 값을 불린으로 가져옵니다
        /// </summary>
        public static bool GetBool(Dictionary<string, object> rowData, string key, bool defaultValue = false)
        {
            return GetValue(rowData, key, defaultValue);
        }
    }
}
