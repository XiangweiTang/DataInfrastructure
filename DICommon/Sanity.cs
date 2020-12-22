using System;
using System.Collections.Generic;
using System.Text;

namespace DICommon
{
    public static class Sanity
    {
        public static void Requires(bool valid, string message)
        {
            if (!valid)
                throw new DIException(message);
        }

        public static void Requires(bool valid)
        {
            if (!valid)
                throw new DIException();
        }
    }

    public class DIException : Exception
    {
        public DIException() : base() { }
        public DIException(string msg) : base(msg) { }
    }
}
