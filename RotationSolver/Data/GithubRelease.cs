﻿namespace RotationSolver.Data
{
    public class GithubRelease
    {
        public class Release
        {
            public Release()
            {
                Url = string.Empty;
                AssetsUrl = string.Empty;
                UploadUrl = string.Empty;
                HtmlUrl = string.Empty;
                NodeId = string.Empty;
                TagName = string.Empty;
                TargetCommitish = string.Empty;
                Name = string.Empty;
                Assets = [];
                TarballUrl = string.Empty;
                ZipballUrl = string.Empty;
                Body = string.Empty;
                Author = new Author();
            }

            [JsonProperty("url")]
            public string Url { get; set; }

            [JsonProperty("assets_url")]
            public string AssetsUrl { get; set; }

            [JsonProperty("upload_url")]
            public string UploadUrl { get; set; }

            [JsonProperty("html_url")]
            public string HtmlUrl { get; set; }

            [JsonProperty("id")]
            public long Id { get; set; }

            [JsonProperty("node_id")]
            public string NodeId { get; set; }

            [JsonProperty("tag_name")]
            public string TagName { get; set; }

            [JsonProperty("target_commitish")]
            public string TargetCommitish { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("draft")]
            public bool Draft { get; set; }

            [JsonProperty("prerelease")]
            public bool Prerelease { get; set; }

            [JsonProperty("created_at")]
            public DateTime CreatedAt { get; set; }

            [JsonProperty("published_at")]
            public DateTime PublishedAt { get; set; }

            [JsonProperty("assets")]
            public List<Asset> Assets { get; set; }

            [JsonProperty("tarball_url")]
            public string TarballUrl { get; set; }

            [JsonProperty("zipball_url")]
            public string ZipballUrl { get; set; }

            [JsonProperty("body")]
            public string Body { get; set; }

            [JsonProperty("mentions_count")]
            public int MentionsCount { get; set; }

            [JsonProperty("author")]
            public Author Author { get; set; }
        }

        public class Author
        {
            public Author()
            {
                Login = string.Empty;
                NodeId = string.Empty;
                AvatarUrl = string.Empty;
                GravatarId = string.Empty;
                Url = string.Empty;
                HtmlUrl = string.Empty;
                FollowersUrl = string.Empty;
                FollowingUrl = string.Empty;
                GistsUrl = string.Empty;
                StarredUrl = string.Empty;
                SubscriptionsUrl = string.Empty;
                OrganizationsUrl = string.Empty;
                ReposUrl = string.Empty;
                EventsUrl = string.Empty;
                ReceivedEventsUrl = string.Empty;
                Type = string.Empty;
            }

            [JsonProperty("login")]
            public string Login { get; set; }

            [JsonProperty("id")]
            public long Id { get; set; }

            [JsonProperty("node_id")]
            public string NodeId { get; set; }

            [JsonProperty("avatar_url")]
            public string AvatarUrl { get; set; }

            [JsonProperty("gravatar_id")]
            public string GravatarId { get; set; }

            [JsonProperty("url")]
            public string Url { get; set; }

            [JsonProperty("html_url")]
            public string HtmlUrl { get; set; }

            [JsonProperty("followers_url")]
            public string FollowersUrl { get; set; }

            [JsonProperty("following_url")]
            public string FollowingUrl { get; set; }

            [JsonProperty("gists_url")]
            public string GistsUrl { get; set; }

            [JsonProperty("starred_url")]
            public string StarredUrl { get; set; }

            [JsonProperty("subscriptions_url")]
            public string SubscriptionsUrl { get; set; }

            [JsonProperty("organizations_url")]
            public string OrganizationsUrl { get; set; }

            [JsonProperty("repos_url")]
            public string ReposUrl { get; set; }

            [JsonProperty("events_url")]
            public string EventsUrl { get; set; }

            [JsonProperty("received_events_url")]
            public string ReceivedEventsUrl { get; set; }

            [JsonProperty("type")]
            public string Type { get; set; }

            [JsonProperty("site_admin")]
            public bool SiteAdmin { get; set; }
        }

        public class Asset
        {
            public Asset()
            {
                Url = string.Empty;
                NodeId = string.Empty;
                Name = string.Empty;
                Label = string.Empty;
                Uploader = new Uploader();
                ContentType = string.Empty;
                State = string.Empty;
                BrowserDownloadUrl = string.Empty;
            }

            [JsonProperty("url")]
            public string Url { get; set; }

            [JsonProperty("id")]
            public long Id { get; set; }

            [JsonProperty("node_id")]
            public string NodeId { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("label")]
            public string Label { get; set; }

            [JsonProperty("uploader")]
            public Uploader Uploader { get; set; }

            [JsonProperty("content_type")]
            public string ContentType { get; set; }

            [JsonProperty("state")]
            public string State { get; set; }

            [JsonProperty("size")]
            public long Size { get; set; }

            [JsonProperty("download_count")]
            public int DownloadCount { get; set; }

            [JsonProperty("created_at")]
            public DateTime CreatedAt { get; set; }

            [JsonProperty("updated_at")]
            public DateTime UpdatedAt { get; set; }

            [JsonProperty("browser_download_url")]
            public string BrowserDownloadUrl { get; set; }
        }

        public class Uploader
        {
            public Uploader()
            {
                Login = string.Empty;
                NodeId = string.Empty;
                AvatarUrl = string.Empty;
                GravatarId = string.Empty;
                Url = string.Empty;
                HtmlUrl = string.Empty;
                FollowersUrl = string.Empty;
                FollowingUrl = string.Empty;
                GistsUrl = string.Empty;
                StarredUrl = string.Empty;
                SubscriptionsUrl = string.Empty;
                OrganizationsUrl = string.Empty;
                ReposUrl = string.Empty;
                EventsUrl = string.Empty;
                ReceivedEventsUrl = string.Empty;
                Type = string.Empty;
            }

            [JsonProperty("login")]
            public string Login { get; set; }

            [JsonProperty("id")]
            public long Id { get; set; }

            [JsonProperty("node_id")]
            public string NodeId { get; set; }

            [JsonProperty("avatar_url")]
            public string AvatarUrl { get; set; }

            [JsonProperty("gravatar_id")]
            public string GravatarId { get; set; }

            [JsonProperty("url")]
            public string Url { get; set; }

            [JsonProperty("html_url")]
            public string HtmlUrl { get; set; }

            [JsonProperty("followers_url")]
            public string FollowersUrl { get; set; }

            [JsonProperty("following_url")]
            public string FollowingUrl { get; set; }

            [JsonProperty("gists_url")]
            public string GistsUrl { get; set; }

            [JsonProperty("starred_url")]
            public string StarredUrl { get; set; }

            [JsonProperty("subscriptions_url")]
            public string SubscriptionsUrl { get; set; }

            [JsonProperty("organizations_url")]
            public string OrganizationsUrl { get; set; }

            [JsonProperty("repos_url")]
            public string ReposUrl { get; set; }

            [JsonProperty("events_url")]
            public string EventsUrl { get; set; }

            [JsonProperty("received_events_url")]
            public string ReceivedEventsUrl { get; set; }

            [JsonProperty("type")]
            public string Type { get; set; }

            [JsonProperty("site_admin")]
            public bool SiteAdmin { get; set; }
        }
    }
}
