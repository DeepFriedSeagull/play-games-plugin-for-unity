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

#if UNITY_ANDROID
using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using GooglePlayGames.BasicApi;
using GooglePlayGames.OurUtils;
using System.Text;

namespace GooglePlayGames.Android {
    public class AndroidClient : IPlayGamesClient {
        GameHelperManager mGHManager = null;
        
        // In what state of the authentication process are we?
        enum AuthState {
            NoAuth, // not authenticated
            AuthPending, // we want to authenticate, but GameHelper is busy
            InProgress, // we are authenticating
            LoadingAchs, // we are signed in and are doing the initial achievement load
            Done // we are authenticated!
        };
        AuthState mAuthState = AuthState.NoAuth;

        // are we trying silent authentication? If so, then we can't show UIs in the process:
        // we have to fail instead
        bool mSilentAuth = false;
		
        // user's ID and display name (retrieved on sign in)
        string mUserId = null, mUserDisplayName = null;

        // the auth callback that we have to call at the end of the auth process
        System.Action<bool> mAuthCallback = null;

        // the achievements we've loaded
        AchievementBank mAchievementBank = new AchievementBank();

        // Sometimes we have to execute an action on the UI thread involving GamesClient,
        // but we might hit that unlucky moment when GamesClient is still in the process
        // of connecting and can't take API calls. So, if that happens, we queue the
        // actions here and execute when we get onSignInSucceeded or onSignInFailed,.
        List<Action> mActionsPendingSignIn = new List<Action>();
        
        // Are we currently in the process of signing out?
        private bool mSignOutInProgress = false;

        // Result code for child activities whose result we don't care about
        const int RC_SELECT_PLAYERS = 9002;
        const int RC_MATCH_INBOX =  9003;
        const int RC_UNUSED = 9999;
		
		// Horrible design hack
		// Boolean reprensenting if we are creating a turned base game
		bool m_pendingTBMGCreation = false;

		// Turned base game creation listener
		ITurnBasedMatchListerner mTurnBasedListener;

        public AndroidClient() {
            RunOnUiThread(() => {
                Logger.d("Initializing Android Client.");
                Logger.d("Creating GameHelperManager to manage GameHelper.");
                mGHManager = new GameHelperManager(this);
                Logger.d("GameHelper manager is set up.");
            });
            // now we wait for the result of the initial auth, which will trigger
            // a call to either OnSignInSucceeded or OnSignInFailed
        }

        // called from game thread
        public void Authenticate(System.Action<bool> callback, bool silent) {
            if (mAuthState != AuthState.NoAuth) {
                Logger.w("Authenticate() called while an authentication process was active. " + 
                        mAuthState);
                mAuthCallback = callback;
                return;
            }

            // make sure the helper GameObject is ready (we use it for the auth callback)
            Logger.d("Making sure PlayGamesHelperObject is ready.");
            PlayGamesHelperObject.CreateObject();
            Logger.d("PlayGamesHelperObject created.");

            mSilentAuth = silent;
            Logger.d("AUTH: starting auth process, silent=" + mSilentAuth);
            RunOnUiThread(() => {
                switch (mGHManager.State) {
                    case GameHelperManager.ConnectionState.Connected:
                        Logger.d("AUTH: already connected! Proceeding to achievement load phase.");
                        mAuthCallback = callback;
                        DoInitialAchievementLoad();
                        break;
                    case GameHelperManager.ConnectionState.Connecting:
                        Logger.d("AUTH: connection in progress; auth now pending.");
                        mAuthCallback = callback;
                        mAuthState = AuthState.AuthPending;
                        // we'll do the right thing in OnSignInSucceeded/Failed
                        break;
                    default:
                        mAuthCallback = callback;
                        if (mSilentAuth) {
                            Logger.d("AUTH: not connected and silent=true, so failing.");
                            mAuthState = AuthState.NoAuth;
                            InvokeAuthCallback(false);
                        } else {
                            Logger.d("AUTH: not connected and silent=false, so starting flow.");
                            mAuthState = AuthState.InProgress;
                            mGHManager.BeginUserInitiatedSignIn();
                            // we'll do the right thing in OnSignInSucceeded/Failed
                        }
                        break;
                }
            });
        }

