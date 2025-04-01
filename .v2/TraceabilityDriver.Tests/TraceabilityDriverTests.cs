global using NUnit.Framework;

[SetUpFixture]
public class Setup
{
    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        OpenTraceability.GDST.Setup.Initialize();
    }
}