using Neptunium.Core.Stations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Neptunium.Model;

namespace Neptunium.Core.Media.Metadata
{
    [DataContract]
    public class SongMetadata
    {
        [DataMember]
        public string Track { get; set; }
        [DataMember]
        public string Artist { get; set; }
        [DataMember]
        public string StationPlayedOn { get; set; }
        [DataMember]
        public Uri StationLogo { get; set; }

        [IgnoreDataMember]
        public bool IsUnknownMetadata { get; internal set; } = false;
        [IgnoreDataMember]
        public string RadioProgram { get; internal set; }

        [DataMember]
        public TimeSpan SongLength { get; internal set; }

        public override string ToString()
        {
            return string.Format("{0} - {1}", Artist ?? "Unknown Artist", Track ?? "Unknown Track");
        }

        internal static SongMetadata Parse(string str)
        {
            if (string.IsNullOrWhiteSpace(str)) return null;

            SongMetadata data = new SongMetadata();

            string[] bits = str.Split(new string[] { " - " }, 2, StringSplitOptions.None);

            data.Artist = bits[0].Trim();
            data.Track = bits[1].Trim();

            return data;
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (!(obj is SongMetadata)) return false;

            SongMetadata other = (SongMetadata)obj;
            return this.Track.Equals(other.Track, StringComparison.CurrentCultureIgnoreCase) && this.Artist.Equals(other.Artist, StringComparison.CurrentCultureIgnoreCase);
        }

        public override int GetHashCode()
        {
            return (Track?.GetHashCode() ?? 0) + (Artist?.GetHashCode() ?? 0) + (StationPlayedOn?.GetHashCode() ?? 0);
        }

        public bool FullyEquals(SongMetadata other)
        {
            if (!this.Equals(other)) return false;

            throw new NotImplementedException(); //todo: compare other fields
        }
    }

    public class ExtendedSongMetadata: SongMetadata
    {
        //todo to fill out

        public ExtendedSongMetadata()
        {

        }
        public ExtendedSongMetadata(SongMetadata original)
        {
            Track = original.Track;
            Artist = original.Artist;
            StationPlayedOn = original.StationPlayedOn;
            StationLogo = original.StationLogo;
            IsUnknownMetadata = original.IsUnknownMetadata;
            RadioProgram = original.RadioProgram;
        }

        public AlbumData Album { get; internal set; }
        public ArtistData ArtistInfo { get; internal set; }
        public JPopAsiaArtistData JPopAsiaArtistInfo { get; internal set; }
        public Uri FanArtTVBackgroundUrl { get; internal set; }
        public string[] FeaturedArtists { get; internal set; }
    }
}
