[![](https://img.shields.io/nuget/v/soenneker.stripe.extensions.decimals.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.stripe.extensions.decimals/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.stripe.extensions.decimals/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.stripe.extensions.decimals/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.stripe.extensions.decimals.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.stripe.extensions.decimals/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.stripe.extensions.decimals/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.stripe.extensions.decimals/actions/workflows/codeql.yml)

# ![](https://user-images.githubusercontent.com/4441470/224455560-91ed3ee7-f510-4041-a8d2-3fc093025112.png) Soenneker.Stripe.Extensions.Decimals

Estimates card and ACH processing fees, net proceeds, fee-inclusive totals, gross amounts required for a target net, and fee breakdowns using the assumptions in `Soenneker.Stripe.Constants`.

## Installation

```bash
dotnet add package Soenneker.Stripe.Extensions.Decimals
```

## Usage

```csharp
using Soenneker.Stripe.Extensions.Decimals;

decimal amount = 100m;

decimal cardFee = amount.CalculateStripeFee();
(decimal net, decimal fee) = amount.CalculateNetAndFee();
decimal grossForOneHundredNet = 100m.CalculateGrossForNetAmount();

decimal achFee = amount.CalculateStripeFee(ach: true);
```

These methods use two-decimal rounding plus fixed U.S. card and ACH assumptions from `StripeConstants`; they do not query Stripe and cannot account for negotiated pricing, international cards, currency conversion, alternative payment methods, refunds, taxes, or account-specific limits. Treat the results as estimates, not settlement or ledger values.
