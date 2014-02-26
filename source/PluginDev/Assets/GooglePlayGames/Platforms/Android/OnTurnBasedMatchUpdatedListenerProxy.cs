
using UnityEngine;

namespace GooglePlayGames.Android 
{
	internal class OnTurnBasedMatchUpdatedListenerProxy: AndroidJavaProxy {
		
		private AndroidClient m_AndroidClient;


		internal OnTurnBasedMatchUpdatedListenerProxy( AndroidClient androidClient ) :
			base("com.google.android.gms.games.multiplayer.turnbased.OnTurnBasedMatchUpdatedListener") 
		{
			m_AndroidClient = androidClient;
		}


		// onTurnBasedMatchUpdated(int statusCode, TurnBasedMatch match)
		private void onTurnBasedMatchUpdated(int statusCode, AndroidJavaObject match)
		{
			m_AndroidClient.OnTurnBasedMatchUpdated( statusCode,  match);
		}
	}
}

