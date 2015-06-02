using System;
using JetBrains.Annotations;
using log4net;
using log4net.Core;

namespace JetBlack.MessageBus.Common.Diagnostics
{
    public static class Log4NetExtensions
    {
        public static void Fine(this ILog log, string message, Exception exception)
        {
            log.Logger.Log(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, Level.Fine, message, exception);
        }

        [StringFormatMethod("format")]
        public static void FineFormat(this ILog log, string format, params object[] args)
        {
            log.Logger.Log(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, Level.Fine, string.Format(format, args), null);
        }

        public static void Finer(this ILog log, string message, Exception exception)
        {
            log.Logger.Log(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, Level.Finer, message, exception);
        }

        [StringFormatMethod("format")]
        public static void FinerFormat(this ILog log, string format, params object[] args)
        {
            log.Logger.Log(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, Level.Finer, string.Format(format, args), null);
        }

        public static void Finest(this ILog log, string message, Exception exception)
        {
            log.Logger.Log(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, Level.Finest, message, exception);
        }

        [StringFormatMethod("format")]
        public static void FinestFormat(this ILog log, string format, params object[] args)
        {
            log.Logger.Log(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, Level.Finest, string.Format(format, args), null);
        }

        public static bool IsFineEnabled(this ILog log)
        {
            return log.Logger.IsEnabledFor(Level.Fine);
        }

        public static bool IsFinerEnabled(this ILog log)
        {
            return log.Logger.IsEnabledFor(Level.Finer);
        }

        public static bool IsFinestEnabled(this ILog log)
        {
            return log.Logger.IsEnabledFor(Level.Finest);
        }
    }
}
