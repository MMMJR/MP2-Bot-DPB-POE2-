﻿using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game;
using log4net;

namespace MP2.EXtensions
{
    public static class GlobalLog
    {
        private static readonly ILog Log = Logger.GetLoggerInstanceForType();

        public static void Debug(object message)
        {
            if(MP2.Debug)
                Log.Debug(message);
        }

        public static void Debug(string message)
        {
            if (MP2.Debug)
                Log.Debug(message);
        }

        public static void Info(object message)
        {
            Log.Info(message);
        }

        public static void Info(string message)
        {
            Log.Info(message);
        }

        public static void Warn(object message)
        {
            Log.Warn(message);
        }

        public static void Warn(string message)
        {
            Log.Warn(message);
        }

        public static void Error(object message)
        {
            Log.Error(message);
        }

        public static void Error(string message)
        {
            Log.Error(message);
        }

        public static void Fatal(object message)
        {
            Log.Fatal(message);
        }

        public static void Fatal(string message)
        {
            Log.Fatal(message);
        }
    }
}
