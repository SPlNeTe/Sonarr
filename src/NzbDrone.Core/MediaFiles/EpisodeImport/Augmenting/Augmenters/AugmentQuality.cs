using NLog;
using NzbDrone.Core.MediaFiles.MediaInfo;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.MediaFiles.EpisodeImport.Augmenting.Augmenters
{
    public class AugmentQuality : IAugmentLocalEpisode
    {
        private readonly Logger _logger;

        public AugmentQuality(Logger logger)
        {
            _logger = logger;
        }

        public LocalEpisode Augment(LocalEpisode localEpisode, bool otherFiles)
        {
            if (localEpisode.FileEpisodeInfo == null)
            {
                return localEpisode;
            }

            var series = localEpisode.Series;
            var quality = localEpisode.FileEpisodeInfo.Quality;
            var downloadClientItemQuality = localEpisode.DownloadClientEpisodeInfo?.Quality;
            var folderQuality = localEpisode.FolderEpisodeInfo?.Quality;

            if (UseOtherQuality(series, quality, downloadClientItemQuality))
            {
                _logger.Debug("Using quality: {0} from download client item instead of file quality: {1} ", downloadClientItemQuality, quality);
                quality = downloadClientItemQuality;
            }
            else if (UseOtherQuality(series, quality, folderQuality))
            {
                _logger.Debug("Using quality: {0} from folder instead of file quality: {1} ", folderQuality, quality);
                quality = folderQuality;
            }

            // Alter the detected quality based on the media info (if available)
            quality = GetQualityFromMediaInfo(quality, localEpisode.MediaInfo);

            _logger.Debug("Using quality: {0}", quality);

            localEpisode.Quality = quality;

            return localEpisode;
        }

        private bool UseOtherQuality(Series series, QualityModel fileQuality, QualityModel otherQuality)
        {
            if (otherQuality == null)
            {
                return false;
            }

            if (otherQuality.Quality == Quality.Unknown)
            {
                return false;
            }

            if (fileQuality.QualityDetectionSource == QualityDetectionSource.Extension)
            {
                return true;
            }

            if (new QualityModelComparer(series.Profile).Compare(otherQuality, fileQuality) > 0)
            {
                return true;
            }

            return false;
        }

        private QualityModel GetQualityFromMediaInfo(QualityModel quality, MediaInfoModel mediaInfo)
        {
            if (mediaInfo == null)
            {
                return quality;
            }

            var width = mediaInfo.Width;
            Quality newQuality = null;

            if (width > 1920)
            {
                if (quality.QualitySource == QualitySource.Bluray)
                {
                    newQuality = Quality.Bluray2160p;
                }
                else if (quality.QualitySource == QualitySource.Web)
                {
                    newQuality = Quality.WEBDL2160p;
                }
                else if (quality.QualitySource == QualitySource.Television)
                {
                    newQuality = Quality.HDTV2160p;
                }
            }
            else if (width > 1280)
            {
                if (quality.QualitySource == QualitySource.Bluray)
                {
                    newQuality = Quality.Bluray1080p;
                }
                else if (quality.QualitySource == QualitySource.Web)
                {
                    newQuality = Quality.WEBDL1080p;
                }
                else if (quality.QualitySource == QualitySource.Television)
                {
                    newQuality = Quality.HDTV1080p;
                }
            }
            else if (width > 854)
            {
                if (quality.QualitySource == QualitySource.Bluray)
                {
                    newQuality = Quality.Bluray720p;
                }
                else if (quality.QualitySource == QualitySource.Web)
                {
                    newQuality = Quality.WEBDL720p;
                }
                else if (quality.QualitySource == QualitySource.Television)
                {
                    newQuality = Quality.HDTV720p;
                }
            }

            if (newQuality != null && quality.Quality != newQuality)
            {
                _logger.Debug("Quality ({0}) differs from the parsed quality ({1})", newQuality, quality.Quality);

                var newQualityModel = new QualityModel(newQuality, quality.Revision);
                newQualityModel.QualityDetectionSource = QualityDetectionSource.MediaInfo;

                return newQualityModel;
            }

            return quality;
        }
    }
}
