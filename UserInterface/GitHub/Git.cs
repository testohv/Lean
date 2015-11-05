/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); 
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
*/
using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json;
using RestSharp;

namespace QuantConnect.Views.GitHub
{
    /// <summary>
    /// Git API management class.
    /// </summary>
    public class Git
    {
        private string _owner;
        private string _baseUrl;

        /// <summary>
        /// Default constructor assumes QuantConnect owner
        /// </summary>
        public Git(string owner)
        {
            _owner = owner;
            _baseUrl = "https://api.github.com/";
        }

        /// <summary>
        /// Get a list of commits for this owner and repo.
        /// </summary>
        /// <param name="repo">String repo name</param>
        /// <returns>List of commit objects</returns>
        public List<Commit> GetCommits(string repo)
        {
            List<Commit> commits;
            var url = _baseUrl + "repos/" + _owner + "/" + repo + "/commits";
            var client = new RestClient(url);
            var request = new RestRequest(Method.GET);
            var response = client.Execute(request);
            commits = JsonConvert.DeserializeObject<List<Commit>>(response.Content);
            return commits;
        }
    }
}
