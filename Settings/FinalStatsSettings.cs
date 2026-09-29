namespace FinalStatsPlugin.Settings
{
    public enum FinalScreenshotPlacementFilter
    {
        All,
        Top4,
        Top3,
        Top2,
        Top1
    }

    public enum FinalBoardDisplayMode
    {
        Compact,
        Detailed
    }

    public sealed class FinalStatsSettings
    {
        public FinalScreenshotPlacementFilter FinalScreenshotOnlyOn
        { get; set; } = FinalScreenshotPlacementFilter.Top3;

        public FinalBoardDisplayMode FinalBoardMode
        { get; set; } = FinalBoardDisplayMode.Compact;

        public int GetFinalScreenshotMaximumPlacement()
        {
            switch (FinalScreenshotOnlyOn)
            {
                case FinalScreenshotPlacementFilter.Top4:
                    return 4;

                case FinalScreenshotPlacementFilter.Top3:
                    return 3;

                case FinalScreenshotPlacementFilter.Top2:
                    return 2;

                case FinalScreenshotPlacementFilter.Top1:
                    return 1;

                default:
                    return int.MaxValue;
            }
        }
    }
}
