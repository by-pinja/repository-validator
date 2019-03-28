using System.Collections.Generic;
using CommandLine;
using CommandLine.Text;

namespace Runner
{
    public abstract class Options
    {
        [Option('s', "SlackReporting", HelpText = "If enabled, results are reported to Slack channel defined by configuration")]
        public bool ReportToSlack { get; }

        [Option('g', "GitHubReporting", HelpText = "If enabled, results are reported to GitHub issues")]
        public bool ReportToGithub { get; }

        [Option("CsvFile", HelpText = "If set, results are written to this CSV file. Old file is overridden")]
        public string CsvFile { get; }

        public Options(bool reportToSlack, bool reportToGithub, string csvFile)
        {
            ReportToSlack = reportToSlack;
            ReportToGithub = reportToGithub;
            CsvFile = csvFile;
        }
    }
}