using System;

namespace Combinatorics.Collections
{
    /// <summary>
    /// Flags controlling set generation behavior.
    /// </summary>
    [Flags]
    public enum GenerateOptions
    {
        /// <summary>
        /// Do not generate equivalent result sets.
        /// </summary>
        None = 0x0,

        /// <summary>
        /// Generate equivalent result sets.
        /// </summary>
        WithRepetition = 0x1,

        /// <summary>
        /// Allow each result set to be mutated in-place and returned as the next result set, rather than copying each set.
        /// </summary>
        AllowMutation = 0x2,
    }
}
