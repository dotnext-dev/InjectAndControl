using System;
using System.Drawing;
using System.Windows.Forms;

namespace Interceptor.WindowsForms
{
    public class Setup
    {
        public static bool Start()
        {
            Application.AddMessageFilter(new MessageFilter());
            return true;
        }
    }

    public class MessageFilter : IMessageFilter
    {
        public bool PreFilterMessage(ref Message m)
        {
            // and use other params to determine mouse click location 
            switch (m.Msg)
            {
                case 0x201:
                    //left button down 
                    return IsInFourthQuadrant();//(filter the message and don't let app process it)
                case 0x202:
                    //left button up, ie. a click
                    break;
                case 0x203:
                    //left button double click 
                    return IsInFourthQuadrant(); //(filter the message and don't let app process it)
            }

            return false;
        }


        private static bool IsInFourthQuadrant()
        {
            var window = Application.OpenForms[0];
            var screen_coords = Cursor.Position;

            var bounds = window.Bounds;
            var fourthQuadrant = new Rectangle(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2, bounds.Width / 2, bounds.Height / 2);

            return fourthQuadrant.Contains(screen_coords);
        }
    }
}
