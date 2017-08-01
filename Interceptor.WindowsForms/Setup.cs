using System;
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
                    return true;//(filter the message and don't let app process it)
                case 0x202:
                    //left button up, ie. a click
                    break;
                case 0x203:
                    //left button double click 
                    return true; //(filter the message and don't let app process it)
            }

            return false;
        }
    }
}
