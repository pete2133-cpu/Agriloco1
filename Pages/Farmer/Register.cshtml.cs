using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using Agriloco.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Agriloco.Api.Pages.Farmer
{
    public class RegisterModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public RegisterModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [BindProperty]
        public RegisterInput Input { get; set; } = new();

        public string? Message { get; set; }

        public class RegisterInput
        {
            [Required]
            public string FarmName { get; set; } = "";

            [Required]
            public string Address { get; set; } = "";

            public string? Phone { get; set; }

            [Required, EmailAddress]
            public string Email { get; set; } = "";

            [Required]
            public string Username { get; set; } = "";

            [Required]
            public string Password { get; set; } = "";
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            try
            {
                var client = _httpClientFactory.CreateClient("AgrilocoApiClient");

                var payload = new MemberRegisterIn
                {
                    FarmName = Input.FarmName,
                    Address = Input.Address,
                    Phone = string.IsNullOrWhiteSpace(Input.Phone) ? null : Input.Phone.Trim(),
                    Email = Input.Email,
                    Username = Input.Username,
                    Password = Input.Password
                };

                var response = await client.PostAsJsonAsync("/api/member/register", payload);

                if (response.IsSuccessStatusCode)
                {
                    // Don't assume the response has Id/MemberId/etc.
                    // Just show success and clear the form.
                    Message = $"Account created successfully for username: {Input.Username}";

                    ModelState.Clear();
                    Input = new RegisterInput();
                    return Page();
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Message = $"Error creating account: {error}";
                    return Page();
                }
            }
            catch (Exception ex)
            {
                Message = $"Error creating account: {ex.Message}";
                return Page();
            }
        }
    }
}