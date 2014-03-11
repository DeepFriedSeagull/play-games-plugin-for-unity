using UnityEngine;
using System.Collections.Generic;
using System;
using GooglePlayGames.OurUtils;


#if UNITY_ANDROID
namespace GooglePlayGames.Android 
{
    class LoadMatchesResultProxy : AndroidJavaProxy, IResultCallback 
    {
        
        private AndroidClient m_AndroidClient;
        Action< List< TurnBasedMatchInfo > > m_Callback;

        public LoadMatchesResultProxy( AndroidClient androidClient, Action< List< TurnBasedMatchInfo > > callback ) :
            base(JavaUtil.ResultCallbackClass) 
        {
            m_AndroidClient = androidClient;
            m_Callback = callback;
        }

        
        public void onResult(AndroidJavaObject result )
        {
            Logger.d("LoadMatchesResultProxy::onResult invoked");
            Logger.d("    result=" + result);
            int statusCode = JavaUtil.GetStatusCode(result);

            List<TurnBasedMatchInfo> resultMatches = new List<TurnBasedMatchInfo>();

            //LoadMatchesResponse
            AndroidJavaObject loadMatchesResponse = JavaUtil.CallNullSafeObjectMethod(result, "getMatches" );
            if (loadMatchesResponse != null) 
            {
                //TurnBasedMatchBuffer
                AndroidJavaObject myTurnMatchesBuffer = JavaUtil.CallNullSafeObjectMethod(loadMatchesResponse, "getMyTurnMatches" );             
                if (myTurnMatchesBuffer !=null)
                {
                    int iCount = myTurnMatchesBuffer.Call<int>("getCount");
                    Debug.Log( string.Format("{0} Match(es) found", iCount) );

                    for (int iMatchIdx = 0; iMatchIdx<iCount; ++iMatchIdx)
                    {
                        AndroidJavaObject turnBasedMatch = JavaUtil.CallNullSafeObjectMethod(myTurnMatchesBuffer, "get", iMatchIdx );                     
                        if ( turnBasedMatch != null ) 
                        {
                            resultMatches.Add ( new TurnBasedMatchInfo( turnBasedMatch ) );

                            turnBasedMatch.Dispose();
                        }
                    }

                    myTurnMatchesBuffer.Call("close");
                    myTurnMatchesBuffer.Dispose();

                    if (m_Callback != null )
                    {
                        PlayGamesHelperObject.RunOnGameThread(() => {
                            m_Callback.Invoke( resultMatches );
                        });
                    }

                }
                loadMatchesResponse.Dispose();
            }
        }

    }
}
#endif