        // call from UI thread only!
        private void DoInitialAchievementLoad() {
            Logger.d("AUTH: Now performing initial achievement load...");
            mAuthState = AuthState.LoadingAchs;            
            mGHManager.CallGmsApiWithResult("games.Games", "Achievements", "load",
                    new OnAchievementsLoadedResultProxy(this), false);
            Logger.d("AUTH: Initial achievement load call made.");
        }
        
        // UI thread
        private void OnAchievementsLoaded(int statusCode, AndroidJavaObject buffer) {
            if (mAuthState == AuthState.LoadingAchs) {
                Logger.d("AUTH: Initial achievement load finished.");

                if (statusCode == JavaConsts.STATUS_OK || statusCode == JavaConsts.STATUS_STALE_DATA) {
                    // successful load (either from network or local cache)
                    Logger.d("Processing buffer.");
                    mAchievementBank.ProcessBuffer(buffer);
                    Logger.d("AUTH: Auth process complete!");
                    mAuthState = AuthState.Done;
                    InvokeAuthCallback(true);
                } else {
                    Logger.w("AUTH: Failed to load achievements, status code " + statusCode);
                    mAuthState = AuthState.NoAuth;
                    InvokeAuthCallback(false);
                }
            } else {
                Logger.w("OnAchievementsLoaded called unexpectedly in auth state " + mAuthState);
            }
        }

        // UI thread
        private void InvokeAuthCallback(bool success) {
            if (mAuthCallback == null) 
            {
                Logger.w("InvokeAuthCallback invoke with an empty callback ");
                return;
            }
            Logger.d("AUTH: Calling auth callback: success=" + success);
            System.Action<bool> cb = mAuthCallback;

			// CJG: WIP: Setting null will cause problems with notifications
			// Indeed notification can cause account switch, so we need to be aware 
			// of loggin success so we need the cal back to be called on the onStart/onResume 
            // mAuthCallback = null;

            PlayGamesHelperObject.RunOnGameThread(() => {
                cb.Invoke(success);
            });

        }

        private void RetrieveUserInfo() {
            Logger.d("Attempting to retrieve player info.");
            
            using (AndroidJavaObject playerObj = mGHManager.CallGmsApi<AndroidJavaObject>(
                    "games.Games", "Players", "getCurrentPlayer")) {
        

                mUserId = playerObj.Call<string>("getPlayerId");
                Logger.d("Player ID: " + mUserId);


                mUserDisplayName = playerObj.Call<string>("getDisplayName");
                Logger.d("Player display name: " + mUserDisplayName);
            }
        }

        private void ResetUserInfo() {
            mUserId = null;
            mUserDisplayName = null;
        }

        // called (on the UI thread) by GameHelperManager to notify us that sign in succeeded
        internal void OnSignInSucceeded() {
            Logger.d("AndroidClient got OnSignInSucceeded.");

            if (mAuthState == AuthState.AuthPending || mAuthState == AuthState.InProgress) {
                Logger.d("AUTH: Auth succeeded. Proceeding to achievement loading.");
                
                RetrieveUserInfo();
                DoInitialAchievementLoad();
            } else if (mAuthState == AuthState.LoadingAchs) {
                Logger.w("AUTH: Got OnSignInSucceeded() while in achievement loading phase (unexpected).");
                Logger.w("AUTH: Trying to fix by issuing a new achievement load call.");
                
                RetrieveUserInfo();
                DoInitialAchievementLoad();
            } else {
                // we will hit this case during the normal lifecycle (for example, Activity
                // was brought to the foreground and sign in has succeeded even though
                // we were not in an auth flow).
                Logger.d("Normal lifecycle OnSignInSucceeded received. AuthState = " + mAuthState.ToString() );

                // CJG Checking we are connected to same user
				// Notification can switch account
                using (AndroidJavaObject playerObj = mGHManager.CallGmsApi<AndroidJavaObject>(
                    "games.Games", "Players", "getCurrentPlayer"))
                {
                    if (playerObj.Call<string>("getPlayerId") != mUserId )
                    {
                        // Workaround: setting the mAutState to InProgress and Calling OnSignInSucceeded again.
                        
                        Logger.d("Not the same player connected!! Fixing it by calling OnSignInSucceeded again.");
                        mAuthState = AuthState.InProgress;
                        OnSignInSucceeded();
                    }
                }


				// CJG TODO: This local var is horrible
                if ( m_pendingTBMGCreation == true )
                {
                    m_pendingTBMGCreation = false;
					CreateTurnBasedMatch();
                }

                RunPendingActions();

            }
        }

