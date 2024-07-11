using System.Collections.Generic;
using System.Linq;
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
               CleanLocations();
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