using AwesomeAssertions;
using Soenneker.Stripe.Constants;
using Soenneker.Tests.Unit;
using System;
using System.Globalization;
using Xunit;

namespace Soenneker.Stripe.Extensions.Decimals.Tests;

public sealed class StripeDecimalExtensionTests : UnitTest
{
    private static decimal D(string s) => decimal.Parse(s, CultureInfo.InvariantCulture);

    private static decimal CardFee(decimal amt) =>
        amt * StripeConstants.DefaultCardFeePercentage + StripeConstants.DefaultCardFixedFee;

    private static decimal AchFee(decimal amt) =>
        Math.Min(amt * StripeConstants.AchFeePercentage, StripeConstants.AchMaxFee);

    private static decimal R(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);

    // 1. RoundStripeCurrency
    [Theory]
    [InlineData("1.234", "1.23")]
    [InlineData("1.235", "1.24")]
    [InlineData("2.005", "2.01")]
    [InlineData("0.000", "0.00")]
    public void RoundStripeCurrency_produces_expected_values(string rawS, string expectedS)
    {
        decimal raw = D(rawS);
        decimal expected = D(expectedS);
        raw.RoundStripeCurrency().Should().Be(expected);
    }

    // 2. CalculateStripeFee (card)
    [Theory]
    [InlineData("10.00")]
    [InlineData("100.00")]
    [InlineData("250.49")]
    [InlineData("9999.99")]
    public void CalculateStripeFee_card_is_percentage_plus_fixed(string amountS)
    {
        decimal amount = D(amountS);
        decimal expected = R(CardFee(amount));
        amount.CalculateStripeFee().Should().Be(expected);
    }

    // 3. CalculateStripeFee (ACH)
    [Theory]
    [InlineData("400.00")]
    [InlineData("625.00")]
    [InlineData("800.00")]
    public void CalculateStripeFee_ach_obeys_percentage_and_cap(string amountS)
    {
        decimal amount = D(amountS);
        decimal expected = R(AchFee(amount));
        amount.CalculateStripeFee(ach: true).Should().Be(expected);
    }

    // 5. Gross ↔ Net (ACH, incl. cap)
    [Theory]
    [InlineData("50.00")]
    [InlineData("624.99")]
    [InlineData("1000.00")]
    public void Gross_for_net_and_back_are_close_enough_for_ach(string desiredNetS)
    {
        decimal desiredNet = D(desiredNetS);
        decimal gross = desiredNet.CalculateGrossForNetAmount(ach: true);
        decimal netBack = gross.CalculateNetAfterStripeFee(ach: true);
        netBack.Should().BeApproximately(desiredNet, 0.05m);
    }

    // 6. Fee-breakdown (card)
    [Theory]
    [InlineData("150.00")]
    [InlineData("7.99")]
    public void Fee_breakdown_card_parts_sum_to_total(string amountS)
    {
        decimal amount = D(amountS);
        (decimal total, decimal percentage, decimal fixedPart) = amount.CalculateStripeFeeBreakdown();
        total.Should().Be(percentage + fixedPart);
        total.Should().Be(amount.CalculateStripeFee());
    }

    // 7. Fee-breakdown (ACH)
    [Theory]
    [InlineData("450.00")]
    [InlineData("900.00")]
    public void Fee_breakdown_ach_parts_sum_to_total(string amountS)
    {
        decimal amount = D(amountS);
        (decimal total, decimal percentage, decimal fixedPart) = amount.CalculateStripeFeeBreakdown(ach: true);
        fixedPart.Should().Be(0m);
        total.Should().Be(percentage);
        total.Should().Be(amount.CalculateStripeFee(ach: true));
    }

    // 8. Edge: invalid card payment amounts throw
    [Theory]
    [InlineData("0.00")]
    [InlineData("0.01")]
    public void Too_small_card_payments_throw(string amountS)
    {
        decimal amount = D(amountS);
        Action act = () => amount.CalculateStripeFee();
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // 9. Edge: valid tiny ACH payments do not throw
    [Theory]
    [InlineData("0.00")]
    [InlineData("0.01")]
    [InlineData("1.00")]
    public void Tiny_ach_payments_are_accepted(string amountS)
    {
        decimal amount = D(amountS);
        Action act = () => amount.CalculateStripeFee(ach: true);
        act.Should().NotThrow();
    }
}
