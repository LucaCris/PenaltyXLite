using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Xamarin.Forms;

namespace Penaltyx
{
    public class Ball : Image
    {
        public enum St { Rolling = 1, Shoot, InGoal, Out, Demo };
        public St State = 0;
        public double Dx;
        public double Dy;
        public double Ax;
        public double Ay;
        public double H;
        public double Rx;
        public int Timer;
        public bool OverBarrier;
        public int FixedPos;
        public long TimePos;

        public Image Shadow;

        public Ball()
        {
            Source = ImageSource.FromResource("Penaltyx.Gfx.Ball.png", typeof(ImageResourceExtension).GetTypeInfo().Assembly);
            //BackgroundColor = Color.Yellow;
            Shadow = new Image() { Source = ImageSource.FromResource("Penaltyx.Gfx.Shadow.png", typeof(ImageResourceExtension).GetTypeInfo().Assembly) };
            FixedPos = -1;
        }

        public void EnterRolling(bool GameOver)
        {
            State = St.Rolling;
            Dx = -.005;
            Dy = 0;
            Ax = 0;
            Ay = 0;
            H = 0;
            Rx = 3;
            OverBarrier = false;
            AbsoluteLayout.SetLayoutFlags(this, AbsoluteLayoutFlags.All);
            AbsoluteLayout.SetLayoutBounds(this, new Rectangle(1.2, 0.99, MainPage.BALLSIZE / MainPage.SCRRATIO, MainPage.BALLSIZE));
            if (GameOver) {
                Dy = MainPage.rnd.NextDouble() * -0.6;
                Dx *= 1 + Dy;
                State = St.Demo;

                Dx *= 1.5;
                Rx *= 1.5;
            }
            //AbsoluteLayout.SetLayoutBounds(b, new Rectangle(.5, 1, MainPage.BALLSIZE / MainPage.SCRRATIO , MainPage.BALLSIZE)); b.Dx = 0;

            AbsoluteLayout.SetLayoutFlags(Shadow, AbsoluteLayoutFlags.All);
        }

        public void PosLaunch(int pos)
        {
            FixedPos = pos;
            State = St.Rolling;
            TimePos = DateTime.Now.Ticks;
            Dx = 0;
            Dy = 0;
            Ax = 0;
            Ay = 0;
            H = 0;
            Rx = 3;
            OverBarrier = false;
            AbsoluteLayout.SetLayoutFlags(this, AbsoluteLayoutFlags.All);
            AbsoluteLayout.SetLayoutBounds(this, new Rectangle(.02 + pos * 0.96 / 3.0, 0.99, MainPage.BALLSIZE / MainPage.SCRRATIO, MainPage.BALLSIZE));
            AbsoluteLayout.SetLayoutFlags(Shadow, AbsoluteLayoutFlags.All);
        }

        public void EnterShoot(double diff, double diffY)
        {
            State = St.Shoot;
            Ax = Math.Abs(diff) > .1 ? diff / 25 : 0;
            var r = AbsoluteLayout.GetLayoutBounds(this);
            Dx = (.5 - r.X) * .015;
            Dy = -.02;
            Ay = Math.Max(0, Math.Min(0.005, 0.001 - diffY / 100));
            Rx = 2;
        }

        public void EnterInGoal()
        {
            State = St.InGoal;
            Timer = 60;
            this.FadeTo(0, 1000);
        }

        public bool EnterOut(Rectangle r)
        {
            State = St.Out;
            Dx = r.X < .5 ? -.025 : .025;
            Dy = 0;
            if ((r.X < .2 && r.X > .17) || (r.X > .8 && r.X < .83))
                return true;
            return false;
        }

        public void Remove(IList<Image> toremove)
        {
            toremove.Add(this);
            toremove.Add(Shadow);
        }
    }

    public class Barrier : Image
    {
        Rectangle Pos;
        double Dx;

        public Barrier()
        {
            Source = ImageSource.FromResource("Penaltyx.Gfx.barrier.png", typeof(ImageResourceExtension).GetTypeInfo().Assembly);
            //      BackgroundColor = Color.Yellow;
        }

        public void Setup()
        {
            Pos = new Rectangle(.5, .4, .35, .45);
            Dx = MainPage.rnd.NextDouble() < 0.5 ? .015 : -.015;
            AbsoluteLayout.SetLayoutFlags(this, AbsoluteLayoutFlags.All);
        }

        public void Move()
        {
            Pos.X += Dx;
            if (Pos.X < .1 || Pos.X > .9)
                Dx = -Dx;

            AbsoluteLayout.SetLayoutBounds(this, Pos);
        }

