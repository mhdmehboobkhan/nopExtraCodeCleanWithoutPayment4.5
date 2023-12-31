﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Media;
using Nop.Core.Infrastructure;
using Nop.Services.Customers;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Media;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Web.Controllers
{
    public partial class DownloadController : BasePublicController
    {
        private readonly CustomerSettings _customerSettings;
        private readonly IDownloadService _downloadService;
        private readonly ILocalizationService _localizationService;
        private readonly IWorkContext _workContext;
        private readonly ICustomerService _customerService;
        private readonly INopFileProvider _fileProvider;
        private readonly ILogger _logger;

        public DownloadController(CustomerSettings customerSettings,
            IDownloadService downloadService,
            ILocalizationService localizationService,
            IWorkContext workContext,
            ICustomerService customerService,
            INopFileProvider fileProvider,
            ILogger logger)
        {
            _customerSettings = customerSettings;
            _downloadService = downloadService;
            _localizationService = localizationService;
            _workContext = workContext;
            _customerService = customerService;
            _fileProvider = fileProvider;
            _logger = logger;
        }

        [HttpPost]
        //do not validate request token (XSRF)
        [IgnoreAntiforgeryToken]
        public virtual async Task<IActionResult> AsyncUpload()
        {
            var customer = await _workContext.GetCurrentCustomerAsync();
            if (!await _customerService.IsRegisteredAsync(customer))
                return Json(new { success = false, message = "Customer cannot be loaded" });

            var httpPostedFile = Request.Form.Files.FirstOrDefault();
            if (httpPostedFile == null)
            {
                return Json(new
                {
                    success = false,
                    message = "No file uploaded"
                });
            }

            var fileBinary = await _downloadService.GetDownloadBitsAsync(httpPostedFile);

            var qqFileNameParameter = "qqfilename";
            var fileName = httpPostedFile.FileName;
            if (string.IsNullOrEmpty(fileName) && Request.Form.ContainsKey(qqFileNameParameter))
                fileName = Request.Form[qqFileNameParameter].ToString();
            //remove path (passed in IE)
            fileName = _fileProvider.GetFileName(fileName);

            var contentType = httpPostedFile.ContentType;

            var fileExtension = _fileProvider.GetFileExtension(fileName);
            if (!string.IsNullOrEmpty(fileExtension))
                fileExtension = fileExtension.ToLowerInvariant();

            var download = new Download
            {
                DownloadGuid = Guid.NewGuid(),
                UseDownloadUrl = false,
                DownloadUrl = string.Empty,
                DownloadBinary = new byte[0],
                ContentType = contentType,
                //we store filename without extension for downloads
                Filename = _fileProvider.GetFileNameWithoutExtension(fileName),
                Extension = fileExtension,
                IsNew = true
            };

            try
            {
                await _downloadService.InsertDownloadAsync(download);

                var uploadPath = _fileProvider.MapPath(string.Format(NopMediaDefaults.DefaultNotesFilesFolder, download.DownloadGuid));
                if (!_fileProvider.DirectoryExists(uploadPath))
                    _fileProvider.CreateDirectory(uploadPath);

                uploadPath = _fileProvider.Combine(uploadPath, fileName);

                //delete the file if already exist
                if (_fileProvider.FileExists(uploadPath))
                    _fileProvider.DeleteFile(uploadPath);

                await _fileProvider.WriteAllBytesAsync(uploadPath, fileBinary);

                //when returning JSON the mime-type must be set to text/plain
                //otherwise some browsers will pop-up a "Save As" dialog.
                return Json(new
                {
                    success = true,
                    downloadId = download.Id,
                    downloadUrl = Url.Action("DownloadFile", new { downloadGuid = download.DownloadGuid })
                });
            }
            catch (Exception exc)
            {
                await _logger.ErrorAsync(exc.Message, exc, await _workContext.GetCurrentCustomerAsync());

                return Json(new
                {
                    success = false,
                    message = "File cannot be saved"
                });
            }
        }

        public virtual async Task<IActionResult> DownloadFile(Guid downloadGuid)
        {
            var download = await _downloadService.GetDownloadByGuidAsync(downloadGuid);
            if (download == null)
                return Content("No download record found with the specified id");

            //A warning (SCS0027 - Open Redirect) from the "Security Code Scan" analyzer may appear at this point. 
            //In this case, it is not relevant. Url may not be local.
            if (download.UseDownloadUrl)
                return new RedirectResult(download.DownloadUrl);

            //use stored data
            if (download.DownloadBinary == null)
                return Content($"Download data is not available any more. Download GD={download.Id}");

            var fileName = (!string.IsNullOrWhiteSpace(download.Filename) ? download.Filename : download.Id.ToString()) + download.Extension;
            var fileBinary = download.DownloadBinary;
            if (download.DownloadBinary.Length == 0)
            {
                var filePath = _fileProvider.MapPath(string.Format(NopMediaDefaults.DefaultNotesFilesFolder, download.DownloadGuid));
                fileName = download.Filename + download.Extension;
                filePath = _fileProvider.Combine(filePath, fileName);

                if (!_fileProvider.FileExists(filePath))
                    return Content($"Download data is not available any more. Download GD={download.Id}");

                fileBinary = await _fileProvider.ReadAllBytesAsync(filePath);
            }

            var contentType = !string.IsNullOrWhiteSpace(download.ContentType)
                ? download.ContentType
                : MimeTypes.ApplicationOctetStream;
            return new FileContentResult(fileBinary, contentType)
            {
                FileDownloadName = fileName
            };
        }

        public virtual async Task<IActionResult> GetFileUpload(Guid downloadId)
        {
            var download = await _downloadService.GetDownloadByGuidAsync(downloadId);
            if (download == null)
                return Content("Download is not available any more.");

            //A warning (SCS0027 - Open Redirect) from the "Security Code Scan" analyzer may appear at this point. 
            //In this case, it is not relevant. Url may not be local.
            if (download.UseDownloadUrl)
                return new RedirectResult(download.DownloadUrl);

            //binary download
            if (download.DownloadBinary == null)
                return Content("Download data is not available any more.");

            var fileName = (!string.IsNullOrWhiteSpace(download.Filename) ? download.Filename : download.Id.ToString()) + download.Extension;
            var fileBinary = download.DownloadBinary;
            if (download.DownloadBinary.Length == 0)
            {
                var filePath = _fileProvider.MapPath(string.Format(NopMediaDefaults.DefaultNotesFilesFolder, download.DownloadGuid));
                fileName = download.Filename + download.Extension;
                filePath = _fileProvider.Combine(filePath, fileName);

                if (!_fileProvider.FileExists(filePath))
                    return Content($"Download data is not available any more. Download GD={download.Id}");

                fileBinary = await _fileProvider.ReadAllBytesAsync(filePath);
            }

            var contentType = !string.IsNullOrWhiteSpace(download.ContentType)
                ? download.ContentType
                : MimeTypes.ApplicationOctetStream;
            return new FileContentResult(fileBinary, contentType)
            {
                FileDownloadName = fileName
            };
        }

        //ignore SEO friendly URLs checks
        [CheckLanguageSeoCode(true)]
        public virtual async Task<IActionResult> GetCustomerNoteFile(int customerNoteid)
        {
            var customerNote = await _customerService.GetCustomerNoteByIdAsync(customerNoteid);
            if (customerNote == null)
                return InvokeHttp404();

            var customerObj = await _customerService.GetCustomerByIdAsync(customerNote.CustomerId);
            var customer = await _workContext.GetCurrentCustomerAsync();
            if (customer == null || customerObj.Id != customer.Id)
                return Challenge();

            var download = await _downloadService.GetDownloadByIdAsync(customerNote.DownloadId);
            if (download == null)
                return Content("Download is not available any more.");

            //A warning (SCS0027 - Open Redirect) from the "Security Code Scan" analyzer may appear at this point. 
            //In this case, it is not relevant. Url may not be local.
            if (download.UseDownloadUrl)
                return new RedirectResult(download.DownloadUrl);

            //binary download
            if (download.DownloadBinary == null)
                return Content("Download data is not available any more.");

            var fileName = (!string.IsNullOrWhiteSpace(download.Filename) ? download.Filename : download.Id.ToString()) + download.Extension;
            var fileBinary = download.DownloadBinary;
            if (download.DownloadBinary.Length == 0)
            {
                var filePath = _fileProvider.MapPath(string.Format(NopMediaDefaults.DefaultNotesFilesFolder, download.DownloadGuid));
                fileName = download.Filename + download.Extension;
                filePath = _fileProvider.Combine(filePath, fileName);

                if (!_fileProvider.FileExists(filePath))
                    return Content($"Download data is not available any more. Download GD={download.Id}");

                fileBinary = await _fileProvider.ReadAllBytesAsync(filePath);
            }

            var contentType = !string.IsNullOrWhiteSpace(download.ContentType)
                ? download.ContentType
                : MimeTypes.ApplicationOctetStream;
            return new FileContentResult(fileBinary, contentType)
            {
                FileDownloadName = fileName
            };
        }
    }
}