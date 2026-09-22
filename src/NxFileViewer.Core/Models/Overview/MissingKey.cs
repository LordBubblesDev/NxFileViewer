using System;
using LibHac.Common.Keys;

namespace Emignatik.NxFileViewer.Models.Overview;

public class MissingKey(string keyName, KeyType keyType) : IEquatable<MissingKey>
{
    public string KeyName { get; } = keyName ?? throw new ArgumentNullException(nameof(keyName));

    public KeyType KeyType { get; } = keyType;

    public bool Equals(MissingKey? other)
    {
        if (other == null) {
            return false;
        }
            
        return KeyName == other.KeyName && KeyType == other.KeyType;
    }

    public override bool Equals(object? obj)
    {
        return obj is MissingKey other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(KeyName, (int) KeyType);
    }
}
