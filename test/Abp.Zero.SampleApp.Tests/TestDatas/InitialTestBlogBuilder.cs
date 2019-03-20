﻿using Abp.Zero.SampleApp.EntityFramework;
using Abp.Zero.SampleApp.EntityHistory;

namespace Abp.Zero.SampleApp.Tests.TestDatas
{
    public class InitialTestBlogBuilder
    {
        private readonly AppDbContext _context;

        public InitialTestBlogBuilder(AppDbContext context)
        {
            _context = context;
        }

        public void Build()
        {
            var blog1 = new Blog("test-blog-1", "http://testblog1.myblogs.com");

            _context.Blogs.Add(blog1);
            _context.SaveChanges();

            var post1 = new Post { Blog = blog1, Title = "test-post-1-title", Body = "test-post-1-body" };
            var post2 = new Post { Blog = blog1, Title = "test-post-2-title", Body = "test-post-2-body" };
            var post3 = new Post { Blog = blog1, Title = "test-post-3-title", Body = "test-post-3-body-deleted", IsDeleted = true };
            var post4 = new Post { Blog = blog1, Title = "test-post-4-title", Body = "test-post-4-body", TenantId = 42 };

            _context.Posts.AddRange(new Post[] { post1, post2, post3, post4 });

            var comment1 = new Comment { Post = post1, Content = "test-comment-1-content" };

            _context.Comments.Add(comment1);
        }
    }
}