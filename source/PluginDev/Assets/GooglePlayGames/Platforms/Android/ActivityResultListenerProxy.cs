#if UNITY_ANDROID
using System;
using UnityEngine;

namespace GooglePlayGames.Android 
{
    class ActivityResultListenerProxy : AndroidJavaProxy
    {
        private Action<TurnBasedMatchInfo> mCallback;

        public ActivityResultListenerProxy(Action<TurnBasedMatchInfo> callback) : 
            base ( "com.cedco.six.player.UnityPlayerNativeActivity$ActivityResultListener" )
        {
            mCallback = callback;
        }
        public void onMatchSelected(AndroidJavaObject result )
        {
            Debug.Log("ActivityResultListenerProxy::onMatchSelected");
            mCallback.Invoke( new TurnBasedMatchInfo( result ) );
        }

        public void onInvitationSelected(AndroidJavaObject result )
        {
        }
    }
}
#endif