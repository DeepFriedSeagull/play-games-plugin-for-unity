#if UNITY_ANDROID
using UnityEngine;

using GooglePlayGames.OurUtils;

namespace GooglePlayGames.Android 
{
	internal class OnTurnBasedMatchUpdatedResultProxy: AndroidJavaProxy, IResultCallback {
		
		private AndroidClient m_AndroidClient;


        internal OnTurnBasedMatchUpdatedResultProxy( AndroidClient androidClient ) :
            base(JavaUtil.ResultCallbackClass) 
		{
			m_AndroidClient = androidClient;
		}

        public void onResult(AndroidJavaObject result )
        {
            Logger.d("OnTurnBasedMatchUpdatedListenerProxy::onResult invoked");
            Logger.d("    result=" + result);
            int statusCode = JavaUtil.GetStatusCode(result);
            AndroidJavaObject match = JavaUtil.CallNullSafeObjectMethod(result, "getMatch" );
            m_AndroidClient.OnTurnBasedMatchUpdated(statusCode, match);
            if (match != null) {
                match.Dispose();
            }
        }

//		// onTurnBasedMatchUpdated(int statusCode, TurnBasedMatch match)
//		private void onTurnBasedMatchUpdated(int statusCode, AndroidJavaObject match)
//		{
//			m_AndroidClient.OnTurnBasedMatchUpdated( statusCode,  match);
//		}
	}
}

#endif