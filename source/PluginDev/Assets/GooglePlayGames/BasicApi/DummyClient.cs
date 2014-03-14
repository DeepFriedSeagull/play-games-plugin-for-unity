/*
 * Copyright (C) 2013 Google Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Collections;
using GooglePlayGames;

namespace GooglePlayGames.BasicApi {
    public class DummyClient : IPlayGamesClient {
        public void Authenticate(System.Action<bool> callback, bool silent) {
            if (callback != null) {
                callback.Invoke(false);
            }
        }

        public bool IsAuthenticated() {
            return false;
        }
        
        public void SignOut() {
        }

        public string GetUserId() {
            return "DummyID";
        }

        public string GetUserDisplayName() {
            return "Player";
        }

        public List<Achievement> GetAchievements() {
            return new List<Achievement>();
        }

        public Achievement GetAchievement(string achId) {
            return null;
        }

        public void UnlockAchievement(string achId, Action<bool> callback) {
            if (callback != null) {
                callback.Invoke(false);
            }
        }

        public void RevealAchievement(string achId, Action<bool> callback) {
            if (callback != null) {
                callback.Invoke(false);
            }
        }

        public void IncrementAchievement(string achId, int steps, Action<bool> callback) {
            if (callback != null) {
                callback.Invoke(false);
            }
        }

        public void ShowAchievementsUI() {}
        public void ShowLeaderboardUI(string lbId) {}

		#region CGJ
        // Show player selection UI
        public void ShowPlayerSelectionUI(int minPlayers, int maxPlayers) {}
        // Show Match Inbox UI
        public void ShowMatchInboxUI(Action<TurnBasedMatchInfo> callback) {}
		// Set Client Callbaks for TurnBasedGame
		public void SetTurnBasedMatchListerner( ITurnBasedMatchListerner listerner ) {}
		// Take a turn in turn based game
		public void TBMG_TakeTurn( string matchId, byte[] newData, string pendingParticipant ) {}
        // Take a turn in TBMG and declare it as a win for winningParticipantId
        public void TBMG_TakeFinalTurnAndDeclareWinner( string matchId, byte[] newData, string winningParticipantId) {}
        // Finish a match, accepting the result        
        public void TBMG_FinishMatch( string matchId ) {}
		// Return the TurnBasedMatchInfo for the launching Intent
		public TurnBasedMatchInfo GetTurnBasedMatch() { return null; }
        // Return a list of all pending TurnBasedMatch
        public void GetPendingMatches( Action< List< TurnBasedMatchInfo > > callback) {}
   		#endregion
		
        public void SubmitScore(string lbId, long score, Action<bool> callback) {
            if (callback != null) {
                callback.Invoke(false);
            }
        }

        public void LoadState(int slot, OnStateLoadedListener listener) {
            if (listener != null) {
                listener.OnStateLoaded(false, slot, null);
            }
        }

        public void UpdateState(int slot, byte[] data, OnStateLoadedListener listener) {}

        public void SetCloudCacheEncrypter(BufferEncrypter encrypter) {}
	 }
}

