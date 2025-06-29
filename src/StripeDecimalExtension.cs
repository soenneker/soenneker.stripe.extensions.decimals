using System;
using Soenneker.Extensions.Decimal;
using Soenneker.Stripe.Constants;

namespace Soenneker.Stripe.Extensions.Decimals;

/// <summary>
/// Provides extension methods for calculating and rounding Stripe-related fees using <see cref="decimal"/> values.
/// </summary>
public static class StripeDecimalExtension
{
    private static void EnsureCardInRange(decimal value, string paramName)
    {
        if (value is < StripeConstants.CardMinAmount or > StripeConstants.CardMaxAmount)
            throw new ArgumentOutOfRangeException(paramName, value,
                $"Card charge must be between {StripeConstants.CardMinAmount:C} and {StripeConstants.CardMaxAmount:C}.");
    }

    private static void EnsureAchInRange(decimal value, string paramName)
    {
        if (value is < 0m or > StripeConstants.AchMaximumDebitAmount)
            throw new ArgumentOutOfRangeException(paramName, value, $"ACH debit must be between $0.00 and {StripeConstants.AchMaximumDebitAmount:C}.");
    }

    /// <summary>
    /// Calculates the Stripe processing fee for the given transaction amount.
    /// </summary>
    /// <param name="amount">The amount to calculate the fee for.</param>
    /// <param name="ach">Set to <c>true</c> to calculate using ACH fee structure; otherwise, uses card fee structure.</param>
    /// <returns>The total Stripe fee, rounded to two decimal places.</returns>
    public static decimal CalculateStripeFee(this decimal amount, bool ach = false)
    {
        if (ach)
            EnsureAchInRange(amount, nameof(amount));
        else
            EnsureCardInRange(amount, nameof(amount));

        decimal roundedAmount = amount.ToCurrency(); // Even rounding

        decimal fee = ach
            ? Math.Min(roundedAmount * StripeConstants.AchFeePercentage, StripeConstants.AchMaxFee)
            : roundedAmount * StripeConstants.DefaultCardFeePercentage + StripeConstants.DefaultCardFixedFee;

        return fee.RoundStripeCurrency();
    }

    /// <summary>
    /// Calculates the amount remaining after Stripe fees are deducted from the given transaction amount.
    /// </summary>
    /// <param name="amount">The gross amount before fees.</param>
    /// <param name="ach">Set to <c>true</c> for ACH; otherwise, uses card fee structure.</param>
    /// <returns>The net amount received after deducting Stripe fees.</returns>
    public static decimal CalculateNetAfterStripeFee(this decimal amount, bool ach = false)
    {
        amount.CalculateStripeFee(ach); // triggers validation
        decimal fee = amount.CalculateStripeFee(ach);
        return (amount - fee).RoundStripeCurrency();
    }

    /// <summary>
    /// Rounds the decimal value using Stripe’s rounding rules (two decimal places, midpoint away from zero).
    /// </summary>
    /// <param name="value">The value to round.</param>
    /// <returns>The rounded value.</returns>
    public static decimal RoundStripeCurrency(this decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);

    /// <summary>
    /// Calculates the gross charge required to achieve a specific net amount after Stripe fees are deducted.
    /// </summary>
    /// <param name="netAmount">The desired net amount after fees.</param>
    /// <param name="ach">Set to <c>true</c> to use ACH fee rules; otherwise, uses card fee rules.</param>
    /// <returns>The gross amount that should be charged to achieve the desired net amount.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the resulting gross amount is outside of Stripe's allowed range.</exception>
    public static decimal CalculateGrossForNetAmount(this decimal netAmount, bool ach = false)
    {
        if (netAmount <= 0)
            throw new ArgumentOutOfRangeException(nameof(netAmount), netAmount, "Net amount must be positive.");

        decimal percentage = ach ? StripeConstants.AchFeePercentage : StripeConstants.DefaultCardFeePercentage;
        decimal fixedFee = ach ? 0m : StripeConstants.DefaultCardFixedFee;

        decimal gross;

        if (ach)
        {
            decimal percentFee = netAmount * percentage;

            gross = percentFee >= StripeConstants.AchMaxFee
                ? netAmount + StripeConstants.AchMaxFee
                : netAmount / (1 - percentage);
        }
        else
        {
            gross = (netAmount + fixedFee) / (1 - percentage);
        }

        gross = gross.RoundStripeCurrency();

        if (ach)
            EnsureAchInRange(gross, nameof(netAmount));
        else
            EnsureCardInRange(gross, nameof(netAmount));

        return gross;
    }

