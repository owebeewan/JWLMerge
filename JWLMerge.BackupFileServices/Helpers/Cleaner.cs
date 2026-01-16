using System.Collections.Generic;
using System.Linq;
using DocumentFormat.OpenXml.Spreadsheet;
using JWLMerge.BackupFileServices.Models.DatabaseModels;
using Serilog;

namespace JWLMerge.BackupFileServices.Helpers;

/// <summary>
/// Cleans jwlibrary files by removing redundant or anomalous database rows.
/// </summary>
internal sealed class Cleaner(Database database)
{
    /// <summary>
    /// Cleans the data, removing unused rows.
    /// </summary>
    /// <returns>Number of rows removed.</returns>
    public int Clean()
    {
        // see also DatabaseForeignKeyChecker
        return CleanBlockRanges() +
               CleanTagMaps() +
               CleanLocations() +
               CleanIndependentMedias() +
               CleanPlaylistItemMarkers() +
               CleanPlaylistItemMarkerBibleVerseMaps() +
               CleanPlaylistItemMarkerParagraphMaps() +
               CleanPlaylistItemLocationMaps() +
               CleanPlaylistItemIndependentMediaMaps() +
               CleanPlaylistItems();
    }

    private HashSet<int> GetValidUserMarkIds()
    {
        var result = new HashSet<int>();

        foreach (var userMark in database.UserMarks)
        {
            result.Add(userMark.UserMarkId);
        }

        return result;
    }

    private HashSet<int> GetValidTagIds()
    {
        var result = new HashSet<int>();

        foreach (var tag in database.Tags)
        {
            result.Add(tag.TagId);
        }

        return result;
    }

    private HashSet<int> GetValidNoteIds()
    {
        var result = new HashSet<int>();

        foreach (var note in database.Notes)
        {
            result.Add(note.NoteId);
        }

        return result;
    }

    private HashSet<int> GetValidLocationIds()
    {
        var result = new HashSet<int>();

        foreach (var location in database.Locations)
        {
            result.Add(location.LocationId);
        }

        return result;
    }

    private HashSet<int> GetLocationIdsInUse()
    {
        var result = new HashSet<int>();

        foreach (var bookmark in database.Bookmarks)
        {
            result.Add(bookmark.LocationId);
            result.Add(bookmark.PublicationLocationId);
        }

        foreach (var note in database.Notes)
        {
            if (note.LocationId != null)
            {
                result.Add(note.LocationId.Value);
            }
        }

        foreach (var userMark in database.UserMarks)
        {
            result.Add(userMark.LocationId);
        }

        foreach (var tagMap in database.TagMaps)
        {
            if (tagMap.LocationId != null)
            {
                result.Add(tagMap.LocationId.Value);
            }
        }

        foreach (var inputFld in database.InputFields)
        {
            result.Add(inputFld.LocationId);
        }

        foreach (var playlistItemLocationMap in database.PlaylistItemLocationMaps)
        {
            result.Add(playlistItemLocationMap.LocationId);
        }

        Log.Logger.Debug($"Found {result.Count} location Ids in use");

        return result;
    }

    /// <summary>
    /// Cleans the locations.
    /// </summary>
    /// <returns>Number of location rows removed.</returns>
    private int CleanLocations()
    {
        int removed = 0;

        var locations = database.Locations;
        if (locations.Count != 0)
        {
            var locationIds = GetLocationIdsInUse();

            foreach (var location in Enumerable.Reverse(locations))
            {
                if (!locationIds.Contains(location.LocationId))
                {
                    Log.Logger.Debug($"Removing redundant location id: {location.LocationId}");
                    locations.Remove(location);
                    ++removed;
                }
            }
        }

        return removed;
    }

    /// <summary>
    /// Cleans the playlists
    /// </summary>
    /// <returns>Number of playlist item rows removed.</returns>
    private int CleanPlaylistItems()
    {
        var removed = 0;
        var playlistItems = database.PlaylistItems;
        if (playlistItems.Count != 0)
        {
            var playlistItemIds = GetPlaylistItemIdsInUse();

            foreach (var playlistItem in Enumerable.Reverse(playlistItems))
            {
                if (!playlistItemIds.Contains(playlistItem.PlaylistItemId))
                {
                    Log.Logger.Debug($"Removing redundant playlist item id: {playlistItem.PlaylistItemId}");
                    playlistItems.Remove(playlistItem);
                    ++removed;
                }
            }
        }

        return removed;
    }