        // called (on the UI thread) by GameHelperManager to notify us that sign in failed
        internal void OnSignInFailed() {
            Logger.d("AndroidClient got OnSignInFailed.");
            ResetUserInfo();
            if (mAuthState == AuthState.AuthPending) {
                // we have yet to start the auth flow
                if (mSilentAuth) {
                    Logger.d("AUTH: Auth flow was pending, but silent=true, so failing.");
                    mAuthState = AuthState.NoAuth;
                    InvokeAuthCallback(false);
                } else {
                    Logger.d("AUTH: Auth flow was pending and silent=false, so doing noisy auth.");
                    mAuthState = AuthState.InProgress;
                    mGHManager.BeginUserInitiatedSignIn();
                }
            } else if (mAuthState == AuthState.InProgress) {
                // authentication was in progress, but failed: notify callback
                Logger.d("AUTH: FAILED!");
                mAuthState = AuthState.NoAuth;
                InvokeAuthCallback(false);
            } else if (mAuthState == AuthState.LoadingAchs) {
                // we were loading achievements and got disconnected: notOnTurnBasedMatchInitiatedResultProxyify callback
                Logger.d("AUTH: FAILED (while loading achievements).");
                mAuthState = AuthState.NoAuth;
                InvokeAuthCallback(false);
            } else if (mAuthState == AuthState.NoAuth) {
                // we will hit this case during the normal lifecycle (for example, Activity
                // was brought to the foreground and sign in has failed).
                Logger.d("Normal OnSignInFailed received.");
            } else if (mAuthState == AuthState.Done) {
                // we lost authentication (for example, the token might have expired,
                // or the user revoked it)
                Logger.e("Authentication has been lost!");
                mAuthState = AuthState.NoAuth;
            }
        }

        // Called from Game Thread
		// Returned the turn base match info
		public TurnBasedMatchInfo GetTurnBasedMatch()
		{
            return mGHManager.GetTurnBasedMatch();
		}


        // Runs any actions pending in the mActionsPendingSignIn queue
        private void RunPendingActions() {
            if (mActionsPendingSignIn.Count > 0) {
                Logger.d("Running pending actions on the UI thread.");
                while (mActionsPendingSignIn.Count > 0) {
                    Action a = mActionsPendingSignIn[0];
                    mActionsPendingSignIn.RemoveAt(0);
                    a.Invoke();
                }
                Logger.d("Done running pending actions on the UI thread.");
            } else {
                Logger.d("No pending actions to run on UI thread.");
            }
        }

        // runs on the game thread
        public bool IsAuthenticated() {
            return mAuthState == AuthState.Done && !mSignOutInProgress;
        }
        
        public void SignOut() {
            Logger.d("AndroidClient.SignOut");
            ResetUserInfo();
            mSignOutInProgress = true;
            RunWhenConnectionStable(() => {
                Logger.d("Calling GHM.SignOut");
                mGHManager.SignOut();
                mAuthState = AuthState.NoAuth;
                mSignOutInProgress = false;
                Logger.d("Now signed out.");
            });
        }
        
        // Returns the game's Activity
        internal AndroidJavaObject GetActivity() {
            using (AndroidJavaClass jc = new AndroidJavaClass("com.unity3d.player.UnityPlayer")) {
                return jc.GetStatic<AndroidJavaObject>("currentActivity");
            }
        }

        internal void RunOnUiThread(System.Action action) {
            using (AndroidJavaObject activity = GetActivity()) {
                activity.Call("runOnUiThread", new AndroidJavaRunnable(action));
            }
        }

