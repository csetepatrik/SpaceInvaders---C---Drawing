//-----------------------------------------------------------------------
// <copyright file="ViewModel.cs<GameItem>" company="CompanyName">
//     Company copyright tag.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.Diagnostics;
using System.Windows.Media.Imaging;

namespace WpfApplication1
{
    abstract class GameItem
    {
        private Geometry area;

        protected Geometry Area
        {
            get { return area; }
            set { area = value; }
        }
        protected int degree = 0;
        protected int cx;
        protected int cy;

        protected double Rad
        {
            get
            {
                return degree * Math.PI / 180;
            }
            set
            {
                degree = (int)(180 * value / Math.PI);
            }
        }

        public Geometry RealArea
        {
            get
            {
                TransformGroup tg = new TransformGroup();
                tg.Children.Add(new TranslateTransform(cx, cy));
                tg.Children.Add(new RotateTransform(degree, cx, cy));
                area.Transform = tg;
                return area.GetFlattenedPathGeometry();
            }
        }

        public bool IsCollision(GameItem other)
        {
            return Geometry.Combine(RealArea, other.RealArea,
                GeometryCombineMode.Intersect, null).GetArea() > 0;
        }

        protected void Transzformál(Transform transzformáció)
        {
            //Azért kell a másolat, mert ha esetleg Freeze-elve lenne az objektum, akkor nem lehetne
            //módosítani, ezért ekkor hiba keletkezne. Ezt kivédve egy másolatot változtatunk, majd
            //az objektumot felülírjuk.

            //másolat
            Geometry másolat = this.area.Clone();  //létrehoz egy másolatot
            másolat.Transform = transzformáció; //megadjuk, hogy milyen transzformáció lesz elvégezve rajta
            Geometry eredmény = másolat.GetFlattenedPathGeometry(); //elvégez minden rajta lévő transzformációt

            this.area = eredmény;
        }
    }
    class SpaceShip : GameItem
    {
        int dx = 0;
        int szög;

        public int Szög
        {
            get { return szög; }
            set { szög = value; }
        }
        public int Dx
        {
            get { return dx; }
        }
        public SpaceShip(int newcx, int newcy)
        {
            GeometryGroup g = new GeometryGroup();
            g.Children.Add(new LineGeometry(new Point(20, 30), new Point(-20, -20)));
            g.Children.Add(new LineGeometry(new Point(-30, -30), new Point(-40, 30)));
            Area = g.GetWidenedPathGeometry(new Pen(Brushes.Black, 2));
            cx = newcx;
            cy = newcy;
        }
        public void Boost(int change)
        {
            cx += change;
        }
    }

    class Urleny : GameItem
    {
        const int gap = 120;
        const int w = 20;
        const int move = 300;

        static Random R = new Random();
        bool eletbenvan;
        public bool EletbenVan
        {
            get { return eletbenvan; }
            set { eletbenvan = value; }
        }

        public Urleny(int height, int newcx, int newcy)
        {
            this.eletbenvan = true;
            cx = newcx; cy = newcy;
            int rect_height = height / 8;
            GeometryGroup g = new GeometryGroup();
            Rect r1 = new Rect(-w / 4, -rect_height - gap / 2, 25, 25);
            g.Children.Add(new RectangleGeometry(r1));
            Area = g;
        }

        public void Moving_Left(int h, int w, Urleny p)
        {
            cx += 1;
            if (p.RealArea.Bounds.Right > w)
            {
                cx = 0;
            }

        }
        public void Moving_Right(int h, int w, Urleny p)
        {
            cx -= 1;
            if (p.RealArea.Bounds.Left < 0)
            {
                cx = w - 10;
            }

        }

    }
    class ViewModel
    {
        public int shipLife = 5;
        public int Points { get; set; }
        public SpaceShip Ship { get; private set; }
        public List<PresentAmmo> Presents { get; private set; }
        public List<Urleny> Urlenyek { get; private set; }
        public List<Ammo> Ammos { get; private set; }
        public List<EnemyAmmo> EnemyAmmo { get; private set; }
        public ViewModel(int w, int h)
        {
            Ship = new SpaceShip(w / 2, h - 30);
            Urlenyek = new List<Urleny>();
            Ammos = new List<Ammo>();
            EnemyAmmo = new List<EnemyAmmo>();
            Presents = new List<PresentAmmo>();
            int dist = w / 10;
            int rows = h / 3;
            for (int i = 0; i < 5; i++)
            {
                for (int x = dist; x <= w; x += dist)
                {
                    Urlenyek.Add(new Urleny(h, x, rows));
                }
                rows += 50;
            }
        }
    }

