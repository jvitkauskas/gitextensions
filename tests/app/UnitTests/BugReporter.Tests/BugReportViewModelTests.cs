using GitUI.Presentation.BugReporter;

namespace GitUITests;
public class BugReportViewModelTests
{
    [TestCase("", false)]
    [TestCase("\t\r\n\t\t   \r   \n   \r", false)]
    [TestCase("\t\r\n\t\t  a \r   \n   \r", true)]
    public void Test(string input, bool expected)
    {
        BugReportViewModel.ContainsInfo(input).Should().Be(expected);
    }
}