        private class OnAchievementsLoadedResultProxy : AndroidJavaProxy {
            AndroidClient mOwner;

            internal OnAchievementsLoadedResultProxy(AndroidClient c) :
                    base(JavaUtil.ResultCallbackClass) {
                mOwner = c;
            }

            public void onResult(AndroidJavaObject result) {
                Logger.d("OnAchievementsLoadedResultProxy invoked");
                Logger.d("    result=" + result);
                int statusCode = JavaUtil.GetStatusCode(result);
                AndroidJavaObject achBuffer = JavaUtil.CallNullSafeObjectMethod(result, 
                        "getAchievements");
                mOwner.OnAchievementsLoaded(statusCode, achBuffer);
                if (achBuffer != null) {
                    achBuffer.Call("close");
                    achBuffer.Dispose();
                }
            }
        }


        // Runs the given action on the UI thread when the state of the GameHelper connection
        // becomes stable (i.e. not in the temporary lapse between Activity startup and
        // connection). So when the action runs, we will either be definitely signed in,
        // or have definitely failed to sign in.
        private void RunWhenConnectionStable(Action a) {
            RunOnUiThread(() => {
                if (mGHManager.State == GameHelperManager.ConnectionState.Connecting) {
                    // we're in the middle of establishing a connection, so we'll
                    // have to queue this action to execute once the connection is
                    // established (or fails)
                    Logger.d("Action scheduled for later (connection currently in progress).");
                    mActionsPendingSignIn.Add(a);
                } else {
                    // connection is in a definite state, so we can run it right away
                    a.Invoke();
                }
            });
        }

        private void CallClientApi(string desc, Action call, Action<bool> callback) {
            Logger.d("Requesting API call: " + desc);
            RunWhenConnectionStable(() => {
                // we got a stable connection state to the games service
                // (either connected or disconnected, but not in progress).
                if (mGHManager.IsConnected()) {
                    // we are connected, so make the API call
                    Logger.d("Connected! Calling API: " + desc);
                    call.Invoke();
                    if (callback != null) {
                        PlayGamesHelperObject.RunOnGameThread(() => {
                            callback.Invoke(true);
                        });
                    }
                } else {
                    // we are not connected, so fail the API call
                    Logger.w("Not connected! Failed to call API :" + desc);
                    if (callback != null) {
                        PlayGamesHelperObject.RunOnGameThread(() => {
                            callback.Invoke(false);
                        });
                    }
                }
            });
        }

        // called from game thread
        public string GetUserId() {
            return mUserId;
        }

        // called from game thread
        public string GetUserDisplayName() {
            return mUserDisplayName;
        }

        // called from game thread
        public void UnlockAchievement(string achId, Action<bool> callback) {
            // if the local cache says it's unlocked, we don't have to do anything
            Logger.d("AndroidClient.UnlockAchievement: " + achId);
            Achievement a = GetAchievement(achId);
            if (a != null && a.IsUnlocked) {
                Logger.d("...was already unlocked, so no-op.");
                if (callback != null) {
                    callback.Invoke(true);
                }
                return;
            }

            CallClientApi("unlock ach " + achId, () => {
                mGHManager.CallGmsApi("games.Games", "Achievements", "unlock", achId);
            }, callback);

            // update local cache
            a = GetAchievement(achId);
            if (a != null) {
                a.IsUnlocked = a.IsRevealed = true;
            }
        }

        // called from game thread
        public void RevealAchievement(string achId, Action<bool> callback) {
            Logger.d("AndroidClient.RevealAchievement: " + achId);
            Achievement a = GetAchievement(achId);
            if (a != null && a.IsRevealed) {
                Logger.d("...was already revealed, so no-op.");
                if (callback != null) {
                    callback.Invoke(true);
                }
                return;
            }

            CallClientApi("reveal ach " + achId, () => {
                mGHManager.CallGmsApi("games.Games", "Achievements", "reveal", achId);
            }, callback);

            // update local cache
            a = GetAchievement(achId);
            if (a != null) {
                a.IsRevealed = true;
            }
        }