        internal bool Save(Ball ball)
        {
            //      return false;
            return (Bounds.IntersectsWith(ball.Bounds));
        }
    }

    public class GoalKeeper : Image
    {
        Rectangle Pos;
        double Dx;
        bool m_SaveMode;
        ImageSource gk1, gk2;
        int m_SaveTimer;

        public GoalKeeper()
        {
            gk1 = ImageSource.FromResource("Penaltyx.Gfx.gk.png", typeof(ImageResourceExtension).GetTypeInfo().Assembly);
            gk2 = ImageSource.FromResource("Penaltyx.Gfx.gk2.png", typeof(ImageResourceExtension).GetTypeInfo().Assembly);
            SaveMode(false);
            //      BackgroundColor = Color.Yellow;
        }

        public void Setup()
        {
            Pos = new Rectangle(.5, .2, .25 / MainPage.SCRRATIO, .25);
            Dx = MainPage.rnd.NextDouble() < 0.5 ? .008 : -.008;
            AbsoluteLayout.SetLayoutFlags(this, AbsoluteLayoutFlags.All);
        }

        public void SaveMode(bool active)
        {
            m_SaveMode = active;
            Source = active ? gk2 : gk1;
            m_SaveTimer = 60;
        }

        public void Move()
        {
            if (!m_SaveMode) {
                Pos.X += Dx;
                if (Pos.X < .2 || Pos.X > .8)
                    Dx = -Dx;
                else if (Pos.X > .45 && Pos.X < .55 && MainPage.rnd.NextDouble() < .01)
                    Dx = -Dx;
            } else {
                m_SaveTimer--;
                if (m_SaveTimer <= 0)
                    SaveMode(false);
            }

            AbsoluteLayout.SetLayoutBounds(this, Pos);
        }

        internal bool Save(Ball ball)
        {
            return (Bounds.IntersectsWith(ball.Bounds));
        }
    }

    public class LevelData
    {
        public LevelData(int n)
        {
            LevelNum = n;
            if (n == 0) {
                LevelMode = 0;
                BallsTotal = 0;
                GoalsTarget = 1;
            } else {
                LevelMode = 0;
                BallsTotal = 29 + n;
                GoalsTarget = 8 + n * 2;
            }
            ErrorsMax = Math.Max(1, 10 - n / 2);

            //GoalsTarget = 1;
            //BallsTotal = 1;
        }
        public int LevelNum;
        public int LevelMode;
        public int BallsTotal;
        public int GoalsTarget;
        public int BallsDeployed;
        public int Goals;
        public int Errors;
        public int ErrorsMax;
    }

    class GameCore
    {
        MainPage m_Main;
        public LevelData m_Data;
        public GoalKeeper m_GoalKeeper;
        public Barrier m_Barrier;
        DateTime m_LastTime = DateTime.Now;
        DateTime m_LastGoalTime = DateTime.Now;
        AbsoluteLayout m_view;

        public GameCore(MainPage main, AbsoluteLayout view)
        {
            m_Main = main;
            m_view = view;
        }

        public void Setup()
        {
            m_GoalKeeper = new GoalKeeper();
            m_GoalKeeper.Setup();
            m_Barrier = new Barrier();
            m_Barrier.Setup();

            m_Data = new LevelData(m_Main.m_Level);
            m_Main.DoUpdateData = true;

            m_GoalKeeper.Opacity = 0;
            m_GoalKeeper.FadeTo(1, 250);

            m_Barrier.Opacity = 0;
            m_Barrier.FadeTo(1, 500);
        }

        void Move()
        {
            m_GoalKeeper.Move();
            m_Barrier.Move();
        }

        bool Deploy(bool GameOver)
        {
            int ballsOut = m_view.Children.OfType<Ball>().Count();

            if (!GameOver && m_Data.BallsDeployed >= m_Data.BallsTotal) {
                return ballsOut == 0;
            }

            Ball b = null;
            var now = DateTime.Now;
            if (m_Data.LevelMode == 0) {
                if (ballsOut < (GameOver ? 8 : 5) && now > m_LastTime.AddSeconds(1)) {
                    m_LastTime = now.AddSeconds(MainPage.rnd.Next(GameOver ? 1 : 2));
                    b = new Ball();
                    b.EnterRolling(GameOver);
                }
            }

            if (b != null) {
                Data(() => m_Data.BallsDeployed++);
                m_view.Children.Insert(0, b.Shadow);
                m_view.Children.Insert(1, b);
            }

            return false;
        }

        void Data(Action action)
        {
            action.Invoke();
            m_Main.DoUpdateData = true;
        }

        public void DoPause()
        {
            m_LastTime = DateTime.Now;

            foreach (var ball in m_view.Children.OfType<Ball>()) {
                ball.Rotation -= ball.Rx;
            }
        }

