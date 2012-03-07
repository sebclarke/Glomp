using System;
using Gtk;

namespace glomp
{
    class MainClass
    {
        public static void Main (string[] args)
        {
            //Console.WriteLine(OpenTK.Graphics.GraphicsMode.Default);
            OpenTK.Toolkit.Init();
            Application.Init ();
            MainWindow win = new MainWindow ();
            win.Show ();
            Application.Run ();
        }
    }
}
