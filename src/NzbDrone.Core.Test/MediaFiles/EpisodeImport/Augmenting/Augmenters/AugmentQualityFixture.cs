using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.MediaFiles.EpisodeImport.Augmenting.Augmenters;
using NzbDrone.Core.MediaFiles.MediaInfo;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Test.MediaFiles.EpisodeImport.Augmenting.Augmenters
{
    [TestFixture]
    public class AugmentQualityFixture : CoreTest<AugmentQuality>
    {
        private Series _series;

        [SetUp]
        public void Setup()
        {
            _series = Builder<Series>.CreateNew()
                                     .With(e => e.Profile = new Profile { Items = Qualities.QualityFixture.GetDefaultQualities() })
                                     .Build();
        }

        private ParsedEpisodeInfo GivenParsedEpisodeInfo(Quality quality, QualityDetectionSource qualityDetectionSource = QualityDetectionSource.Name)
        {
            var info = Builder<ParsedEpisodeInfo>.CreateNew()
                                                .With(p => p.Quality = new QualityModel(quality))
                                                .Build();

            info.Quality.QualityDetectionSource = qualityDetectionSource;

            return info;
        }

        private MediaInfoModel GivenMediaInfo(int width)
        {
            return Builder<MediaInfoModel>.CreateNew()
                                          .With(m => m.Width = width)
                                          .Build();
        }

        [Test]
        public void should_return_file_quality_if_other_information_is_not_available()
        {
            var expectedQuality = Quality.SDTV;
            var fileEpisodeInfo = GivenParsedEpisodeInfo(expectedQuality);
            var localEpisode = new LocalEpisode
                               {
                                   FileEpisodeInfo = fileEpisodeInfo,
                                   Series = _series
                               };

            Subject.Augment(localEpisode, false);

            localEpisode.Quality.Quality.Should().Be(expectedQuality);
        }

        [Test]
        public void should_use_folder_quality_when_file_quality_was_determined_by_the_extension()
        {
            var expectedQuality = Quality.SDTV;
            var fileEpisodeInfo = GivenParsedEpisodeInfo(Quality.HDTV720p, QualityDetectionSource.Extension);
            var folderEpisodeInfo = GivenParsedEpisodeInfo(expectedQuality);
            var localEpisode = new LocalEpisode
            {
                FileEpisodeInfo = fileEpisodeInfo,
                FolderEpisodeInfo = folderEpisodeInfo,
                Series = _series
            };

            Subject.Augment(localEpisode, false);

            localEpisode.Quality.Quality.Should().Be(expectedQuality);
        }

        [Test]
        public void should_use_folder_quality_when_greater_than_file_quality()
        {
            var expectedQuality = Quality.WEBDL720p;
            var fileEpisodeInfo = GivenParsedEpisodeInfo(Quality.HDTV720p);
            var folderEpisodeInfo = GivenParsedEpisodeInfo(expectedQuality);
            var localEpisode = new LocalEpisode
            {
                FileEpisodeInfo = fileEpisodeInfo,
                FolderEpisodeInfo = folderEpisodeInfo,
                Series = _series
            };

            Subject.Augment(localEpisode, false);

            localEpisode.Quality.Quality.Should().Be(expectedQuality);
        }

        [Test]
        public void should_use_download_client_item_quality_when_file_quality_was_determined_by_the_extension()
        {
            var expectedQuality = Quality.SDTV;
            var fileEpisodeInfo = GivenParsedEpisodeInfo(Quality.HDTV720p, QualityDetectionSource.Extension);
            var downloadClientEpisodeInfo = GivenParsedEpisodeInfo(expectedQuality);
            var localEpisode = new LocalEpisode
            {
                FileEpisodeInfo = fileEpisodeInfo,
                DownloadClientEpisodeInfo = downloadClientEpisodeInfo,
                Series = _series
            };

            Subject.Augment(localEpisode, false);

            localEpisode.Quality.Quality.Should().Be(expectedQuality);
        }

        [Test]
        public void should_use_download_client_item_quality_when_greater_than_file_quality()
        {
            var expectedQuality = Quality.WEBDL720p;
            var fileEpisodeInfo = GivenParsedEpisodeInfo(Quality.HDTV720p);
            var downloadClientEpisodeInfo = GivenParsedEpisodeInfo(expectedQuality);
            var localEpisode = new LocalEpisode
            {
                FileEpisodeInfo = fileEpisodeInfo,
                DownloadClientEpisodeInfo = downloadClientEpisodeInfo,
                Series = _series
            };

            Subject.Augment(localEpisode, false);

            localEpisode.Quality.Quality.Should().Be(expectedQuality);
        }

        [Test]
        public void should_use_download_client_item_quality_over_folder_quality()
        {
            var expectedQuality = Quality.WEBDL720p;
            var fileEpisodeInfo = GivenParsedEpisodeInfo(Quality.HDTV720p, QualityDetectionSource.Extension);
            var downloadClientEpisodeInfo = GivenParsedEpisodeInfo(expectedQuality);
            var folderEpisodeInfo = GivenParsedEpisodeInfo(Quality.Bluray720p);
            var localEpisode = new LocalEpisode
            {
                FileEpisodeInfo = fileEpisodeInfo,
                FolderEpisodeInfo = folderEpisodeInfo,
                DownloadClientEpisodeInfo = downloadClientEpisodeInfo,
                Series = _series
            };

            Subject.Augment(localEpisode, false);

            localEpisode.Quality.Quality.Should().Be(expectedQuality);
        }

        [Test]
        public void should_use_media_info_width_to_determine_quality()
        {
            var expectedQuality = Quality.HDTV1080p;
            var fileEpisodeInfo = GivenParsedEpisodeInfo(Quality.HDTV720p);
            var mediaInfo = GivenMediaInfo(1920);
            var localEpisode = new LocalEpisode
            {
                FileEpisodeInfo = fileEpisodeInfo,
                MediaInfo = mediaInfo,
                Series = _series
            };

            Subject.Augment(localEpisode, false);

            localEpisode.Quality.Quality.Should().Be(expectedQuality);
        }

        [Test]
        public void should_use_media_info_width_along_with_folder_info_to_determine_quality()
        {
            var expectedQuality = Quality.Bluray2160p;
            var fileEpisodeInfo = GivenParsedEpisodeInfo(Quality.HDTV720p);
            var folderEpisodeInfo = GivenParsedEpisodeInfo(Quality.Bluray720p);
            var mediaInfo = GivenMediaInfo(2160);
            var localEpisode = new LocalEpisode
            {
                FileEpisodeInfo = fileEpisodeInfo,
                FolderEpisodeInfo = folderEpisodeInfo,
                MediaInfo = mediaInfo,
                Series = _series
            };

            Subject.Augment(localEpisode, false);

            localEpisode.Quality.Quality.Should().Be(expectedQuality);
        }

        [Test]
        public void should_use_media_info_width_along_with_download_client_info_to_determine_quality()
        {
            var expectedQuality = Quality.Bluray2160p;
            var fileEpisodeInfo = GivenParsedEpisodeInfo(Quality.HDTV720p);
            var downloadClientEpisodeInfo = GivenParsedEpisodeInfo(Quality.Bluray720p);
            var mediaInfo = GivenMediaInfo(2160);
            var localEpisode = new LocalEpisode
            {
                FileEpisodeInfo = fileEpisodeInfo,
                DownloadClientEpisodeInfo = downloadClientEpisodeInfo,
                MediaInfo = mediaInfo,
                Series = _series
            };

            Subject.Augment(localEpisode, false);

            localEpisode.Quality.Quality.Should().Be(expectedQuality);
        }
    }
}
