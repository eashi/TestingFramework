﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information

namespace LogicAppUnit.Hosting
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using Newtonsoft.Json;

    /// <summary>
    /// Flow callback URL definition.
    /// </summary>
    internal class CallbackUrlDefinition
    {
        /// <summary>
        /// Gets or sets the value, without any relative path component.
        /// </summary>
        [JsonProperty]
        public Uri Value { get; set; }

        /// <summary>
        /// Gets or sets the method.
        /// </summary>
        [JsonProperty]
        public string Method { get; set; }

        /// <summary>
        /// Gets or sets the base path.
        /// </summary>
        [JsonProperty]
        public Uri BasePath { get; set; }

        /// <summary>
        /// Gets or sets the relative path.
        /// </summary>
        [JsonProperty]
        public string RelativePath { get; set; }

        /// <summary>
        /// Gets or sets relative path parameters.
        /// </summary>
        [JsonProperty]
        public List<string> RelativePathParameters { get; set; }

        /// <summary>
        /// Gets or sets queries.
        /// </summary>
        [JsonProperty]
        public Dictionary<string, string> Queries { get; set; }

        /// <summary>
        /// Gets the queries as a query string, without a leading question mark.
        /// </summary>
        public string QueryString
        {
            get
            {
                return string.Join("&", Queries.Select(q => $"{WebUtility.UrlEncode(q.Key)}={WebUtility.UrlEncode(q.Value)}"));
            }
        }

        /// <summary>
        /// Gets the value, with a relative path component.
        /// </summary>
        /// <param name="queryParams">The query parameters to be passed to the workflow.</param>
        /// <param name="relativePath">The relative path to be used in the trigger. The path must already be URL-encoded.</param>
        public Uri ValueWithQueryAndRelativePath(Dictionary<string, string> queryParams, string relativePath)
        {
            // If there is a relative path, remove the preceding "/"
            // Relative path should not have a preceding "/";
            // See Remark under https://learn.microsoft.com/en-us/dotnet/api/system.uri.-ctor?view=net-7.0#system-uri-ctor(system-uri-system-string)
            if (!string.IsNullOrEmpty(relativePath))
                relativePath = relativePath.TrimStart('/');

            // If there are query parameters, add them to the Queries property
            if (queryParams is not null)
                foreach (var pair in queryParams)
                    Queries.Add(pair.Key, pair.Value);

            // Make sure the base path has a trailing slash to preserve the relative path in 'Value'
            string basePathAsString = BasePath.ToString();
            var baseUri = new Uri(basePathAsString + (basePathAsString.EndsWith("/") ? "" : "/"));

            return new UriBuilder(new Uri(baseUri, relativePath))
            {
                Query = QueryString
            }.Uri;
        }
    }
}
