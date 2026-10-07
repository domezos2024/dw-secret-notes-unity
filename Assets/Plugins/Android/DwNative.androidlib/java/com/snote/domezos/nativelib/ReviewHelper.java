package com.snote.domezos.nativelib;

import android.app.Activity;
import android.content.ActivityNotFoundException;
import android.content.Intent;
import android.net.Uri;
import com.google.android.play.core.review.ReviewManager;
import com.google.android.play.core.review.ReviewManagerFactory;

public final class ReviewHelper {
    private ReviewHelper() {}

    public static void launch(Activity a, Runnable done) {
        try {
            ReviewManager m = ReviewManagerFactory.create(a);
            m.requestReviewFlow().addOnCompleteListener(t -> {
                if (t.isSuccessful()) m.launchReviewFlow(a, t.getResult()).addOnCompleteListener(x -> done.run());
                else { openStore(a); done.run(); }
            });
        } catch (Throwable e) { openStore(a); done.run(); }
    }

    public static void openStore(Activity a) {
        String pkg = a.getPackageName();
        try {
            a.startActivity(new Intent(Intent.ACTION_VIEW, Uri.parse("market://details?id=" + pkg)).addFlags(Intent.FLAG_ACTIVITY_NEW_TASK));
        } catch (ActivityNotFoundException e) {
            a.startActivity(new Intent(Intent.ACTION_VIEW, Uri.parse("https://play.google.com/store/apps/details?id=" + pkg)).addFlags(Intent.FLAG_ACTIVITY_NEW_TASK));
        }
    }
}
