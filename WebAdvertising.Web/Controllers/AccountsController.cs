using Amazon.AspNetCore.Identity.Cognito;
using Amazon.Extensions.CognitoAuthentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebAdvertising.Web.Models.Accounts;

namespace WebAdvertising.Web.Controllers
{
    public class AccountsController : Controller
    {
        private SignInManager<CognitoUser> _signInManager;
        private UserManager<CognitoUser> _userManager;
        private CognitoUserPool _pool;

        public AccountsController(
            SignInManager<CognitoUser> signInManager,
            UserManager<CognitoUser> userManager, 
            CognitoUserPool pool)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _pool = pool;
        }

        public IActionResult SignUp()
        {
            var model = new SignupViewModel();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SignUp(SignupViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = _pool.GetUser(model.Email);
                if (user.Status != null)
                {
                    ModelState.AddModelError("UserExists", "User with this e-mail already exists");
                    return View(model);
                }

                user.Attributes.Add(CognitoAttribute.Email.ToString(), model.Email);

                var createdUser = await _userManager.CreateAsync(user, model.Password);

                if (createdUser.Succeeded)
                {
                    return RedirectToAction("EmailConfirmation");
                }
                else
                {
                    foreach (var error in createdUser.Errors)
                    {
                        ModelState.AddModelError(error.Code, error.Description);
                    }
                }
            }

            return View();
        }

        [HttpGet]
        [ActionName("EmailConfirmation")]
        public IActionResult EmailConfirmation()
        {
            return View(new EmailConfirmationViewModel());
        }

        [HttpPost]
        [ActionName("EmailConfirmationPost")]
        public async Task<IActionResult> EmailConfirmationPost(EmailConfirmationViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);

                if(user == null)
                {
                    ModelState.AddModelError("Not Found", "A user with the given email address was not found");
                    return View(model);
                }

                var result = await (_userManager as CognitoUserManager<CognitoUser>).ConfirmSignUpAsync(user, model.Code, true);

                if (result.Succeeded)
                {
                    return RedirectToAction("EmailConfirmationSucceeded", new { confirmedEmail = model.Email });
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(error.Code, error.Description);
                    }
                }
            }

            return View(model);
        }

        public IActionResult EmailConfirmationSucceeded(string confirmedEmail)
        {
            var model = new EmailConfirmationViewModel { Email = confirmedEmail };
            return View(model);
        }

        [HttpGet]
        [ActionName("Login")]
        public IActionResult Login()
        {
            return View("SignIn", new SignInViewModel());
        }

        [HttpPost]
        [ActionName("LoginPost")]
        public async Task<IActionResult> LoginPost(SignInViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, false);

                if (result.Succeeded)
                {
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ModelState.AddModelError("Login Error", "Email or Password do not match");
                }
            }

            return View("SignIn", model);
        }

        public IActionResult LoginSucceeded(string confirmedEmail)
        {
            var model = new EmailConfirmationViewModel { Email = confirmedEmail };
            return View(model);
        }
    }
}
