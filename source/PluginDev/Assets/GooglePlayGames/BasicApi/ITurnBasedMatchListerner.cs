
namespace GooglePlayGames.BasicApi 
{
	public interface ITurnBasedMatchListerner
	{
		byte[] GetInitialData( TurnBasedMatchInfo match);
		string GetInitialParticipant( TurnBasedMatchInfo match);
		
		void OnTurnBasedMatchInitiated(int statusCode, TurnBasedMatchInfo match);
		void OnTurnBasedMatchUpdated(int statusCode, TurnBasedMatchInfo match);
	}
}

