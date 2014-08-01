﻿using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using Newtonsoft.Json.Linq;
using NzbDrone.Common;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Notifications.Xbmc.Model;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Notifications.Xbmc
{
    public class JsonApiProvider : IApiProvider
    {
        private readonly IXbmcJsonApiProxy _proxy;
        private readonly Logger _logger;

        public JsonApiProvider(IXbmcJsonApiProxy proxy, Logger logger)
        {
            _proxy = proxy;
            _logger = logger;
        }

        public bool CanHandle(XbmcVersion version)
        {
            return version >= new XbmcVersion(5);
        }

        public void Notify(XbmcSettings settings, string title, string message)
        {
            _proxy.Notify(settings, title, message);
        }

        public void Update(XbmcSettings settings, Series series)
        {
            if (!settings.AlwaysUpdate)
            {
                _logger.Debug("Determining if there are any active players on XBMC host: {0}", settings.Address);
                var activePlayers = _proxy.GetActivePlayers(settings);

                if (activePlayers.Any(a => a.Type.Equals("video")))
                {
                    _logger.Debug("Video is currently playing, skipping library update");
                    return;
                }
            }

            UpdateLibrary(settings, series);
        }
        
        public void Clean(XbmcSettings settings)
        {
            _proxy.CleanLibrary(settings);
        }

        public List<ActivePlayer> GetActivePlayers(XbmcSettings settings)
        {
            return _proxy.GetActivePlayers(settings); 
        }

        public String GetSeriesPath(XbmcSettings settings, Series series)
        {
            var allSeries = _proxy.GetSeries(settings);

            if (!allSeries.Any())
            {
                _logger.Debug("No TV shows returned from XBMC");
                return null;
            }

            var matchingSeries = allSeries.FirstOrDefault(s => s.ImdbNumber == series.TvdbId || s.Label == series.Title);

            if (matchingSeries != null) return matchingSeries.File;

            return null;
        }

        private void UpdateLibrary(XbmcSettings settings, Series series)
        {
            try
            {
                var seriesPath = GetSeriesPath(settings, series);

                JObject postJson;

                if (seriesPath != null)
                {
                    _logger.Debug("Updating series {0} (Path: {1}) on XBMC host: {2}", series, seriesPath, settings.Address);
                }

                else
                {
                    _logger.Debug("Series {0} doesn't exist on XBMC host: {1}, Updating Entire Library", series,
                                 settings.Address);
                }

                var response = _proxy.UpdateLibrary(settings, null);

                if (!response.Equals("OK", StringComparison.InvariantCultureIgnoreCase))
                {
                    _logger.Debug("Failed to update library for: {0}", settings.Address);
                }
            }

            catch (Exception ex)
            {
                _logger.DebugException(ex.Message, ex);
            }
        }
    }
}
