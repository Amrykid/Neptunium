namespace Neptunium.Core.Stations
{
    public enum StationStreamServerFormat
    {
        Other = -1,
        Direct = 0,
        Shoutcast = 1,
        Icecast = 2, //very similar to shoutcast
        Radionomy = 3, //shoutcast with quirks on top
    }
}