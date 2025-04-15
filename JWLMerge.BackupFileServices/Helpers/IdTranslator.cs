using System.Collections.Generic;

namespace JWLMerge.BackupFileServices.Helpers;

/// <summary>
/// Used by the <see cref="Merger"/> to map old and new id values./>
/// </summary>
internal sealed class IdTranslator
{
    private readonly Dictionary<int, int> _ids;

    public IdTranslator()
    {
        _ids = [];
    }

    public int GetTranslatedId(int oldId) => _ids.TryGetValue(oldId, out var translatedId) ? translatedId : 0;

    public void Add(int oldId, int translatedId) => _ids[oldId] = translatedId;

    public void Remove(int oldId) => _ids.Remove(oldId);

    public void Clear() => _ids.Clear();
}