    /// <summary>
    /// Adds the Stripe processing fee to the base amount.
    /// </summary>
    /// <param name="baseAmount">The original transaction amount before fees.</param>
    /// <param name="ach">Set to <c>true</c> for ACH; otherwise, uses card fee rules.</param>
    /// <returns>The total amount including Stripe fees.</returns>
    public static decimal AddStripeFee(this decimal baseAmount, bool ach = false)
    {
        decimal fee = baseAmount.CalculateStripeFee(ach);
        return (baseAmount + fee).RoundStripeCurrency();
    }

    /// <summary>
    /// Calculates the total amount including Stripe fees and returns both the total and the fee separately.
    /// </summary>
    /// <param name="baseAmount">The original transaction amount before fees.</param>
    /// <param name="ach">Set to <c>true</c> for ACH; otherwise, uses card fee rules.</param>
    /// <returns>A tuple containing the total amount and the Stripe fee.</returns>
    public static (decimal Total, decimal Fee) AddStripeFeeWithBreakdown(this decimal baseAmount, bool ach = false)
    {
        decimal fee = baseAmount.CalculateStripeFee(ach);
        decimal total = (baseAmount + fee).RoundStripeCurrency();
        return (total, fee);
    }

    /// <summary>
    /// Calculates and returns both the net amount after fees and the Stripe fee itself.
    /// </summary>
    /// <param name="amount">The gross transaction amount.</param>
    /// <param name="ach">Set to <c>true</c> for ACH; otherwise, uses card fee rules.</param>
    /// <returns>A tuple containing the net amount and the Stripe fee.</returns>
    public static (decimal Net, decimal Fee) CalculateNetAndFee(this decimal amount, bool ach = false)
    {
        decimal fee = amount.CalculateStripeFee(ach);
        decimal net = (amount - fee).RoundStripeCurrency();
        return (net, fee);
    }

    /// <summary>
    /// Breaks down the Stripe fee into its component parts: percentage-based and fixed components.
    /// </summary>
    /// <param name="amount">The transaction amount to analyze.</param>
    /// <param name="ach">Set to <c>true</c> to use ACH fee structure; otherwise, uses card fee structure.</param>
    /// <returns>A tuple containing the total fee, percentage-based portion, and fixed portion.</returns>
    public static (decimal Total, decimal PercentagePortion, decimal FixedPortion) CalculateStripeFeeBreakdown(this decimal amount, bool ach = false)
    {
        amount.CalculateStripeFee(ach); // validates range

        decimal roundedAmount = amount.ToCurrency();

        if (ach)
        {
            decimal percentageFee = roundedAmount * StripeConstants.AchFeePercentage;
            decimal cappedFee = Math.Min(percentageFee, StripeConstants.AchMaxFee);
            return (cappedFee.RoundStripeCurrency(), cappedFee.RoundStripeCurrency(), 0m);
        }

        decimal percentagePortion = roundedAmount * StripeConstants.DefaultCardFeePercentage;
        decimal fixedPortion = StripeConstants.DefaultCardFixedFee;
        decimal total = (percentagePortion + fixedPortion).RoundStripeCurrency();

        return (total, percentagePortion.RoundStripeCurrency(), fixedPortion);
    }
}
