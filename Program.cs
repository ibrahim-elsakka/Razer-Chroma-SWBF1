using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ColorManager;
using Corale.Colore;
using Corale.Colore.Core;
using Corale.Colore.Razer;
using Color = Corale.Colore.Core.Color;

namespace swbf_2015_Chroma
{
    class Program
    {

      
        [DllImport("user32.dll")]
        static extern bool GetAsyncKeyState(System.Windows.Forms.Keys vKey);
        
        static void Main(string[] args)
        {
            Chroma.Instance.Keyboard.SetAll(Color.Black);
            Console.WriteLine("");
            Console.ForegroundColor = ConsoleColor.Cyan;
            VAMemory mem = new VAMemory("starwarsbattlefront");
            ColorManager.CRenderer CR = new CRenderer();
            
            for (;;)
            {
                Int64 pGameContext = mem.ReadInt64((IntPtr) (0x142AE8080));
                Int64 pPlayerManager = mem.ReadInt64((IntPtr) (pGameContext + 0x68));
                Int64 pLocalPlayer = mem.ReadInt64((IntPtr) (pPlayerManager + 0x550));
                Int64 pLocalSoldier = mem.ReadInt64((IntPtr) (pLocalPlayer + 0x2cb8));
                Int64 pHealthComponent = mem.ReadInt64((IntPtr) (pLocalSoldier + 0x198));
                float pHealth = mem.ReadFloat((IntPtr) (pHealthComponent + 0x20));
                float pHealthMax = mem.ReadFloat((IntPtr) (pHealthComponent + 0x24));
                int health = (int) ((pHealth / pHealthMax) * 100);



                Int64 pAimer = mem.ReadInt64((IntPtr) (pLocalSoldier + 0x5e0));
                Int64 pWeap = mem.ReadInt64((IntPtr) (pAimer + 0x1b8));
                Int64 pPrimary = mem.ReadInt64((IntPtr) (pWeap + 0x5130));
                float Heat = mem.ReadFloat((IntPtr) (pPrimary + 0x1c8));
                int heat = (int) (100-(Heat * 100));
               // Console.WriteLine(heat);
                CR.DrawHeat(health, true);
              //  Console.WriteLine(pHealthMax);

                /*if (GetAsyncKeyState(Keys.Up)) health += 5;
                if (GetAsyncKeyState(Keys.Down)) health -= 5;
                if (health > 100) health = 100;
                if (health < 0) health = 0;*/
                CR.DrawHealth(heat, true);
                Thread.Sleep(100);
            }

        }
    }
}

namespace  ColorManager
{
    class DesktopMgr
    {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetWindowDC(IntPtr window);

        [DllImport("gdi32.dll", SetLastError = true)]
        public static extern uint GetPixel(IntPtr dc, int x, int y);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int ReleaseDC(IntPtr window, IntPtr dc);
        public DesktopMgr() { }
        public  System.Drawing.Color GetColorPoint(int x, int y)
        {
            IntPtr desk = GetDesktopWindow();
            IntPtr dc = GetWindowDC(desk);
            int a = (int) GetPixel(dc, x, y);
            ReleaseDC(desk, dc);
            return System.Drawing.Color.FromArgb(255, (a >> 0) & 0xff, (a >> 8) & 0xff, (a >> 16) & 0xff);
        }

        public System.Drawing.Color GetColorRect(Rectangle rect, int spacingdivision)
        {
            System.Drawing.Color outColor = GetColorPoint(rect.X, rect.Y);
            for (int i = 0; i < spacingdivision; i++)
            {
                float swidth = rect.Right - rect.Left;
                float WidthSegment = swidth / spacingdivision;
                for (int w = 0; w < spacingdivision; w++)
                {
                    float sheight = rect.Bottom - rect.Top;
                    float HeightSegment = sheight / spacingdivision;
                    outColor = Blend(outColor, GetColorPoint(rect.X + (int) (WidthSegment * i), rect.Y + (int)(HeightSegment * i)), .5f);
                }

            }
            return outColor;

        }
        public  System.Drawing.Color Blend(System.Drawing.Color color, System.Drawing.Color backColor, double amount)
        {
            byte r = (byte)((color.R * amount) + backColor.R * (1 - amount));
            byte g = (byte)((color.G * amount) + backColor.G * (1 - amount));
            byte b = (byte)((color.B * amount) + backColor.B * (1 - amount));
            return System.Drawing.Color.FromArgb(r, g, b);
        }
    }



    class CRenderer
    {
        private List<System.Drawing.Color> colors = new List<System.Drawing.Color>() {System.Drawing.Color.Red, System.Drawing.Color.Green};
        private int time = 2000;
        private bool looping = false;
        public CRenderer() { }