        // called from game thread
        public void IncrementAchievement(string achId, int steps, Action<bool> callback) {
            Logger.d("AndroidClient.IncrementAchievement: " + achId + ", steps " + steps);
            
            CallClientApi("increment ach " + achId, () => {
                mGHManager.CallGmsApi("games.Games", "Achievements", "increment",
                        achId, steps);
            }, callback);
            
            // update local cache
            Achievement a = GetAchievement(achId);
            if (a != null) {
                a.CurrentSteps += steps;
                if (a.CurrentSteps >= a.TotalSteps) {
                    a.CurrentSteps = a.TotalSteps;
                }
            }
        }

        // called from game thread
        public List<Achievement> GetAchievements() {
            return mAchievementBank.GetAchievements();
        }

        // called from game thread
        public Achievement GetAchievement(string achId) {
            return mAchievementBank.GetAchievement(achId);
        }
        
        // called from game thread
        public void ShowAchievementsUI() {
            Logger.d("AndroidClient.ShowAchievementsUI.");
            CallClientApi("show achievements ui", () => {
                using (AndroidJavaObject intent = mGHManager.CallGmsApi<AndroidJavaObject>(
                        "games.Games", "Achievements", "getAchievementsIntent")) {
                    using (AndroidJavaObject activity = GetActivity()) {
                        Logger.d("About to show achievements UI with intent " + intent +
                            ", activity " + activity);
                        if (intent != null && activity != null) {
                            activity.Call("startActivityForResult", intent, RC_UNUSED);
                        }
                    }
                }
            }, null);
        }
        
        // called from UI thread
        private AndroidJavaObject GetLeaderboardIntent(string lbId) {
            return (lbId == null) ?
                mGHManager.CallGmsApi<AndroidJavaObject>(
                    "games.Games", "Leaderboards", "getAllLeaderboardsIntent") :
                mGHManager.CallGmsApi<AndroidJavaObject>(
                    "games.Games", "Leaderboards", "getLeaderboardIntent", lbId);
        }

        // called from game thread
        public void ShowLeaderboardUI(string lbId) {
            Logger.d("AndroidClient.ShowLeaderboardUI, lb=" + (lbId == null ? "(all)" : lbId));
            CallClientApi("show LB ui", () => {
                using (AndroidJavaObject intent = GetLeaderboardIntent(lbId)) {
                    using (AndroidJavaObject activity = GetActivity()) {
                        Logger.d("About to show LB UI with intent " + intent +
                            ", activity " + activity);
                        if (intent != null && activity != null) {
                            activity.Call("startActivityForResult", intent, RC_UNUSED);
                        }
                    }
                }
            }, null);
        }
		
		public void SetTurnBasedMatchListerner( ITurnBasedMatchListerner listerner )
		{
			mTurnBasedListener = listerner;
		}
        
        // called from UI thread
        private AndroidJavaObject getSelectOpponentsIntent(int minPlayers, int maxPlayers)
        {
            return mGHManager.CallGmsApi<AndroidJavaObject>(
                "games.Games", "TurnBasedMultiplayer", "getSelectOpponentsIntent", minPlayers, maxPlayers, false);
        }

        // called from game thread
        public void ShowPlayerSelectionUI(int minPlayers, int maxPlayers)
        {  
            Logger.d("AndroidClient.ShowPlayerSelectionUI, " + minPlayers.ToString() + "- " + maxPlayers.ToString() );

            CallClientApi("show PlaySelection ui", () => {
                using (AndroidJavaObject intent = getSelectOpponentsIntent(minPlayers, maxPlayers) ) { 
                        using (AndroidJavaObject activity = GetActivity() ) {
                            Logger.d("About to show PlayerSelection UI with intent " + intent + ", activity " + activity);
                            if (intent != null && activity != null) {
                                activity.Call("startActivityForResult", intent, RC_SELECT_PLAYERS);
                            }
                        }
                    }
            }, null);

            m_pendingTBMGCreation = true;
        }

