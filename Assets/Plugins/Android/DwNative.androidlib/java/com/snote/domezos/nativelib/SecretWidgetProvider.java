package com.snote.domezos.nativelib;

import android.app.PendingIntent;
import android.appwidget.AppWidgetManager;
import android.appwidget.AppWidgetProvider;
import android.content.ComponentName;
import android.content.Context;
import android.content.Intent;
import android.view.View;
import android.widget.RemoteViews;

public final class SecretWidgetProvider extends AppWidgetProvider {
    public static final String ACTION_ENCRYPT = "com.snote.domezos.WIDGET_ENCRYPT";
    public static final String EXTRA_WIDGET_ID = "dw_widget_id";
    private static final int SHARE_OFFSET = 100_000;

    @Override public void onUpdate(Context c, AppWidgetManager m, int[] ids) { for (int id : ids) update(c, m, id); }
    @Override public void onDeleted(Context c, int[] ids) { for (int id : ids) WidgetStore.clearLink(c, id); }

    public static void updateAll(Context c) {
        AppWidgetManager m = AppWidgetManager.getInstance(c);
        for (int id : m.getAppWidgetIds(new ComponentName(c, SecretWidgetProvider.class))) update(c, m, id);
    }

    static void update(Context c, AppWidgetManager m, int id) {
        RemoteViews v = new RemoteViews(c.getPackageName(), R.layout.dw_widget_secret);
        v.setInt(R.id.widget_root, "setBackgroundColor", WidgetStore.bg(c));
        v.setTextColor(R.id.widget_field_idle, WidgetStore.on(c));
        v.setTextColor(R.id.widget_field_result, WidgetStore.on(c));
        v.setInt(R.id.widget_btn_encrypt, "setBackgroundColor", WidgetStore.sec(c));
        v.setTextColor(R.id.widget_btn_encrypt, WidgetStore.onSec(c));
        v.setInt(R.id.widget_btn_share, "setBackgroundColor", WidgetStore.sec(c));
        v.setTextColor(R.id.widget_btn_share, WidgetStore.onSec(c));
        Intent launch = c.getPackageManager().getLaunchIntentForPackage(c.getPackageName());
        if (launch == null) return;
        launch.setAction(ACTION_ENCRYPT).putExtra(EXTRA_WIDGET_ID, id).addFlags(Intent.FLAG_ACTIVITY_NEW_TASK | Intent.FLAG_ACTIVITY_SINGLE_TOP);
        PendingIntent enc = PendingIntent.getActivity(c, id, launch, PendingIntent.FLAG_UPDATE_CURRENT | PendingIntent.FLAG_IMMUTABLE);
        String link = WidgetStore.link(c, id);
        if (link == null) {
            v.setViewVisibility(R.id.idle_container, View.VISIBLE);
            v.setViewVisibility(R.id.result_container, View.GONE);
            v.setTextViewText(R.id.widget_field_idle, WidgetStore.hint(c));
            v.setTextViewText(R.id.widget_btn_encrypt, WidgetStore.encrypt(c));
            v.setOnClickPendingIntent(R.id.widget_field_idle, enc);
            v.setOnClickPendingIntent(R.id.widget_btn_encrypt, enc);
        } else {
            v.setViewVisibility(R.id.idle_container, View.GONE);
            v.setViewVisibility(R.id.result_container, View.VISIBLE);
            v.setTextViewText(R.id.widget_field_result, link);
            v.setTextViewText(R.id.widget_btn_share, WidgetStore.share(c));
            v.setOnClickPendingIntent(R.id.widget_field_result, enc);
            Intent send = new Intent(Intent.ACTION_SEND).setType("text/plain").putExtra(Intent.EXTRA_TEXT, WidgetStore.shareMessage(c, link));
            Intent chooser = Intent.createChooser(send, WidgetStore.shareTitle(c)).addFlags(Intent.FLAG_ACTIVITY_NEW_TASK);
            v.setOnClickPendingIntent(R.id.widget_btn_share, PendingIntent.getActivity(c, id + SHARE_OFFSET, chooser, PendingIntent.FLAG_UPDATE_CURRENT | PendingIntent.FLAG_IMMUTABLE));
        }
        m.updateAppWidget(id, v);
    }
}
