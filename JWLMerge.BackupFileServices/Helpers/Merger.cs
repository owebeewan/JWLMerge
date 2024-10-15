using System;
using System.Collections.Generic;
using System.Linq;
using JWLMerge.BackupFileServices.Events;
using JWLMerge.BackupFileServices.Models.DatabaseModels;
using Serilog;

namespace JWLMerge.BackupFileServices.Helpers;

/// <summary>
/// Merges the SQLite databases.
/// </summary>
internal sealed class Merger
{
    private readonly IdTranslator _translatedLocationIds = new();
    private readonly IdTranslator _translatedTagIds = new();
    private readonly IdTranslator _translatedUserMarkIds = new();
    private readonly IdTranslator _translatedNoteIds = new();
    private readonly IdTranslator _translatedIndependentMediaIds = new();
    private readonly IdTranslator _translatedPlaylistItemIds = new();

    private int _maxLocationId;
    private int _maxUserMarkId;
    private int _maxNoteId;
    private int _maxTagId;
    private int _maxTagMapId;
    private int _maxBlockRangeId;
    private int _maxBookmarkId;
    private int _maxIndependentMediaId;
    private int _maxPlaylistItemId;
    private int _maxPlaylistItemMarkerId;

    public event EventHandler<ProgressEventArgs>? ProgressEvent;

    /// <summary>
    /// Merges the specified databases.
    /// </summary>
    /// <param name="databasesToMerge">The databases to merge.</param>
    /// <returns><see cref="Database"/></returns>
    public Database Merge(IEnumerable<Database> databasesToMerge)
    {
        var result = new Database();
        result.InitBlank();

        ClearMaxIds();

        var databaseIndex = 1;
        foreach (var database in databasesToMerge)
        {
            ProgressMessage($"MERGING DATABASE {databaseIndex++}:");
            Merge(database, result);
        }

        return result;
    }

    private void ClearMaxIds()
    {
        _maxLocationId = 0;
        _maxUserMarkId = 0;
        _maxNoteId = 0;
        _maxTagId = 0;
        _maxTagMapId = 0;
        _maxBlockRangeId = 0;
        _maxBookmarkId = 0;
        _maxIndependentMediaId = 0;
        _maxPlaylistItemId = 0;
        _maxPlaylistItemMarkerId = 0;
    }

    private void Merge(Database source, Database destination)
    {
        ClearTranslators();

        source.FixupAnomalies();

        MergeUserMarks(source, destination);
        MergeNotes(source, destination);
        MergeInputFields(source, destination);
        MergeIndependentMedias(source, destination);
        MergePlaylistItems(source, destination);
        MergePlaylistItemIndependentMediaMaps(source, destination);
        MergePlaylistItemLocationMap(source, destination);
        MergePlaylistItemMarker(source, destination);
        MergeTags(source, destination);
        MergeTagMap(source, destination);
        MergeBlockRanges(source, destination);
        MergeBookmarks(source, destination);

        ProgressMessage(" Checking validity");
        destination.CheckValidity();
    }

    private void ClearTranslators()
    {
        _translatedLocationIds.Clear();
        _translatedTagIds.Clear();
        _translatedUserMarkIds.Clear();
        _translatedNoteIds.Clear();
        _translatedIndependentMediaIds.Clear();
        _translatedPlaylistItemIds.Clear();
    }

    private void MergeBookmarks(Database source, Database destination)
    {
        ProgressMessage(" Bookmarks");

        foreach (var bookmark in source.Bookmarks)
        {
            var location1 = source.FindLocation(bookmark.LocationId);
            var location2 = source.FindLocation(bookmark.PublicationLocationId);

            if (location1 != null && location2 != null)
            {
                InsertLocation(location1, destination);
                InsertLocation(location2, destination);

                var locationId = _translatedLocationIds.GetTranslatedId(bookmark.LocationId);
                var publicationLocationId = _translatedLocationIds.GetTranslatedId(bookmark.PublicationLocationId);
                if (locationId > 0 && publicationLocationId > 0)
                {
                    var existingBookmark = destination.FindBookmark(locationId, publicationLocationId);
                    if (existingBookmark == null)
                    {
                        InsertBookmark(bookmark, destination);
                    }
                }
            }
            else
            {
                if (location1 == null)
                {
                    Log.Logger.Error($"Could not find location for bookmark {bookmark.BookmarkId}");
                }

                if (location2 == null)
                {
                    Log.Logger.Error($"Could not find publication location for bookmark {bookmark.BookmarkId}");
                }
            }
        }
    }

