using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DD.Youtube.YoutubeApi
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await Upload();
        }

        private static async Task Upload()
        {
            UserCredential credential;
            using (var stream = new FileStream("Secrets.json", FileMode.Open, FileAccess.Read))
            {
                credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    // This OAuth 2.0 access scope allows an application to upload files to the
                    // authenticated user's YouTube channel, but doesn't allow other types of access.
                    new[] { YouTubeService.Scope.YoutubeUpload },
                    "user",
                    CancellationToken.None
                );
            }

            var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "dd-youtube-youtubeapi"
            });

            var video = new Video();
            video.Snippet = new VideoSnippet();
            video.Snippet.Title = "Default Video Title";
            video.Snippet.Description = "Default Video Description";
            video.Snippet.Tags = new string[] { "tag1", "tag2" };
            video.Snippet.CategoryId = "22";
            video.Snippet.ChannelId = "UClZY2Jh-lNQGa3j9KUIa82w";
            video.Status = new VideoStatus();
            video.Status.PrivacyStatus = "unlisted"; 
            var filePath = @"C:\Users\daniel.diazg\Desktop\gif.mp4";

            using (var fileStream = new FileStream(filePath, FileMode.Open))
            {
                var videosInsertRequest = youtubeService.Videos.Insert(video, "snippet,status", fileStream, "video/*");
                videosInsertRequest.ProgressChanged += videosInsertRequest_ProgressChanged;
                videosInsertRequest.ResponseReceived += videosInsertRequest_ResponseReceived;

                await videosInsertRequest.UploadAsync();
            }
        }


        static void videosInsertRequest_ProgressChanged(Google.Apis.Upload.IUploadProgress progress)
        {
            switch (progress.Status)
            {
                case UploadStatus.Uploading:
                    Console.WriteLine("{0} bytes sent.", progress.BytesSent);
                    break;

                case UploadStatus.Failed:
                    Console.WriteLine("An error prevented the upload from completing.\n{0}", progress.Exception);
                    break;
            }
        }

        static void videosInsertRequest_ResponseReceived(Video video)
        {
            Console.WriteLine("Video id '{0}' was successfully uploaded.", video.Id);
        }


        private static async Task Search()
        {
            Console.WriteLine("Search term:");
            var searchTerm = Console.ReadLine();

            var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = "REPLACE_APIKEY",
                ApplicationName = "dd-youtube-youtubeapi",
            });

            var searchListRequest = youtubeService.Search.List("snippet");
            searchListRequest.Q = searchTerm;
            searchListRequest.MaxResults = 50;

            var searchListResponse = await searchListRequest.ExecuteAsync();

            List<string> videos = new List<string>();

            foreach (var searchResult in searchListResponse.Items)
            {
                switch (searchResult.Id.Kind)
                {
                    case "youtube#video":
                        videos.Add(String.Format("{0} ({1})", searchResult.Snippet.Title, searchResult.Id.VideoId));
                        break;
                }
            }

            Console.WriteLine(String.Format("Videos:\n{0}\n", string.Join("\n", videos)));
        }
    }
}
