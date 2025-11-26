using JWLMerge.BackupFileServices.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using Serilog;

namespace JWLMerge.BackupFileServices.Models.DatabaseModels;

public class Database
{
    private readonly Dictionary<int, int> _bookmarkSlots = [];

    private Lazy<Dictionary<Guid, Note>> _notesGuidIndex = null!;
    private Lazy<Dictionary<int, Note>> _notesIdIndex = null!;
    private Lazy<Dictionary<int, List<InputField>>> _inputFieldsIndex = null!;
    private Lazy<Dictionary<BibleBookChapterAndVerse, List<Note>>> _notesVerseIndex = null!;
    private Lazy<Dictionary<Guid, UserMark>> _userMarksGuidIndex = null!;
    private Lazy<Dictionary<int, UserMark>> _userMarksIdIndex = null!;
    private Lazy<Dictionary<int, List<UserMark>>> _userMarksLocationIdIndex = null!;
    private Lazy<Dictionary<int, Location>> _locationsIdIndex = null!;
    private Lazy<Dictionary<string, Location>> _locationsValueIndex = null!;
    private Lazy<Dictionary<string, Location>> _locationsBibleChapterIndex = null!;
    private Lazy<Dictionary<TagTypeAndName, Tag>> _tagsNameIndex = null!;
    private Lazy<Dictionary<int, Tag>> _tagsIdIndex = null!;
    private Lazy<Dictionary<string, TagMap>> _tagMapNoteIndex = null!;
    private Lazy<Dictionary<string, TagMap>> _tagMapLocationIndex = null!;
    private Lazy<Dictionary<string, TagMap>> _tagMapPlaylistItemIndex = null!;
    private Lazy<Dictionary<int, List<BlockRange>>> _blockRangesUserMarkIdIndex = null!;
    private Lazy<Dictionary<string, Bookmark>> _bookmarksIndex = null!;
    private Lazy<Dictionary<string, IndependentMedia>> _independentMediasFilePathIndex = null!;
    private Lazy<Dictionary<string, IndependentMedia>> _independentMediasHashIndex = null!;
    private Lazy<Dictionary<int, IndependentMedia>> _independentMediasIdIndex = null!;
    private Lazy<Dictionary<int, PlaylistItem>> _playlistItemsIdIndex = null!;
    private Lazy<Dictionary<string, PlaylistItem>> _playlistItemsValueIndex = null!;
    private Lazy<Dictionary<string, PlaylistItemIndependentMediaMap>> _playlistItemIndependentMediaMapsValueIndex = null!;
    private Lazy<Dictionary<int, PlaylistItemMarker>> _playlistItemMarkersIdIndex = null!;
    private Lazy<Dictionary<int, PlaylistItemMarkerBibleVerseMap>> _playlistItemMarkersBibleVerseMapIndex = null!;
    private Lazy<Dictionary<int, PlaylistItemMarkerParagraphMap>> _playlistItemMarkersParagraphMapIndex = null!;
    private Lazy<Dictionary<string, PlaylistItemLocationMap>> _playlistItemLocationMapsValueIndex = null!;

    public Database()
    {
        ReinitializeIndexes();
    }

    public LastModified LastModified { get; } = new();

    public List<Location> Locations { get; } = [];

    public List<Note> Notes { get; } = [];

    public List<InputField> InputFields { get; } = [];

    public List<Tag> Tags { get; } = [];

    public List<TagMap> TagMaps { get; } = [];

    public List<BlockRange> BlockRanges { get; } = [];

    public List<Bookmark> Bookmarks { get; } = [];

    public List<UserMark> UserMarks { get; } = [];

    public List<IndependentMedia> IndependentMedias { get; } = [];

    public List<PlaylistItem> PlaylistItems { get; } = [];

    public List<PlaylistItemIndependentMediaMap> PlaylistItemIndependentMediaMaps { get; } = [];

    public List<PlaylistItemLocationMap> PlaylistItemLocationMaps { get; } = [];

    public List<PlaylistItemMarker> PlaylistItemMarkers { get; } = [];