        public bool Do(bool GameOver)
        {
            if (!GameOver)
                Move();

            if (Deploy(GameOver))
                if (!GameOver)
                    return true;

            var toremove = new List<Image>();

            long tnow = m_Data.LevelMode == 1 ? DateTime.Now.Ticks : 0;
            const long span = 100000000;

            foreach (var ball in m_view.Children.OfType<Ball>()) {
                var r = AbsoluteLayout.GetLayoutBounds(ball);
                r.Y += ball.H;
                if (ball.State == Ball.St.Rolling) {
                    r.X += ball.Dx;
                    ball.Rotation -= ball.Rx;
                    if (tnow > 0 && tnow > ball.TimePos + span) {
                        r.X = -1;
                        m_Main.Sound[m_Main.snd_booo].Play();
                        m_LastTime = DateTime.Now;
                    }
                } else {
                    r.X += ball.Dx + ball.Ax;
                    ball.Ax *= .9;
                    r.Y += ball.Dy;
                    if (GameOver)
                        ball.Dy = 0;  // Un singolo colpo di Dy viene dato in gameover, per ridimensionare la palla. Poi qui si azzera per procedere orizontale.

                    if (ball.State == Ball.St.Shoot)
                        ball.H += ball.Ay;
                    if (ball.State == Ball.St.InGoal)
                        ball.H = Math.Max(0, ball.H - ball.Timer / 2500.0);

                    double size = MainPage.BALLSIZE - (1 - r.Y) * .2;
                    r.Width = size / MainPage.SCRRATIO;
                    r.Height = size;
                    if (ball.State == Ball.St.InGoal) {
                        ball.Timer--;
                        if (ball.Timer < 0)
                            ball.Remove(toremove);
                    } else
                        ball.Rotation -= ball.Rx;
                }
                var dispr = r;
                dispr.Y -= ball.H;
                AbsoluteLayout.SetLayoutBounds(ball, dispr);
                var shr = r;
                shr.Y += .005;
                AbsoluteLayout.SetLayoutBounds(ball.Shadow, shr);
                if (r.X < -0.5 || r.X > 1.5) {
                    ball.Remove(toremove);
                    if (ball.State == Ball.St.Rolling)
                        Data(() => m_Data.Errors++);
                }
                if (r.Y < MainPage.BARLINE && !ball.OverBarrier) {
                    if (ball.State == Ball.St.Shoot && m_Barrier.Save(ball)) {
                        m_Main.Sound[m_Main.snd_hit2].Play();
                        ball.EnterOut(r);
                        ball.Dy = .005;
                    }
                    ball.OverBarrier = true;
                }
                if (r.Y < MainPage.GKLINE) {
                    if (ball.State == Ball.St.Shoot && m_GoalKeeper.Save(ball)) {
                        if (ball.H > .050) {
                            m_GoalKeeper.SaveMode(true);
                            m_Main.Sound[m_Main.snd_save].Play();
                            ball.Remove(toremove);
                            break;
                        } else {
                            m_Main.Sound[m_Main.snd_save].Play();
                            ball.EnterOut(r);
                            ball.Dy = .005;
                        }
                    }
                }
                if (r.Y < MainPage.GOALLINE) {
                    if (r.X < .2 || r.X > .8) {
                        if (ball.State != Ball.St.Out && ball.State != Ball.St.InGoal) {
                            if (ball.EnterOut(r)) {
                                m_Main.Sound[m_Main.snd_hitw].Play();
                                m_Main.Sound[m_Main.snd_gasp].Play();
                            } else {
                                m_Main.Sound[m_Main.snd_booo].Play();
                                Data(() => m_Data.Errors++);
                            }
                        }
                    } else {
                        ball.Dx = 0;
                        if (ball.State != Ball.St.InGoal) {
                            ball.EnterInGoal();

                            var now = DateTime.Now;
                            if (now < m_LastGoalTime.AddSeconds(1)) {
                                m_Main.m_Score += 500;
                                m_Main.Sound[m_Main.snd_combo].Play();
                            } else {
                                m_Main.Sound[m_Main.snd_goal + m_Main.goal_stereo].Play();
                                m_Main.goal_stereo = 1 - m_Main.goal_stereo;
                            }

                            m_Main.m_Score += 250;
                            Data(() => m_Data.Goals++);

                            m_LastGoalTime = now;
                        }
                    }
                    ball.Dy = 0;
                }
            }
            foreach (var item in toremove)
                m_view.Children.Remove(item);

            if (toremove.Count > 0 && m_Data.Goals >= m_Data.GoalsTarget)
                return true;

            return false;
        }
    }
}
