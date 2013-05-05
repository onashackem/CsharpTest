using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Chat.Client
{
    class ErrorMessageEventArgs : EventArgs
    {
        public string Error { get; set; }
    }
}
