using System.Linq;
using System.Threading.Tasks;
using IdentityServer4;
using IdentityServer4.Models;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Cik.Services.Auth.AuthService.Features.Consent
{
    public class ConsentController : Controller
    {
        private readonly IClientStore _clientStore;
        private readonly ConsentInteraction _consentInteraction;
        private readonly ILocalizationService _localization;
        private readonly ILogger<ConsentController> _logger;
        private readonly IScopeStore _scopeStore;

        public ConsentController(
            ILogger<ConsentController> logger,
            ConsentInteraction consentInteraction,
            IClientStore clientStore,
            IScopeStore scopeStore,
            ILocalizationService localization)
        {
            _logger = logger;
            _consentInteraction = consentInteraction;
            _clientStore = clientStore;
            _scopeStore = scopeStore;
            _localization = localization;
        }

        [HttpGet(Constants.RoutePaths.Consent, Name = "Consent")]
        public async Task<IActionResult> Index(string id)
        {
            var vm = await BuildViewModelAsync(id);
            if (vm != null)
            {
                return View("Index", vm);
            }

            return View("Error");
        }

        [HttpPost(Constants.RoutePaths.Consent)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(string button, string id, ConsentInputModel model)
        {
            if (button == "no")
            {
                return new ConsentResult(id, ConsentResponse.Denied);
            }
            if (button == "yes" && model != null)
            {
                if (model.ScopesConsented != null && model.ScopesConsented.Any())
                {
                    return new ConsentResult(id, new ConsentResponse
                    {
                        RememberConsent = model.RememberConsent,
                        ScopesConsented = model.ScopesConsented
                    });
                }
                ModelState.AddModelError("", "You must pick at least one permission.");
            }
            else
            {
                ModelState.AddModelError("", "Invalid Selection");
            }

            var vm = await BuildViewModelAsync(id, model);
            if (vm != null)
            {
                return View("Index", vm);
            }

            return View("Error");
        }

        private async Task<IActionResult> BuildConsentResponse(string id, string[] scopesConsented, bool rememberConsent)
        {
            if (id != null)
            {
                var request = await _consentInteraction.GetRequestAsync(id);
            }

            return View("Error");
        }

        private async Task<ConsentViewModel> BuildViewModelAsync(string id, ConsentInputModel model = null)
        {
            if (id != null)
            {
                var request = await _consentInteraction.GetRequestAsync(id);
                if (request != null)
                {
                    var client = await _clientStore.FindClientByIdAsync(request.ClientId);
                    if (client != null)
                    {
                        var scopes = await _scopeStore.FindScopesAsync(request.ScopesRequested);
                        if (scopes != null && scopes.Any())
                        {
                            return new ConsentViewModel(model, id, request, client, scopes, _localization);
                        }
                        _logger.LogError("No scopes matching: {0}",
                            request.ScopesRequested.Aggregate((x, y) => x + ", " + y));
                    }
                    else
                    {
                        _logger.LogError("Invalid client id: {0}", request.ClientId);
                    }
                }
                else
                {
                    _logger.LogError("No consent request matching id: {0}", id);
                }
            }
            else
            {
                _logger.LogError("No id passed");
            }

            return null;
        }
    }
}