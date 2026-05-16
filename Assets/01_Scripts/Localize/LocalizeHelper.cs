namespace TrainDefense.Localize
{
    public static class LocalizeHelper
    {
        public static string GetByKey(string keyName, string fallback)
        {
#if HAS_UNITASK
            string result = Localization.GetByKey(keyName);
            return result ?? fallback;
#else
            return fallback;
#endif
        }
    }
}