    class GameScreen : FrameworkElement
    {
        ViewModel VM;
        Stopwatch sw;
        Stopwatch sw1;
        int w;
        int h;
        DispatcherTimer t;
        Pen black = new Pen(Brushes.Black, 2);
        Pen red = new Pen(Brushes.Red, 2);
        Typeface font = new Typeface("Arial");
        Point loc = new Point(10, 10);
        Point lifeloc = new Point(450, 10);

        public GameScreen()
        {
            Loaded += GameScreen_Loaded;
        }

        void GameScreen_Loaded(object sender, RoutedEventArgs e)
        {
            sw = new Stopwatch();
            sw1 = new Stopwatch();
            sw1.Start();
            sw.Start();
            w = (int)ActualWidth; h = (int)ActualHeight;
            VM = new ViewModel(w, h);
            Window akt = Window.GetWindow(this);
            if (akt != null)
            {
                akt.KeyDown += GameScreen_KeyDown;
                t = new DispatcherTimer();
                t.Interval = TimeSpan.FromMilliseconds(100);
                t.Tick += timer_Tick;
                t.Start();
            }
            InvalidateVisual();
        }
        Random r = new Random();
        void GameScreen_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {

            int sugar = 10;
            int kozeppontX = r.Next(sugar, (int)ActualWidth - sugar);
            int kozeppontY = r.Next(sugar, (int)ActualHeight - sugar);

            switch (e.Key)
            {
                case Key.Left: VM.Ship.Boost(-5); break;
                case Key.Right: VM.Ship.Boost(5); break;
                case Key.Space:
                    Ammo lovedek = new Ammo(
                   new EllipseGeometry(
                       new Point(kozeppontX, kozeppontY), 5, 5), (int)VM.Ship.RealArea.Bounds.X, (int)VM.Ship.RealArea.Bounds.Y);
                    VM.Ammos.Add(lovedek);
                    break;
            }
            InvalidateVisual();
        }

