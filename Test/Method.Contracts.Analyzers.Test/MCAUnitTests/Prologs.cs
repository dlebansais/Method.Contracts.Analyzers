namespace Contracts.Analyzers.Test;

internal static class Prologs
{
    public const string Default = @"
using System;
using System.Threading.Tasks;
using Contracts;

";

    public const int DefaultLineCount = 4;

    public const string Nullable = @"
#nullable enable

using System;
using System.Threading.Tasks;
using Contracts;

";

    public const int NullableLineCount = 6;

    public const string IsExternalInit = @"
#nullable enable

using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Contracts;

namespace System.Runtime.CompilerServices
{
    internal class IsExternalInit { }
}

";

    public const int IsExternalInitLineCount = 12;

    public const string NoContract = @"
using System;
using System.Threading.Tasks;

";

    public const int NoContractLineCount = 3;
}
