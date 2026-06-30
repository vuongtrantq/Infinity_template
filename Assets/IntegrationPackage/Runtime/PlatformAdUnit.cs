using System;
using UnityEngine;

namespace PartnerIntegration
{
    [Serializable]
    public sealed class PlatformAdUnit
    {
        [SerializeField] private string key = "Default";
        [SerializeField] private string androidId;
        [SerializeField] private string iosId;
        [SerializeField] private string androidRemoteConfigKey;
        [SerializeField] private string iosRemoteConfigKey;

        public PlatformAdUnit()
        {
        }

        public PlatformAdUnit(string key, string androidId, string iosId, string androidRemoteConfigKey = null, string iosRemoteConfigKey = null)
        {
            this.key = key;
            this.androidId = androidId;
            this.iosId = iosId;
            this.androidRemoteConfigKey = androidRemoteConfigKey;
            this.iosRemoteConfigKey = iosRemoteConfigKey;
        }

        public string Key => string.IsNullOrWhiteSpace(key) ? "Default" : key.Trim();

        public string CurrentId
        {
            get
            {
#if UNITY_ANDROID
                return androidId;
#elif UNITY_IOS
                return iosId;
#else
                return androidId;
#endif
            }
        }

        public string CurrentRemoteConfigKey
        {
            get
            {
#if UNITY_ANDROID
                return androidRemoteConfigKey;
#elif UNITY_IOS
                return iosRemoteConfigKey;
#else
                return androidRemoteConfigKey;
#endif
            }
        }

        public bool HasCurrentId => !string.IsNullOrWhiteSpace(CurrentId);

        public void SetCurrentPlatformId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

#if UNITY_ANDROID
            androidId = value;
#elif UNITY_IOS
            iosId = value;
#else
            androidId = value;
#endif
        }
    }
}