    private void InsertBookmark(Bookmark bookmark, Database destination)
    {
        Bookmark newBookmark = bookmark.Clone();
        newBookmark.BookmarkId = ++_maxBookmarkId;

        newBookmark.LocationId = _translatedLocationIds.GetTranslatedId(bookmark.LocationId);
        newBookmark.PublicationLocationId = _translatedLocationIds.GetTranslatedId(bookmark.PublicationLocationId);
        newBookmark.Slot = destination.GetNextBookmarkSlot(newBookmark.PublicationLocationId);

        destination.Bookmarks.Add(newBookmark);
    }

    private void MergeBlockRanges(Database source, Database destination)
    {
        ProgressMessage(" Block ranges");

        foreach (var range in source.BlockRanges)
        {
            var userMarkId = _translatedUserMarkIds.GetTranslatedId(range.UserMarkId);
            if (userMarkId != 0)
            {
                var existingRanges = destination.FindBlockRanges(userMarkId);

                if (existingRanges == null ||
                    !existingRanges.Any(x => OverlappingBlockRanges(x, range)))
                {
                    InsertBlockRange(range, destination);
                }
            }
        }
    }

    private static bool OverlappingBlockRanges(BlockRange blockRange1, BlockRange blockRange2)
    {
        if (blockRange1.StartToken == blockRange2.StartToken &&
            blockRange1.EndToken == blockRange2.EndToken)
        {
            return true;
        }

        if (blockRange1.StartToken == null ||
            blockRange1.EndToken == null ||
            blockRange2.StartToken == null ||
            blockRange2.EndToken == null)
        {
            return false;
        }

        return blockRange2.StartToken < blockRange1.EndToken
               && blockRange2.EndToken > blockRange1.StartToken;
    }

    private void MergeTagMap(Database source, Database destination)
    {
        ProgressMessage(" Tag maps");

        foreach (var sourceTagMap in source.TagMaps)
        {
            var tagId = _translatedTagIds.GetTranslatedId(sourceTagMap.TagId);
            var id = 0;
            TagMap? existingTagMap = null;

            if (sourceTagMap.LocationId != null)
            {
                // a tag on a location.
                id = _translatedLocationIds.GetTranslatedId(sourceTagMap.LocationId.Value);
                if (id == 0)
                {
                    // must add location...
                    var location = source.FindLocation(sourceTagMap.LocationId.Value);
                    if (location != null)
                    {
                        InsertLocation(location, destination);
                        id = _translatedLocationIds.GetTranslatedId(sourceTagMap.LocationId.Value);
                    }
                }

                existingTagMap = destination.FindTagMapForLocation(tagId, id);
            }
            else if (sourceTagMap.NoteId != null)
            {
                // a tag on a Note
                id = _translatedNoteIds.GetTranslatedId(sourceTagMap.NoteId.Value);
                existingTagMap = destination.FindTagMapForNote(tagId, id);
            }
            else if (sourceTagMap.PlaylistItemId != null)
            {
                // a tag on a playlist
                id = _translatedPlaylistItemIds.GetTranslatedId(sourceTagMap.PlaylistItemId.Value);
                existingTagMap = destination.FindTagMapForPlaylistItem(tagId, id);
            }

            if (id != 0 && existingTagMap == null)
            {
                InsertTagMap(sourceTagMap, destination);
            }
        }

        NormaliseTagMapPositions(destination.TagMaps);
    }

