using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_PURCHASING
using UnityEngine.Purchasing;
#endif

namespace PartnerIntegration
{
    public static class IapManager
    {
        private static IntegrationSettings settings;
        private static readonly Dictionary<string, IapProductDefinition> ProductsByKey = new Dictionary<string, IapProductDefinition>();
        private static readonly Dictionary<string, IapProductDefinition> ProductsByStoreId = new Dictionary<string, IapProductDefinition>();
        private static readonly Dictionary<string, Action<IapPurchaseResult>> PendingPurchases = new Dictionary<string, Action<IapPurchaseResult>>();

#if UNITY_PURCHASING
        private static IStoreController controller;
        private static IExtensionProvider extensions;
#endif

        public static bool IsReady
        {
            get
            {
#if UNITY_PURCHASING
                return controller != null && extensions != null;
#else
                return false;
#endif
            }
        }

        public static void Initialize(IntegrationSettings integrationSettings)
        {
            settings = integrationSettings;
            ProductsByKey.Clear();
            ProductsByStoreId.Clear();
            PendingPurchases.Clear();

            if (settings == null || !settings.InitializeIap)
            {
                return;
            }

            var products = settings.IapProducts;
            for (var i = 0; i < products.Count; i++)
            {
                var product = products[i];
                if (product == null || string.IsNullOrWhiteSpace(product.Key) || string.IsNullOrWhiteSpace(product.StoreId))
                {
                    continue;
                }

                ProductsByKey[product.Key] = product;
                ProductsByStoreId[product.StoreId] = product;
            }

            if (ProductsByKey.Count == 0)
            {
                Debug.LogWarning("[IntegrationPackage] IAP skipped: no products configured.");
                return;
            }

#if UNITY_PURCHASING
            if (controller != null)
            {
                return;
            }

            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
            foreach (var product in ProductsByKey.Values)
            {
                builder.AddProduct(product.StoreId, ConvertProductType(product.Type));
            }

            UnityPurchasing.Initialize(new StoreListener(), builder);
#else
            Debug.LogWarning("[IntegrationPackage] IAP package is not resolved yet. Add com.unity.purchasing and let Unity refresh packages.");
#endif
        }

        public static void Purchase(string key, Action<IapPurchaseResult> onComplete = null)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                onComplete?.Invoke(Fail(key, string.Empty, "Missing IAP key."));
                return;
            }

            if (!ProductsByKey.TryGetValue(key, out var productDefinition))
            {
                onComplete?.Invoke(Fail(key, string.Empty, "Unknown IAP key."));
                return;
            }

#if UNITY_PURCHASING
            if (!IsReady)
            {
                onComplete?.Invoke(Fail(key, productDefinition.StoreId, "IAP is not initialized."));
                return;
            }

            var product = controller.products.WithID(productDefinition.StoreId);
            if (product == null || !product.availableToPurchase)
            {
                onComplete?.Invoke(Fail(key, productDefinition.StoreId, "Product is not available."));
                return;
            }

            PendingPurchases[productDefinition.StoreId] = onComplete;
            controller.InitiatePurchase(product);
#else
            onComplete?.Invoke(Fail(key, productDefinition.StoreId, "Unity IAP package is not available."));
#endif
        }

        public static void RestorePurchases(Action<bool> onComplete = null)
        {
#if UNITY_PURCHASING && (UNITY_IOS || UNITY_TVOS || UNITY_STANDALONE_OSX)
            if (!IsReady)
            {
                onComplete?.Invoke(false);
                return;
            }

            extensions.GetExtension<IAppleExtensions>().RestoreTransactions((success, message) =>
            {
                Debug.Log("[IntegrationPackage] IAP restore result: " + success + " " + message);
                onComplete?.Invoke(success);
            });
#else
            Debug.Log("[IntegrationPackage] IAP restore is only required on Apple stores.");
            onComplete?.Invoke(true);
#endif
        }

        private static IapPurchaseResult Fail(string key, string productId, string message)
        {
            Debug.LogWarning("[IntegrationPackage] IAP failed: " + message);
            return new IapPurchaseResult(false, key, productId, string.Empty, string.Empty, message);
        }

#if UNITY_PURCHASING
        private static ProductType ConvertProductType(IapProductType type)
        {
            switch (type)
            {
                case IapProductType.Consumable:
                    return ProductType.Consumable;
                case IapProductType.Subscription:
                    return ProductType.Subscription;
                default:
                    return ProductType.NonConsumable;
            }
        }

        private sealed class StoreListener : IDetailedStoreListener
        {
            public void OnInitialized(IStoreController storeController, IExtensionProvider extensionProvider)
            {
                controller = storeController;
                extensions = extensionProvider;
                Debug.Log("[IntegrationPackage] IAP initialized.");
            }

            public void OnInitializeFailed(InitializationFailureReason error)
            {
                Debug.LogWarning("[IntegrationPackage] IAP initialize failed: " + error);
            }

            public void OnInitializeFailed(InitializationFailureReason error, string message)
            {
                Debug.LogWarning("[IntegrationPackage] IAP initialize failed: " + error + " " + message);
            }

            public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
            {
                var product = args.purchasedProduct;
                var storeId = product.definition.id;
                ProductsByStoreId.TryGetValue(storeId, out var definition);

                var key = definition != null ? definition.Key : storeId;
                var result = new IapPurchaseResult(true, key, storeId, product.transactionID, product.receipt, "Purchased");
                TrackPurchase(definition, product);

                if (PendingPurchases.TryGetValue(storeId, out var callback))
                {
                    PendingPurchases.Remove(storeId);
                    callback?.Invoke(result);
                }

                return PurchaseProcessingResult.Complete;
            }

            public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
            {
                CompleteFailedPurchase(product, failureReason.ToString());
            }

            public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
            {
                CompleteFailedPurchase(product, failureDescription.message);
            }

            private static void CompleteFailedPurchase(Product product, string message)
            {
                var storeId = product != null ? product.definition.id : string.Empty;
                ProductsByStoreId.TryGetValue(storeId, out var definition);
                var key = definition != null ? definition.Key : storeId;
                var result = Fail(key, storeId, message);

                if (PendingPurchases.TryGetValue(storeId, out var callback))
                {
                    PendingPurchases.Remove(storeId);
                    callback?.Invoke(result);
                }
            }

            private static void TrackPurchase(IapProductDefinition definition, Product product)
            {
                if (definition == null || string.IsNullOrWhiteSpace(definition.AdjustPurchaseEventToken))
                {
                    return;
                }

                var metadata = product.metadata;
                var revenue = decimal.ToDouble(metadata.localizedPrice);
                var currency = string.IsNullOrWhiteSpace(metadata.isoCurrencyCode) ? "USD" : metadata.isoCurrencyCode;
                AdjustTracker.TrackPurchase(definition.AdjustPurchaseEventToken, revenue, currency, product.transactionID);
            }
        }
#endif
    }
}
