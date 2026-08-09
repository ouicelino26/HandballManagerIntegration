using System;
using System.Collections.Generic;
using System.Text;

namespace HandballIntegration.Data
{
    public sealed class ApiSettings
    {
            public string ApiBaseUrl { get; set; } = string.Empty;
            public string ApplicationName { get; set; } = "HandWStat Administration";
            public string EnvironmentLabel { get; set; } = "Unknown";
            public int TimeoutSeconds { get; set; } = 30;
        public int DefaultCompetitionId { get; set; } = 1;
    }
}
