using Microsoft.Extensions.Logging;

namespace TVGL
{
    public static class OutputServices
    {
        private static ILogger logger;
        private static IPresenter3D presenter3D;
        private static IPresenter2D presenter2D;

        public static ILogger Logger
        {
            set { logger = value; }
            get
            {
                if (logger == null) SetLogger(LogLevel.Trace);
                return logger;
            }
        }

        public static IPresenter3D Presenter3D
        {
            set { presenter3D = value; }
            get
            {
                if (presenter3D == null)
                    presenter3D = new EmptyPresenter3D();
                return presenter3D;
            }
        }
        public static IPresenter2D Presenter2D
        {
            set { presenter2D = value; }
            get
            {
                if (presenter2D == null)
                    presenter2D = new EmptyPresenter2D();
                return presenter2D;
            }
        }

        public static void SetLogger(LogLevel minimumLevelToReport)
        {
            using ILoggerFactory factory = LoggerFactory.Create(builder =>
            {
                builder.AddSimpleConsole(options =>
                {
                    options.SingleLine = true;
                    options.IncludeScopes = false;
                })
                .SetMinimumLevel(minimumLevelToReport);
            });
            logger = factory.CreateLogger("TVGL");
        }
    }
}