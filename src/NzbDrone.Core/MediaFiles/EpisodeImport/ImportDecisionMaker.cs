using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download;
using NzbDrone.Core.MediaFiles.EpisodeImport.Augmenting;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.MediaFiles.EpisodeImport
{
    public interface IMakeImportDecision
    {
        List<ImportDecision> GetImportDecisions(List<string> videoFiles, Series series);
        List<ImportDecision> GetImportDecisions(List<string> videoFiles, Series series, DownloadClientItem downloadClientItem, ParsedEpisodeInfo folderInfo, bool sceneSource);
    }

    public class ImportDecisionMaker : IMakeImportDecision
    {
        private readonly IEnumerable<IImportDecisionEngineSpecification> _specifications;
        private readonly IMediaFileService _mediaFileService;
        private readonly IAugmentingService _augmentingService;
        private readonly IDiskProvider _diskProvider;
        private readonly IDetectSample _detectSample;
        private readonly Logger _logger;

        public ImportDecisionMaker(IEnumerable<IImportDecisionEngineSpecification> specifications,
                                   IMediaFileService mediaFileService,
                                   IAugmentingService augmentingService,
                                   IDiskProvider diskProvider,
                                   IDetectSample detectSample,
                                   Logger logger)
        {
            _specifications = specifications;
            _mediaFileService = mediaFileService;
            _augmentingService = augmentingService;
            _diskProvider = diskProvider;
            _detectSample = detectSample;
            _logger = logger;
        }

        public List<ImportDecision> GetImportDecisions(List<string> videoFiles, Series series)
        {
            return GetImportDecisions(videoFiles, series, null, null, false);
        }

        public List<ImportDecision> GetImportDecisions(List<string> videoFiles, Series series, DownloadClientItem downloadClientItem, ParsedEpisodeInfo folderInfo, bool sceneSource)
        {
            var newFiles = _mediaFileService.FilterExistingFiles(videoFiles.ToList(), series);

            _logger.Debug("Analyzing {0}/{1} files.", newFiles.Count, videoFiles.Count());

            ParsedEpisodeInfo downloadClientItemInfo = null;

            if (downloadClientItem != null)
            {
                downloadClientItemInfo = Parser.Parser.ParseTitle(downloadClientItem.Title);
            }

            var nonSampleVideoFileCount = NonSampleVideoFileCount(newFiles, series, downloadClientItemInfo, folderInfo);

            var decisions = new List<ImportDecision>();

            foreach (var file in newFiles)
            {
                decisions.AddIfNotNull(GetDecision(file, series, downloadClientItem, downloadClientItemInfo, folderInfo, nonSampleVideoFileCount > 1, sceneSource));
            }

            return decisions;
        }

        private ImportDecision GetDecision(string file, Series series, DownloadClientItem downloadClientItem, ParsedEpisodeInfo downloadClientEpisodeInfo, ParsedEpisodeInfo folderEpisodeInfo, bool otherFiles, bool sceneSource)
        {
            ImportDecision decision = null;

            var fileEpisodeInfo = Parser.Parser.ParsePath(file);
            var localEpisode = new LocalEpisode
                               {
                                   Series = series,
                                   FileEpisodeInfo = fileEpisodeInfo,
                                   DownloadClientEpisodeInfo = downloadClientEpisodeInfo,
                                   FolderEpisodeInfo = folderEpisodeInfo,
                                   Path = file,
                                   Size = _diskProvider.GetFileSize(file),
                                   SceneSource = sceneSource
                               };

            try
            {
                _augmentingService.Augment(localEpisode, otherFiles);

                if (localEpisode.Episodes.Empty())
                {
                    decision = new ImportDecision(localEpisode, new Rejection("Invalid season or episode"));
                }
                else
                {
                    decision = GetDecision(localEpisode, downloadClientItem);
                }
            }
            catch (AugmentingFailedException)
            {
                decision = new ImportDecision(localEpisode, new Rejection("Unable to parse file"));
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Couldn't import file. {0}", file);

                decision = new ImportDecision(localEpisode, new Rejection("Unexpected error processing file"));
            }

            if (decision == null)
            {
                _logger.Error("Unable to make a decision on {0}", file);
            }

            return decision;
        }

        private ImportDecision GetDecision(LocalEpisode localEpisode, DownloadClientItem downloadClientItem)
        {
            var reasons = _specifications.Select(c => EvaluateSpec(c, localEpisode, downloadClientItem))
                                         .Where(c => c != null);

            return new ImportDecision(localEpisode, reasons.ToArray());
        }

        private Rejection EvaluateSpec(IImportDecisionEngineSpecification spec, LocalEpisode localEpisode, DownloadClientItem downloadClientItem)
        {
            try
            {
                var result = spec.IsSatisfiedBy(localEpisode, downloadClientItem);

                if (!result.Accepted)
                {
                    return new Rejection(result.Reason);
                }
            }
            catch (Exception e)
            {
                //e.Data.Add("report", remoteEpisode.Report.ToJson());
                //e.Data.Add("parsed", remoteEpisode.ParsedEpisodeInfo.ToJson());
                _logger.Error(e, "Couldn't evaluate decision on {0}", localEpisode.Path);
                return new Rejection($"{spec.GetType().Name}: {e.Message}");
            }

            return null;
        }

        private int NonSampleVideoFileCount(List<string> videoFiles, Series series, ParsedEpisodeInfo downloadClientItemInfo, ParsedEpisodeInfo folderInfo)
        {
            var isPossibleSpecialEpisode = downloadClientItemInfo?.IsPossibleSpecialEpisode ?? false;
            // If we might already have a special, don't try to get it from the folder info.
            isPossibleSpecialEpisode = isPossibleSpecialEpisode || (folderInfo?.IsPossibleSpecialEpisode ?? false);

            return videoFiles.Count(file =>
            {
                var sample = _detectSample.IsSample(series, file, isPossibleSpecialEpisode);

                if (sample == DetectSampleResult.Sample)
                {
                    return false;
                }

                return true;
            });
        }
    }
}
