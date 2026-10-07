package com.snote.domezos.nativelib;

import android.content.Context;
import android.content.SharedPreferences;

public final class WidgetStore {
    private static final String FILE = "dw_widget";
    private WidgetStore() {}
    static SharedPreferences p(Context c) { return c.getSharedPreferences(FILE, Context.MODE_PRIVATE); }

    public static void saveStyle(Context c, int bg, int on, int sec, int onSec, String hint, String encrypt, String share, String shareMsg, String shareTitle) {
        p(c).edit().putInt("bg", bg).putInt("on", on).putInt("sec", sec).putInt("onSec", onSec)
            .putString("hint", hint).putString("encrypt", encrypt).putString("share", share)
            .putString("shareMsg", shareMsg).putString("shareTitle", shareTitle).apply();
    }
    public static int bg(Context c) { return p(c).getInt("bg", 0xFF050D1F); }
    public static int on(Context c) { return p(c).getInt("on", 0xFFF0F4FF); }
    public static int sec(Context c) { return p(c).getInt("sec", 0xFFF0C040); }
    public static int onSec(Context c) { return p(c).getInt("onSec", 0xFF3F2E00); }
    public static String hint(Context c) { return p(c).getString("hint", "Enter your text"); }
    public static String encrypt(Context c) { return p(c).getString("encrypt", "Encrypt"); }
    public static String share(Context c) { return p(c).getString("share", "Share"); }
    public static String shareTitle(Context c) { return p(c).getString("shareTitle", "Share encrypted link"); }
    public static String shareMessage(Context c, String link) {
        String t = p(c).getString("shareMsg", "I sent you a secret message: {0}");
        return t.replace("{0}", link);
    }
    public static String link(Context c, int id) { return p(c).getString("link_" + id, null); }
    public static void setLink(Context c, int id, String link) { p(c).edit().putString("link_" + id, link).apply(); }
    public static void clearLink(Context c, int id) { p(c).edit().remove("link_" + id).apply(); }
}