        void timer_Tick(object sender, EventArgs e)
        {
            bool vege = false;
            foreach (Urleny item in VM.Urlenyek)
            {
                if (item.RealArea.Bounds.Y < 30)
                {
                    item.Moving_Left((int)ActualHeight, (int)ActualWidth, item);
                }
                if (item.RealArea.Bounds.Y > 30 && item.RealArea.Bounds.Y < 60)
                {
                    item.Moving_Right((int)ActualHeight, (int)ActualWidth, item);
                }
                if (item.RealArea.Bounds.Y > 60 && item.RealArea.Bounds.Y < 120)
                {
                    item.Moving_Left((int)ActualHeight, (int)ActualWidth, item);
                }
                if (item.RealArea.Bounds.Y > 150 && item.RealArea.Bounds.Y < 200)
                {
                    item.Moving_Right((int)ActualHeight, (int)ActualWidth, item);
                }
                if (item.RealArea.Bounds.Y > 200)
                {
                    item.Moving_Left((int)ActualHeight, (int)ActualWidth, item);
                }

            }

            foreach (Ammo lovedek in VM.Ammos)
            {
                lovedek.Mozog();
                lovedek.Elettartam--;
                if (lovedek.Elettartam < 0)
                    lovedek.EletbenVan = false;
                foreach (Urleny pipe in VM.Urlenyek)
                {
                    if (pipe.IsCollision(lovedek))
                    {
                        pipe.EletbenVan = false;
                        lovedek.EletbenVan = false;
                    }
                }
                foreach (PresentAmmo item in VM.Presents)
                {
                    if (item.IsCollision(lovedek))
                    {
                        item.EletbenVan = false;
                        lovedek.EletbenVan = false;
                    }
                }

            }

            for (int i = 0; i < VM.EnemyAmmo.Count; i++)
            {
                if (VM.Ship.IsCollision(VM.EnemyAmmo[i]))
                {
                    VM.shipLife--;
                    VM.EnemyAmmo.Remove(VM.EnemyAmmo[i]);

                }
            }

            foreach (EnemyAmmo enemyammo in VM.EnemyAmmo)
            {
                enemyammo.EnemyAmmo_Move();
            }
            foreach (PresentAmmo present in VM.Presents)
            {
                present.Present_Move();
            }
            for (int i = 0; i < VM.Presents.Count; i++)
            {
                foreach (Ammo lovedek in VM.Ammos)
                {
                    if (lovedek.IsCollision(VM.Presents[i]))
                    {
                        VM.Points += 50;
                    }
                }
            }
            InaktivLovedekekTorlese();
            InaktivUrlenyekTorlese();
            InaktivEnemyLovedekekTorlese();
            InaktivPresentTorlese();
            PresentAppear();

            EnemyAmmo();

            if (VM.Urlenyek.Count == 0)
                vege = true;

            if (VM.shipLife == 0)
            {
                vege = true;
                MessageBox.Show("Vesztettél");
            }
            if (vege)
            {
                t.Stop();
                InvalidateVisual();
                MessageBox.Show("Vége a játéknak!");
                Application.Current.Shutdown();
            }
            InvalidateVisual();
        }
        void PresentAppear()
        {
            if (sw1.ElapsedMilliseconds > 7000)
            {
                Random r = new Random();

                int x = r.Next(0, 20);
                int y = r.Next(10, 200);
                int sugar = 10;
                int kozeppontX = r.Next(sugar, (int)ActualWidth - sugar);
                int kozeppontY = r.Next(sugar, (int)ActualHeight - sugar);

                PresentAmmo present = new PresentAmmo(
                new EllipseGeometry(
            new Point(kozeppontX, kozeppontY), 5, 5), x, y);

                VM.Presents.Add(present);
                sw1 = new Stopwatch();
                sw1.Start();
            }
        }
        void EnemyAmmo()
        {
            if (sw.ElapsedMilliseconds > 3000)
            {
                Random r = new Random();

                int szam = r.Next(0, VM.Urlenyek.Count);
                int sugar = 10;
                int kozeppontX = r.Next(sugar, (int)ActualWidth - sugar);
                int kozeppontY = r.Next(sugar, (int)ActualHeight - sugar);

                EnemyAmmo lovedek = new EnemyAmmo(
                new EllipseGeometry(
            new Point(kozeppontX, kozeppontY), 5, 5), (int)(VM.Urlenyek[szam].RealArea.Bounds.X), (int)(VM.Urlenyek[szam].RealArea.Bounds.Y));
                VM.EnemyAmmo.Add(lovedek);
                sw = new Stopwatch();
                sw.Start();
            }

        }

        void InaktivLovedekekTorlese()
        {
            //inaktív lövedékek törlése
            int i = 0;
            while (i < VM.Ammos.Count)
            {
                if (!VM.Ammos[i].EletbenVan)
                {
                    VM.Ammos.RemoveAt(i);
                    i = 0;
                }
                else
                    i++;
            }
        }
        void InaktivEnemyLovedekekTorlese()
        {
            //inaktív lövedékek törlése
            int i = 0;
            while (i < VM.EnemyAmmo.Count)
            {
                if (!VM.EnemyAmmo[i].EletbenVan)
                {
                    VM.EnemyAmmo.RemoveAt(i);
                    i = 0;
                }
                else
                    i++;
            }
        }

