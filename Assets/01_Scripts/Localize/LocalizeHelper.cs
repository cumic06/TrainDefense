using System.Collections.Generic;
using UnityEngine;

namespace TrainDefense.Localize
{
    public static class LocalizeHelper
    {
        private static readonly HashSet<string> _warnedKeys = new();

        public static string GetByKey(string keyName, string fallback)
        {
            string result = Localization.GetByKey(keyName);

            if (result == null && Localization.IsInitialized && !string.IsNullOrEmpty(keyName) && _warnedKeys.Add(keyName))
                Debug.LogWarning($"[Localize] 키 누락: '{keyName}' — fallback: '{fallback}'");

            return result ?? fallback;
        }
    }
}
