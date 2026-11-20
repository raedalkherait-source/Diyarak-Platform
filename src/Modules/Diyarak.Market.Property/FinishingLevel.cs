namespace Diyarak.Market.Property;

/// <summary>
/// The construction finishing level of a unit (درجة الإكساء).
/// </summary>
public enum FinishingLevel
{
    /// <summary>Structural shell without finishing (على العظم / على الهيكل).</summary>
    Unfinished = 1,

    /// <summary>Partially finished.</summary>
    PartiallyFinished = 2,

    /// <summary>Standard finishing (إكساء عادي).</summary>
    Standard = 3,

    /// <summary>Deluxe finishing (ديلوكس).</summary>
    Deluxe = 4,

    /// <summary>Super-deluxe finishing (سوبر ديلوكس).</summary>
    SuperDeluxe = 5
}