    public List<PlaylistItemMarkerBibleVerseMap> PlaylistItemMarkerBibleVerseMaps { get; } = [];

    public List<PlaylistItemMarkerParagraphMap> PlaylistItemMarkerParagraphMaps { get; } = [];

    public static string GetDateTimeUtcAsDbString(DateTime dateTime)
    {
        return $"{dateTime:s}Z";
    }

    public void InitBlank()
    {
        LastModified.Reset();
        Locations.Clear();
        Notes.Clear();
        InputFields.Clear();
        Tags.Clear();
        TagMaps.Clear();
        BlockRanges.Clear();
        Bookmarks.Clear();
        UserMarks.Clear();
    }

    public void CheckValidity()
    {
        ReinitializeIndexes();
        DatabaseForeignKeyChecker.Execute(this);
    }

    public void FixupAnomalies()
    {
        var count = 0;

        count += FixupLocationValidity();
        count += FixupBlockRangeValidity();
        count += FixupBookmarkValidity();
        count += FixupNoteValidity();
        count += FixupTagMapValidity();
        count += FixupUserMarkValidity();

        if (count > 0)
        {
            ReinitializeIndexes();
        }
    }

    public void AddBibleNoteAndUpdateIndex(
        BibleBookChapterAndVerse verseRef,
        Note value,
        TagMap? tagMap)
    {
        ArgumentNullException.ThrowIfNull(value);

        Notes.Add(value);

        if (tagMap != null)
        {
            TagMaps.Add(tagMap);
        }

        if (_notesGuidIndex.IsValueCreated && Guid.TryParse(value.Guid, out var g))
        {
            _notesGuidIndex.Value.TryAdd(g, value);
        }

        if (_notesIdIndex.IsValueCreated)
        {
            _notesIdIndex.Value.Add(value.NoteId, value);
        }

        if (_notesVerseIndex.IsValueCreated)
        {
            if (!_notesVerseIndex.Value.TryGetValue(verseRef, out var notes))
            {
                notes = [];
                _notesVerseIndex.Value.Add(verseRef, notes);
            }

            notes.Add(value);
        }
    }

    public void AddBlockRangeAndUpdateIndex(BlockRange value)
    {
        ArgumentNullException.ThrowIfNull(value);

        BlockRanges.Add(value);

        if (_blockRangesUserMarkIdIndex.IsValueCreated)
        {
            if (!_blockRangesUserMarkIdIndex.Value.TryGetValue(value.UserMarkId, out var blockRangeList))
            {
                blockRangeList = [];
                _blockRangesUserMarkIdIndex.Value.Add(value.UserMarkId, blockRangeList);
            }

            blockRangeList.Add(value);
        }
    }

    public void AddUserMarkAndUpdateIndex(UserMark value)
    {
        ArgumentNullException.ThrowIfNull(value);

        UserMarks.Add(value);

        if (_userMarksGuidIndex.IsValueCreated && Guid.TryParse(value.UserMarkGuid, out var g))
        {
            _userMarksGuidIndex.Value.TryAdd(g, value);
        }

        if (_userMarksIdIndex.IsValueCreated)
        {
            _userMarksIdIndex.Value.Add(value.UserMarkId, value);
        }

        if (_userMarksLocationIdIndex.IsValueCreated)
        {
            if (!_userMarksLocationIdIndex.Value.TryGetValue(value.LocationId, out var marks))
            {
                marks = [];
                _userMarksLocationIdIndex.Value.Add(value.LocationId, marks);
            }

            marks.Add(value);
        }
    }

    public void AddLocationAndUpdateIndex(Location value)
    {
        ArgumentNullException.ThrowIfNull(value);

        Locations.Add(value);

        if (_locationsIdIndex.IsValueCreated)
        {
            _locationsIdIndex.Value.Add(value.LocationId, value);
        }

        if (_locationsValueIndex.IsValueCreated)
        {
            var key = GetLocationByValueKey(value);
            _locationsValueIndex.Value.TryAdd(key, value);
        }

        if (_locationsBibleChapterIndex.IsValueCreated &&
            value.BookNumber != null &&
            value.ChapterNumber != null)
        {
            var key = GetLocationByBibleChapterKey(
                value.BookNumber.Value,
                value.ChapterNumber.Value,
                value.KeySymbol);

            _locationsBibleChapterIndex.Value.TryAdd(key, value);
        }
    }

