using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.EpisodeImport.Augmenting.Augmenters
{
    public class AugmentEpisodes : IAugmentLocalEpisode
    {
        private readonly IParsingService _parsingService;

        public AugmentEpisodes(IParsingService parsingService)
        {
            _parsingService = parsingService;
        }

        public LocalEpisode Augment(LocalEpisode localEpisode, bool otherFiles)
        {
            localEpisode.Episodes = _parsingService.GetEpisodes(localEpisode.ParsedEpisodeInfo, localEpisode.Series, localEpisode.SceneSource);

            return localEpisode;
        }
    }
}
