using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using MyMultiTurnBot;

namespace MyMultiTurnBot.Dialogs
{
    public class UserProfileDialog : ComponentDialog
    {
        private readonly IStatePropertyAccessor<UserProfile> _userProfileAccessor;
        public class EmailPrompt : TextPrompt
        {
            public EmailPrompt(string dialogId, PromptValidator<string> validator = null)
                : base(dialogId, validator)
            {
            }
        }

        public class PhNoPrompt : NumberPrompt<long>
        {
            public PhNoPrompt(string dialogId, PromptValidator<long> validator = null)
                : base(dialogId, validator)
            {
            }
        }

        // Define a custom prompt for salary
        public class SalaryPrompt : NumberPrompt<long>
        {
            public SalaryPrompt(string dialogId, PromptValidator<long> validator = null)
                : base(dialogId, validator)
            {
            }
        }

        public UserProfileDialog(UserState userState)
            : base(nameof(UserProfileDialog))
        {
            _userProfileAccessor = userState.CreateProperty<UserProfile>("UserProfile");

            // This array defines how the Waterfall will execute.
            var waterfallSteps = new WaterfallStep[]
            {
                NameStepAsync,
                EmailStepAsync,
                PhoneStepAsync,
                SalaryStepAsync,
                ConfirmStepAsync,
                SummaryStepAsync,
            };

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt), ValidateYesNo));
            AddDialog(new EmailPrompt(nameof(EmailPrompt), EmailPromptValidatorAsync));
            AddDialog(new PhNoPrompt(nameof(PhNoPrompt), PhNoPromptValidatorAsync));
            AddDialog(new SalaryPrompt(nameof(SalaryPrompt), SalaryPromptValidatorAsync));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> NameStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync(
                nameof(TextPrompt),
                new PromptOptions { Prompt = MessageFactory.Text("Please enter your name.") },
                cancellationToken);
        }

        private async Task<DialogTurnResult> EmailStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["name"] = (string)stepContext.Result;

            return await stepContext.PromptAsync(
                nameof(EmailPrompt),
                new PromptOptions { Prompt = MessageFactory.Text("Please enter your email address."),
                    RetryPrompt = MessageFactory.Text("The email you entered is not valid. Try again."),
                },
                cancellationToken);
        }

        private async Task<DialogTurnResult> PhoneStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["email"] = (string)stepContext.Result;

            return await stepContext.PromptAsync(
                nameof(PhNoPrompt),
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Please enter your phone number."),
                    RetryPrompt = MessageFactory.Text("The phone number must be a valid Indian phone number. Try again."),
                },
                cancellationToken);
        }

        private async Task<DialogTurnResult> SalaryStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["phone"] = (long)stepContext.Result;

            return await stepContext.PromptAsync(
                nameof(SalaryPrompt),
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Please enter your monthly salary."),
                    RetryPrompt = MessageFactory.Text("The salary should be greater than zero. Try again."),
                },
                cancellationToken);
        }

        private async Task<DialogTurnResult> ConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["salary"] = (long)stepContext.Result;

            var userProfile = new UserProfile
            {
                Name = (string)stepContext.Values["name"],
                Email = (string)stepContext.Values["email"],
                Phone = (long)stepContext.Values["phone"],
                Salary = (long)stepContext.Values["salary"],
            };

            await stepContext.Context.SendActivityAsync(
                MessageFactory.Text($"Please confirm your information:\n\nName: {userProfile.Name}\nEmail: {userProfile.Email}\nPhone: {userProfile.Phone}\nSalary: {userProfile.Salary}"),
                cancellationToken);

            return await stepContext.PromptAsync(
                nameof(ConfirmPrompt),
                new PromptOptions { Prompt = MessageFactory.Text("Is this information correct?") },
                cancellationToken);
        }

        private async Task<DialogTurnResult> SummaryStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var isConfirmed = (bool)stepContext.Result;

            if (isConfirmed)
            {
                var userProfile = new UserProfile
                {
                    Name = (string)stepContext.Values["name"],
                    Email = (string)stepContext.Values["email"],
                    Phone = (long)stepContext.Values["phone"],
                    Salary = (long)stepContext.Values["salary"],
                };

                // Save the user's profile in user state
                await _userProfileAccessor.SetAsync(stepContext.Context, userProfile, cancellationToken);
                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Following information has been saved:\n\nName: {userProfile.Name}\nEmail: {userProfile.Email}\nPhone: {userProfile.Phone}\nSalary: {userProfile.Salary}"), 
                    cancellationToken);
            }
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Your information has not been saved."), cancellationToken);
            }

            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }

        private Task<bool> ValidateYesNo(PromptValidatorContext<bool> promptContext, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        private Task<bool> EmailPromptValidatorAsync(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            //return Task.FromResult(true);
            string emailPattern = @"^\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*$";
            bool isValidEmail = Regex.IsMatch(promptContext.Recognized.Value, emailPattern);

            return Task.FromResult(isValidEmail);
        }

        private Task<bool> PhNoPromptValidatorAsync(PromptValidatorContext<long> promptContext, CancellationToken cancellationToken)
        {
            // Validate if the entered number is a valid Indian phone number
            return Task.FromResult(promptContext.Recognized.Succeeded && promptContext.Recognized.Value > 6000000000 && promptContext.Recognized.Value < 9999999999);
        }

        private static Task<bool> SalaryPromptValidatorAsync(PromptValidatorContext<long> promptContext, CancellationToken cancellationToken)
        {
            // Validate if the salary is greater than zero
            return Task.FromResult(promptContext.Recognized.Succeeded && promptContext.Recognized.Value > 0);
        }
    }
}