    public void AddPlaylistItemIndependentMediaMapAndUpdateIndex(PlaylistItemIndependentMediaMap value)
    {
        ArgumentNullException.ThrowIfNull(value);

        PlaylistItemIndependentMediaMaps.Add(value);

        if (_playlistItemIndependentMediaMapsValueIndex.IsValueCreated)
        {
            var key = GetPlaylistItemIndependentMediaMapKey(value);
            _playlistItemIndependentMediaMapsValueIndex.Value.TryAdd(key, value);
        }
    }

    public void AddPlaylistItemLocationMapAndUpdateIndex(PlaylistItemLocationMap value)
    {
        ArgumentNullException.ThrowIfNull(value);

        PlaylistItemLocationMaps.Add(value);

        if (_playlistItemLocationMapsValueIndex.IsValueCreated)
        {
            var key = GetPlaylistItemLocationMapKey(value);
            _playlistItemLocationMapsValueIndex.Value.TryAdd(key, value);
        }
    }

    public Note? FindNote(string noteGuid)
    {
        if (!Guid.TryParse(noteGuid, out var g))
        {
            return null;
        }

        return _notesGuidIndex.Value.TryGetValue(g, out var note) ? note : null;
    }

    public Note? FindNote(int noteId) => _notesIdIndex.Value.TryGetValue(noteId, out var note) ? note : null;

    public IEnumerable<Note>? FindNotes(BibleBookChapterAndVerse verseRef) => _notesVerseIndex.Value.TryGetValue(verseRef, out var notes) ? notes : null;

    public UserMark? FindUserMark(string userMarkGuid)
    {
        if (!Guid.TryParse(userMarkGuid, out var g))
        {
            return null;
        }

        return _userMarksGuidIndex.Value.TryGetValue(g, out var userMark) ? userMark : null;
    }

    public UserMark? FindUserMark(int userMarkId) => _userMarksIdIndex.Value.TryGetValue(userMarkId, out var userMark) ? userMark : null;

    public IEnumerable<UserMark>? FindUserMarks(int locationId) => _userMarksLocationIdIndex.Value.TryGetValue(locationId, out var userMarks) ? userMarks : null;

    public Tag? FindTag(int tagType, string tagName) => _tagsNameIndex.Value.TryGetValue(new TagTypeAndName(tagType, tagName), out var tag) ? tag : null;

    public Tag? FindTag(int tagId) => _tagsIdIndex.Value.TryGetValue(tagId, out var tag) ? tag : null;

    public TagMap? FindTagMapForNote(int tagId, int noteId) => _tagMapNoteIndex.Value.TryGetValue(GetTagMapNoteKey(tagId, noteId), out var tag) ? tag : null;

    public TagMap? FindTagMapForLocation(int tagId, int locationId) => _tagMapLocationIndex.Value.TryGetValue(GetTagMapLocationKey(tagId, locationId), out var tag) ? tag : null;

    public TagMap? FindTagMapForPlaylistItem(int tagId, int playlistItemId) => _tagMapPlaylistItemIndex.Value.TryGetValue(GetTagMapPlaylistItemKey(tagId, playlistItemId), out var tag) ? tag : null;

    public Location? FindLocation(int locationId) => _locationsIdIndex.Value.TryGetValue(locationId, out var location) ? location : null;

    public IndependentMedia? FindIndependentMedia(string filePath) => _independentMediasFilePathIndex.Value.TryGetValue(filePath, out var media) ? media : null;

    public IndependentMedia? FindIndependentMedia(int mediaId) => _independentMediasIdIndex.Value.TryGetValue(mediaId, out var media) ? media : null;
   
