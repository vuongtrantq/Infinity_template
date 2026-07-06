using System;
using UnityEngine;

namespace PartnerIntegration
{
    public enum IapProductType
    {
        Consumable,
        NonConsumable,
        Subscription
    }

    [Serializable]
    public sealed class IapProductDefinition
    {
        [SerializeField] private string key;
        [SerializeField] private string androidId;
        [SerializeField] private string iosId;
        [SerializeField] private IapProductType type = IapProductType.NonConsumable;
        [SerializeField] private string adjustPurchaseEventToken;

        public IapProductDefinition(string key, string androidId, string iosId, IapProductType type, string adjustPurchaseEventToken)
        {
            this.key = key;
            this.androidId = androidId;
            this.iosId = iosId;
            this.type = type;
            this.adjustPurchaseEventToken = adjustPurchaseEventToken;
        }

        public string Key => key;
        public string AndroidId => androidId;
        public string IosId => iosId;
        public IapProductType Type => type;
        public string AdjustPurchaseEventToken => adjustPurchaseEventToken;

        public string StoreId
        {
            get
            {
#if UNITY_IOS
                return string.IsNullOrWhiteSpace(iosId) ? androidId : iosId;
#else
                return string.IsNullOrWhiteSpace(androidId) ? iosId : androidId;
#endif
            }
        }
    }
}