        public CRenderer(int _time)
        {
            time = _time;
        }
        public CRenderer(List<System.Drawing.Color> _colors, int _time)
        {
            colors = _colors;
            time = _time;
        }
        public CRenderer(List<System.Drawing.Color> _colors, int _time, bool _looping)
        {
            colors = _colors;
            time = _time;
            if (_looping)
            {
                colors.Add(colors[0]);
                looping = _looping;
            }
        }
        public CRenderer(List<System.Drawing.Color> _colors, int _time, bool _looping, bool _cumulativeTime)
        {
            colors = _colors;
            
            if (_looping)
            {
                colors.Add(colors[0]);
                looping = _looping;
            }
            if (_cumulativeTime)
            {
                time = _time / colors.Count;
            }
            else
            {
                time = _time;
            }
        }
        public void AddColor(System.Drawing.Color c)
        {
            if (looping)
            {
                colors.Insert(colors.Count - 1, c);
            }
            else
            {
                colors.Add(c);
            }
        }

        public void ClearColors(List<System.Drawing.Color> _colors)
        {
            colors = _colors;
        }
        public void RenderSync()
        {
            for (int i = 0; i < colors.Count - 1; i++)
            {
                ColorBlender cb1 = new ColorManager.ColorBlender(colors[i], colors[i+1]);
                for (int w = 0; w <= 100; w++)
                {
                    System.Threading.Thread.Sleep(time / 100);
                    Chroma.Instance.Keyboard.SetAll(cb1.GetColor2(w));
                }
            }  
        }

        public void DrawHealth(int healthperc, bool CompleteColor)
        {
            ColorBlender cb = new ColorBlender(System.Drawing.Color.Red, System.Drawing.Color.Green);
            cb.bluecomplete = CompleteColor;
            cb.CompleteColor = System.Drawing.Color.Aqua;
            Color c = cb.GetColor2(healthperc);
            try
            {
                for (int i = 1; i < (int)(18 * healthperc / 100); i++)
            {              
                    Chroma.Instance.Keyboard[0, i] = c; 
            }
                for (int i = (int)(18 * healthperc / 100); i < 18; i++)
                {
                    Chroma.Instance.Keyboard[0, i] = Color.Black;
                }
            }
            catch (Exception) { }
        }
        public void DrawHeat(int healthperc, bool CompleteColor)
        {
            ColorBlender cb = new ColorBlender(System.Drawing.Color.Red, System.Drawing.Color.Green);
            cb.bluecomplete = CompleteColor;
            cb.CompleteColor = System.Drawing.Color.Aqua;
            Color c = cb.GetColor2(healthperc);
            try
            {
                for (int i = 1; i < 22; i++)
                {
                    for (int r = 2; r < 5; r++)
                    {
                        Chroma.Instance.Keyboard[r, i] = c;
                    }
                }

            }
            catch (Exception) { }
        }

    }
    class ColorBlender
    {
        public float max = 100;
        public bool bluecomplete = false;
        public System.Drawing.Color LowColor = System.Drawing.Color.Red;
        public System.Drawing.Color HighColor = System.Drawing.Color.Green;
        public System.Drawing.Color CompleteColor = System.Drawing.Color.Blue;
        public ColorBlender() { }
        public ColorBlender(System.Drawing.Color _lowColor, System.Drawing.Color _highColor)
        {
            LowColor = _lowColor;
            HighColor = _highColor;
        }
        public ColorBlender(System.Drawing.Color _lowColor, System.Drawing.Color _highColor, float _max)
        {
            LowColor = _lowColor;
            HighColor = _highColor;
            max = _max;
        }
        public ColorBlender(System.Drawing.Color _lowColor, System.Drawing.Color _highColor, float _max, System.Drawing.Color _completeColor)
        {
            LowColor = _lowColor;
            HighColor = _highColor;
            max = _max;
            CompleteColor = _completeColor;
        }
        public Corale.Colore.Core.Color GetColor2(float value)
        {
            if ((value == max) && bluecomplete)
            {
                return new Corale.Colore.Core.Color((byte)CompleteColor.R, (byte)CompleteColor.G, (byte)CompleteColor.B);
            }
            float perc = value / max;
            int r = (int)((1 - perc) * LowColor.R + perc * HighColor.R);
            int g = (int)((1 - perc) * LowColor.G + perc * HighColor.G);
            int b = (int)((1 - perc) * LowColor.B + perc * HighColor.B);
            return new Corale.Colore.Core.Color((byte)r, (byte)g, (byte)b);
        }
    }

}
