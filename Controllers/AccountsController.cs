using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Extensions.CognitoAuthentication;
using Amazon.AspNetCore.Identity.Cognito;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebAdvert.Web.Models.Accounts;

namespace WebAdvert.Web.Controllers
{
    public class AccountsController: Controller
    {
        private readonly SignInManager<CognitoUser> signinManager;
        private readonly UserManager<CognitoUser> userManager;
        private readonly CognitoUserPool userPool;

        public AccountsController(SignInManager<CognitoUser> signinManager,
            UserManager<CognitoUser> userManager, CognitoUserPool userPool)
        {
            this.signinManager = signinManager;
            this.userManager = userManager;
            this.userPool = userPool;
        }

        public async Task<IActionResult> Signup()
        {
            var model = new SignUpModel();

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Signup(SignUpModel model)
        {
            var user = this.userPool.GetUser(model.Email);

            if (user?.Status != null)
            {
                ModelState.AddModelError("UserExists", "User with this email already exists");
                return View(model);
            }

            user.Attributes.Add(CognitoAttribute.Email.AttributeName, model.Email);
            var createdUser = await this.userManager.CreateAsync(user, model.Password);
            if (createdUser.Succeeded)
            {
                return RedirectToAction("Confirm", routeValues: model);
            }
            else
            {
                foreach (var item in createdUser.Errors)
                {
                    ModelState.AddModelError(item.Code, item.Description);
                }
            }
            return View(model);
        }

        public async Task<IActionResult> Confirm(ConfirmModel confirmModel)
        {
            return View(confirmModel);
        }

        [HttpPost]
        [ActionName("Confirm")]
        public async Task<IActionResult> Confirm_Post(ConfirmModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await this.userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    ModelState.AddModelError("NotFound", "A user with the given email address was not found");
                    return View(model);
                }

                var result = await (this.userManager as CognitoUserManager<CognitoUser>).ConfirmSignUpAsync(user, model.Code, true);
                if (result.Succeeded)
                {
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    foreach (var item in result.Errors)
                    {
                        ModelState.AddModelError(item.Code, item.Description);
                    }
                    return View(model);

                }
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Login(LoginModel model)
        {
            return View(model);
        }

        [HttpPost]
        [ActionName("Login")]
        public async Task<IActionResult> Login_Post(LoginModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await this.userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    ModelState.AddModelError("NotFound", "A user with the given email address was not found");
                    return RedirectToAction("Signup");
                }
                var result = await (this.signinManager as CognitoSignInManager<CognitoUser>).PasswordSignInAsync(user.UserID, model.Password,
                    model.RememberMe, false);
                //var res = await this.signinManager
                //    .PasswordSignInAsync(model.Email, model.Password, model.RememberMe, false);
                if (result.Succeeded)
                {
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ModelState.AddModelError("LoginError", "Email and password do not match");
                }
            }

            return View("Login",model);
        }

        [HttpGet]
        public async Task<IActionResult> Logout(LoginModel model)
        {
            await (this.signinManager as CognitoSignInManager<CognitoUser>).SignOutAsync();
            return RedirectToAction("Index", "Home");
        }


    }
}