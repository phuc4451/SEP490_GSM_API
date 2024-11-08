using Firebase.Database;
using System.Numerics;
using System.Reactive.Joins;

namespace Alpha_API.Models
{
	public class RegisterResult
	{
		public GymMembership Membership { get; set; }
		public BoxingMembershipPlan BoxingPlan { get; set; }
		public TrainerRentalPlan RentalPlan { get; set; }
		public RentalOption RentalOption { get; set; }
		public BoxingOption BoxingOption { get; set; }
		public FirebaseObject<string> Registration { get; set; }
		public Payment Payment { get; set; }
		public string Info { get; set; }

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
						TotalPrice = Membership.Price,
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
						TotalPrice = Payment.Amount
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
			//return new
			//{
			//	gymMembership = new
			//	{
			//		Membership.Name,
			//		Membership.DurationMonths,
			//		Membership.SessionCount,
			//		TotalPrice = Membership.Price,
			//	},

			//	trainerRentalPlan = new
			//	{
			//		RentalPlan.TrainerId,
			//		RentalOption.Description,
			//		RentalOption.PricePerPersonPerSession,
			//		RentalOption.PricePerPersonPerMonth,
			//		RentalOption.MemberCount,
			//		TotalPrice = Payment.Amount
			//	},

			//	boxingMembershipPlan = new
			//	{
			//		BoxingPlan.BoxingTrainerId,
			//		BoxingOption.Description,
			//		BoxingOption.TotalPrice,
			//		BoxingOption.Sessions,
			//		BoxingOption.MemberCount,
			//		BoxingOption.Months,
			//	},
			//};

			return null;
		}
	}

}
