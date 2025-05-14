#if NETSTANDARD2_0
// Allows me to use newish C# features in a netstanard 2.0 build.

using System.ComponentModel;

namespace System.Runtime.CompilerServices;

[EditorBrowsable(EditorBrowsableState.Never)]
internal static class IsExternalInit { }
#endif
