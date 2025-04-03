using System.Collections;

namespace KCD2;

public class RelativePath : ICloneable
{
    internal string[] _path;

    public RelativePath(string path) => _path = path.Trim().Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

    public RelativePath(IList<string> path) => _path = [.. path];

    public RelativePath(DirectoryInfo relativeTo, DirectoryInfo path) : this(Path.GetRelativePath(relativeTo.FullName, path.FullName)) { }

    public RelativePath(DirectoryInfo relativeTo, FileInfo path) : this(Path.GetRelativePath(relativeTo.FullName, path.FullName)) { }

    public bool IsFile => !string.IsNullOrWhiteSpace(FileName);

    public bool IsFolder => !IsFile;

    public string FileName => _path[^1];

    public string FileNameWithoutExtension => Path.GetFileNameWithoutExtension(FileName);

    public string Extension => Path.GetExtension(FileName);

    public RelativePath ParentFolder
    {
        get
        {
            if (_path.Length == 1) return new RelativePath([""]);
            if (IsFolder && _path.Length == 2) return new RelativePath([""]);
            if (IsFile) return new RelativePath([.. _path[0..^1], ""]);
            return new RelativePath([.. _path[0..^2], ""]);
        }
    }

    private static readonly RelativePath RootPath = new([""]);

    public bool IsWithinFolder(RelativePath relativePath)
    {
        if (relativePath.IsFile) return false;

        RelativePath? workingPath = Clone() as RelativePath;

        do
        {
            if (workingPath is null) return false;
            if (workingPath == relativePath) return true;

            workingPath = workingPath.ParentFolder;
        } while (workingPath != RootPath);

        return false;
    }

    public override string ToString() => ToString(Path.DirectorySeparatorChar);

    public string ToString(char directorySeperator) => string.Join(directorySeperator, _path);

    public override int GetHashCode() => (_path as IStructuralEquatable).GetHashCode(StringComparer.OrdinalIgnoreCase);

    public override bool Equals(object? obj) => Equals(obj as RelativePath);

    public bool Equals(RelativePath? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;
        if (_path.Length != other._path.Length) return false;

        return _path.SequenceEqual(other._path, StringComparer.OrdinalIgnoreCase);
    }

    public object Clone() => new RelativePath(ToString());

    public static bool operator ==(RelativePath left, RelativePath right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(RelativePath left, RelativePath right) => !(left == right);

    public static RelativePath operator +(RelativePath left, RelativePath right)
    {
        if (left._path.Length == 1 && string.IsNullOrEmpty(left._path[0])) return new(right.ToString());
        if (right._path.Length == 1 && string.IsNullOrEmpty(right._path[0])) return new(left.ToString());
        return new(left._path.Concat(right._path).ToArray());
    }
}