    public IndependentMedia? FindIndependentMediaByHash(string hash) => _independentMediasHashIndex.Value.TryGetValue(hash, out var mediaFromHash) ? mediaFromHash : null;

    public PlaylistItem? FindPlaylistItem(int playlistId) => _playlistItemsIdIndex.Value.TryGetValue(playlistId, out var playlist) ? playlist : null;

    /// <summary>
    /// Finds by value using label and thumbnail file as keys.
    /// </summary>
    /// <param name="playlistValues">The playlist item to check for</param>
    /// <returns>The playlist item if one is found</returns>
    public PlaylistItem? FindPlaylistItemByValues(PlaylistItem playlistValues)
    {
        ArgumentNullException.ThrowIfNull(playlistValues);

        var key = GetPlaylistItemByValueKey(playlistValues);
        return _playlistItemsValueIndex.Value.TryGetValue(key, out var playlistItem) ? playlistItem : null;
    }

    public PlaylistItemLocationMap? FindPlaylistItemLocationMapByValues(PlaylistItemLocationMap locationMap)
    {
        ArgumentNullException.ThrowIfNull(locationMap);

        var key = GetPlaylistItemLocationMapKey(locationMap);
        return _playlistItemLocationMapsValueIndex.Value.TryGetValue(key, out var itemLocation) ? itemLocation : null;
    }

    public PlaylistItemIndependentMediaMap? FindPlaylistItemIndependentMediaMapByValues(PlaylistItemIndependentMediaMap independentMediaMap)
    {
        ArgumentNullException.ThrowIfNull(independentMediaMap);

        var key = GetPlaylistItemIndependentMediaMapKey(independentMediaMap);
        return _playlistItemIndependentMediaMapsValueIndex.Value.TryGetValue(key, out var map) ? map : null;
    }

    public PlaylistItemMarker? FindPlaylistItemMarker(int playlistItemMarkerId) => _playlistItemMarkersIdIndex.Value.TryGetValue(playlistItemMarkerId, out var marker) ? marker : null;

    public PlaylistItemMarkerBibleVerseMap? FindPlaylistItemMarkerBibleVerseMap(int playlistItemMarkerId) => _playlistItemMarkersBibleVerseMapIndex.Value.TryGetValue(playlistItemMarkerId, out var map) ? map : null;

    public PlaylistItemMarkerParagraphMap? FindPlaylistItemMarkerParagraphMap(int playlistItemMarkerId) => _playlistItemMarkersParagraphMapIndex.Value.TryGetValue(playlistItemMarkerId, out var map) ? map : null;

    public InputField? FindInputField(int locationId, string textTag)
    {
        if (!_inputFieldsIndex.Value.TryGetValue(locationId, out var list))
        {
            return null;
        }

        return list.SingleOrDefault(x => x.TextTag.Equals(textTag, StringComparison.OrdinalIgnoreCase));
    }

    public Location? FindLocationByValues(Location locationValues)
    {
        ArgumentNullException.ThrowIfNull(locationValues);

        var key = GetLocationByValueKey(locationValues);
        return _locationsValueIndex.Value.TryGetValue(key, out var location) ? location : null;
    }

    public Location? FindLocationByBibleChapter(string bibleKeySymbol, int bibleBookNumber, int bibleChapter)
    {
        var key = GetLocationByBibleChapterKey(bibleBookNumber, bibleChapter, bibleKeySymbol);
        return _locationsBibleChapterIndex.Value.TryGetValue(key, out var location) ? location : null;
    }

    public IReadOnlyCollection<BlockRange>? FindBlockRanges(int userMarkId)
    {
        return _blockRangesUserMarkIdIndex.Value.TryGetValue(userMarkId, out var ranges) ? ranges : null;
    }

    public Bookmark? FindBookmark(int locationId, int publicationLocationId)
    {
        string key = GetBookmarkKey(locationId, publicationLocationId);
        return _bookmarksIndex.Value.TryGetValue(key, out var bookmark) ? bookmark : null;
    }

