﻿using NLog;
using System;
using ILogger = OMS.Common.Cloud.Logger.ICloudLogger;

namespace Common.Web.Loggers
{
    public class FileLogger : ILogger
    {
        private static Logger _logger;

        public FileLogger()
        {
            _logger = LogManager.GetCurrentClassLogger();
        }

        public void LogDebug(string message, Exception e = null)
        {
            _logger.Debug(message);
        }

        public void LogError(Exception e, string message = null)
        {
            _logger.Error(e, message ?? e.Message);
        }

        public void LogError(string message, Exception e = null)
        {
            _logger.Error(e, message ?? e.Message);
        }

        public void LogFatal(string message, Exception e = null)
        {
            _logger.Fatal(e, message ?? e.Message);
        }

        public void LogInformation(string message, Exception e = null)
        {
            _logger.Info(message);
        }

        public void LogWarning(string message, Exception e = null)
        {
            _logger.Warn(message);
        }

        public void LogVerbose(string message, Exception e = null)
        {
            throw new NotImplementedException();
        }
    }
}
