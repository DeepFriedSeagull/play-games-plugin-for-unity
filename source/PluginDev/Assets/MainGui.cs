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

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SocialPlatforms;
using System;
using GooglePlayGames;

public class MainGui : MonoBehaviour, GooglePlayGames.BasicApi.OnStateLoadedListener,
    GooglePlayGames.BasicApi.ITurnBasedMatchListerner {

    const int Margin = 20, Spacing = 10;
    const float FontSizeFactor = 30;
    const int GridCols = 2;
    const int GridRows = 10;

    public GUISkin GuiSkin;

    bool mStandby = false;
    string mStandbyMessage = "";
    string mStatus = "Ready.";
    string mMatchStatus = "";
    TurnBasedMatchInfo mPendingMatch;

    bool mHadCloudConflict = false;

    void Start()
    {

        PlayGamesPlatform.Activate();

        Debug.Log("Starting authentication");
        DoAuthenticate();
    }

    Rect CalcGrid(int col, int row) {
        return CalcGrid(col, row, 1, 1);
    }

    Rect CalcGrid(int col, int row, int colcount, int rowcount) {
        int cellW = (Screen.width - 2 * Margin - (GridCols - 1) * Spacing) / GridCols;
        int cellH = (Screen.height - 2 * Margin - (GridRows - 1) * Spacing) / GridRows;
        return new Rect(Margin + col * (cellW + Spacing),
            Margin + row * (cellH + Spacing),
            cellW + (colcount - 1) * (Spacing + cellW),
            cellH + (rowcount - 1) * (Spacing + cellH));
    }

    void ShowStandbyUi() {
        GUI.Label(CalcGrid(0, 2, 2, 1), mStandbyMessage);
    }

    void DrawTitle() {
        GUI.Label(CalcGrid(0, 0, 2, 1), "Play Games Unity Plugin - Smoke Test");
    }

    void DrawStatus() {
        GUI.Label(CalcGrid(0, 8, 2, 1), mStatus);
    }
    void DrawMatchStatus() {
        GUI.Label(CalcGrid(0, 9, 2, 1), mMatchStatus);
    }

    /*
    void OnApplicationPause(bool pauseStatus)
    {
        Debug(
        if(

    }
    */

    void ShowNotAuthUi() {
        DrawTitle();
        DrawStatus();
        DrawMatchStatus();

        if (GUI.Button(CalcGrid(1,1), "Authenticate")) 
        {
            DoAuthenticate();
        }
    }

    void ShowRegularUi() {
        DrawTitle();
        DrawStatus();
        DrawMatchStatus();

        if (GUI.Button(CalcGrid(0,1), "Ach Reveal")) {
            DoAchievementReveal();
        } else if (GUI.Button(CalcGrid(0,2), "Ach Unlock")) {
            DoAchievementUnlock();
        } else if (GUI.Button(CalcGrid(0,3), "Ach Increment")) {
            DoAchievementIncrement();
        } else if (GUI.Button(CalcGrid(0,4), "Ach Show UI")) {
            DoAchievementUI();
        }

        if (GUI.Button(CalcGrid(1,1), "Post Score")) {
            DoPostScore();
        } else if (GUI.Button(CalcGrid(1,2), "LB Show UI")) {
            DoLeaderboardUI();
        } else if (GUI.Button(CalcGrid(1,3), "Cloud Save")) {
            DoCloudSave();
        } else if (GUI.Button(CalcGrid(1,4), "Cloud Load")) {
            DoCloudLoad();
        }

        if (GUI.Button(CalcGrid(0,5), "Reload Scene")) {
            Application.LoadLevel(Application.loadedLevel);
        }
        
        if (GUI.Button(CalcGrid(1,5), "Sign Out")) {
            DoSignOut();
        }

        if (GUI.Button(CalcGrid(0,6), "Create TBMG")) 
        {
            DoCreateTurnBasedMultiplayerGame();
        }
        GUI.enabled = (mPendingMatch != null && mPendingMatch.IsPendingParticipantLoggedInPlayer() );
        if (GUI.Button(CalcGrid(1,6), "Take Turn")) 
        {
            DoTakeTurn();
        }
        GUI.enabled = true;

        if (GUI.Button(CalcGrid(0,7), "Test") )
        {
            using (AndroidJavaClass player = new AndroidJavaClass("com.unity3d.player.UnityPlayer")) {
                using (AndroidJavaObject activity = player.GetStatic<AndroidJavaObject>("currentActivity") ) {
                    Debug.Log("1");
                    string testNotEmpty = activity.Call<string>("TestStringNotEmpty");
                    Debug.Log("2");
                    Debug.Log(testNotEmpty);

                    try
                    {
                        AndroidJavaObject testEmpty = activity.Call<AndroidJavaObject>("TestStringEmpty");
                        Debug.Log(testEmpty.ToString() );

                    }
                    catch ( AndroidJavaException e) 
                    {
                        Debug.LogError( "AndroidJavaException :"+ e.Message );
                    }
                    catch ( System.Exception e)
                    {
                        Debug.LogError( "Type :"+ e.GetType().ToString() + "Message :" + e.Message );
                    }
                    Debug.Log("3");
                }
            }
        }

        if (GUI.Button(CalcGrid(1,7), "Invitation Inbox") )
        {
            DoShowMatchInbox();
        }
    }

    void ShowEffect(bool success) {
        Camera.main.backgroundColor = success ?
            new Color(0.0f, 0.0f, 0.8f, 1.0f) :
            new Color(0.8f, 0.0f, 0.0f, 1.0f);
    }


    int CalcFontSize() {
        return (int)(Screen.width * FontSizeFactor / 1000.0f);
    }

    // Update is called once per frame
    void OnGUI() {
        GUI.skin = GuiSkin;
        GUI.skin.label.fontSize = CalcFontSize();
        GUI.skin.button.fontSize = CalcFontSize();

        if (mStandby) {
            ShowStandbyUi();
        } else if (Social.localUser.authenticated) {
            ShowRegularUi();
        } else {
            mStatus = "";
            mMatchStatus = "";
            ShowNotAuthUi();
        }
    }

    void SetStandBy(string message) {
        mStandby = true;
        mStandbyMessage = message;
    }

    void EndStandBy() {
        mStandby = false;
    }

    void DoAuthenticate() {
        SetStandBy("Authenticating...");

        PlayGamesPlatform.DebugLogEnabled = true;

        Social.localUser.Authenticate((bool success) => {
            
            PlayGamesPlatform.Instance.SetTurnBasedMatchListerner(this);

            Debug.Log("Authentifaction Callback");
            EndStandBy();
            if (success) {
                mStatus = "Authenticated. Hello, " + Social.localUser.userName + " (" +
                    Social.localUser.id + ")";
                PlayGamesPlatform platform = (PlayGamesPlatform) Social.Active;

                mPendingMatch = platform.GetTurnBasedMatch();
                UpdateMatchStatus();

            } else {
                mStatus = "*** Failed to authenticate.";
                mMatchStatus = "";
            }
            ShowEffect(success);
        });
    }

    private void UpdateMatchStatus()
    {   
        if (mPendingMatch != null )
        {
            Debug.Log( mPendingMatch.ToString() );
            Debug.Log( mPendingMatch.IsPendingParticipantLoggedInPlayer() );
            
            mMatchStatus = string.Format("Intent: Match {0}\n{1}", mPendingMatch.Guid, 
                                         System.Text.UTF8Encoding.Default.GetString( mPendingMatch.CurrentData ) );
        }
        else
        {
            
            PlayGamesPlatform platform = (PlayGamesPlatform) Social.Active;
            platform.GetPendingMatches( (List<TurnBasedMatchInfo> result) => { 
                
                switch( result.Count)
                {
                case 1:
                    mPendingMatch = result[0];
                    mMatchStatus = string.Format("1 Match {0} Found\n{1}", mPendingMatch.Guid, 
                                                 System.Text.UTF8Encoding.Default.GetString( mPendingMatch.CurrentData ) );
                    break; 
                case 0: 
                    mMatchStatus = string.Format("No Pending Match found.");
                    break;
                default:
                    mMatchStatus = string.Format("Mutiple Pending Matches ({0}) Found\nPlease selec games througt the Inbox UI", result.Count);
                    break;
                }
                
            } );
        }
    }    
    
    void DoSignOut() {
        mStatus = "Signing out.";
        mMatchStatus = "";
        ((PlayGamesPlatform) Social.Active).SignOut();
    }
    
    void DoAchievementReveal() {
        SetStandBy("Revealing achievement...");
        Social.ReportProgress(Settings.AchievementToReveal, 0.0f, (bool success) => {
            EndStandBy();
            mStatus = success ? "Revealed successfully." : "*** Failed to reveal ach.";
            ShowEffect(success);
        });
    }

    void DoAchievementUnlock() {
        SetStandBy("Unlocking achievement...");
        Social.ReportProgress(Settings.AchievementToUnlock, 100.0f, (bool success) => {
            EndStandBy();
            mStatus = success ? "Unlocked successfully." : "*** Failed to unlock ach.";
            ShowEffect(success);
        });
    }

    void DoAchievementIncrement() {
        PlayGamesPlatform p = (PlayGamesPlatform) Social.Active;

        SetStandBy("Incrementing achievement...");
        p.IncrementAchievement(Settings.AchievementToIncrement, 1,(bool success) => {
            EndStandBy();
            mStatus = success ? "Incremented successfully." : "*** Failed to increment ach.";
            ShowEffect(success);
        });

    }

    long GenScore() {
        return (long)(DateTime.Today.Subtract(new DateTime(2013, 1, 1, 0, 0, 0)).TotalSeconds);
    }

    void DoPostScore() {
        long score = GenScore();
        SetStandBy("Posting score: " + score);
        Social.ReportScore(score, Settings.Leaderboard, (bool success) => {
            EndStandBy();
            mStatus = success ? "Successfully reported score " + score :
                "*** Failed to report score " + score;
            ShowEffect(success);
        });
    }

    void DoLeaderboardUI() {
        Social.ShowLeaderboardUI();
        ShowEffect(true);
    }

    void DoAchievementUI() {
        Social.ShowAchievementsUI();
        ShowEffect(true);
    }

    char RandCharFrom(string s) {
        int i = UnityEngine.Random.Range(0, s.Length);
        i = i < 0 ? 0 : i >= s.Length ? s.Length - 1 : i;
        return s[i];
    }

    string GenString() {
        string x = "";
        int syl = UnityEngine.Random.Range(4, 7);
        while (x.Length < syl) {
            x += RandCharFrom("bcdfghjklmnpqrstvwxyz");
            x += RandCharFrom("aeiou");
            if (UnityEngine.Random.Range(0,10) > 7) {
                x += RandCharFrom("nsr");
            }
        }
        return x;
    }

    void DoCloudSave() {
        string word = GenString();

        SetStandBy("Saving string to cloud: " + word);
        PlayGamesPlatform p = (PlayGamesPlatform) Social.Active;
        p.UpdateState(0, System.Text.ASCIIEncoding.Default.GetBytes(word), this);
        EndStandBy();
        mStatus = "Saved string to cloud: " + word;
        ShowEffect(true);
    }


    public void OnStateLoaded(bool success, int slot, byte[] data) {
        EndStandBy();
        if (success) {
            mStatus = "Loaded from cloud: " + System.Text.ASCIIEncoding.Default.GetString(data);
        } else {
            mStatus = "*** Failed to load from cloud.";
        }

        mStatus += ". conflict=" + (mHadCloudConflict ? "yes" : "no");
        ShowEffect(success);
    }

    public byte[] OnStateConflict(int slot, byte[] local, byte[] server) {
        mHadCloudConflict = true;
        return local;
    }

    public void OnStateSaved(bool success, int slot) {
        mStatus = "Cloud save " + (success ? "successful" : "failed");
        ShowEffect(success);
    }

    void DoCloudLoad() {
        mHadCloudConflict = false;
        SetStandBy("Loading from cloud...");
        ((PlayGamesPlatform) Social.Active).LoadState(0, this);
    }

    void DoCreateTurnBasedMultiplayerGame()
    {
        PlayGamesPlatform p = (PlayGamesPlatform) Social.Active;
        mMatchStatus = "Starting Match Creation";
        p.CreateTurnBasedMatch( this, 1, 1 );
    }

    void DoShowMatchInbox()
    {
        PlayGamesPlatform p = (PlayGamesPlatform) Social.Active;
        p.ShowMatchInboxUI((TurnBasedMatchInfo match) => {
            mPendingMatch = match;
            UpdateMatchStatus();
        } );

    }

    void DoTakeTurn()
    {
        if ( mPendingMatch !=  null )
        {
            mMatchStatus = string.Format( "Taking turn in {0} ", mPendingMatch.Guid );
            PlayGamesPlatform p = (PlayGamesPlatform) Social.Active;

            string data = System.Text.UTF8Encoding.Default.GetString( mPendingMatch.CurrentData )
                + " " + UnityEngine.Random.Range(0, 10).ToString();

            p.TBMG_TakeTurn( mPendingMatch.Guid,
                            System.Text.UTF8Encoding.Default.GetBytes( data ),
                            GetNextRoundRobinParticipantId( mPendingMatch) );
        }
        else
        {
            Debug.LogError( "Error, mPendingMatch is null");
        }
    }

    #region BasicImplementation ofI TurnBasedMatchListerner
    public byte[] GetInitialData( TurnBasedMatchInfo match)
    {
        return System.Text.UTF8Encoding.Default.GetBytes("D:"); 
    }

    public string GetInitialParticipant( TurnBasedMatchInfo match) 
    {
        return match.ParticipantIds[0]; 
    }

    public void OnTurnBasedMatchInitiated(int statusCode, TurnBasedMatchInfo match)
    {
        mMatchStatus = string.Format("Match {0} Initiated", match.Guid);
        mPendingMatch = match;
    }

    public void OnTurnBasedMatchUpdated(int statusCode, TurnBasedMatchInfo match)
    {
        
        mPendingMatch = null;

        if (match != null)
        {
            mMatchStatus = string.Format("Match {0} Upated\n{1}", match.Guid, 
                                         System.Text.UTF8Encoding.Default.GetString( match.CurrentData ) );
            if ( match.IsPendingParticipantLoggedInPlayer() )
            {
                mPendingMatch = match;
            }

        }
    }

    private string GetNextRoundRobinParticipantId( TurnBasedMatchInfo match )
    {
        Debug.Log(" GetNextRoundRobinParticipantId ");
        Debug.Log( match.ToString() );

        int currentPlayerIdx = -1;
        for( int i =0; i< match.ParticipantIds.Count;i++)
        {
            if ( match.ParticipantIds[i] == match.PendingParticipantId)
            {
                currentPlayerIdx = i ;
            }
        }
        int nextPlayerIdx = (currentPlayerIdx+1) % (match.ParticipantIds.Count);

        Debug.Log( string.Format( "CurrentPlayerIdx:{0}, NextPlayerIdx:{1}", currentPlayerIdx, nextPlayerIdx) );
        
        return match.ParticipantIds[ nextPlayerIdx ];
    }

    #endregion
}