    private static void NormaliseTagMapPositions(List<TagMap> entries)
    {
        // there is unique constraint on TagId, Position
        var tmpStorage = entries.GroupBy(x => x.TagId).ToDictionary(x => x.Key);

        foreach (var item in tmpStorage)
        {
            var pos = 0;
            foreach (var entry in item.Value.OrderBy(x => x.Position))
            {
                entry.Position = pos++;
            }
        }
    }

    private void MergeTags(Database source, Database destination)
    {
        ProgressMessage(" Tags");

        foreach (var tag in source.Tags)
        {
            var existingTag = destination.FindTag(tag.Type, tag.Name);
            if (existingTag != null)
            {
                _translatedTagIds.Add(tag.TagId, existingTag.TagId);
            }
            else
            {
                InsertTag(tag, destination);
            }
        }
    }

    private void MergeUserMarks(Database source, Database destination)
    {
        ProgressMessage(" User marks");

        foreach (var sourceUserMark in source.UserMarks)
        {
            var existingUserMark = destination.FindUserMark(sourceUserMark.UserMarkGuid);
            if (existingUserMark != null)
            {
                // user mark already exists in destination...
                _translatedUserMarkIds.Add(sourceUserMark.UserMarkId, existingUserMark.UserMarkId);
            }
            else
            {
                var referencedLocation = sourceUserMark.LocationId;
                var location = source.FindLocation(referencedLocation);
                if (location != null)
                {
                    InsertLocation(location, destination);
                }

                InsertUserMark(sourceUserMark, destination);
            }
        }
    }

    private void InsertLocation(Location location, Database destination)
    {
        if (_translatedLocationIds.GetTranslatedId(location.LocationId) == 0)
        {
            var found = destination.FindLocationByValues(location);
            if (found != null)
            {
                _translatedLocationIds.Add(location.LocationId, found.LocationId);
            }
            else
            {
                Location newLocation = location.Clone();
                newLocation.LocationId = ++_maxLocationId;
                destination.Locations.Add(newLocation);

                _translatedLocationIds.Add(location.LocationId, newLocation.LocationId);
            }
        }
    }

    private static void InsertInputField(InputField inputField, int locationId, Database destination)
    {
        var inputFldClone = inputField.Clone();
        inputFldClone.LocationId = locationId;
        destination.InputFields.Add(inputFldClone);
    }

    private void InsertUserMark(UserMark userMark, Database destination)
    {
        UserMark newUserMark = userMark.Clone();
        newUserMark.UserMarkId = ++_maxUserMarkId;
        newUserMark.LocationId = _translatedLocationIds.GetTranslatedId(userMark.LocationId);
        destination.UserMarks.Add(newUserMark);

        _translatedUserMarkIds.Add(userMark.UserMarkId, newUserMark.UserMarkId);
    }

    private void InsertTag(Tag tag, Database destination)
    {
        Tag newTag = tag.Clone();
        newTag.TagId = ++_maxTagId;
        destination.Tags.Add(newTag);

        _translatedTagIds.Add(tag.TagId, newTag.TagId);
    }

    private void InsertTagMap(TagMap tagMap, Database destination)
    {
        TagMap newTagMap = tagMap.Clone();
        newTagMap.TagMapId = ++_maxTagMapId;
        newTagMap.TagId = _translatedTagIds.GetTranslatedId(tagMap.TagId);

        newTagMap.LocationId = null;
        newTagMap.PlaylistItemId = null;
        newTagMap.NoteId = null;

        if (tagMap.LocationId != null)
        {
            newTagMap.LocationId = _translatedLocationIds.GetTranslatedId(tagMap.LocationId.Value);
        }
        else if (tagMap.NoteId != null)
        {
            newTagMap.NoteId = _translatedNoteIds.GetTranslatedId(tagMap.NoteId.Value);
        }
        else if (tagMap.PlaylistItemId != null)
        {
            newTagMap.PlaylistItemId = _translatedPlaylistItemIds.GetTranslatedId(tagMap.PlaylistItemId.Value);
        }

        if (newTagMap.LocationId != null || newTagMap.NoteId != null || newTagMap.PlaylistItemId != null)
        {
            destination.TagMaps.Add(newTagMap);
        }
    }

