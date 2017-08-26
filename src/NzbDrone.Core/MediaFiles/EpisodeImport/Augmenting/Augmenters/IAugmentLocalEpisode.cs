using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.EpisodeImport.Augmenting.Augmenters
{
    public interface IAugmentLocalEpisode
    {
        LocalEpisode Augment(LocalEpisode localEpisode, bool otherFiles);
    }
}
