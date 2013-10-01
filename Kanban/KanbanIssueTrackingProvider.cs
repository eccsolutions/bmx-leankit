using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Providers;
using Inedo.BuildMaster.Extensibility.Providers.IssueTracking;
using Inedo.BuildMaster.Web;
using Inedo.Linq;

namespace Inedo.BuildMasterExtensions.LeanKit.Kanban
{
    /// <summary>
    /// Provides integration with the LeanKit Kanban system.
    /// </summary>
    [ProviderProperties(
        "LeanKit Kanban",
        "Provides integration with the LeanKit Kanban system.")]
    [CustomEditor(typeof(KanbanIssueTrackingProviderEditor))]
    public sealed class KanbanIssueTrackingProvider : IssueTrackingProviderBase, ICategoryFilterable, IUpdatingProvider
    {
        private static readonly Regex HtmlRemoverRegex = new Regex(@"<[^>]*>", RegexOptions.Compiled | RegexOptions.Singleline);

        /// <summary>
        /// Initializes a new instance of the <see cref="KanbanIssueTrackingProvider"/> class.
        /// </summary>
        public KanbanIssueTrackingProvider()
        {
            this.ReleaseNumberTagFormat = "rel-%RELNO%";
        }

        [Persistent]
        public string AccountName { get; set; }
        [Persistent]
        public string UserName { get; set; }
        [Persistent]
        public string Password { get; set; }
        [Persistent]
        public string ReleaseNumberTagFormat { get; set; }

        public string[] CategoryIdFilter { get; set; }
        public string[] CategoryTypeNames
        {
            get { return new[] { "Board" }; }
        }
        public bool CanAppendIssueDescriptions
        {
            get { return true; }
        }
        public bool CanChangeIssueStatuses
        {
            get { return true; }
        }
        public bool CanCloseIssues
        {
            get { return true; }
        }

        private string ApiUrl
        {
            get
            {
                return string.Format("http://{0}.leankitkanban.com/Kanban/Api/", HttpUtility.UrlEncode(this.AccountName));
            }
        }

        public override bool IsAvailable()
        {
            return true;
        }
        public override void ValidateConnection()
        {
            this.GetCategories();
        }
        public override string GetIssueUrl(Issue issue)
        {
            return string.Format(
                "http://{0}.leankitkanban.com/Boards/View/{1}/{2}",
                this.AccountName,
                this.CategoryIdFilter[0],
                issue.IssueId
            );
        }
        public override Issue[] GetIssues(string releaseNumber)
        {
            if (this.CategoryIdFilter == null || this.CategoryIdFilter.Length == 0)
                return new KanbanIssue[0];

            var board = (JavaScriptObject)this.GetData<JavaScriptArray>("Boards/" + this.CategoryIdFilter[0])[0];
            var lanes = (JavaScriptArray)board["Lanes"];

            int lastLaneId = lanes
                .Cast<JavaScriptObject>()
                .OrderByDescending(l => Convert.ToInt32(l["Index"]))
                .Select(l => Convert.ToInt32(l["Id"]))
                .FirstOrDefault();

            var releaseTag = this.ReleaseNumberTagFormat.Replace("%RELNO%", releaseNumber);

            var issues = new List<KanbanIssue>();

            foreach (JavaScriptObject lane in lanes)
            {
                int laneId = Convert.ToInt32(lane["Id"]);
                var status = (string)lane["Title"];

                var cards = (JavaScriptArray)lane["Cards"];
                foreach (JavaScriptObject card in cards)
                {
                    object tagsField;
                    card.TryGetValue("Tags", out tagsField);
                    var tags = (tagsField ?? string.Empty).ToString().Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

                    if (tags.Contains(releaseTag, StringComparer.OrdinalIgnoreCase))
                    {
                        int id = Convert.ToInt32(card["Id"]);
                        var title = Convert.ToString(card["Title"]);
                        object description;
                        card.TryGetValue("Description", out description);

                        var descriptionText = HtmlRemoverRegex.Replace((description ?? string.Empty).ToString(), string.Empty);
                        try
                        {
                            descriptionText = HttpUtility.HtmlDecode(descriptionText);
                        }
                        catch
                        {
                        }

                        issues.Add(new KanbanIssue(id, status, title, descriptionText, releaseNumber, laneId == lastLaneId));
                    }
                }
            }

            return issues.ToArray();
        }
        public override bool IsIssueClosed(Issue issue)
        {
            return ((KanbanIssue)issue).IsClosed;
        }
        public CategoryBase[] GetCategories()
        {
            // For some reason boards is wrapped in two arrays
            var boards = (JavaScriptArray)this.GetData<JavaScriptArray>("Boards")[0];
            var categories = new List<KanbanCategory>(boards.Length);
            foreach (JavaScriptObject obj in boards)
                categories.Add(new KanbanCategory(Convert.ToInt32(obj["Id"]), Convert.ToString(obj["Title"])));

            return categories.ToArray();
        }
        public void AppendIssueDescription(string issueId, string textToAppend)
        {
            if (string.IsNullOrEmpty(textToAppend))
                return;

            var card = (JavaScriptObject)this.GetData<JavaScriptArray>("Board/" + this.CategoryIdFilter[0] + "/GetCard/" + issueId)[0];

            object descriptionField;
            card.TryGetValue("Description", out descriptionField);

            card["Description"] = descriptionField + textToAppend;

            this.PostData<JavaScriptObject>("Board/" + this.CategoryIdFilter[0] + "/UpdateCard", card);
        }
        public void ChangeIssueStatus(string issueId, string newStatus)
        {
            var board = (JavaScriptObject)this.GetData<JavaScriptArray>("Boards/" + this.CategoryIdFilter[0])[0];
            var lanes = (JavaScriptArray)board["Lanes"];

            int? laneId = lanes
                .Cast<JavaScriptObject>()
                .Where(l => l["Title"].ToString().Equals(newStatus, StringComparison.OrdinalIgnoreCase))
                .Select(l => (int?)Convert.ToInt32(l["Id"]))
                .FirstOrDefault();

            if (laneId == null)
                throw new ArgumentException("Lane " + newStatus + " is not valid.");

            int id = int.Parse(issueId);

            var card = lanes
                .Cast<JavaScriptObject>()
                .SelectMany(l => ((JavaScriptArray)l["Cards"]).Cast<JavaScriptObject>())
                .FirstOrDefault(c => Convert.ToInt32(c["Id"]) == id);

            if (card == null)
                throw new ArgumentException("Card " + issueId + " was not found.");

            card["LaneId"] = (int)laneId;

            this.PostData<JavaScriptObject>("Board/" + this.CategoryIdFilter[0] + "/UpdateCard", card);
        }
        public void CloseIssue(string issueId)
        {
            var board = (JavaScriptObject)this.GetData<JavaScriptArray>("Boards/" + this.CategoryIdFilter[0])[0];
            var lanes = (JavaScriptArray)board["Lanes"];

            int lastLaneId = lanes
                .Cast<JavaScriptObject>()
                .OrderByDescending(l => Convert.ToInt32(l["Index"]))
                .Select(l => Convert.ToInt32(l["Id"]))
                .First();

            int id = int.Parse(issueId);

            var card = lanes
                .Cast<JavaScriptObject>()
                .SelectMany(l => ((JavaScriptArray)l["Cards"]).Cast<JavaScriptObject>())
                .FirstOrDefault(c => Convert.ToInt32(c["Id"]) == id);

            if (card == null)
                throw new ArgumentException("Card " + issueId + " was not found.");

            card["LaneId"] = lastLaneId;

            this.PostData<JavaScriptObject>("Board/" + this.CategoryIdFilter[0] + "/UpdateCard", card);
        }
        public override string ToString()
        {
            return "Provides integration with the LeanKit Kanban system.";
        }

