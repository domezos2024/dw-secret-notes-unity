package com.snote.domezos.nativelib;

import android.app.Activity;
import android.content.Context;
import android.util.Log;
import com.android.billingclient.api.AcknowledgePurchaseParams;
import com.android.billingclient.api.BillingClient;
import com.android.billingclient.api.BillingClientStateListener;
import com.android.billingclient.api.BillingFlowParams;
import com.android.billingclient.api.BillingResult;
import com.android.billingclient.api.ConsumeParams;
import com.android.billingclient.api.PendingPurchasesParams;
import com.android.billingclient.api.ProductDetails;
import com.android.billingclient.api.Purchase;
import com.android.billingclient.api.QueryProductDetailsParams;
import com.android.billingclient.api.QueryPurchasesParams;
import java.util.ArrayList;
import java.util.Collections;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

public final class DonationHelper {
    private static final String TAG = "DW.Donate";
    public interface Result { void on(String value); }

    private static BillingClient client;
    private static Result donateCallback;
    private static final Map<String, ProductDetails> details = new HashMap<>();
    private DonationHelper() {}

    private static void connect(Context ctx, Runnable ok, Runnable fail) {
        if (client != null && client.isReady()) { ok.run(); return; }
        if (client == null) {
            client = BillingClient.newBuilder(ctx.getApplicationContext())
                    .setListener(DonationHelper::onPurchases)
                    .enablePendingPurchases(PendingPurchasesParams.newBuilder().enableOneTimeProducts().build())
                    .build();
        }
        client.startConnection(new BillingClientStateListener() {
            @Override public void onBillingSetupFinished(BillingResult r) {
                if (r.getResponseCode() == BillingClient.BillingResponseCode.OK) { ok.run(); consumeOpen(); }
                else { Log.w(TAG, "setup failed " + r.getResponseCode() + " " + r.getDebugMessage()); fail.run(); }
            }
            @Override public void onBillingServiceDisconnected() { Log.w(TAG, "disconnected"); }
        });
    }

    public static void query(Context ctx, String[] ids, Result cb) {
        connect(ctx, () -> {
            List<QueryProductDetailsParams.Product> ps = new ArrayList<>();
            for (String id : ids) ps.add(QueryProductDetailsParams.Product.newBuilder().setProductId(id).setProductType(BillingClient.ProductType.INAPP).build());
            client.queryProductDetailsAsync(QueryProductDetailsParams.newBuilder().setProductList(ps).build(), (r, list) -> {
                StringBuilder sb = new StringBuilder();
                if (r.getResponseCode() == BillingClient.BillingResponseCode.OK && list != null) {
                    synchronized (details) { details.clear(); for (ProductDetails d : list) details.put(d.getProductId(), d); }
                    for (String id : ids) {
                        ProductDetails d = details.get(id);
                        if (d == null || d.getOneTimePurchaseOfferDetails() == null) continue;
                        if (sb.length() > 0) sb.append(';');
                        sb.append(id).append('|').append(d.getOneTimePurchaseOfferDetails().getFormattedPrice());
                    }
                } else Log.w(TAG, "query failed " + r.getResponseCode() + " " + r.getDebugMessage());
                cb.on(sb.toString());
            });
        }, () -> cb.on(""));
    }

    public static void donate(Activity act, String id, Result cb) {
        ProductDetails d;
        synchronized (details) { d = details.get(id); }
        if (d == null || client == null || !client.isReady()) { cb.on("error"); return; }
        donateCallback = cb;
        BillingFlowParams.ProductDetailsParams pd = BillingFlowParams.ProductDetailsParams.newBuilder().setProductDetails(d).build();
        BillingFlowParams fp = BillingFlowParams.newBuilder().setProductDetailsParamsList(Collections.singletonList(pd)).build();
        act.runOnUiThread(() -> {
            BillingResult r = client.launchBillingFlow(act, fp);
            if (r.getResponseCode() != BillingClient.BillingResponseCode.OK) finish("error");
        });
    }

    private static void finish(String value) {
        Result cb = donateCallback; donateCallback = null;
        if (cb != null) cb.on(value);
    }

    private static void onPurchases(BillingResult r, List<Purchase> purchases) {
        int code = r.getResponseCode();
        if (code == BillingClient.BillingResponseCode.USER_CANCELED) { finish("cancel"); return; }
        if (code == BillingClient.BillingResponseCode.ITEM_ALREADY_OWNED) { consumeOpen(); finish("error"); return; }
        if (code != BillingClient.BillingResponseCode.OK || purchases == null) { Log.w(TAG, "purchase failed " + code); finish("error"); return; }
        boolean any = false;
        for (Purchase p : purchases) {
            if (p.getPurchaseState() == Purchase.PurchaseState.PURCHASED) { any = true; consume(p, true); }
            else if (p.getPurchaseState() == Purchase.PurchaseState.PENDING) any = true;
        }
        if (!any) finish("error");
    }

    private static void consume(Purchase p, boolean report) {
        client.consumeAsync(ConsumeParams.newBuilder().setPurchaseToken(p.getPurchaseToken()).build(), (r, token) -> {
            boolean ok = r.getResponseCode() == BillingClient.BillingResponseCode.OK;
            Log.i(TAG, "consume " + r.getResponseCode());
            if (report) finish(ok ? "ok" : "error");
        });
    }

    private static void consumeOpen() {
        client.queryPurchasesAsync(QueryPurchasesParams.newBuilder().setProductType(BillingClient.ProductType.INAPP).build(), (r, list) -> {
            if (r.getResponseCode() != BillingClient.BillingResponseCode.OK || list == null) return;
            for (Purchase p : list) if (p.getPurchaseState() == Purchase.PurchaseState.PURCHASED) consume(p, false);
        });
    }
}
