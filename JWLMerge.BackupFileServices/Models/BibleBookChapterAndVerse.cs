using System;

namespace JWLMerge.BackupFileServices.Models;

public readonly struct BibleBookChapterAndVerse(int bookNum, int chapterNum, int verseNum) : IEquatable<BibleBookChapterAndVerse>
{
    public int BookNumber { get; } = bookNum;

    public int ChapterNumber { get; } = chapterNum;

    public int VerseNumber { get; } = verseNum;

    public static bool operator ==(BibleBookChapterAndVerse lhs, BibleBookChapterAndVerse rhs) => lhs.Equals(rhs);

    public static bool operator !=(BibleBookChapterAndVerse lhs, BibleBookChapterAndVerse rhs) => !lhs.Equals(rhs);

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = BookNumber;
            hashCode = (hashCode * 397) ^ ChapterNumber;
            hashCode = (hashCode * 397) ^ VerseNumber;
            return hashCode;
        }
    }

    public override bool Equals(object? obj) => obj is BibleBookChapterAndVerse other && Equals(other);

    public bool Equals(BibleBookChapterAndVerse other)
        => BookNumber == other.BookNumber && ChapterNumber == other.ChapterNumber && VerseNumber == other.VerseNumber;
}