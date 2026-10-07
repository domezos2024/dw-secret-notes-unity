package com.snote.domezos.nativelib;

import android.app.Activity;
import android.content.Intent;
import android.net.Uri;
import android.os.Build;
import android.os.Bundle;
import android.provider.MediaStore;

public final class ImagePickActivity extends Activity {
    public interface Listener { void onResult(String path); }
    private static final int REQ = 4711;
    private static volatile Listener listener;
    private static int maxEdge = 1600, quality = 80;

    public static void start(Activity from, int edge, int q, Listener l) {
        listener = l; maxEdge = edge; quality = q;
        from.startActivity(new Intent(from, ImagePickActivity.class));
    }

    @Override protected void onCreate(Bundle state) {
        super.onCreate(state);
        if (state != null) return;
        Intent pick;
        if (Build.VERSION.SDK_INT >= 33) {
            pick = new Intent(MediaStore.ACTION_PICK_IMAGES).setType("image/*");
        } else {
            pick = new Intent(Intent.ACTION_GET_CONTENT).setType("image/*").addCategory(Intent.CATEGORY_OPENABLE);
        }
        try { startActivityForResult(pick, REQ); } catch (Exception e) { deliver(null); finish(); }
    }

    @Override protected void onActivityResult(int requestCode, int resultCode, Intent data) {
        super.onActivityResult(requestCode, resultCode, data);
        final Uri uri = resultCode == RESULT_OK && data != null ? data.getData() : null;
        if (uri == null) { deliver(null); finish(); return; }
        final android.content.Context app = getApplicationContext();
        new Thread(() -> deliver(ImageCodec.prepareToCache(app, uri, maxEdge, quality)), "dw-image").start();
        finish();
    }

    private static void deliver(String path) {
        Listener l = listener; listener = null;
        if (l != null) l.onResult(path == null ? "" : path);
    }
}
