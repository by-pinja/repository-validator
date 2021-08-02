using System.Collections.Generic;
using CommandLine;
using CommandLine.Text;

namespace Runner
{
    [Verb("scan-selected", HelpText = "Scans selected repositories")]
    public class ScanSelectedOptions : Options
    {
        [Option('r', "Repository", Required = true, HelpText = "Name of the scanned repositories (without owner(s))")]
        public IEnumerable<string> Repositories { get; }

        public ScanSelectedOptions(IEnumerable<string> repositories, bool reportToSlack, bool reportToGithub, bool autofix, bool ignoreRepositoryRules) : base(reportToSlack, reportToGithub, autofix, ignoreRepositoryRules)
        {
            Repositories = repositories;
        }

        private static readonly IEnumerable<UnParserSettings> _exampleSettings = new[]
        {
            new UnParserSettings { PreferShortName = true },
            new UnParserSettings { PreferShortName = false }
        };

        [Usage(ApplicationAlias = "dotnet run --")]
        public static IEnumerable<Example> Examples
        {
            get
            {
                return new List<Example>
                {
                    new Example("Scan repository called 'repository-validator' and only report to console",
                        _exampleSettings, new ScanSelectedOptions(new []{"repository-validator"}, false, false, false, false)),
                    new Example("Scan repository called 'repository-validator' and report to Slack",
                        _exampleSettings, new ScanSelectedOptions(new []{"repository-validator"}, true, false, false, false)),
                    new Example("Scan repository called 'repository-validator' and report to GitHub",
                        _exampleSettings, new ScanSelectedOptions(new []{"repository-validator"}, false, true, false, false)),
                    new Example("Scan repository called 'repository-validator' and report to GitHub and Slack",
                        _exampleSettings, new ScanSelectedOptions(new []{"repository-validator"}, true, true, false, false)),
                    new Example("Scan repository called 'repository-validator' and create pull request if needed.",
                        _exampleSettings, new ScanSelectedOptions(new []{"repository-validator"}, false, false, true, false)),
                    new Example("Scan repository called 'repository-validator' and create pull request if needed while ignoring repository specific configurations.",
                        _exampleSettings, new ScanSelectedOptions(new []{"repository-validator"}, false, false, true, false)),

                };
            }
        }
    }
}
