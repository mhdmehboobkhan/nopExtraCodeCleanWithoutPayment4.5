﻿using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Services.Common;

namespace Nop.Web.Controllers
{
    public partial class PreferencesController : BasePublicController
    {
        #region Fields

        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IWorkContext _workContext;

        #endregion

        #region Ctor

        public PreferencesController(IGenericAttributeService genericAttributeService,
            IWorkContext workContext)
        {
            _genericAttributeService = genericAttributeService;
            _workContext = workContext;
        }

        #endregion

        #region Methods

        [HttpPost]
        /// <returns>A task that represents the asynchronous operation</returns>
        public virtual async Task<IActionResult> SavePreference(string name, bool value)
        {
            //permission validation is not required here
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            await _genericAttributeService.SaveAttributeAsync(await _workContext.GetCurrentCustomerAsync(), name, value);

            return Json(new
            {
                Result = true
            });
        }

        #endregion
    }
}