    public int GetNextBookmarkSlot(int publicationLocationId)
    {
        // there are only 10 slots available for each publication...
        if (_bookmarkSlots.TryGetValue(publicationLocationId, out var slot))
        {
            ++slot;
        }

        _bookmarkSlots[publicationLocationId] = slot;
        return slot;
    }

    private Dictionary<int, List<InputField>> InputFieldsIndexValueFactory()
    {
        var result = new Dictionary<int, List<InputField>>();

        foreach (var fld in InputFields)
        {
            if (!result.TryGetValue(fld.LocationId, out var list))
            {
                list = [];
                result.Add(fld.LocationId, list);
            }

            list.Add(fld);
        }

        return result;
    }

    private Dictionary<BibleBookChapterAndVerse, List<Note>> NoteVerseIndexValueFactory()
    {
        Dictionary<BibleBookChapterAndVerse, List<Note>> result = [];

        foreach (var note in Notes)
        {
            if (note.BlockType == 2 && // A note on a Bible verse
                note.LocationId != null &&
                note.BlockIdentifier != null)
            {
                var location = FindLocation(note.LocationId.Value);
                if (location?.BookNumber != null && location.ChapterNumber != null)
                {
                    var verseRef = new BibleBookChapterAndVerse(
                        location.BookNumber.Value,
                        location.ChapterNumber.Value,
                        note.BlockIdentifier.Value);

                    if (!result.TryGetValue(verseRef, out var notesOnVerse))
                    {
                        notesOnVerse = [];
                        result.Add(verseRef, notesOnVerse);
                    }

                    notesOnVerse.Add(note);
                }
            }
        }

        return result;
    }

    private Dictionary<int, List<UserMark>> UserMarksLocationIdIndexValueFactory()
    {
        var result = new Dictionary<int, List<UserMark>>();

        foreach (var userMark in UserMarks)
        {
            if (!result.TryGetValue(userMark.LocationId, out var marks))
            {
                marks = [];
                result.Add(userMark.LocationId, marks);
            }

            marks.Add(userMark);
        }

        return result;
    }

    private Dictionary<string, PlaylistItem> PlaylistItemsValueIndexFactory()
    {
        var result = new Dictionary<string, PlaylistItem>();

        foreach (var playlist in PlaylistItems)
        {
            var key = GetPlaylistItemByValueKey(playlist);
            result.TryAdd(key, playlist);
        }

        return result;
    }

    private Dictionary<string, PlaylistItemLocationMap> PlaylistItemLocationMapsValueFactory()
    {
        var result = new Dictionary<string, PlaylistItemLocationMap>();

        foreach (var locationMap in PlaylistItemLocationMaps)
        {
            var key = GetPlaylistItemLocationMapKey(locationMap);
            result.TryAdd(key, locationMap);
        }

        return result;
    }

    private Dictionary<string, PlaylistItemIndependentMediaMap> PlaylistItemIndependentMediaMapsValueIndexFactory()
    {
        var result = new Dictionary<string, PlaylistItemIndependentMediaMap>();

        foreach (var map in PlaylistItemIndependentMediaMaps)
        {
            var key = GetPlaylistItemIndependentMediaMapKey(map);
            result.TryAdd(key, map);
        }

        return result;
    }

    private Dictionary<string, Location> LocationsByValueIndexValueFactory()
    {
        var result = new Dictionary<string, Location>();

        foreach (var location in Locations)
        {
            var key = GetLocationByValueKey(location);
            result.TryAdd(key, location);
        }

        return result;
    }

    private Dictionary<string, Location> LocationsByBibleChapterIndexValueFactory()
    {
        var result = new Dictionary<string, Location>();

        foreach (var location in Locations)
        {
            if (location.BookNumber != null && location.ChapterNumber != null)
            {
                var key = GetLocationByBibleChapterKey(
                    location.BookNumber.Value,
                    location.ChapterNumber.Value,
                    location.KeySymbol);

                result.TryAdd(key, location);
            }
        }

        return result;
    }

