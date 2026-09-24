using BugReporter;
using BugReporter.Serialization;
using GitUI.Presentation.BugReporter;

namespace GitExtensions.UITests.NBugReports;

/// <summary>The details of the exception that the bug report dialog lists (as the <c>ExceptionDetails</c> of <c>BugReportForm</c>).</summary>
public class BugReportDialogTests
{
    [Test]
    public void Should_show_load_exception_info_correctly()
    {
        string path = Path.Combine(TestContext.CurrentContext.TestDirectory, @"MockData/SimpleException.txt");
        string content = File.ReadAllText(path);

        SerializableException exception = SerializableException.FromXmlString(content);

        BugReportException reportException = BugReportDialog.ToReportException(exception);

        reportException.Type.Should().Be(exception.Type);
        IReadOnlyList<BugReportProperty> properties = reportException.Properties;
        properties.Should().HaveCount(10);

        int index = 0;
        properties[index].Should().Be(new BugReportProperty("Exception", exception.Type!));
        index++;
        properties[index].Should().Be(new BugReportProperty("Message", exception.Message!));
        index++;
        properties[index].Should().Be(new BugReportProperty("Target Site", exception.TargetSite!));
        index++;
        properties[index].Should().Be(new BugReportProperty("Inner Exception", exception.InnerException!.Type!));
        index++;
        properties[index].Should().Be(new BugReportProperty("Source", exception.Source!));
        index++;
        properties[index].Should().Be(new BugReportProperty("Stack Trace", exception.StackTrace!));

        foreach (KeyValuePair<string, object> info in exception.ExtendedInformation!)
        {
            index++;
            properties[index].Should().Be(new BugReportProperty(info.Key, info.Value.ToString()!, IsExtended: true));
        }

        // Count=1-based, index=0-based
        index.Should().Be(properties.Count - 1);

        reportException.InnerExceptions.Should().ContainSingle().Which.Type.Should().Be(exception.InnerException.Type);
    }
}
