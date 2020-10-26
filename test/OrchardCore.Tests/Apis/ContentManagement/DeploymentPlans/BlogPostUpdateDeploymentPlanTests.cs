using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Records;
using OrchardCore.Environment.Shell;
using OrchardCore.Tests.Apis.Context;
using OrchardCore.Tests.Apis.Context.Attributes;
using Xunit;
using YesSql;

namespace OrchardCore.Tests.Apis.ContentManagement.DeploymentPlans
{
    public class BlogPostUpdateDeploymentPlanTests
    {
        [Theory]
        [SqliteData]
        [SqlServerData]
        [MySqlData]
        [PostgreSqlData]
        public async Task ShouldUpdateExistingContentItemVersion(string databaseProvider, string connectionString)
        {
            using (var context = new BlogPostDeploymentContext())
            {
                // Setup
                await context.InitializeAsync(databaseProvider, connectionString);

                // Act
                var recipe = context.GetContentStepRecipe(context.OriginalBlogPost, jItem =>
                {
                    jItem[nameof(ContentItem.DisplayText)] = "existing version mutated";
                });

                await context.PostRecipeAsync(recipe);

                // Test
                var shellScope = await BlogPostDeploymentContext.ShellHost.GetScopeAsync(context.TenantName);
                await shellScope.UsingAsync(async scope =>
                {
                    var session = scope.ServiceProvider.GetRequiredService<ISession>();
                    var blogPosts = await session.Query<ContentItem, ContentItemIndex>(x =>
                        x.ContentType == "BlogPost").ListAsync();

                    Assert.Single(blogPosts);
                    var mutatedVersion = blogPosts.FirstOrDefault(x => x.ContentItemVersionId == context.OriginalBlogPostVersionId);
                    Assert.Equal("existing version mutated", mutatedVersion?.DisplayText);
                });
            }
        }

        [Theory]
        [SqliteData]
        [SqlServerData]
        [MySqlData]
        [PostgreSqlData]
        public async Task ShouldDiscardDraftThenUpdateExistingContentItemVersion(string databaseProvider, string connectionString)
        {
            using (var context = new BlogPostDeploymentContext())
            {
                // Setup
                await context.InitializeAsync(databaseProvider, connectionString);

                var content = await context.Client.PostAsJsonAsync("api/content?draft=true", context.OriginalBlogPost);
                var draftContentItemVersionId = (await content.Content.ReadAsAsync<ContentItem>()).ContentItemVersionId;

                // Act
                var recipe = context.GetContentStepRecipe(context.OriginalBlogPost, jItem =>
                {
                    jItem[nameof(ContentItem.DisplayText)] = "existing version mutated";
                });

                await context.PostRecipeAsync(recipe);

                // Test
                var shellScope = await BlogPostDeploymentContext.ShellHost.GetScopeAsync(context.TenantName);
                await shellScope.UsingAsync(async scope =>
                {
                    var session = scope.ServiceProvider.GetRequiredService<ISession>();
                    var blogPosts = await session.Query<ContentItem, ContentItemIndex>(x =>
                        x.ContentType == "BlogPost").ListAsync();

                    Assert.Equal(2, blogPosts.Count());

                    var mutatedVersion = blogPosts.FirstOrDefault(x => x.ContentItemVersionId == context.OriginalBlogPostVersionId);
                    Assert.True(mutatedVersion?.Latest);
                    Assert.True(mutatedVersion?.Published);
                    Assert.Equal("existing version mutated", mutatedVersion?.DisplayText);

                    var draftVersion = blogPosts.FirstOrDefault(x => x.ContentItemVersionId == draftContentItemVersionId);
                    Assert.False(draftVersion.Latest);
                });
            }
        }

        [Theory]
        [SqliteData]
        [SqlServerData]
        [MySqlData]
        [PostgreSqlData]
        public async Task ShouldUpdateDraftThenPublishExistingContentItemVersion(string databaseProvider, string connectionString)
        {
            using (var context = new BlogPostDeploymentContext())
            {
                // Setup
                await context.InitializeAsync(databaseProvider, connectionString);

                var content = await context.Client.PostAsJsonAsync("api/content?draft=true", context.OriginalBlogPost);
                var draftContentItem = (await content.Content.ReadAsAsync<ContentItem>());

                // Act
                var recipe = context.GetContentStepRecipe(draftContentItem, jItem =>
                {
                    jItem[nameof(ContentItem.DisplayText)] = "draft version mutated";
                    jItem[nameof(ContentItem.Published)] = true;
                    jItem[nameof(ContentItem.Latest)] = true;
                });

                await context.PostRecipeAsync(recipe);

                // Test
                var shellScope = await BlogPostDeploymentContext.ShellHost.GetScopeAsync(context.TenantName);
                await shellScope.UsingAsync(async scope =>
                {
                    var session = scope.ServiceProvider.GetRequiredService<ISession>();
                    var blogPosts = await session.Query<ContentItem, ContentItemIndex>(x =>
                        x.ContentType == "BlogPost").ListAsync();

                    Assert.Equal(2, blogPosts.Count());

                    var originalVersion = blogPosts.FirstOrDefault(x => x.ContentItemVersionId == context.OriginalBlogPostVersionId);
                    Assert.False(originalVersion?.Latest);
                    Assert.False(originalVersion?.Published);

                    var draftVersion = blogPosts.FirstOrDefault(x => x.ContentItemVersionId == draftContentItem.ContentItemVersionId);
                    Assert.True(draftVersion?.Latest);
                    Assert.True(draftVersion?.Published);
                    Assert.Equal("draft version mutated", draftVersion?.DisplayText);
                });
            }
        }
    }
}