    private Dictionary<int, List<BlockRange>> BlockRangeIndexValueFactory()
    {
        var result = new Dictionary<int, List<BlockRange>>();

        foreach (var range in BlockRanges)
        {
            if (!result.TryGetValue(range.UserMarkId, out var blockRangeList))
            {
                blockRangeList = [];
                result.Add(range.UserMarkId, blockRangeList);
            }

            blockRangeList.Add(range);
        }

        return result;
    }

    private static string GetBookmarkKey(int locationId, int publicationLocationId) => $"{locationId}-{publicationLocationId}";

    private static string GetLocationByValueKey(Location location)
        => $"{location.KeySymbol}|{location.IssueTagNumber}|{location.MepsLanguage}|{location.Type}|{location.BookNumber ?? -1}|{location.ChapterNumber ?? -1}|{location.DocumentId ?? -1}|{location.Track ?? -1}";

    private static string GetLocationByBibleChapterKey(int bibleBookNumber, int chapterNumber, string? bibleKeySymbol) => $"{bibleBookNumber}-{chapterNumber}-{bibleKeySymbol ?? string.Empty}";

    private static string GetPlaylistItemLocationMapKey(PlaylistItemLocationMap locationMap) => $"locationid-{locationMap.LocationId}";

    private static string GetPlaylistItemByValueKey(PlaylistItem playlistItem) => $"{playlistItem.Label}|{playlistItem.ThumbnailFilePath}";

    private static string GetPlaylistItemIndependentMediaMapKey(PlaylistItemIndependentMediaMap media) => $"{media.PlaylistItemId}|{media.IndependentMediaId}";

    private Dictionary<string, Bookmark> BookmarkIndexValueFactory()
    {
        var result = new Dictionary<string, Bookmark>();

        foreach (var bookmark in Bookmarks)
        {
            var key = GetBookmarkKey(bookmark.LocationId, bookmark.PublicationLocationId);
            result.TryAdd(key, bookmark);
        }

        return result;
    }

    private static string GetTagMapNoteKey(int tagId, int noteId) => $"{tagId}-{noteId}";

    private static string GetTagMapLocationKey(int tagId, int locationId) => $"{tagId}-{locationId}";

    private static string GetTagMapPlaylistItemKey(int tagId, int playlistItemId) => $"{tagId}-{playlistItemId}";

    private Dictionary<string, TagMap> TagMapNoteIndexValueFactory()
    {
        var result = new Dictionary<string, TagMap>();

        foreach (var tagMap in TagMaps)
        {
            if (tagMap.NoteId != null)
            {
                var key = GetTagMapNoteKey(tagMap.TagId, tagMap.NoteId.Value);
                result.Add(key, tagMap);
            }
        }

        return result;
    }

    private Dictionary<string, TagMap> TagMapLocationIndexValueFactory()
    {
        var result = new Dictionary<string, TagMap>();

        foreach (var tagMap in TagMaps)
        {
            if (tagMap.LocationId != null)
            {
                var key = GetTagMapLocationKey(tagMap.TagId, tagMap.LocationId.Value);
                result.Add(key, tagMap);
            }
        }

        return result;
    }

    private Dictionary<string, TagMap> TagMapPlaylistItemIndexValueFactory()
    {
        var result = new Dictionary<string, TagMap>();

        foreach (var tagMap in TagMaps)
        {
            if (tagMap.PlaylistItemId != null)
            {
                var key = GetTagMapPlaylistItemKey(tagMap.TagId, tagMap.PlaylistItemId.Value);
                result.Add(key, tagMap);
            }
        }

        return result;
    }

