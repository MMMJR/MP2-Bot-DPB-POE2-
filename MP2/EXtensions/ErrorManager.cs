﻿using System;
using System.Collections.Generic;
using DreamPoeBot.Loki.Bot;

namespace MP2.EXtensions
{
    public static class ErrorManager
    {
        public const int MaxErrors = 20;
        public static int ErrorCount { get; private set; }

        private static readonly Dictionary<string, int> SpecificErrors = new Dictionary<string, int>();

        static ErrorManager()
        {
            Events.AreaChanged += (sender, args) => Reset();
        }

        public static void Reset()
        {
            GlobalLog.Info("[ErrorManager] Error count has been reset.");
            ErrorCount = 0;
            SpecificErrors.Clear();
        }

        public static int GetErrorCount(string error)
        {
            SpecificErrors.TryGetValue(error, out var count);
            return count;
        }

        public static async void ReportError()
        {
            ++ErrorCount;
            GlobalLog.Error($"[ErrorManager] Error count: {ErrorCount}/{MaxErrors}");

            if (ErrorCount >= MaxErrors)
            {
                GlobalLog.Error("[ErrorManager] Error threshold has been reached. Now requesting bot to stop.");

                await PlayerAction.Logout();
                ErrorCount = 0;
            }
        }

        public static void ReportError(string error)
        {
            SpecificErrors.TryGetValue(error, out var count);
            SpecificErrors[error] = ++count;
            GlobalLog.Error($"[ErrorManager] \"{error}\" error count: {count}");
        }

        public static void ReportCriticalError()
        {
            GlobalLog.Error("[CRITICAL ERROR] Now requesting bot to stop.");

            BotManager.Stop();
            throw new Exception("CRITICAL_ERROR");
        }

    }
}
