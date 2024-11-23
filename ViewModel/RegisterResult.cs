using Alpha_API.Models;
using Firebase.Database;
using System.Numerics;
using System.Reactive.Joins;

namespace Alpha_API.ViewModel
{
    public class RegisterResult
    {
        public GymMembership Membership { get; set; }
        public BoxingMembershipPlan BoxingPlan { get; set; }
        public TrainerRentalPlan RentalPlan { get; set; }
        public RentalOption RentalOption { get; set; }
        public BoxingOption BoxingOption { get; set; }
        public string RegistrationType { get; set; }
        public string RegistrationId { get; set; }
        public decimal MoneyToPay { get; set; }
        public string TransactionContent { get; set; }

        public object ToQrDetails()
        {
            if (Membership != null)
            {
                return new
                {
                    gymMembership = new
                    {
                        Membership.Name,
                        Membership.DurationMonths,
                        Membership.SessionCount,
                        TotalPrice = MoneyToPay,
                    },
                };
            }

            if (RentalPlan != null)
            {
                return new
                {
                    trainerRentalPlan = new
                    {
                        RentalPlan.TrainerId,
                        RentalOption.Description,
                        RentalOption.PricePerPersonPerSession,
                        RentalOption.PricePerPersonPerMonth,
                        RentalOption.MemberCount,
                        TotalPrice = MoneyToPay
                    },
                };
            }

            if (BoxingPlan != null)
            {
                return new
                {
                    boxingMembershipPlan = new
                    {
                        BoxingPlan.BoxingTrainerId,
                        BoxingOption.Description,
                        BoxingOption.TotalPrice,
                        BoxingOption.Sessions,
                        BoxingOption.MemberCount,
                        BoxingOption.Months,
                    },
                };
            }
            return null;
        }
    }

}
