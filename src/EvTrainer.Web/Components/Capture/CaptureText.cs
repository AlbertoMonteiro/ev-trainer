using EvTrainer.Web.Domain;

using Microsoft.Extensions.Localization;

namespace EvTrainer.Web.Components.Capture;

/// <summary>Localized texts shared by the capture components.</summary>
public static class CaptureText
{
    public static string Verdict(IStringLocalizer l, Verdict verdict) => verdict switch
    {
        VerdictKnown(var grade) => l[$"Capture.Verdict.{grade}"],
        VerdictUncertain(var low, var high) => l["Capture.Verdict.Uncertain", l[$"Capture.Verdict.{low}"].Value, l[$"Capture.Verdict.{high}"].Value],
        VerdictUnavailable(var reason) => l[$"Capture.Verdict.{reason}"],
    };
}