        void InaktivPresentTorlese()
        {
            int i = 0;
            while (i < VM.Presents.Count)
            {
                if (!VM.Presents[i].EletbenVan)
                {
                    VM.Presents.RemoveAt(i);
                    i = 0;
                }
                else
                    i++;
            }
        }
        void InaktivUrlenyekTorlese()
        {
            //inaktív aszteroidák törlése
            int i = 0;
            while (i < VM.Urlenyek.Count)
            {
                if (!VM.Urlenyek[i].EletbenVan)
                {
                    VM.Urlenyek.RemoveAt(i);
                    i = 0;
                    VM.Points++;
                }
                else
                    i++;
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (VM != null)
            {
                drawingContext.DrawImage(Forrasok.bg, new Rect(0, 0, ActualWidth, ActualHeight));
                // drawingContext.DrawGeometry(Brushes.Red, red, VM.Ship.RealArea);
                drawingContext.DrawImage(Forrasok.bi1, new Rect(VM.Ship.RealArea.Bounds.X, VM.Ship.RealArea.Bounds.Y, VM.Ship.RealArea.Bounds.Width, VM.Ship.RealArea.Bounds.Height));

                foreach (Urleny one in VM.Urlenyek)
                {
                    if (one.RealArea.Bounds.Y < 30)
                    {
                        drawingContext.DrawImage(Forrasok.invaderwhite, new Rect(one.RealArea.Bounds.X, one.RealArea.Bounds.Y, one.RealArea.Bounds.Width, one.RealArea.Bounds.Height));
                    }
                    if (one.RealArea.Bounds.Y > 30 && one.RealArea.Bounds.Y < 60)
                    {
                        drawingContext.DrawImage(Forrasok.invaderblue, new Rect(one.RealArea.Bounds.X, one.RealArea.Bounds.Y, one.RealArea.Bounds.Width, one.RealArea.Bounds.Height));
                    }
                    if (one.RealArea.Bounds.Y > 60 && one.RealArea.Bounds.Y < 120)
                    {
                        drawingContext.DrawImage(Forrasok.invadergreen, new Rect(one.RealArea.Bounds.X, one.RealArea.Bounds.Y, one.RealArea.Bounds.Width, one.RealArea.Bounds.Height));
                    }
                    if (one.RealArea.Bounds.Y > 150 && one.RealArea.Bounds.Y < 200)
                    {
                        drawingContext.DrawImage(Forrasok.invaderred, new Rect(one.RealArea.Bounds.X, one.RealArea.Bounds.Y, one.RealArea.Bounds.Width, one.RealArea.Bounds.Height));
                    }
                    if (one.RealArea.Bounds.Y > 200)
                    {
                        drawingContext.DrawImage(Forrasok.invaderyellow, new Rect(one.RealArea.Bounds.X, one.RealArea.Bounds.Y, one.RealArea.Bounds.Width, one.RealArea.Bounds.Height));
                    }
                    //drawingContext.DrawGeometry(Brushes.Green, black, one.RealArea);
                    //drawingContext.DrawImage(Forrasok.bi, new Rect(one.RealArea.Bounds.X,one.RealArea.Bounds.Y,one.RealArea.Bounds.Width,one.RealArea.Bounds.Height));

                }

                foreach (Ammo lovedek in VM.Ammos)
                {
                    drawingContext.DrawImage(Forrasok.ammo, new Rect(lovedek.RealArea.Bounds.X, lovedek.RealArea.Bounds.Y, lovedek.RealArea.Bounds.Width, lovedek.RealArea.Bounds.Height));

                    // drawingContext.DrawGeometry(Brushes.Black, black, lovedek.RealArea);


                }
                foreach (EnemyAmmo enemyammo in VM.EnemyAmmo)
                {
                    drawingContext.DrawImage(Forrasok.enemyammo, new Rect(enemyammo.RealArea.Bounds.X, enemyammo.RealArea.Bounds.Y, enemyammo.RealArea.Bounds.Width, enemyammo.RealArea.Bounds.Height));
                    //drawingContext.DrawGeometry(Brushes.Aqua, black, enemyammo.RealArea); 
                }
                foreach (PresentAmmo present in VM.Presents)
                {
                    drawingContext.DrawImage(Forrasok.present, new Rect(present.RealArea.Bounds.X, present.RealArea.Bounds.Y, present.RealArea.Bounds.Width, present.RealArea.Bounds.Height));
                    // drawingContext.DrawGeometry(Brushes.Pink, black, present.RealArea);   
                }

                FormattedText formattedText = new FormattedText(
                    VM.Points.ToString(),
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    font,
                    25,
                    Brushes.YellowGreen);
                drawingContext.DrawText(formattedText, loc);

                FormattedText formattedText1 = new FormattedText(
                    "Life: " + VM.shipLife.ToString(),
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    font,
                    20,
                    Brushes.Red);
                drawingContext.DrawText(formattedText1, lifeloc);
            }
        }
    }

