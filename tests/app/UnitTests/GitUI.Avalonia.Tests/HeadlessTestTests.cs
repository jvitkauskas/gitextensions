namespace GitUI.AvaloniaTests;

[TestFixture]
public sealed class HeadlessTestTests : HeadlessTest
{
    [Test]
    public void Asynchronous_UI_test_failures_reach_the_test_runner()
    {
        Assert.ThrowsAsync<InvalidOperationException>(() => OnUiThreadAsync(async () =>
        {
            await Task.Yield();
            throw new InvalidOperationException("Test failure after an asynchronous continuation");
        }));
    }
}
