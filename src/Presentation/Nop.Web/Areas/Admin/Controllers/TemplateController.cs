﻿using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.Topics;
using Nop.Services.Localization;
using Nop.Services.Security;
using Nop.Services.Topics;
using Nop.Web.Areas.Admin.Factories;
using Nop.Web.Areas.Admin.Infrastructure.Mapper.Extensions;
using Nop.Web.Areas.Admin.Models.Templates;
using Nop.Web.Framework.Mvc;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Web.Areas.Admin.Controllers
{
    public partial class TemplateController : BaseAdminController
    {
        #region Fields

        private readonly ILocalizationService _localizationService;
        private readonly IPermissionService _permissionService;
        private readonly ITemplateModelFactory _templateModelFactory;
        private readonly ITopicTemplateService _topicTemplateService;

        #endregion

        #region Ctor

        public TemplateController(ILocalizationService localizationService,
            IPermissionService permissionService,
            ITemplateModelFactory templateModelFactory,
            ITopicTemplateService topicTemplateService)
        {
            _localizationService = localizationService;
            _permissionService = permissionService;
            _templateModelFactory = templateModelFactory;
            _topicTemplateService = topicTemplateService;
        }

        #endregion

        #region Methods

        public virtual async Task<IActionResult> List()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageMaintenance))
                return AccessDeniedView();

            //prepare model
            var model = await _templateModelFactory.PrepareTemplatesModelAsync(new TemplatesModel());

            return View(model);
        }

        #region Topic templates
        
        [HttpPost]
        public virtual async Task<IActionResult> TopicTemplates(TopicTemplateSearchModel searchModel)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageMaintenance))
                return await AccessDeniedDataTablesJson();

            //prepare model
            var model = await _templateModelFactory.PrepareTopicTemplateListModelAsync(searchModel);

            return Json(model);
        }

        [HttpPost]
        public virtual async Task<IActionResult> TopicTemplateUpdate(TopicTemplateModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageMaintenance))
                return AccessDeniedView();

            if (!ModelState.IsValid)
                return ErrorJson(ModelState.SerializeErrors());

            //try to get a topic template with the specified id
            var template = await _topicTemplateService.GetTopicTemplateByIdAsync(model.Id)
                ?? throw new ArgumentException("No template found with the specified id");

            template = model.ToEntity(template);
            await _topicTemplateService.UpdateTopicTemplateAsync(template);

            return new NullJsonResult();
        }

        [HttpPost]
        public virtual async Task<IActionResult> TopicTemplateAdd(TopicTemplateModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageMaintenance))
                return AccessDeniedView();

            if (!ModelState.IsValid)
                return ErrorJson(ModelState.SerializeErrors());

            var template = new TopicTemplate();
            template = model.ToEntity(template);
            await _topicTemplateService.InsertTopicTemplateAsync(template);

            return Json(new { Result = true });
        }

        [HttpPost]
        public virtual async Task<IActionResult> TopicTemplateDelete(int id)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageMaintenance))
                return AccessDeniedView();

            if ((await _topicTemplateService.GetAllTopicTemplatesAsync()).Count == 1)
                return ErrorJson(await _localizationService.GetResourceAsync("Admin.System.Templates.NotDeleteOnlyOne"));

            //try to get a topic template with the specified id
            var template = await _topicTemplateService.GetTopicTemplateByIdAsync(id)
                ?? throw new ArgumentException("No template found with the specified id");

            await _topicTemplateService.DeleteTopicTemplateAsync(template);

            return new NullJsonResult();
        }

        #endregion

        #endregion
    }
}