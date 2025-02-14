﻿using JWLMerge.BackupFileServices.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using Serilog;

namespace JWLMerge.BackupFileServices.Models.DatabaseModels;

public class Database
{
    private readonly Dictionary<int, int> _bookmarkSlots = new();
        
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
    private Lazy<Dictionary<int, List<BlockRange>>> _blockRangesUserMarkIdIndex = null!;
    private Lazy<Dictionary<string, Bookmark>> _bookmarksIndex = null!;

    public Database()
    {
        ReinitializeIndexes();
    }

    public LastModified LastModified { get; } = new();

    public List<Location> Locations { get; } = new();

    public List<Note> Notes { get; } = new();

    public List<InputField> InputFields { get; } = new();

    public List<Tag> Tags { get; } = new();

    public List<TagMap> TagMaps { get; } = new();

    public List<BlockRange> BlockRanges { get; } = new();

    public List<Bookmark> Bookmarks { get; } = new();

    public List<UserMark> UserMarks { get; } = new();
    public List<PlaylistItemAccuracy> PlaylistItemAccuracies { get; } = new();
    public List<IndependentMedia> IndependentMedias { get; } = new();
    public List<PlaylistItemIndependentMediaMap> PlaylistItemIndependentMediaMaps { get; } = new();
    public List<PlaylistItemLocationMap> PlaylistItemLocationMaps { get; } = new();
    public List<PlaylistItemMarker> PlaylistItemMarkers { get; } = new();
    public List<PlaylistItemMarkerBibleVerseMap> PlaylistItemMarkerBibleVerseMaps { get; } = new();
    public List<PlaylistItemMarkerParagraphMap> PlaylistItemMarkerParagraphMaps { get; } = new();


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
        IndependentMedias.Clear();
        PlaylistItemAccuracies.Clear();
        PlaylistItemIndependentMediaMaps.Clear();
        PlaylistItemLocationMaps.Clear();
        PlaylistItemMarkers.Clear();
        PlaylistItemMarkerBibleVerseMaps.Clear();
        PlaylistItemMarkerParagraphMaps.Clear();
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
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

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
                notes = new List<Note>();
                _notesVerseIndex.Value.Add(verseRef, notes);
            }

            notes.Add(value);
        }
    }

    public void AddBlockRangeAndUpdateIndex(BlockRange value)
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        BlockRanges.Add(value);

        if (_blockRangesUserMarkIdIndex.IsValueCreated)
        {
            if (!_blockRangesUserMarkIdIndex.Value.TryGetValue(value.UserMarkId, out var blockRangeList))
            {
                blockRangeList = new List<BlockRange>();
                _blockRangesUserMarkIdIndex.Value.Add(value.UserMarkId, blockRangeList);
            }

            blockRangeList.Add(value);
        }
    }

    public void AddUserMarkAndUpdateIndex(UserMark value)
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

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
                marks = new List<UserMark>();
                _userMarksLocationIdIndex.Value.Add(value.LocationId, marks);
            }

            marks.Add(value);
        }
    }

    public void AddLocationAndUpdateIndex(Location value)
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

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

    public Note? FindNote(string noteGuid)
    {
        if (!Guid.TryParse(noteGuid, out var g))
        {
            return null;
        }

        return _notesGuidIndex.Value.TryGetValue(g, out var note) ? note : null;
    }

    public Note? FindNote(int noteId)
    {
        return _notesIdIndex.Value.TryGetValue(noteId, out var note) ? note : null;
    }

    public IEnumerable<Note>? FindNotes(BibleBookChapterAndVerse verseRef)
    {
        return _notesVerseIndex.Value.TryGetValue(verseRef, out var notes) ? notes : null;
    }

    public UserMark? FindUserMark(string userMarkGuid)
    {
        if (!Guid.TryParse(userMarkGuid, out var g))
        {
            return null;
        }

        return _userMarksGuidIndex.Value.TryGetValue(g, out var userMark) ? userMark : null;
    }

    public UserMark? FindUserMark(int userMarkId)
    {
        return _userMarksIdIndex.Value.TryGetValue(userMarkId, out var userMark) ? userMark : null;
    }

    public IEnumerable<UserMark>? FindUserMarks(int locationId)
    {
        return _userMarksLocationIdIndex.Value.TryGetValue(locationId, out var userMarks) ? userMarks : null;
    }

    public Tag? FindTag(int tagType, string tagName)
    {
        var key = new TagTypeAndName(tagType, tagName);
        return _tagsNameIndex.Value.TryGetValue(key, out var tag) ? tag : null;
    }

    public Tag? FindTag(int tagId)
    {
        return _tagsIdIndex.Value.TryGetValue(tagId, out var tag) ? tag : null;
    }

    public TagMap? FindTagMapForNote(int tagId, int noteId)
    {
        return _tagMapNoteIndex.Value.TryGetValue(GetTagMapNoteKey(tagId, noteId), out var tag) ? tag : null;
    }

    public TagMap? FindTagMapForLocation(int tagId, int locationId)
    {
        return _tagMapLocationIndex.Value.TryGetValue(GetTagMapLocationKey(tagId, locationId), out var tag) ? tag : null;
    }

    public Location? FindLocation(int locationId)
    {
        return _locationsIdIndex.Value.TryGetValue(locationId, out var location) ? location : null;
    }

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
        if (locationValues == null)
        {
            throw new ArgumentNullException(nameof(locationValues));
        }

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

    private Dictionary<Guid, Note> NoteIndexValueFactory()
    {
        return Notes.ToDictionary(note => Guid.Parse(note.Guid));
    }

    private Dictionary<int, List<InputField>> InputFieldsIndexValueFactory()
    {
        var result = new Dictionary<int, List<InputField>>();

        foreach (var fld in InputFields)
        {
            if (!result.TryGetValue(fld.LocationId, out var list))
            {
                list = new List<InputField>();
                result.Add(fld.LocationId, list);
            }

            list.Add(fld);
        }

        return result;
    }

    private Dictionary<int, Note> NoteIdIndexValueFactory()
    {
        return Notes.ToDictionary(note => note.NoteId);
    }

    private Dictionary<BibleBookChapterAndVerse, List<Note>> NoteVerseIndexValueFactory()
    {
        Dictionary<BibleBookChapterAndVerse, List<Note>> result = new();

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
                        notesOnVerse = new List<Note>();
                        result.Add(verseRef, notesOnVerse);
                    }

                    notesOnVerse.Add(note);
                }
            }
        }

        return result;
    }

    private Dictionary<Guid, UserMark> UserMarkIndexValueFactory()
    {
        return UserMarks.ToDictionary(userMark => Guid.Parse(userMark.UserMarkGuid));
    }

    private Dictionary<int, UserMark> UserMarkIdIndexValueFactory()
    {
        return UserMarks.ToDictionary(userMark => userMark.UserMarkId);
    }

    private Dictionary<int, List<UserMark>> UserMarksLocationIdIndexValueFactory()
    {
        var result = new Dictionary<int, List<UserMark>>();

        foreach (var userMark in UserMarks)
        {
            if (!result.TryGetValue(userMark.LocationId, out var marks))
            {
                marks = new List<UserMark>();
                result.Add(userMark.LocationId, marks);
            }

            marks.Add(userMark);
        }

        return result;
    }

    private Dictionary<int, Location> LocationsIndexValueFactory()
    {
        return Locations.ToDictionary(location => location.LocationId);
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
                blockRangeList = new List<BlockRange>();
                result.Add(range.UserMarkId, blockRangeList);
            }

            blockRangeList.Add(range);
        }
            
        return result;
    }

    private static string GetBookmarkKey(int locationId, int publicationLocationId)
    {
        return $"{locationId}-{publicationLocationId}";
    }

    private static string GetLocationByValueKey(Location location)
    {
        return $"{location.KeySymbol}|{location.IssueTagNumber}|{location.MepsLanguage}|{location.Type}|{location.BookNumber ?? -1}|{location.ChapterNumber ?? -1}|{location.DocumentId ?? -1}|{location.Track ?? -1}";
    }

    private static string GetLocationByBibleChapterKey(int bibleBookNumber, int chapterNumber, string? bibleKeySymbol)
    {
        return $"{bibleBookNumber}-{chapterNumber}-{bibleKeySymbol ?? string.Empty}";
    }

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

    private Dictionary<TagTypeAndName, Tag> TagIndexValueFactory()
    {
        return Tags.ToDictionary(tag => new TagTypeAndName(tag.Type, tag.Name));
    }

    private Dictionary<int, Tag> TagIdIndexValueFactory()
    {
        return Tags.ToDictionary(tag => tag.TagId);
    }

    private static string GetTagMapNoteKey(int tagId, int noteId)
    {
        return $"{tagId}-{noteId}";
    }

    private static string GetTagMapLocationKey(int tagId, int locationId)
    {
        return $"{tagId}-{locationId}";
    }

    private Dictionary<string, TagMap> TagMapNoteIndexValueFactory()
    {
        var result = new Dictionary<string, TagMap>();

        foreach (var tagMap in TagMaps)
        {
            if (tagMap.NoteId != null)
            {
                string key = GetTagMapNoteKey(tagMap.TagId, tagMap.NoteId.Value);
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
                string key = GetTagMapLocationKey(tagMap.TagId, tagMap.LocationId.Value);
                result.Add(key, tagMap);
            }
        }

        return result;
    }

    private void ReinitializeIndexes()
    {
        _notesGuidIndex = new Lazy<Dictionary<Guid, Note>>(NoteIndexValueFactory);
        _notesIdIndex = new Lazy<Dictionary<int, Note>>(NoteIdIndexValueFactory);
        _inputFieldsIndex = new Lazy<Dictionary<int, List<InputField>>>(InputFieldsIndexValueFactory);
        _notesVerseIndex = new Lazy<Dictionary<BibleBookChapterAndVerse, List<Note>>>(NoteVerseIndexValueFactory);
        _userMarksGuidIndex = new Lazy<Dictionary<Guid, UserMark>>(UserMarkIndexValueFactory);
        _userMarksIdIndex = new Lazy<Dictionary<int, UserMark>>(UserMarkIdIndexValueFactory);
        _userMarksLocationIdIndex = new Lazy<Dictionary<int, List<UserMark>>>(UserMarksLocationIdIndexValueFactory);
        _locationsIdIndex = new Lazy<Dictionary<int, Location>>(LocationsIndexValueFactory);
        _locationsValueIndex = new Lazy<Dictionary<string, Location>>(LocationsByValueIndexValueFactory);
        _locationsBibleChapterIndex = new Lazy<Dictionary<string, Location>>(LocationsByBibleChapterIndexValueFactory);
        _tagsNameIndex = new Lazy<Dictionary<TagTypeAndName, Tag>>(TagIndexValueFactory);
        _tagsIdIndex = new Lazy<Dictionary<int, Tag>>(TagIdIndexValueFactory);
        _tagMapNoteIndex = new Lazy<Dictionary<string, TagMap>>(TagMapNoteIndexValueFactory);
        _tagMapLocationIndex = new Lazy<Dictionary<string, TagMap>>(TagMapLocationIndexValueFactory);
        _blockRangesUserMarkIdIndex = new Lazy<Dictionary<int, List<BlockRange>>>(BlockRangeIndexValueFactory);
        _bookmarksIndex = new Lazy<Dictionary<string, Bookmark>>(BookmarkIndexValueFactory);
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

            if (keys.Contains(key))
            {
                // found a duplicate that will cause a constraint exception.
                ++fixupCount;
                Locations.RemoveAt(n);

                Log.Logger.Error($"Removed duplicate location {loc.LocationId}");
            }
            else
            {
                keys.Add(key);
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