    /// <summary>
    /// Cleans the playlist independent media maps
    /// </summary>
    /// <returns>Number of independent media maps removed</returns>
    private int CleanPlaylistItemIndependentMediaMaps()
    {
        int removed = 0;

        var maps = database.PlaylistItemIndependentMediaMaps;
        if (maps.Count != 0)
        {
            var playlistItemIds = GetPlaylistItemIdsInUse();

            foreach (var map in Enumerable.Reverse(maps))
            {
                if (!playlistItemIds.Contains(map.PlaylistItemId))
                {
                    Log.Logger.Debug($"Removing redundant independent media map: {map.IndependentMediaId}");
                    maps.Remove(map);
                    ++removed;
                }
            }
        }

        return removed;
    }

    /// <summary>
    /// Cleans the independent media list.
    /// </summary>
    /// <returns>Number of removed media</returns>
    private int CleanIndependentMedias()
    {
        int removed = 0;

        var medias = database.IndependentMedias;
        if (medias.Count > 0)
        {
            var mediaIds = GetIndependentMediaIdsInUse();

            foreach (var media in Enumerable.Reverse(medias))
            {
                if (!mediaIds.Contains(media.IndependentMediaId))
                {
                    Log.Logger.Debug($"Removing redundant independent media: {media.IndependentMediaId}");
                    medias.Remove(media);
                    ++removed;
                }
            }
        }

        return removed;
    }

    /// <summary>
    /// Cleans the playlist markers
    /// </summary>
    /// <returns>Number of playlist markers removed</returns>
    private int CleanPlaylistItemMarkers()
    {
        int removed = 0;

        var markers = database.PlaylistItemMarkers;
        if (markers.Count > 0)
        {
            var playlistItemIds = GetPlaylistItemIdsInUse();

            foreach (var marker in Enumerable.Reverse(markers))
            {
                if (!playlistItemIds.Contains(marker.PlaylistItemId))
                {
                    Log.Logger.Debug($"Removing redundant playlist item marker: {marker.PlaylistItemMarkerId}");
                    markers.Remove(marker);
                    ++removed;
                }
            }
        }

        return removed;
    }

    /// <summary>
    /// Clean playlist item marker bible verse maps
    /// </summary>
    /// <returns>Number of removed playlist item marker bible verses</returns>
    private int CleanPlaylistItemMarkerBibleVerseMaps()
    {
        int removed = 0;
        var maps = database.PlaylistItemMarkerBibleVerseMaps;
        if (maps.Count != 0)
        {
            var markerIds = GetValidPlaylistItemMarkerIds();
            foreach (var map in Enumerable.Reverse(maps))
            {
                if (!markerIds.Contains(map.PlaylistItemMarkerId))
                {
                    Log.Logger.Debug($"Removing redundant playlist item marker bible verse map: {map.PlaylistItemMarkerId}");
                    maps.Remove(map);
                    ++removed;
                }
            }
        }
        return removed;
    }

    /// <summary>
    /// Clean playlist item marker paragraph maps
    /// </summary>
    /// <returns>Number of removed playlist item marker paragraphs</returns>
    private int CleanPlaylistItemMarkerParagraphMaps()
    {
        int removed = 0;
        var maps = database.PlaylistItemMarkerParagraphMaps;
        if (maps.Count != 0)
        {
            var markerIds = GetValidPlaylistItemMarkerIds();
            foreach (var map in Enumerable.Reverse(maps))
            {
                if (!markerIds.Contains(map.PlaylistItemMarkerId))
                {
                    Log.Logger.Debug($"Removing redundant playlist item marker paragraph map: {map.PlaylistItemMarkerId}");
                    maps.Remove(map);
                    ++removed;
                }
            }
        }
        return removed;
    }

