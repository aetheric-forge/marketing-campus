using AethericForge.Runtime.Abstractions.Interfaces.Institutions;
using AethericForge.Runtime.Models.Institutions;

namespace MarketingCampus.Institutions.Marketing;

public sealed class Marketing(IInstitutionContext context) : InstitutionBase(context), IMarketing;
