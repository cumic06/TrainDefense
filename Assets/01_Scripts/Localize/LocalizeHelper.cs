namespace TrainDefense.Localize
{
    public static class LocalizeHelper
    {
        public static string GetByKey(string keyName, string fallback)
        {
            string result = Localization.GetByKey(keyName);
            return result ?? fallback;
        }
    }
}
