using Plugin.SimpleAudioPlayer;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Penaltyx
{
    [DesignTimeVisible(false)]
    public partial class MainPage : ContentPage
    {
        public const double BALLSIZE = 0.18;
        public const double SCRRATIO = 16 / 9.0;
        public const double GOALLINE = 0.34;
        public const double GKLINE = 0.38;
        public const double BARLINE = 0.65;

        public int m_Level = 0;
        public int m_Score;

        bool m_Timer = false;
        bool m_TimerStop = false;
        bool m_GameOver = true;
        bool m_LevelCleared;
        bool m_Pause;
        int m_LostState;
        List<string> m_GOMsgs = new List<string>();
        int m_GOIdx;
        DateTime m_GOLast;

        public static Random rnd = new Random();

        public List<ISimpleAudioPlayer> Sound = new List<ISimpleAudioPlayer>();
        public int snd_foot, snd_goal, snd_booo, snd_hitw, snd_hit2, snd_gasp, snd_save, snd_combo, snd_well, snd_over, snd_hiscore;
        public int goal_stereo = 0, foot_stereo = 0;
        GameCore m_Core;
        public bool DoUpdateData;

        public MainPage()
        {
            InitializeComponent();
            if (DesignMode.IsDesignModeEnabled)
                return;

            m_Core = new GameCore(this, view);

            snd_foot = AddAudio("foot.wav");
            AddAudio("foot.wav");
            snd_goal = AddAudio("goal.wav", 0.2);
            AddAudio("goal.wav", 0.2);
            snd_booo = AddAudio("booo.wav");
            snd_hitw = AddAudio("hitw.wav");
            snd_hit2 = AddAudio("hitw2.wav");
            snd_gasp = AddAudio("gasp.wav", 0.25);
            snd_save = AddAudio("save.wav");
            snd_combo = AddAudio("combo.wav", 0.5);
            snd_well = AddAudio("welldone.wav");
            snd_over = AddAudio("gameover.wav");
            snd_hiscore = AddAudio("cheering.wav");

            for (int y = 0; y < 2; y++)
                for (int x = 0; x < 6; x++) {
                    var l = new Label
                    {
                        HorizontalOptions = LayoutOptions.Center,
                        VerticalOptions = LayoutOptions.Center,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = y == 0 ? Color.Yellow : Color.White,
                        ScaleY = y == 0 ? 1.5 : 1,
                    };
                    Grid.SetRow(l, y);
                    Grid.SetColumn(l, x);
                    DataGrid.Children.Add(l);
                }

            GetLabel(0, 1).Text = "SCORE";
            GetLabel(1, 1).Text = "LEVEL";
            GetLabel(2, 1).Text = "BALLS";
            GetLabel(3, 1).Text = "ERRORS";
            GetLabel(4, 1).Text = "GOALS";
            GetLabel(5, 1).Text = "TARGET";

            MainGrid.SizeChanged += OnSize;
            WellD.SizeChanged += HideAfterSizing;

            GoGameOver();
            DoTimer();
        }

        private void HideAfterSizing(object sender, EventArgs e)
        {
            WellD.IsVisible = false;
        }

        Label GetLabel(int x, int y)
        {
            return DataGrid.Children[y * 6 + x] as Label;
        }

        public void Sleep(bool DoSleeep)
        {
            if (DoSleeep) {
                m_TimerStop = true;
            }
            if (!DoSleeep) {
                DoTimer();
            }
            if (!m_GameOver)
                GoPause(true);
        }

        private void OnSize(object sender, EventArgs e)
        {
            double w = MainGrid.Width;
            double h = MainGrid.Height;
            double hh = h * .9;
            double ww = hh * SCRRATIO;
            if (w / hh < SCRRATIO) {
                ww = w;
                hh = w / SCRRATIO;
            }
            FieldCol.Width = ww;
            FieldRow.Height = hh;

            double fs = (hh * .1) / 3.5;
            foreach (var l in DataGrid.Children.OfType<Label>())
                l.FontSize = fs;

            MsgCtrl.FontSize = (hh * .1) / 2;
            WellD.FontSize = hh / 4;
        }

        int AddAudio(string name, double volume = 1, int reload = -1)
        {
            var assembly = IntrospectionExtensions.GetTypeInfo(typeof(App)).Assembly;
            int res = reload;
            if (reload == -1) {
                Sound.Add(CrossSimpleAudioPlayer.CreateSimpleAudioPlayer());
                res = Sound.Count - 1;
            }
            var stream = assembly.GetManifestResourceStream("Penaltyx.Audio." + name);
            Sound[res].Load(stream);
            Sound[res].Volume = volume;
            return res;
        }

        void PlayWait(int n)
        {
            Sound[n].Play();
            Task.Delay((int)(Sound[n].Duration * 1000)).Wait();
        }

        public void NextLevel()
        {
            if (m_GameOver)
                return;

            m_Core.m_Data.Goals = m_Core.m_Data.GoalsTarget;
            view.Children.Clear();
            m_LevelCleared = true;
        }

        void DoTimer()
        {
            if (!m_Timer) {
                m_Timer = true;
                Device.StartTimer(TimeSpan.FromMilliseconds(1), MainLoop);
            }
        }

        void GoGameOver()
        {
            view.Children.Clear();
            m_GameOver = true;
            m_Pause = false;
            m_LostState = 0;
            m_Core.m_Data = new LevelData(0);
            PlayBtn.IsVisible = false;
            MsgCtrl.IsVisible = true;

            m_GOIdx = 0;
            m_GOLast = DateTime.Now.AddHours(-1);
            m_GOMsgs.Clear();

            int hs = GetHiScore();
            if (m_Score > hs) {
                SetHiScore();
                Task.Delay(250).Wait();
                Sound[snd_hiscore].Play();
                hs = m_Score;
                m_GOMsgs.Add("⚽⚽⚽ NEW HIGH SCORE ⚽⚽⚽");
            }

            m_GOMsgs.Add($"GAME OVER");
            m_GOMsgs.Add($"HIGH SCORE: {hs:D6}");
            if (m_Level > 0)
                m_GOMsgs.Add($"LAST SCORE: {m_Score:D6} - LEVEL: {m_Level}");
            m_GOMsgs.Add($"KICK ANY ⚽ TO PLAY");
            m_GOMsgs.Add("PENALTYX - BY LALE SOFTWARE");
        }

        void GoNextLevel()
        {
            m_Level++;
            m_Score += (m_Core.m_Data.BallsTotal - m_Core.m_Data.BallsDeployed) * 75;

            Task.Delay(250).Wait();
            Sound[snd_goal + goal_stereo].Play();
            goal_stereo = 1 - goal_stereo;
            Task.Delay(250).Wait();
            Sound[snd_goal + goal_stereo].Play();

            PlayWait(snd_well);
            StartGame();
        }

        private bool MainLoop()
        {
            if (m_TimerStop) {
                m_TimerStop = false;
                m_Timer = false;
                return false;
            }

            if (m_GameOver) {
                if (DateTime.Now > m_GOLast.AddSeconds(3)) {
                    m_GOLast = DateTime.Now;
                    MsgCtrl.Text = m_GOMsgs[m_GOIdx++];
                    if (m_GOIdx >= m_GOMsgs.Count())
                        m_GOIdx = 0;
                }
            }

            if (m_LevelCleared) {
                GoNextLevel();
                return true;
            }

            if (m_LostState == 1) {
                m_LostState = 2;
                m_Pause = true;
                PlayBtn.IsVisible = false;
                m_Core.m_GoalKeeper.FadeTo(0, 500).ContinueWith((x) => Sound[snd_over].Play());
                m_Core.m_Barrier.FadeTo(0, 1000).ContinueWith((x) => m_LostState = 3);
                return true;
            } else if (m_LostState == 3) {
                GoGameOver();
                return true;
            }

            if (!m_Pause) {
                if (m_Core.Do(m_GameOver)) {
                    if (m_Core.m_Data.Goals >= m_Core.m_Data.GoalsTarget) {
                        view.Children.Clear();
                        WellD.IsVisible = true;
                        m_LevelCleared = true;
                    } else
                        m_LostState = 1;
                }

                if (DoUpdateData && !m_GameOver) {
                    DoUpdateData = false;
                    var d = m_Core.m_Data;

                    GetLabel(0, 0).Text = m_Score.ToString("D6");
                    GetLabel(1, 0).Text = d.LevelNum.ToString("D2");
                    GetLabel(2, 0).Text = $"{d.BallsDeployed} out of {d.BallsTotal}";
                    GetLabel(3, 0).Text = $"{d.Errors} out of {d.ErrorsMax}";
                    GetLabel(4, 0).Text = d.Goals.ToString();
                    GetLabel(5, 0).Text = d.GoalsTarget.ToString();
                }

                if (m_Core.m_Data.Errors > m_Core.m_Data.ErrorsMax) {
                    m_LostState = 1;
                }
            } else
                m_Core.DoPause();

            return true;
        }

        void StartGame()
        {
            view.Children.Clear();
            m_GameOver = false;
            m_Core.Setup();
            view.Children.Add(m_Core.m_GoalKeeper);
            view.Children.Add(m_Core.m_Barrier);
            PlayBtn.IsVisible = true;
            MsgCtrl.IsVisible = false;
            WellD.IsVisible = false;
            m_LevelCleared = false;
        }

        private void TapGestureRecognizer_Tapped(object sender, EventArgs e)
        {
            //System.Diagnostics.Process.GetCurrentProcess().Kill();
            GoPause(null);
        }

        void GoPause(bool? force)
        {
            if (force == null)
                m_Pause = !m_Pause;
            else
                m_Pause = (bool)force;

            m_Core.m_Barrier.Opacity = m_Pause ? 0.5 : 1;
            m_Core.m_GoalKeeper.Opacity = m_Pause ? 0.5 : 1;
            Paused.IsVisible = m_Pause;
        }

        void NewGame()
        {
            m_Level = 1;
            m_Score = 0;
            m_Pause = false;
            m_LostState = 0;
            StartGame();
        }

        private void OnTouchAction(object sender, TouchTracking.TouchActionEventArgs args)
        {
            if (args.Type == TouchTracking.TouchActionType.Pressed && !m_Pause) {
                Point loc = args.Location;
                loc.X -= (MainGrid.Width - FieldCol.Width.Value) / 2;
                foreach (var ball in view.Children.OfType<Ball>()) {
                    if ((ball.State != Ball.St.Rolling) && (ball.State != Ball.St.Demo))
                        continue;
                    var b = ball.Bounds;
                    if (b.Contains(loc)) {
                        ball.EnterShoot((b.Center.X - loc.X) / b.Width, (b.Center.Y - loc.Y) / b.Height);
                        Sound[snd_foot + foot_stereo].Play();
                        foot_stereo = 1 - foot_stereo;
                        if (m_GameOver) {
                            NewGame();
                            return;
                        }
                    }
                }
            }
        }

        int GetHiScore()
        {
            if (App.Current.Properties.ContainsKey("hiscore"))
                return (int)App.Current.Properties["hiscore"];
            else
                return 0;
        }

        void SetHiScore()
        {
            App.Current.Properties["hiscore"] = m_Score;
            App.Current.SavePropertiesAsync();
        }
    }
}
