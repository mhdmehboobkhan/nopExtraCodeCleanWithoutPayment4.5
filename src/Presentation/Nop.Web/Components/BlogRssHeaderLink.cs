﻿using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.Blogs;
using Nop.Web.Framework.Components;

namespace Nop.Web.Components
{
    public class BlogRssHeaderLinkViewComponent : NopViewComponent
    {
        private readonly BlogSettings _blogSettings;

        public BlogRssHeaderLinkViewComponent(BlogSettings blogSettings)
        {
            _blogSettings = blogSettings;
        }

        public IViewComponentResult Invoke()
        {
            if (!_blogSettings.Enabled || !_blogSettings.ShowHeaderRssUrl)
                return Content("");

            return View();
        }
    }
}
