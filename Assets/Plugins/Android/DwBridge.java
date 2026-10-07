package com.snote.domezos.unity;

import android.app.Activity;
import android.content.ClipData;
import android.content.ClipboardManager;
import android.content.Context;
import android.content.Intent;
import android.net.Uri;
import android.util.Log;
import android.view.WindowManager;
import com.snote.domezos.nativelib.ImagePickActivity;
import com.snote.domezos.nativelib.LauncherWidgetProvider;
import com.snote.domezos.nativelib.ReviewHelper;
import com.snote.domezos.nativelib.SecretWidgetProvider;
import com.snote.domezos.nativelib.WidgetStore;
import com.unity3d.player.UnityPlayer;

public final class DwBridge {
    private static final String TAG = "DW.Bridge";
    private static final String RECEIVER = "DwNative";
    private static final String HOST = "domezos-ware.com";
    private static final String CONSUMED = "dw_consumed";
    private DwBridge() {}

    private static Activity act() { return UnityPlayer.currentActivity; }
    private static void send(String method, String arg) { UnityPlayer.UnitySendMessage(RECEIVER, method, arg == null ? "" : arg); }

    public static void pickImage(int maxEdge, int quality) {
        Log.i(TAG, "pickImage");
        ImagePickActivity.start(act(), maxEdge, quality, path -> send("OnImagePicked", path));
    }

    public static boolean shareText(String text, String title) {
        try {
            Intent send = new Intent(Intent.ACTION_SEND).setType("text/plain").putExtra(Intent.EXTRA_TEXT, text);
            act().startActivity(Intent.createChooser(send, title));
            return true;
        } catch (Exception e) { Log.e(TAG, "share failed", e); return false; }
    }

    public static void copyText(String text) {
        Activity a = act();
        a.runOnUiThread(() -> {
            ClipboardManager cm = (ClipboardManager) a.getSystemService(Context.CLIPBOARD_SERVICE);
            if (cm != null) cm.setPrimaryClip(ClipData.newPlainText("link", text));
        });
    }

    public static String readClipboard() {
        try {
            ClipboardManager cm = (ClipboardManager) act().getSystemService(Context.CLIPBOARD_SERVICE);
            if (cm == null || !cm.hasPrimaryClip() || cm.getPrimaryClip().getItemCount() == 0) return "";
            CharSequence t = cm.getPrimaryClip().getItemAt(0).coerceToText(act());
            return t == null ? "" : t.toString();
        } catch (Exception e) { return ""; }
    }

    public static void haptic(int type) {
        Activity a = act();
        a.runOnUiThread(() -> a.getWindow().getDecorView().performHapticFeedback(type));
    }

    public static void setSecure(boolean secure) {
        Activity a = act();
        a.runOnUiThread(() -> {
            if (secure) a.getWindow().addFlags(WindowManager.LayoutParams.FLAG_SECURE);
            else a.getWindow().clearFlags(WindowManager.LayoutParams.FLAG_SECURE);
        });
    }

    public static String consumeLaunchRequest() {
        Intent it = act().getIntent();
        if (it == null || it.getBooleanExtra(CONSUMED, false)) return "";
        it.putExtra(CONSUMED, true);
        String action = it.getAction();
        if (SecretWidgetProvider.ACTION_ENCRYPT.equals(action)) return "widget\n" + it.getIntExtra(SecretWidgetProvider.EXTRA_WIDGET_ID, -1);
        Uri data = it.getData();
        if (data != null && HOST.equalsIgnoreCase(data.getHost())) return "view\n" + data.toString();
        if (Intent.ACTION_SEND.equals(action) && "text/plain".equals(it.getType())) {
            String text = it.getStringExtra(Intent.EXTRA_TEXT);
            return text == null ? "" : "send\n" + text;
        }
        return "";
    }

    public static void requestReview() {
        Activity a = act();
        a.runOnUiThread(() -> ReviewHelper.launch(a, () -> send("OnReviewDone", "1")));
    }

    public static void setWidgetLink(int id, String link) {
        Context c = act().getApplicationContext();
        WidgetStore.setLink(c, id, link);
        SecretWidgetProvider.updateAll(c);
    }

    public static void updateWidgets(int bg, int on, int sec, int onSec, String hint, String encrypt, String share, String shareMsg, String shareTitle) {
        Context c = act().getApplicationContext();
        WidgetStore.saveStyle(c, bg, on, sec, onSec, hint, encrypt, share, shareMsg, shareTitle);
        LauncherWidgetProvider.refreshAll(c);
    }

    public static void moveTaskToBack() { act().moveTaskToBack(true); }
}
