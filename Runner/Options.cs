using CommandLine;

namespace Runner
{
    public abstract class Options
    {
        [Option('s', "SlackReporting", HelpText = "If enabled, results are reported to Slack channel defined by configuration")]
        public bool ReportToSlack { get; }

        [Option('g', "GitHubReporting", HelpText = "If enabled, results are reported to GitHub issues")]
        public bool ReportToGithub { get; }

        [Option('a', "AutoFix", HelpText = "If enabled, fixing pull request is automatically created.")]
        public bool AutoFix { get; }

        [Option('i', "IgnoreRepositoryRules", HelpText = "If enabled, repository-validator.json is ignored in checking.")]
        public bool IgnoreRepositoryRules { get; }

        public Options(bool reportToSlack, bool reportToGithub, bool autoFix, bool ignoreRepositoryRules)
        {
            ReportToSlack = reportToSlack;
            ReportToGithub = reportToGithub;
            AutoFix = autoFix;
            IgnoreRepositoryRules = ignoreRepositoryRules;
        }
    }
}