    private void ReinitializeIndexes()
    {
        _notesGuidIndex = new Lazy<Dictionary<Guid, Note>>(() => Notes.ToDictionary(note => Guid.Parse(note.Guid)));
        _notesIdIndex = new Lazy<Dictionary<int, Note>>(() => Notes.ToDictionary(note => note.NoteId));
        _inputFieldsIndex = new Lazy<Dictionary<int, List<InputField>>>(InputFieldsIndexValueFactory);
        _notesVerseIndex = new Lazy<Dictionary<BibleBookChapterAndVerse, List<Note>>>(NoteVerseIndexValueFactory);
        _userMarksGuidIndex = new Lazy<Dictionary<Guid, UserMark>>(() => UserMarks.ToDictionary(userMark => Guid.Parse(userMark.UserMarkGuid)));
        _userMarksIdIndex = new Lazy<Dictionary<int, UserMark>>(() => UserMarks.ToDictionary(userMark => userMark.UserMarkId));
        _userMarksLocationIdIndex = new Lazy<Dictionary<int, List<UserMark>>>(UserMarksLocationIdIndexValueFactory);
        _locationsIdIndex = new Lazy<Dictionary<int, Location>>(() => Locations.ToDictionary(location => location.LocationId));
        _locationsValueIndex = new Lazy<Dictionary<string, Location>>(LocationsByValueIndexValueFactory);
        _locationsBibleChapterIndex = new Lazy<Dictionary<string, Location>>(LocationsByBibleChapterIndexValueFactory);
        _tagsNameIndex = new Lazy<Dictionary<TagTypeAndName, Tag>>(() => Tags.ToDictionary(tag => new TagTypeAndName(tag.Type, tag.Name)));
        _tagsIdIndex = new Lazy<Dictionary<int, Tag>>(() => Tags.ToDictionary(tag => tag.TagId));
        _tagMapNoteIndex = new Lazy<Dictionary<string, TagMap>>(TagMapNoteIndexValueFactory);
        _tagMapLocationIndex = new Lazy<Dictionary<string, TagMap>>(TagMapLocationIndexValueFactory);
        _tagMapPlaylistItemIndex = new Lazy<Dictionary<string, TagMap>>(TagMapPlaylistItemIndexValueFactory);
        _blockRangesUserMarkIdIndex = new Lazy<Dictionary<int, List<BlockRange>>>(BlockRangeIndexValueFactory);
        _bookmarksIndex = new Lazy<Dictionary<string, Bookmark>>(BookmarkIndexValueFactory);
        _independentMediasFilePathIndex = new Lazy<Dictionary<string, IndependentMedia>>(() => IndependentMedias.ToDictionary(media => media.FilePath.Trim()));
        _independentMediasHashIndex = new Lazy<Dictionary<string, IndependentMedia>>(() => IndependentMedias.ToDictionary(media => media.Hash.Trim()));
        _independentMediasIdIndex = new Lazy<Dictionary<int, IndependentMedia>>(() => IndependentMedias.ToDictionary(media => media.IndependentMediaId));
        _playlistItemsIdIndex = new Lazy<Dictionary<int, PlaylistItem>>(() => PlaylistItems.ToDictionary(playlist => playlist.PlaylistItemId));
        _playlistItemsValueIndex = new Lazy<Dictionary<string, PlaylistItem>>(PlaylistItemsValueIndexFactory);
        _playlistItemIndependentMediaMapsValueIndex = new Lazy<Dictionary<string, PlaylistItemIndependentMediaMap>>(PlaylistItemIndependentMediaMapsValueIndexFactory);
        _playlistItemMarkersIdIndex = new Lazy<Dictionary<int, PlaylistItemMarker>>(() => PlaylistItemMarkers.ToDictionary(marker => marker.PlaylistItemMarkerId));
        _playlistItemMarkersBibleVerseMapIndex = new Lazy<Dictionary<int, PlaylistItemMarkerBibleVerseMap>>(() => PlaylistItemMarkerBibleVerseMaps.ToDictionary(marker => marker.PlaylistItemMarkerId));
        _playlistItemMarkersParagraphMapIndex = new Lazy<Dictionary<int, PlaylistItemMarkerParagraphMap>>(() => PlaylistItemMarkerParagraphMaps.ToDictionary(marker => marker.PlaylistItemMarkerId));
        _playlistItemLocationMapsValueIndex = new Lazy<Dictionary<string, PlaylistItemLocationMap>>(PlaylistItemLocationMapsValueFactory);
    }

