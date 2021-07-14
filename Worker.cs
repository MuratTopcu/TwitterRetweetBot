using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tweetinvi;
using Tweetinvi.Models;
using Tweetinvi.Parameters;

namespace TwitterRetweetBot
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private IConfiguration _configuration;

        public Worker(ILogger<Worker> logger,IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }
       
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Retweet bot is starting ...");

                string customerKey = _configuration["TwitterSettings:customerKey"];
                string customerSecret = _configuration["TwitterSettings:customerSecret"];
                string userAccessToken = _configuration["TwitterSettings:userAccessToken"];
                string userAccessSecret = _configuration["TwitterSettings:userAccessSecret"];

                Auth.SetUserCredentials(customerKey, customerSecret, userAccessToken, userAccessSecret);
                var user = User.GetAuthenticatedUser();
                Console.WriteLine(user.Name);
                await Retweet(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
           
        }

        protected override  Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;

        public override  Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Retweet bot stopped");
            return Task.CompletedTask;
        }


        private  readonly string[] SearchTerms = new[]
        {
            "#Şuşa",
            "#şuşa"
        };
        private  readonly string[] FilterTerms = new[] //Any string which you don`t want Tweets contains it
         {
            "#shushi",
            "#shushi"
        };
        public async Task Retweet(IAuthenticatedUser user)
        {
            while (true)
            {
                var searchSince = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(60));
                await SearchAndRetweetTweets(SearchTerms, searchSince, FilterTerms,user);
                Thread.Sleep(TimeSpan.FromMinutes(1));
            }
            
        }
        private async Task SearchAndRetweetTweets(string[] seachTerms, DateTime searchSince, string[] filterTerms,IAuthenticatedUser user)
        {
            var query = string.Join(" OR ", seachTerms);
            var param = new SearchTweetsParameters(query)
            {
                Since = searchSince,
                TweetSearchType = TweetSearchType.OriginalTweetsOnly,
                Filters = TweetSearchFilters.Safe
            };

            var tweets = await SearchAsync.SearchTweets(param);
            if (!(tweets == null))
            {
                foreach (var tweet in tweets)
                {
                    // Exclude tweets that contain excluded words.
                    if (filterTerms.Any(d => tweet.Text.Contains(d, StringComparison.OrdinalIgnoreCase)))
                        continue;

                    Console.WriteLine(tweet.Url);
                    await tweet.PublishRetweetAsync();
                }
            }
            _logger.LogInformation("Null response");

        }
        public  void DeleteTweet(long id,IAuthenticatedUser user)
        {
            try
            {
                var tweets = Tweet.GetRetweets(new TweetIdentifier(id));
                foreach (var item in tweets)
                {
                    Tweet.DestroyTweet(item);
                }
            }
            catch (Exception ex)
            {

                _logger.LogError(ex.Message);
            }
           
        }
    }
}
