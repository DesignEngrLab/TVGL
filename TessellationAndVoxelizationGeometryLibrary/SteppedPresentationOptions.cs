using System;

namespace TVGL
{
    /// <summary>Controls how a stepped three-dimensional presentation retains and advances path history.</summary>
    public sealed class SteppedPresentationOptions
    {
        /// <summary>
        /// Gets the maximum number of recent timesteps whose paths may remain visible.
        /// A null value preserves the complete path history; zero suppresses stepped paths.
        /// This limit does not affect solids or their transform timelines.
        /// </summary>
        public int? PathHistoryStepLimit { get; init; }

        /// <summary>Throws when an option cannot be represented by the stepped presenters.</summary>
        public void Validate()
        {
            if (PathHistoryStepLimit < 0)
                throw new ArgumentOutOfRangeException(nameof(PathHistoryStepLimit),
                    "The path history step limit cannot be negative.");
        }
    }
}
