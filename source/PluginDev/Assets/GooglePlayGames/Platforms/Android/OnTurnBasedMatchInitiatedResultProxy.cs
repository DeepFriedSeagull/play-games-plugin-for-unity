#if UNITY_ANDROID
using UnityEngine;
using GooglePlayGames.OurUtils;

namespace GooglePlayGames.Android {

	internal class OnTurnBasedMatchInitiatedResultProxy: AndroidJavaProxy, IResultCallback {

		private AndroidClient m_AndroidClient;
		
		internal OnTurnBasedMatchInitiatedResultProxy(AndroidClient androidClient ) :
            base(JavaUtil.ResultCallbackClass) 
		{
			m_AndroidClient = androidClient;
		}
        
        public void onResult(AndroidJavaObject result )
        {
            
            Logger.d("OnTurnBasedMatchInitiatedResultProxy::onResult invoked");
            Logger.d("    result=" + result);
            int statusCode = JavaUtil.GetStatusCode(result);
            AndroidJavaObject match = JavaUtil.CallNullSafeObjectMethod(result, "getMatch" );
            m_AndroidClient.OnTurnBasedMatchInitiated( statusCode,  match);
            if (match != null) {
                match.Dispose();
            }
        }
	}
}
#endif