#if UNITY_ANDROID
using UnityEngine;


namespace GooglePlayGames.Android 
{
    public interface IResultCallback
    {
        void onResult(AndroidJavaObject result );
    }
}

#endif