        private TData GetData<TData>(string method)
        {
            var request = (HttpWebRequest)HttpWebRequest.Create(this.ApiUrl + method);
            request.Method = "GET";
            request.Credentials = new NetworkCredential(this.UserName, this.Password);
            request.PreAuthenticate = true;
            request.CachePolicy = new RequestCachePolicy(RequestCacheLevel.BypassCache);

            return GetResponse<TData>(request);
        }
        private TData PostData<TData>(string method, object args)
        {
            var request = (HttpWebRequest)HttpWebRequest.Create(this.ApiUrl + method);
            request.Method = "POST";
            request.Credentials = new NetworkCredential(this.UserName, this.Password);
            request.PreAuthenticate = true;
            request.CachePolicy = new RequestCachePolicy(RequestCacheLevel.BypassCache);
            request.ContentType = "application/json";
            using (var requestStream = new StreamWriter(request.GetRequestStream(), Encoding.UTF8))
            {
                JsonWriter.WriteJson(requestStream, args);
            }

            return GetResponse<TData>(request);
        }
        private static TData GetResponse<TData>(HttpWebRequest request)
        {
            string json;
            try
            {
                using (var response = (HttpWebResponse)request.GetResponse())
                using (var stream = new StreamReader(response.GetResponseStream()))
                {
                    json = stream.ReadToEnd();
                }
            }
            catch (WebException ex)
            {
                throw new ConnectionException(ex.Message, ex);
            }

            object parsedJson;
            try
            {
                parsedJson = JsonReader.ParseJson(json);
            }
            catch (Exception ex)
            {
                throw new ConnectionException("Invalid JSON in response from server.", ex);
            }

            ResponseObject<TData> reply;
            try
            {
                reply = new ResponseObject<TData>(parsedJson);
            }
            catch (Exception ex)
            {
                throw new ConnectionException("Unexpected response: " + ex.Message, ex);
            }

            if (reply.ReplyCode != ReplyCode.DataRetrievalSuccess && reply.ReplyCode != ReplyCode.DataUpdateSuccess && reply.ReplyCode != ReplyCode.DataDeleteSuccess && reply.ReplyCode != ReplyCode.DataInsertSuccess)
            {
                if (!string.IsNullOrEmpty(reply.ReplyText))
                    throw new ConnectionException("Server responded with error: " + reply.ReplyText);
                else
                    throw new ConnectionException("Server responded with error: " + reply.ReplyCode);
            }

            return reply.ReplyData;
        }
    }
}