    /// <summary>
    /// Clean playlist item location maps
    /// </summary>
    /// <returns>Number of removed item location maps</returns>
    private int CleanPlaylistItemLocationMaps()
    {
        int removed = 0;

        var maps = database.PlaylistItemLocationMaps;
        if (maps.Count != 0)
        {
            var playlistItemIds = GetPlaylistItemIdsInUse();

            foreach (var map in Enumerable.Reverse(maps))
            {
                if (!playlistItemIds.Contains(map.PlaylistItemId))
                {
                    Log.Logger.Debug($"Removing redundant playlist item location map: {map.LocationId}");
                    maps.Remove(map);
                    ++removed;
                }
            }
        }

        return removed;
    }

    private HashSet<int> GetIndependentMediaIdsInUse()
    {
        var result = new HashSet<int>();

        foreach (var map in database.PlaylistItemIndependentMediaMaps)
        {
            result.Add(map.IndependentMediaId);
        }

        foreach (var playListItem in database.PlaylistItems.Where(p => !string.IsNullOrEmpty(p.ThumbnailFilePath)))
        {
            var media = database.FindIndependentMedia(playListItem.ThumbnailFilePath!);
            if (media != null)
            {
                result.Add(media.IndependentMediaId);
            }
        }

        Log.Logger.Debug($"Found {result.Count} independent media Ids in use");

        return result;
    }

    private HashSet<int> GetPlaylistItemIdsInUse()
    {
        var result = new HashSet<int>();

        foreach (var bookmark in database.PlaylistItemIndependentMediaMaps)
        {
            result.Add(bookmark.PlaylistItemId);
        }

        foreach (var note in database.PlaylistItemLocationMaps)
        {
            result.Add(note.PlaylistItemId);
        }

        foreach (var userMark in database.PlaylistItemMarkers)
        {
            result.Add(userMark.PlaylistItemId);
        }

        foreach (var tagMap in database.TagMaps)
        {
            if (tagMap.PlaylistItemId != null)
            {
                result.Add(tagMap.PlaylistItemId.Value);
            }
        }

        Log.Logger.Debug($"Found {result.Count} playlist item Ids in use");

        return result;
    }

    private HashSet<int> GetValidPlaylistItemMarkerIds()
    {
        var result = new HashSet<int>();
        foreach (var marker in database.PlaylistItemMarkers)
        {
            result.Add(marker.PlaylistItemMarkerId);
        }
        return result;
    }

    private int CleanTagMaps()
    {
        var removed = 0;

        var tagMaps = database.TagMaps;
        if (tagMaps.Count != 0)
        {
            var tagIds = GetValidTagIds();
            var noteIds = GetValidNoteIds();
            var locationIds = GetValidLocationIds();

            foreach (var tag in Enumerable.Reverse(tagMaps))
            {
                if (!tagIds.Contains(tag.TagId) ||
                    (tag.NoteId != null && !noteIds.Contains(tag.NoteId.Value)) ||
                    (tag.LocationId != null && !locationIds.Contains(tag.LocationId.Value)))
                {
                    Log.Logger.Debug($"Removing redundant tag map entry: {tag.TagMapId}");
                    tagMaps.Remove(tag);
                    ++removed;
                }
            }
        }

        return removed;
    }

    /// <summary>
    /// Cleans the block ranges.
    /// </summary>
    /// <returns>Number of ranges removed.</returns>
    private int CleanBlockRanges()
    {
        int removed = 0;

        var ranges = database.BlockRanges;
        if (ranges.Count != 0)
        {
            var userMarkIdsFound = new HashSet<int>();
            var userMarkIds = GetValidUserMarkIds();

            foreach (var range in Enumerable.Reverse(ranges))
            {
                if (!userMarkIds.Contains(range.UserMarkId))
                {
                    Log.Logger.Debug($"Removing redundant range: {range.BlockRangeId}");
                    ranges.Remove(range);
                    ++removed;
                }
                else
                {
                    if (userMarkIdsFound.Contains(range.UserMarkId))
                    {
                        // don't know how to handle this situation - we are expecting 
                        // a unique constraint on the UserMarkId column but have found 
                        // occasional duplication!
                        Log.Logger.Debug($"Removing redundant range (duplicate UserMarkId): {range.BlockRangeId}");
                        ranges.Remove(range);
                        ++removed;
                    }
                    else
                    {
                        userMarkIdsFound.Add(range.UserMarkId);
                    }
                }
            }
        }

        return removed;
    }
}