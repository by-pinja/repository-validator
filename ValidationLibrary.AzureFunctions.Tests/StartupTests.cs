using NUnit.Framework;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using ValidationLibrary.Rules;
using System.Reflection;

namespace ValidationLibrary.AzureFunctions.Tests
{
    public class StartUpTests
    {

        [SetUp]
        public void Setup()
        {
            Environment.SetEnvironmentVariable("GitHub:Organization", "mock");
            Environment.SetEnvironmentVariable("GitHub:Token", "mock");
        }

        [TearDown]
        public void TearDown()
        {
            Environment.SetEnvironmentVariable("GitHub:Organization", null);
            Environment.SetEnvironmentVariable("GitHub:Token", null);
        }

        [Test]
        public void Configure_CanBuildRepositoryValidator()
        {
            IHost host = new HostBuilder().ConfigureWebJobs(new Startup().Configure).Build();
            var validator = host.Services.GetService(typeof(IRepositoryValidator));
            Assert.NotNull(validator);
        }

        [Test]
        public void Configure_ThrowsExceptionIfOrganizationIsMissing()
        {
            Environment.SetEnvironmentVariable("GitHub:Organization", null);

            IHost host = new HostBuilder().ConfigureWebJobs(new Startup().Configure).Build();
            var ex = Assert.Throws<ArgumentNullException>(() => host.Services.GetService(typeof(IRepositoryValidator)));
            Assert.AreEqual("Organization", ex.ParamName);
        }

        [Test]
        public void Configure_ThrowsExceptionIfTokenIsMissing()
        {
            Environment.SetEnvironmentVariable("GitHub:Token", null);

            IHost host = new HostBuilder().ConfigureWebJobs(new Startup().Configure).Build();
            var ex = Assert.Throws<ArgumentNullException>(() => host.Services.GetService(typeof(IRepositoryValidator)));
            Assert.AreEqual("Token", ex.ParamName);
        }

        [Test]
        public void Configure_CheckNormalRuleConfiguration()
        {
            // Get all rule classes.
            var assembly = Assembly.Load("ValidationLibrary.Rules");
            var expectedRules = assembly.GetExportedTypes().Where(t => t.GetInterface(nameof(IValidationRule)) != null && !t.IsAbstract);
            var expectedRuleNames = expectedRules.Select(r =>
            {
                var args = r.GetConstructors()[0].GetParameters().Select(p => (object)null).ToArray();
                return ((IValidationRule)Activator.CreateInstance(r, args)).RuleName;
            });

            IHost host = new HostBuilder().ConfigureWebJobs(new Startup().Configure).Build();
            var validator = (IRepositoryValidator)host.Services.GetService(typeof(IRepositoryValidator));
            var actualRules = validator.Rules;

            Assert.AreEqual(expectedRules.Count(), actualRules.Length);
            foreach (var ruleName in expectedRuleNames)
            {
                Assert.IsTrue(actualRules.Any(r => r.RuleName.Equals(ruleName)));
            }
        }

        [Test]
        public void Configure_CheckExplicitRuleConfiguration()
        {
            // Environment variables for the configuration.
            Environment.SetEnvironmentVariable("Rules:HasLicenseRule", "disable");
            Environment.SetEnvironmentVariable("Rules:HasDescriptionRule", "enable"); // Decoy.

            // Get all rule classes.
            var assembly = Assembly.Load("ValidationLibrary.Rules");
            var allRules = assembly.GetExportedTypes().Where(t => t.GetInterface(nameof(IValidationRule)) != null && !t.IsAbstract);

            IHost host = new HostBuilder().ConfigureWebJobs(new Startup().Configure).Build();
            var validator = (IRepositoryValidator)host.Services.GetService(typeof(IRepositoryValidator));
            var actualRules = validator.Rules;

            Assert.AreEqual(allRules.Count() - 1, actualRules.Length);
            Assert.IsTrue(allRules.Any(r => r.Equals(typeof(HasLicenseRule))));
            Assert.IsFalse(actualRules.Any(r => r.RuleName.Equals("Missing License")));
            Assert.IsTrue(actualRules.Any(r => r.RuleName.Equals("Missing description")));

            // Tear down environment variables.
            Environment.SetEnvironmentVariable("Rules:HasLicenseRule", null);
            Environment.SetEnvironmentVariable("Rules:HasDescriptionRule", null);
        }
    }
}
