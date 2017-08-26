using System;
using Newtonsoft.Json;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Qualities
{
    public class QualityModel : IEmbeddedDocument, IEquatable<QualityModel>
    {
        public Quality Quality { get; set; }
        public Revision Revision { get; set; }

        [JsonIgnore]
        public QualityDetectionSource QualityDetectionSource { get; set; }

        [JsonIgnore]
        public QualitySource QualitySource
        {
            get
            {
                if (Quality == Quality.Bluray2160p || Quality == Quality.Bluray1080p || Quality == Quality.Bluray720p)
                {
                    return QualitySource.Bluray;
                }

                if (Quality == Quality.WEBDL2160p || Quality == Quality.WEBDL1080p || Quality == Quality.WEBDL720p || Quality == Quality.WEBDL480p)
                {
                    return QualitySource.Web;
                }

                if (Quality == Quality.DVD)
                {
                    return QualitySource.DVD;
                }

                if (Quality == Quality.RAWHD || Quality == Quality.HDTV2160p || Quality == Quality.HDTV1080p || Quality == Quality.HDTV720p || Quality == Quality.SDTV)
                {
                    return QualitySource.Television;
                }

                return QualitySource.Unknown;
            }
        }
        
        public QualityModel()
            : this(Quality.Unknown, new Revision())
        {

        }

        public QualityModel(Quality quality, Revision revision = null)
        {
            Quality = quality;
            Revision = revision ?? new Revision();
        }

        public override string ToString()
        {
            return string.Format("{0} {1}", Quality, Revision);
        }

        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hash = 17;
                hash = hash * 23 + Revision.GetHashCode();
                hash = hash * 23 + Quality.GetHashCode();
                return hash;
            }
        }

        public bool Equals(QualityModel other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return other.Quality.Equals(Quality) && other.Revision.Equals(Revision);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;

            return Equals(obj as QualityModel);
        }

        public static bool operator ==(QualityModel left, QualityModel right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(QualityModel left, QualityModel right)
        {
            return !Equals(left, right);
        }
    }
}
