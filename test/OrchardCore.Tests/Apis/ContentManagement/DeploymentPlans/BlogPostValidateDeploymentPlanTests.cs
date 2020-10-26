using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using OrchardCore.Autoroute.Models;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Records;
using OrchardCore.Environment.Shell;
using OrchardCore.Tests.Apis.Context;
using OrchardCore.Tests.Apis.Context.Attributes;
using Xunit;
using YesSql;

namespace OrchardCore.Tests.Apis.ContentManagement.DeploymentPlans
{
    public class BlogPostValidateDeploymentPlanTests
    {
        [Theory]
        [SqliteData]
        [SqlServerData]
        [MySqlData]
        [PostgreSqlData]
        public async Task ShouldFailWhenAutoroutePathIsNotUnique(string databaseProvider, string connectionString)
        {
            using (var context = new BlogPostDeploymentContext())
            {
                // Setup
                await context.InitializeAsync(databaseProvider, connectionString);

                // Act
                var recipe = context.GetContentStepRecipe(context.OriginalBlogPost, jItem =>
                {
                    jItem[nameof(ContentItem.ContentItemId)] = "newcontentitemid";
                    jItem[nameof(ContentItem.ContentItemVersionId)] = "newversion";
                    jItem[nameof(ContentItem.DisplayText)] = "new version";
                    jItem[nameof(AutoroutePart)][nameof(AutoroutePart.Path)] = "blog/another";
                });

                var data = recipe["steps"][0]["Data"] as JArray;
                var secondContentItem = JObject.FromObject(context.OriginalBlogPost);
                secondContentItem[nameof(ContentItem.ContentItemId)] = "secondcontentitemid";
                secondContentItem[nameof(ContentItem.ContentItemVersionId)] = "secondcontentitemversionid";
                secondContentItem[nameof(ContentItem.DisplayText)] = "second content item display text";
                // To be explicit in this test set path to a known value.
                secondContentItem[nameof(AutoroutePart)][nameof(AutoroutePart.Path)] = "blog/post-1";
                data.Add(secondContentItem);

                // Test
                await Assert.ThrowsAnyAsync<Exception>(async () =>
                {
                    var response = await context.PostRecipeAsync(recipe, false);
                    response.EnsureSuccessStatusCode();
                });

                // Confirm creation of both content items was cancelled.
                var shellScope = await BlogPostDeploymentContext.ShellHost.GetScopeAsync(context.TenantName);
                await shellScope.UsingAsync(async scope =>
                {
                    var session = scope.ServiceProvider.GetRequiredService<ISession>();
                    var blogPosts = await session.Query<ContentItem, ContentItemIndex>(x =>
                        x.ContentType == "BlogPost").ListAsync();

                    Assert.Single(blogPosts);
                });
            }
        }
    }
}