    private void InsertNote(Note note, Database destination)
    {
        Note newNote = note.Clone();
        newNote.NoteId = ++_maxNoteId;

        if (note.UserMarkId != null)
        {
            newNote.UserMarkId = _translatedUserMarkIds.GetTranslatedId(note.UserMarkId.Value);
        }

        if (note.LocationId != null)
        {
            newNote.LocationId = _translatedLocationIds.GetTranslatedId(note.LocationId.Value);
        }

        destination.Notes.Add(newNote);
        _translatedNoteIds.Add(note.NoteId, newNote.NoteId);
    }

    private void InsertBlockRange(BlockRange range, Database destination)
    {
        BlockRange newRange = range.Clone();
        newRange.BlockRangeId = ++_maxBlockRangeId;

        newRange.UserMarkId = _translatedUserMarkIds.GetTranslatedId(range.UserMarkId);
        destination.BlockRanges.Add(newRange);
    }

    private void InsertIndependentMedia(IndependentMedia independentMedia, Database destination)
    {
        var newIndependentMedia = independentMedia.Clone();
        newIndependentMedia.IndependentMediaId = ++_maxIndependentMediaId;

        destination.IndependentMedias.Add(newIndependentMedia);
        _translatedIndependentMediaIds.Add(independentMedia.IndependentMediaId, newIndependentMedia.IndependentMediaId);
    }

    private void InsertPlaylistItem(PlaylistItem playlistItem, Database destination)
    {
        var newPlaylistItem = playlistItem.Clone();
        newPlaylistItem.PlaylistItemId = ++_maxPlaylistItemId;

        destination.PlaylistItems.Add(newPlaylistItem);
        _translatedPlaylistItemIds.Add(playlistItem.PlaylistItemId, newPlaylistItem.PlaylistItemId);
    }

    private void InsertPlaylistItemIndependentMediaMap(PlaylistItemIndependentMediaMap mediaMap, Database destination)
    {
        var playlistItemId = _translatedPlaylistItemIds.GetTranslatedId(mediaMap.PlaylistItemId);
        var independentMediaId = _translatedIndependentMediaIds.GetTranslatedId(mediaMap.IndependentMediaId);

        if (playlistItemId > 0 && independentMediaId > 0)
        {
            var newMediaMap = mediaMap.Clone();

            newMediaMap.PlaylistItemId = playlistItemId;
            newMediaMap.IndependentMediaId = independentMediaId;
            destination.PlaylistItemIndependentMediaMaps.Add(newMediaMap);
        }
    }

    private void InsertPlaylistItemLocationMap(PlaylistItemLocationMap locationMap, Database destination)
    {
        var playlistItemId = _translatedPlaylistItemIds.GetTranslatedId(locationMap.PlaylistItemId);
        var locationId = _translatedLocationIds.GetTranslatedId(locationMap.LocationId);
        if (playlistItemId > 0 && locationId > 0)
        {
            var newLocationMap = locationMap.Clone();

            newLocationMap.PlaylistItemId = playlistItemId;
            newLocationMap.LocationId = locationId;
            destination.PlaylistItemLocationMaps.Add(newLocationMap);
        }
    }

    private void InsertPlaylistItemMarker(PlaylistItemMarker marker, Database destination)
    {
        var playlistItemId = _translatedPlaylistItemIds.GetTranslatedId(marker.PlaylistItemId);
        if (playlistItemId > 0)
        {
            var newMarker = marker.Clone();

            newMarker.PlaylistItemMarkerId = ++_maxPlaylistItemMarkerId;
            newMarker.PlaylistItemId = playlistItemId;
            destination.PlaylistItemMarkers.Add(newMarker);
        }
    }

