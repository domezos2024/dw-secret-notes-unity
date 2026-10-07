package com.snote.domezos.nativelib;

import android.content.Context;
import android.graphics.Bitmap;
import android.graphics.BitmapFactory;
import android.graphics.ImageDecoder;
import android.net.Uri;
import android.os.Build;
import android.util.Log;
import java.io.File;
import java.io.FileOutputStream;
import java.io.InputStream;

public final class ImageCodec {
    private static final String TAG = "DW.ImageCodec";
    private ImageCodec() {}

    static int sampleSize(int w, int h, int maxEdge) {
        int s = 1, longest = Math.max(w, h);
        while (longest / (s * 2) >= maxEdge) s *= 2;
        return s;
    }

    static Bitmap decode(Context ctx, Uri uri, int maxEdge) throws Exception {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.P) {
            ImageDecoder.Source src = ImageDecoder.createSource(ctx.getContentResolver(), uri);
            return ImageDecoder.decodeBitmap(src, (decoder, info, s) -> {
                int sample = sampleSize(info.getSize().getWidth(), info.getSize().getHeight(), maxEdge);
                if (sample > 1) decoder.setTargetSampleSize(sample);
                decoder.setAllocator(ImageDecoder.ALLOCATOR_SOFTWARE);
                decoder.setMutableRequired(true);
            });
        }
        BitmapFactory.Options o = new BitmapFactory.Options();
        o.inJustDecodeBounds = true;
        try (InputStream in = ctx.getContentResolver().openInputStream(uri)) { BitmapFactory.decodeStream(in, null, o); }
        o.inSampleSize = sampleSize(o.outWidth, o.outHeight, maxEdge);
        o.inJustDecodeBounds = false;
        try (InputStream in = ctx.getContentResolver().openInputStream(uri)) { return BitmapFactory.decodeStream(in, null, o); }
    }

    static Bitmap scaleDown(Bitmap b, int maxEdge) {
        int longest = Math.max(b.getWidth(), b.getHeight());
        if (longest <= maxEdge) return b;
        float s = (float) maxEdge / longest;
        Bitmap r = Bitmap.createScaledBitmap(b, Math.max(1, (int) (b.getWidth() * s)), Math.max(1, (int) (b.getHeight() * s)), true);
        if (r != b) b.recycle();
        return r;
    }

    public static String prepareToCache(Context ctx, Uri uri, int maxEdge, int quality) {
        try {
            Bitmap b = decode(ctx, uri, maxEdge);
            if (b == null) { Log.e(TAG, "decode returned null"); return null; }
            b = scaleDown(b, maxEdge);
            File f = new File(ctx.getCacheDir(), "dw_pick_" + System.currentTimeMillis() + ".jpg");
            try (FileOutputStream out = new FileOutputStream(f)) { b.compress(Bitmap.CompressFormat.JPEG, quality, out); }
            b.recycle();
            Log.i(TAG, "prepared " + f.length() + " bytes");
            return f.getAbsolutePath();
        } catch (Throwable t) {
            Log.e(TAG, "prepare failed", t);
            return null;
        }
    }
}
