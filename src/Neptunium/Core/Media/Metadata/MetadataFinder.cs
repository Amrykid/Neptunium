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
            //todo to actually fill out.
            return await Task.FromResult<ExtendedSongMetadata>(new ExtendedSongMetadata(originalMetadata));
        }
    }
}
