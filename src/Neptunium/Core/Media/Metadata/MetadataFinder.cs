using Neptunium.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptunium.Core.Media.Metadata
{
    public static class MetadataFinder
    {
        public static async Task<ExtendedSongMetadata> FindMetadataAsync(SongMetadata originalMetadata)
        {
            var metaSrc = new MusicBrainzMetadataSource();
            AlbumData albumData = null;

            try
            {
                albumData = await metaSrc.TryFindAlbumAsync(originalMetadata.Track, originalMetadata.Artist);
            }
            catch { }

            var extendedMetadata = new ExtendedSongMetadata(originalMetadata);

            if (albumData != null)
            {
                //todo cache
                extendedMetadata.Album = albumData;
            }

            return extendedMetadata;
        }
    }
}
