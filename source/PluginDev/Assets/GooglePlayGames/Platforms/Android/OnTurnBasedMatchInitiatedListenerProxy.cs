
using UnityEngine;

namespace GooglePlayGames.Android {

	internal class OnTurnBasedMatchInitiatedListenerProxy: AndroidJavaProxy {

		private AndroidClient m_AndroidClient;
		
		internal OnTurnBasedMatchInitiatedListenerProxy(AndroidClient androidClient ) :
			base("com.google.android.gms.games.multiplayer.turnbased.OnTurnBasedMatchInitiatedListener") 
		{
			m_AndroidClient = androidClient;
		}

		//onTurnBasedMatchInitiated(int statusCode, TurnBasedMatch match)
		private void onTurnBasedMatchInitiated(int statusCode, AndroidJavaObject match)
		{
			m_AndroidClient.OnTurnBasedMatchInitiated( statusCode,  match);
		}
	}
}