    private void MergeInputFields(Database source, Database destination)
    {
        ProgressMessage(" Input Fields");

        foreach (var inputField in source.InputFields)
        {
            var location = source.FindLocation(inputField.LocationId);
            if (location != null)
            {
                InsertLocation(location, destination);

                var locationId = _translatedLocationIds.GetTranslatedId(inputField.LocationId);
                if (locationId > 0)
                {
                    var existingInputField = destination.FindInputField(locationId, inputField.TextTag);
                    if (existingInputField == null)
                    {
                        InsertInputField(inputField, locationId, destination);
                    }
                }
            }
            else
            {
                Log.Logger.Error($"Could not find location for inputField {inputField.LocationId}, {inputField.TextTag}");
            }
        }
    }

    private void MergeNotes(Database source, Database destination)
    {
        ProgressMessage(" Notes");

        foreach (var note in source.Notes)
        {
            var existingNote = destination.FindNote(note.Guid);
            if (existingNote != null)
            {
                // note already exists in destination...
                if (existingNote.GetLastModifiedDateTime() < note.GetLastModifiedDateTime())
                {
                    // ...but it's older
                    UpdateNote(note, existingNote);
                }

                _translatedNoteIds.Add(note.NoteId, existingNote.NoteId);
            }
            else
            {
                // a new note...
                if (note.LocationId != null && _translatedLocationIds.GetTranslatedId(note.LocationId.Value) == 0)
                {
                    var referencedLocation = note.LocationId.Value;
                    var location = source.FindLocation(referencedLocation);
                    if (location != null)
                    {
                        InsertLocation(location, destination);
                    }
                }

                InsertNote(note, destination);
            }
        }
    }

    private void MergeIndependentMedias(Database source, Database destination)
    {
        ProgressMessage(" Independent Media");

        foreach (var independentMedia in source.IndependentMedias)
        {
            var existingIndependentMedia = destination.FindIndependentMedia(independentMedia.FilePath.Trim());
            if (existingIndependentMedia == null)
            {
                InsertIndependentMedia(independentMedia, destination);
            }
        }
    }

    private void MergePlaylistItems(Database source, Database destination)
    {
        ProgressMessage(" Playlists");

        foreach (var playlistItem in source.PlaylistItems)
        {
            var existingPlaylistItem = destination.FindPlaylistItemByValues(playlistItem);
            if (existingPlaylistItem == null)
            {
                // A new playlist item...
                InsertPlaylistItem(playlistItem, destination);
            }
        }
    }

    private void MergePlaylistItemIndependentMediaMaps(Database source, Database destination)
    {
        ProgressMessage(" Playlist Item Independent Media Maps");

        foreach (var mediaMap in source.PlaylistItemIndependentMediaMaps)
        {
            var existingMediaMap = destination.FindPlaylistItemIndependentMediaMapByValues(mediaMap);
            if (existingMediaMap == null)
            {
                InsertPlaylistItemIndependentMediaMap(mediaMap, destination);
            }
        }
    }

    private void MergePlaylistItemLocationMap(Database source, Database destination)
    {
        ProgressMessage(" Playlist Item Location Maps");

        foreach (var playlistItemLocationMap in source.PlaylistItemLocationMaps)
        {
            var existingPlaylistItemLocationMap = destination.FindPlaylistItemLocationMapByValues(playlistItemLocationMap);
            if (existingPlaylistItemLocationMap == null)
            {
                InsertPlaylistItemLocationMap(playlistItemLocationMap, destination);
            }
        }
    }

    private void MergePlaylistItemMarker(Database source, Database destination)
    {
        ProgressMessage(" Playlist Item Markers");

        foreach (var marker in source.PlaylistItemMarkers)
        {
            var existingMarker = destination.FindPlaylistItemMarker(marker.PlaylistItemMarkerId);
            if (existingMarker != null)
            {
                continue;
            }
            else
            {
                InsertPlaylistItemMarker(marker, destination);
            }
        }
    }

    private static void UpdateNote(Note source, Note destination)
    {
        destination.Title = source.Title;
        destination.Content = source.Content;
        destination.LastModified = source.LastModified;
    }

    private void OnProgressEvent(string message)
    {
        ProgressEvent?.Invoke(this, new ProgressEventArgs(message));
    }

    private void ProgressMessage(string logMessage)
    {
        Log.Logger.Information(logMessage);
        OnProgressEvent(logMessage);
    }
}