using System;
using System.IO;
using System.Linq;

namespace ShipFactory.Serialization
{
    public static class SnapshotNameUtility
    {
        public static string SanitizeFileName(string name)
        {
            var candidate = string.IsNullOrWhiteSpace(name) ? "Ship" : name.Trim();
            var invalidChars = Path.GetInvalidFileNameChars();
            return invalidChars.Aggregate(candidate, (current, invalid) => current.Replace(invalid, '_'));
        }

        public static string GetNextCopyName(string baseName, Func<string, bool> nameExists)
        {
            if (nameExists == null)
                throw new ArgumentNullException(nameof(nameExists));

            var normalizedBaseName = string.IsNullOrWhiteSpace(baseName) ? "Ship" : baseName.Trim();
            if (!nameExists(normalizedBaseName))
                return normalizedBaseName;

            for (var copyIndex = 2; copyIndex < 1000; copyIndex++)
            {
                var candidate = $"{normalizedBaseName} ({copyIndex})";
                if (!nameExists(candidate))
                    return candidate;
            }

            throw new InvalidOperationException("[SnapshotNameUtility] Could not find a free copy name.");
        }
    }
}

