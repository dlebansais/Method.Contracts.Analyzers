namespace Contracts.Analyzers.Test;

public static class Prologs
{
    public const string Default = @"
using System;
using Contracts;

";

    public const string Nullable = @"
#nullable enable

using System;
using Contracts;

";

    public const string IsExternalInit = @"
#nullable enable

using System;
using System.ComponentModel;
using Contracts;

namespace System.Runtime.CompilerServices
{
    internal class IsExternalInit { }
}

";

    public const string NoContract = @"
using System;

";
}