        //From Game thread
        public void GetPendingMatches( Action< List< TurnBasedMatchInfo > > callback)
        {
            CallClientApi("loadMatchesByStatus", () => 
            {
                int[] matchTurnStatuses = new int [] {1};
                mGHManager.CallGmsApiWithResult("games.Games", "TurnBasedMultiplayer", "loadMatchesByStatus",
                                                new LoadMatchesResultProxy (this, callback), matchTurnStatuses );
            }, null );
        }
        
		// called from UI thread Only
		private void CreateTurnBasedMatch()
		{
			Logger.d("AndroidClient.CreateTurnBasedMatch");

            try
            {
    			using (AndroidJavaObject activity = GetActivity() ){
                    using ( AndroidJavaObject matchConfig = activity.Call<AndroidJavaObject>("getPendingTurnBasedMatchConfig") ) {
                        mGHManager.CallGmsApiWithResult( "games.Games", "TurnBasedMultiplayer", "createMatch",
                                                new OnTurnBasedMatchInitiatedResultProxy(this), matchConfig);
                    }
                }
            }
            catch ( System.Exception e)
            {
                Debug.LogError( "CreateTurnBasedMatch: " + e.ToString() );  
            }
		}

		// called from UI thread
		public void OnTurnBasedMatchInitiated(int statusCode, AndroidJavaObject match)
		{
			Logger.d("AndroidClient.OnTurnBasedMatchInitiated");

			TurnBasedMatchInfo turnBasedMatchInfo = new TurnBasedMatchInfo( match );

			if ( mTurnBasedListener == null)
			{
				Logger.e("Invalid mTurnBasedListener setting");
				return;

			}
			// Chose the first player
			string prendingPlayer = mTurnBasedListener.GetInitialParticipant( turnBasedMatchInfo );
			// Define StartUp Data
			byte[] initData = mTurnBasedListener.GetInitialData( turnBasedMatchInfo ); 


			// public void takeTurn (OnTurnBasedMatchUpdatedListener listener, String matchId, byte[] matchData, String pendingParticipantId)
            mGHManager.CallGmsApiWithResult( "games.Games", "TurnBasedMultiplayer", "takeTurn", new OnTurnBasedMatchUpdatedResultProxy( this ),
			                       turnBasedMatchInfo.Guid, initData, prendingPlayer );

			
			PlayGamesHelperObject.RunOnGameThread( () => {
				mTurnBasedListener.OnTurnBasedMatchInitiated(statusCode, turnBasedMatchInfo );
			} );

		}
		
		// called from UI thread
		public void OnTurnBasedMatchUpdated(int statusCode, AndroidJavaObject match)
		{
			Logger.d("AndroidClient.OnTurnBasedMatchUpdated");
           
            TurnBasedMatchInfo turnBasedMatchInfo = null;

            if (statusCode == GoogleStatusCode.STATUS_OK )
            {
                turnBasedMatchInfo = new TurnBasedMatchInfo( match );
    			Debug.Log( turnBasedMatchInfo.ToString() );
            }
    				
			PlayGamesHelperObject.RunOnGameThread( () => {
                if ( mTurnBasedListener != null )
                {
				    mTurnBasedListener.OnTurnBasedMatchUpdated(statusCode, turnBasedMatchInfo );
                }
                else
                {
                    Debug.LogError( "TurnBasedListener is not set." );
                }

			} );
            
		}


		// Called from the Game thread
		public void TBMG_TakeTurn( string matchId, byte[] newData, string pendingParticipant ) 
		{
            CallClientApi("TBMG_TakeTurn", () => {
                mGHManager.CallGmsApiWithResult( "games.Games", "TurnBasedMultiplayer", "takeTurn", new OnTurnBasedMatchUpdatedResultProxy( this ),
				       matchId, newData, pendingParticipant );
			}, null );

		}
		
