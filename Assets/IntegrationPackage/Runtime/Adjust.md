 
 *** This we are doing for interstital reward and appopen ads. ***
 // In events OnAdPaid of each we call HandleAdPaid for the adjust.
        public void InterstitialEvent(InterstitialAd ad)
        {

            ...


            ad.OnAdPaid += (AdValue adValue) =>
            {
                GameManager.HandleAdPaid(adValue, interId, "interstitial");
            };
        }
        

# GameManager.cs

    public static void HandleAdPaid(
    AdValue adValue,
    string adUnitId,
    string placement)
    {
        var revenue = adValue.Value / 1000000d;
        var currency = string.IsNullOrWhiteSpace(adValue.CurrencyCode)
        ? "USD"
        : adValue.CurrencyCode;

        TrackAdRevenue(
        AdjustConfig.AdjustAdRevenueSourceAdMob,
        revenue,
        currency,
        "admob",
        adUnitId,
        placement);

        LogAdMobPaidImpression(
        revenue,
        adValue.Value,
        (int)adValue.Precision,
        currency,
        adUnitId,
        placement);
    }


    public static void TrackAdRevenue(string source, double revenue, string currency, string network, string adUnitId, string placement)
    {
        // 1. Create the official Adjust ad revenue object
        AdjustAdRevenue adjustAdRevenue = new AdjustAdRevenue(source);

        // 2. Set the revenue and currency
        adjustAdRevenue.SetRevenue(revenue, currency);

        // 3. Set optional metadata parameters
        adjustAdRevenue.AdRevenueNetwork = network;
        adjustAdRevenue.AdRevenueUnit = adUnitId;
        adjustAdRevenue.AdRevenuePlacement = placement;

        // 4. Send it through the official Adjust SDK
        Adjust.TrackAdRevenue(adjustAdRevenue);
    }

    public static void LogAdMobPaidImpression(double revenue, long value, int precision, string currency, string adUnitId, string placement)
    {
        // 1. Format or log data to your internal analytics or Firebase
        // FirebaseAnalytics.LogEvent("paid_impression", ...);

        // 2. Or bridge it directly into an attribution provider like Adjust
        var adjustAdRevenue = new AdjustAdRevenue(AdjustConfig.AdjustAdRevenueSourceAdMob);
        adjustAdRevenue.SetRevenue(revenue, currency);
        adjustAdRevenue.AdRevenueUnit = adUnitId;
        adjustAdRevenue.AdRevenuePlacement = placement;

        Adjust.TrackAdRevenue(adjustAdRevenue);
    }
