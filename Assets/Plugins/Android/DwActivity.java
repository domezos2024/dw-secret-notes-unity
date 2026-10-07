package com.snote.domezos.unity;

import android.content.Intent;
import com.unity3d.player.UnityPlayer;
import com.unity3d.player.UnityPlayerActivity;

public class DwActivity extends UnityPlayerActivity {
    @Override protected void onNewIntent(Intent intent) {
        super.onNewIntent(intent);
        setIntent(intent);
        UnityPlayer.UnitySendMessage("DwNative", "OnNewIntent", "");
    }
}
