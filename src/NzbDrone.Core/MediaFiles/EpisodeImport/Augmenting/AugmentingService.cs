using System;
using System.Collections.Generic;
using System.IO;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Core.MediaFiles.EpisodeImport.Augmenting.Augmenters;
using NzbDrone.Core.MediaFiles.MediaInfo;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.EpisodeImport.Augmenting
{
    public interface IAugmentingService
    {
        LocalEpisode Augment(LocalEpisode localEpisode, bool otherFiles);
    }

    public class AugmentingService : IAugmentingService
    {
        private readonly IEnumerable<IAugmentLocalEpisode> _augmenters;
        private readonly IParsingService _parsingService;
        private readonly IDiskProvider _diskProvider;
        private readonly IVideoFileInfoReader _videoFileInfoReader;
        private readonly Logger _logger;

        public AugmentingService(IEnumerable<IAugmentLocalEpisode> augmenters,
                                 IParsingService parsingService,
                                 IDiskProvider diskProvider,
                                 IVideoFileInfoReader videoFileInfoReader,
                                 Logger logger)
        {
            _augmenters = augmenters;
            _parsingService = parsingService;
            _diskProvider = diskProvider;
            _videoFileInfoReader = videoFileInfoReader;
            _logger = logger;
        }

        public LocalEpisode Augment(LocalEpisode localEpisode, bool otherFiles)
        {
            localEpisode.ParsedEpisodeInfo = GetBestEpisodeInfo(localEpisode, otherFiles);

            if (localEpisode.ParsedEpisodeInfo == null)
            {
                if (MediaFileExtensions.Extensions.Contains(Path.GetExtension(localEpisode.Path)))
                {
                    throw new AugmentingFailedException("Unable to parse episode info from path: {0}", localEpisode.Path);
                }
            }

            localEpisode.Size = _diskProvider.GetFileSize(localEpisode.Path);
            localEpisode.MediaInfo = _videoFileInfoReader.GetMediaInfo(localEpisode.Path);

            foreach (var augmenter in _augmenters)
            {
                try
                {
                    augmenter.Augment(localEpisode, otherFiles);
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, ex.Message);
                }
            }

            return localEpisode;
        }

        private ParsedEpisodeInfo GetBestEpisodeInfo(LocalEpisode localEpisode, bool otherFiles)
        {
            var parsedEpisodeInfo = localEpisode.FileEpisodeInfo;
            var downloadClientEpisodeInfo = localEpisode.DownloadClientEpisodeInfo;
            var folderEpisodeInfo = localEpisode.FolderEpisodeInfo;

            if (!otherFiles && !SceneChecker.IsSceneTitle(Path.GetFileNameWithoutExtension(localEpisode.Path)))
            {
                if (downloadClientEpisodeInfo != null && !downloadClientEpisodeInfo.FullSeason)
                {
                    parsedEpisodeInfo = localEpisode.DownloadClientEpisodeInfo;
                }
                else if (folderEpisodeInfo != null && !folderEpisodeInfo.FullSeason)
                {
                    parsedEpisodeInfo = localEpisode.FolderEpisodeInfo;
                }
            }

            if (parsedEpisodeInfo == null || parsedEpisodeInfo.IsPossibleSpecialEpisode)
            {
                var title = Path.GetFileNameWithoutExtension(localEpisode.Path);
                var specialEpisodeInfo = _parsingService.ParseSpecialEpisodeTitle(title, localEpisode.Series);

                return specialEpisodeInfo;
            }

            return parsedEpisodeInfo;
        }
    }
}
