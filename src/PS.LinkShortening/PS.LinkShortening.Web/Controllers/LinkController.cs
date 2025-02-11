﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PS.LinkShortening.Domain.Entities;
using PS.LinkShortening.Domain.ViewModels;
using PS.LinkShortening.Service.Interfaces;
using PS.LinkShortening.Service.Models.Settings;
using PS.LinkShortening.Service.Utils;

namespace PS.LinkShortening.Web.Controllers
{
    public class LinkController : Controller
    {
        private readonly AppSettings _config;
        private readonly ILogger<LinkController> _logger;
        private readonly IShortUrlLinkService _shortUrlService;
        private readonly ILinkService _dbLinkService;
        public LinkController(ILinkService dbUnitService, ILogger<LinkController> logger, IShortUrlLinkService shortUrlService, IOptions<AppSettings> config)
        {
            _dbLinkService = dbUnitService;
            _logger = logger;
            _shortUrlService = shortUrlService;
            _config = config.Value;
        }



        [Route("")]
        public async Task<IActionResult> Index()
        {
            var response = await _dbLinkService.GetAllLinksAsync();

            if(response.StatusCode != Domain.Enums.StatusCode.OK)
            {
                return RedirectToAction("Error");
            }

            return View(response.Data);
        }



        [Route("{key}")]
        public async Task<IActionResult> Index(string key)
        {
            Link? item = null;
            try
            {
                var response = await _shortUrlService.GetAsync(key);
                item = response.Data;

                await _dbLinkService.UpdateLinkAsync(item!);

                if (response.StatusCode != Domain.Enums.StatusCode.OK)
                {
                    return RedirectToAction("Error");
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Fetching short url for: {key}");
                return View(new LinkCreateViewModel()
                {
                    ErrorMessage = "Error creating short url."
                });
            }

            if (item == null || item.Expires != null && item.Expires < DateTime.Now)
            {
                return View(new LinkCreateViewModel
                {
                    Text = "Unknown short url"
                });
            }

            return RedirectPermanent(item.LongURL!);
        }






        [HttpGet]
        public IActionResult Create()
        {
            var link = new LinkCreateViewModel();
            return View(link);
        }

        [HttpPost]
        public async Task<IActionResult> Create(LinkCreateViewModel model)
        {
            if (!ModelState.IsValid) { return View(model); }
            model.Url = model.Url?.Trim();



            if (!string.IsNullOrEmpty(model.Url))
            {
                if (!Uri.TryCreate(model.Url, UriKind.Absolute, out var url))
                {
                    model.Text = $"Invalid URL format: {model.Url}";
                    return View(model);
                }

                var link = new Link()
                {
                    LongURL = model.Url!,
                    DateCreated = DateTime.UtcNow
                };

                await CreateShortUrl(link);

                //var key = UrlHelpers.GetRandomKey(_config.Url);
                //link.Key = key;

                //var shortUrl = UrlHelpers.GetShortUrl(_config.Url.OverrideUrl!, Request, link.Key);
                //model.Text = $"New short url created";
                //model.Url = shortUrl;

                //link.Url = shortUrl;

                try
                {
                    await _shortUrlService.CreateAsync(link);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Creating short url for: {url}");
                    model.ErrorMessage = "Error creating short url. Check error logs for details.";
                }

            }

            return RedirectToAction(nameof(Index));
        }


        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var response = await _dbLinkService.GetLinkAsync(id);

            if (response.StatusCode != Domain.Enums.StatusCode.OK)
            {
                return RedirectToAction("Error");
            }

            return View(response.Data);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Link model)
        {
            //if (!ModelState.IsValid) { return View(model); }

            await CreateShortUrl(model);

            model.DateCreated = DateTime.UtcNow;

            await _dbLinkService.UpdateLinkAsync(model);

            return RedirectToAction(nameof(Index));
        }


        [HttpGet]
        public async Task<IActionResult> Delete(Guid id)
        {
            var response = await _dbLinkService.GetLinkAsync(id);

            if (response.StatusCode != Domain.Enums.StatusCode.OK)
            {
                return RedirectToAction("Error");
            }

            return View(response.Data);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(Link model)
        {
            if (model == null) { return View(model); }

            await _dbLinkService.DeleteLinkAsync(model);

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            var response = await _dbLinkService.GetLinkAsync(id);

            if (response.StatusCode != Domain.Enums.StatusCode.OK)
            {
                return RedirectToAction("Error");
            }

            return View(response.Data);
        }


        private Task CreateShortUrl(Link link)
        {
            var key = UrlHelpers.GetRandomKey(_config.Url);
            link.Key = key;

            var shortUrl = UrlHelpers.GetShortUrl(_config.Url.OverrideUrl!, Request, link.Key);

            link.Url = shortUrl;

            return Task.CompletedTask;
        }


    }
}
