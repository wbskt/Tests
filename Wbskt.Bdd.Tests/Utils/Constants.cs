namespace Wbskt.Bdd.Tests.Utils;

public static class Constants
{
#if DEBUG
    public const string WbsktServerAddress = "localhost:5070";
#else
    public const string WbsktServerAddress = "wbskt.com";
#endif
}