		 // Take a final turn in TBMG and declare a winner       
        // Called from the Game thread
        public void TBMG_TakeFinalTurnAndDeclareWinner( string matchId, byte[] newData, string winningParticipantId)        
        {
            // Not every participant is required to have a result, 
            // but providing results for participants who are not in the match is an error.
            // Setting the winner
            AndroidJavaObject[] participantResultsArray  = new AndroidJavaObject[]
            {
                new AndroidJavaObject("com.google.android.gms.games.multiplayer.ParticipantResult",
                                      winningParticipantId, 0, -1 )
            };
       
            CallClientApi("TBMG_TakeFinalTurnAndWin", () => {
                mGHManager.CallGmsApiWithResult( "games.Games", "TurnBasedMultiplayer", "finishMatch",
                                                new OnTurnBasedMatchUpdatedResultProxy( this ),
                                                matchId, newData,
                                                participantResultsArray
                                                );
            }, null );
        }

        // Finish a match, accepting the result    
		// Called from the Game Thread    
        public void TBMG_FinishMatch( string matchId )
        {    

            CallClientApi("TBMG_FinishMatch", () => {
                mGHManager.CallGmsApiWithResult( "games.Games", "TurnBasedMultiplayer", "finishMatch",
                                                new OnTurnBasedMatchUpdatedResultProxy( this ),
                                                matchId );
            }, null );

        }

        // called from game thread
        public void ShowMatchInboxUI( Action<TurnBasedMatchInfo> callback )
        {
            Logger.d("AndroidClient.ShowMatchInboxUI");

            GetActivity().Call("SetActivityResultListener", new ActivityResultListenerProxy( callback ) );
            
            CallClientApi("show match inbox ui", () =>  {
                AndroidJavaObject intent = mGHManager.CallGmsApi<AndroidJavaObject>( "games.Games", "TurnBasedMultiplayer", "getInboxIntent");
                AndroidJavaObject activity = GetActivity();
                Logger.d("About to show ShowMatchInbox UI with intent " + intent + ", activity " + activity);
                if (intent != null && activity != null) {
                activity.Call("startActivityForResult", intent, RC_MATCH_INBOX);
                }
            }, null);
        }


        // called from game thread
        public void SubmitScore(string lbId, long score, Action<bool> callback) {
            Logger.d("AndroidClient.SubmitScore, lb=" + lbId + ", score=" + score);
            CallClientApi("submit score " + score + ", lb " + lbId, () => {
                mGHManager.CallGmsApi("games.Games", "Leaderboards", 
                        "submitScore", lbId, score);
            }, callback);
        }

        // called from game thread
        public void LoadState(int slot, OnStateLoadedListener listener) {
            Logger.d("AndroidClient.LoadState, slot=" + slot);
            CallClientApi("load state slot=" + slot, () => {
                OnStateResultProxy proxy = new OnStateResultProxy(this, listener);
                mGHManager.CallGmsApiWithResult("appstate.AppStateManager", null, "load", 
                        proxy, slot);
            }, null);
        }

        // called from game thread. This is ONLY called internally (OnStateLoadedProxy
        // calls this). This is not part of the IPlayGamesClient interface.
        internal void ResolveState(int slot, string resolvedVersion, byte[] resolvedData,
                OnStateLoadedListener listener) {
            Logger.d(string.Format("AndroidClient.ResolveState, slot={0}, ver={1}, " +
                "data={2}", slot, resolvedVersion, resolvedData));
            CallClientApi("resolve state slot=" + slot, () => {
                mGHManager.CallGmsApiWithResult("appstate.AppStateManager", null, "resolve",
                new OnStateResultProxy(this, listener), slot, resolvedVersion, resolvedData);
            }, null);
        }

        // called from game thread
        public void UpdateState(int slot, byte[] data, OnStateLoadedListener listener) {
            Logger.d(string.Format("AndroidClient.UpdateState, slot={0}, data={1}",
                slot, Logger.describe(data)));
            CallClientApi("update state, slot=" + slot, () => {
                mGHManager.CallGmsApi("appstate.AppStateManager", null, "update", slot, data);
            }, null);

            // On Android, cloud writes always succeeds (because, in the worst case,
            // data gets cached locally to send to the cloud later)
            listener.OnStateSaved(true, slot);
        }

        public void SetCloudCacheEncrypter(BufferEncrypter encrypter) {
            Logger.d("Ignoring cloud cache encrypter (not used in Android)");
            // Not necessary in Android (since the library takes care of storing
            // data locally)
        }
    }
}

#endif
