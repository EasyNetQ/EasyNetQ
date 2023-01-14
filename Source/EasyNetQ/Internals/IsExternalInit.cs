# if NETSTANDARD
using System.ComponentModel;

// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices;

/// <summary>
/// Reserved to be used by the compiler for tracking metadata.
/// This class should not be used by developers in source code.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
// ReSharper disable once UnusedType.Global
internal sealed class IsExternalInit
{
}
# endif
