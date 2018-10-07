using Crystal3.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Neptunium.Model
{
    /// <summary>
    /// A data model representing an song artist.
    /// </summary>
    [DataContract]
    public class ArtistData : ModelBase
    {
        /// <summary>
        /// The name of the artist.
        /// </summary>
        [DataMember]
        public string Name { get; set; }
        /// <summary>
        /// An optional alternative name of the artist.
        /// </summary>
        [DataMember]
        public string AlternateName { get; set; }
        /// <summary>
        /// An ID used to retrieve more information the artist from a metadata source.
        /// </summary>
        [DataMember]
        public string ArtistID { get; set; }
        /// <summary>
        /// The gender of the artist.
        /// </summary>
        [DataMember]
        public string Gender { get; set; }
        /// <summary>
        /// A URL linking to an image of the artist.
        /// </summary>
        [DataMember]
        public string ArtistImage { get; set; }
        /// <summary>
        /// A link to a website for the artist.
        /// </summary>
        [DataMember]
        public string ArtistLinkUrl { get; set; }
        /// <summary>
        /// A link to a wikipedia article on the artist.
        /// </summary>
        [DataMember]
        public string WikipediaUrl { get; internal set; }
        /// <summary>
        /// The country or locale of the artist.
        /// </summary>
        [DataMember]
        public string Country { get; internal set; }
    }
}
