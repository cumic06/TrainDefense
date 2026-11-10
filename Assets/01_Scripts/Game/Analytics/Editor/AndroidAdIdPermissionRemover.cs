using System.IO;
using System.Xml;
using UnityEditor.Android;
using UnityEngine;

namespace TrainDefense.Game.Analytics.Editor
{
    /// <summary>
    /// 게임은 광고를 쓰지 않지만 Firebase Analytics(play-services-measurement-api)가
    /// com.google.android.gms.permission.AD_ID 권한을 매니페스트에 자동으로 병합한다.
    /// 그러면 Play Console 광고 ID 선언에서 "아니오"를 고를 수 없으므로,
    /// Gradle 프로젝트 생성 직후 unityLibrary 매니페스트에 tools:node="remove" 마커를 넣어 병합 시 권한을 제거하고
    /// Analytics의 광고 ID 수집도 끈다.
    /// </summary>
    public class AndroidAdIdPermissionRemover : IPostGenerateGradleAndroidProject
    {
        #region Variables
        private const string ANDROID_NAMESPACE = "http://schemas.android.com/apk/res/android";
        private const string TOOLS_NAMESPACE = "http://schemas.android.com/tools";
        private const string XMLNS_NAMESPACE = "http://www.w3.org/2000/xmlns/";
        private const string AD_ID_PERMISSION = "com.google.android.gms.permission.AD_ID";
        private const string ADID_COLLECTION_META = "google_analytics_adid_collection_enabled";
        private const string MANIFEST_RELATIVE_PATH = "src/main/AndroidManifest.xml";
        #endregion

        #region Fields
        public int callbackOrder => 999;
        #endregion

        #region LifeCycle
        public void OnPostGenerateGradleAndroidProject(string path)
        {
            string manifestPath = Path.Combine(path, MANIFEST_RELATIVE_PATH);

            if (!File.Exists(manifestPath))
            {
                Debug.LogWarning($"[AdId] 매니페스트를 찾지 못해 AD_ID 권한 제거를 건너뜁니다: {manifestPath}");

                return;
            }

            XmlDocument document = new XmlDocument();
            document.Load(manifestPath);

            XmlElement manifest = document.DocumentElement;

            if (manifest == null)
            {
                return;
            }

            _SetPrefixedAttribute(document, manifest, "xmlns", "tools", XMLNS_NAMESPACE, TOOLS_NAMESPACE);

            _AddPermissionRemoveMarker(document, manifest);
            _DisableAdIdCollection(document, manifest);

            document.Save(manifestPath);
            Debug.Log("[AdId] AD_ID 권한 제거 마커와 광고 ID 수집 비활성화를 매니페스트에 적용했습니다.");
        }
        #endregion

        #region Sub/UnSub
        private static void _AddPermissionRemoveMarker(XmlDocument document, XmlElement manifest)
        {
            foreach (XmlNode node in manifest.SelectNodes("uses-permission"))
            {
                if (node is XmlElement existing && existing.GetAttribute("name", ANDROID_NAMESPACE) == AD_ID_PERMISSION)
                {
                    _SetPrefixedAttribute(document, existing, "tools", "node", TOOLS_NAMESPACE, "remove");

                    return;
                }
            }

            XmlElement permission = document.CreateElement("uses-permission");
            _SetPrefixedAttribute(document, permission, "android", "name", ANDROID_NAMESPACE, AD_ID_PERMISSION);
            _SetPrefixedAttribute(document, permission, "tools", "node", TOOLS_NAMESPACE, "remove");
            manifest.AppendChild(permission);
        }

        private static void _DisableAdIdCollection(XmlDocument document, XmlElement manifest)
        {
            if (manifest.SelectSingleNode("application") is not XmlElement application)
            {
                return;
            }

            foreach (XmlNode node in application.SelectNodes("meta-data"))
            {
                if (node is XmlElement existing && existing.GetAttribute("name", ANDROID_NAMESPACE) == ADID_COLLECTION_META)
                {
                    _SetPrefixedAttribute(document, existing, "android", "value", ANDROID_NAMESPACE, "false");

                    return;
                }
            }

            XmlElement metaData = document.CreateElement("meta-data");
            _SetPrefixedAttribute(document, metaData, "android", "name", ANDROID_NAMESPACE, ADID_COLLECTION_META);
            _SetPrefixedAttribute(document, metaData, "android", "value", ANDROID_NAMESPACE, "false");
            application.AppendChild(metaData);
        }

        private static void _SetPrefixedAttribute(XmlDocument document, XmlElement element, string prefix, string localName, string namespaceUri, string value)
        {
            XmlAttribute attribute = document.CreateAttribute(prefix, localName, namespaceUri);
            attribute.Value = value;
            element.SetAttributeNode(attribute);
        }
        #endregion
    }
}
