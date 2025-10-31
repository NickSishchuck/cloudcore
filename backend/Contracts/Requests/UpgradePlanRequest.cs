using CloudCore.Common.Models;

namespace CloudCore.Contracts.Requests
{
    public record UpgradePlanRequest(SubscriptionPlan? NewPlan);

}