    public static class Forrasok
    {
        public static BitmapImage bi = new BitmapImage(new Uri("invader.png", UriKind.Relative));
        public static BitmapImage bi1 = new BitmapImage(new Uri("spaceship.png", UriKind.Relative));
        public static BitmapImage invaderblue = new BitmapImage(new Uri("invaderblue.png", UriKind.Relative));
        public static BitmapImage invadergreen = new BitmapImage(new Uri("invadergreen.png", UriKind.Relative));
        public static BitmapImage invaderred = new BitmapImage(new Uri("invaderred.png", UriKind.Relative));
        public static BitmapImage invaderwhite = new BitmapImage(new Uri("invaderwhite.png", UriKind.Relative));
        public static BitmapImage invaderyellow = new BitmapImage(new Uri("invaderyellow.png", UriKind.Relative));
        public static BitmapImage ammo = new BitmapImage(new Uri("ammo.png", UriKind.Relative));
        public static BitmapImage bg = new BitmapImage(new Uri("bg.jpg", UriKind.Relative));
        public static BitmapImage present = new BitmapImage(new Uri("present.png", UriKind.Relative));
        public static BitmapImage enemyammo = new BitmapImage(new Uri("enemyammo.png", UriKind.Relative));

    }

    class Ammo : GameItem
    {
        bool eletbenvan;

        int elettartam;
        public int Elettartam
        {
            get { return elettartam; }
            set { elettartam = value; }
        }
        public bool EletbenVan
        {
            get { return eletbenvan; }
            set { eletbenvan = value; }
        }

        public Ammo(Geometry geometria, int newcx, int newcy)
        {
            Area = new EllipseGeometry(new Point(10, 0), 15, 15);
            this.eletbenvan = true; elettartam = 100;
            cx = newcx; cy = newcy;
        }

        public void Mozog()
        {
            cy -= 10;
        }
        public void EnemyAmmo_Move()
        { cy += 10; }

        public void Present_Move()
        {
            Random r = new Random();
            int change = r.Next(10, 15);
            cx += change;
        }

    }

    class EnemyAmmo : GameItem
    {
        bool eletbenvan;

        int elettartam;
        public int Elettartam
        {
            get { return elettartam; }
            set { elettartam = value; }
        }
        public bool EletbenVan
        {
            get { return eletbenvan; }
            set { eletbenvan = value; }
        }

        public EnemyAmmo(Geometry geometria, int newcx, int newcy)
        {
            Area = new EllipseGeometry(new Point(10, 0), 15, 15);
            this.eletbenvan = true; elettartam = 100;
            cx = newcx; cy = newcy;
        }

        public void Mozog()
        {
            cy -= 10;
        }
        public void EnemyAmmo_Move()
        { cy += 10; }

        public void Present_Move()
        {
            Random r = new Random();
            int change = r.Next(10, 15);
            cx += change;
        }

    }

    class PresentAmmo : GameItem
    {
        bool eletbenvan;

        int elettartam;
        public int Elettartam
        {
            get { return elettartam; }
            set { elettartam = value; }
        }
        public bool EletbenVan
        {
            get { return eletbenvan; }
            set { eletbenvan = value; }
        }

        public PresentAmmo(Geometry geometria, int newcx, int newcy)
        {
            Area = new EllipseGeometry(new Point(10, 0), 10, 10);
            this.eletbenvan = true; elettartam = 100;
            cx = newcx; cy = newcy;
        }

        public void Mozog()
        {
            cy -= 10;
        }
        public void EnemyAmmo_Move()
        { cy += 10; }

        public void Present_Move()
        {
            Random r = new Random();
            int change = r.Next(10, 15);
            cx += change;
        }

    }
}