    private int FixupLocationValidity()
    {
        var fixupCount = 0;

        // there is a unique index on the following Location table columns:
        // KeySymbol, IssueTagNumber, MepsLanguage, DocumentId, Track, Type
        // Some Locations may have been stored in the db before this constraint
        // was added, so identify them here and remove the duplicate entries.
        // Note that null is treated as a unique value in SQLite
        var keys = new HashSet<string>();

        for (var n = Locations.Count - 1; n >= 0; --n)
        {
            var loc = Locations[n];

            if (loc.Track == null || loc.DocumentId == null)
            {
                continue;
            }

            var key = $"{loc.KeySymbol}|{loc.IssueTagNumber}|{loc.MepsLanguage}|{loc.DocumentId.Value}|{loc.Track.Value}|{loc.Type}";

            if (!keys.Add(key))
            {
                // found a duplicate that will cause a constraint exception.
                ++fixupCount;
                Locations.RemoveAt(n);

                Log.Logger.Error($"Removed duplicate location {loc.LocationId}");
            }
        }

        return fixupCount;
    }

    private int FixupUserMarkValidity()
    {
        var fixupCount = 0;

        for (var n = UserMarks.Count - 1; n >= 0; --n)
        {
            var userMark = UserMarks[n];

            if (FindLocation(userMark.LocationId) == null)
            {
                ++fixupCount;
                UserMarks.RemoveAt(n);

                Log.Logger.Error($"Removed invalid user mark {userMark.UserMarkId}");
            }
        }

        return fixupCount;
    }

    private int FixupTagMapValidity()
    {
        var fixupCount = 0;

        for (var n = TagMaps.Count - 1; n >= 0; --n)
        {
            var tagMap = TagMaps[n];

            if (tagMap.NoteId != null &&
                (FindTag(tagMap.TagId) == null || FindNote(tagMap.NoteId.Value) == null))
            {
                ++fixupCount;
                TagMaps.RemoveAt(n);

                Log.Logger.Error($"Removed invalid tag map {tagMap.TagMapId}");
            }
            else if (tagMap.LocationId != null && FindLocation(tagMap.LocationId.Value) == null)
            {
                ++fixupCount;
                TagMaps.RemoveAt(n);

                Log.Logger.Error($"Removed invalid tag map {tagMap.TagMapId} (missing LocationId)");
            }
        }

        return fixupCount;
    }

    private int FixupNoteValidity()
    {
        var fixupCount = 0;

        for (var n = Notes.Count - 1; n >= 0; --n)
        {
            var note = Notes[n];

            if (note.UserMarkId != null && FindUserMark(note.UserMarkId.Value) == null)
            {
                ++fixupCount;
                note.UserMarkId = null;

                Log.Logger.Error($"Cleared invalid user mark ID for note {note.NoteId}");
            }

            if (note.LocationId != null && FindLocation(note.LocationId.Value) == null)
            {
                ++fixupCount;
                note.LocationId = null;

                Log.Logger.Error($"Cleared invalid location ID for note {note.NoteId}");
            }
        }

        return fixupCount;
    }

    private int FixupBookmarkValidity()
    {
        var fixupCount = 0;

        for (var n = Bookmarks.Count - 1; n >= 0; --n)
        {
            var bookmark = Bookmarks[n];

            if (FindLocation(bookmark.LocationId) == null ||
                FindLocation(bookmark.PublicationLocationId) == null)
            {
                ++fixupCount;
                Bookmarks.RemoveAt(n);

                Log.Logger.Error($"Removed invalid bookmark {bookmark.BookmarkId}");
            }
        }

        return fixupCount;
    }

    private int FixupBlockRangeValidity()
    {
        var fixupCount = 0;

        for (var n = BlockRanges.Count - 1; n >= 0; --n)
        {
            var range = BlockRanges[n];

            if (FindUserMark(range.UserMarkId) == null)
            {
                ++fixupCount;
                BlockRanges.RemoveAt(n);

                Log.Logger.Error($"Removed invalid block range {range.BlockRangeId}");
            }
        }

        return fixupCount;
    }
}