using AI_LMS.API.Extensions;
using AI_LMS.Application.DTOs;
using AI_LMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AI_LMS.API.Controllers;

[Route("api/certificates")]
public sealed class CertificatesController(ICertificateService certificates) : ApiControllerBase
{
    [Authorize]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CertificateDto>>> List(CancellationToken cancellationToken)
    {
        int? studentUserId = User.IsInRole("Admin") || User.IsInRole("Trainer") ? null : CurrentUserId;
        return Ok(await certificates.ListAsync(studentUserId, cancellationToken));
    }

    [Authorize]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<CertificateDto>> Get(int id, CancellationToken cancellationToken)
    {
        var cert = await certificates.GetAsync(id, cancellationToken);
        if (cert is null) return NotFound();
        if (!User.IsInRole("Admin") && !User.IsInRole("Trainer") && cert.StudentName != User.Identity?.Name)
        {
            // Ownership is enforced by user id on the list endpoint; verification is intentionally public.
            var owned = await certificates.ListAsync(CurrentUserId, cancellationToken);
            if (!owned.Any(c => c.Id == cert.Id)) return Forbid();
        }
        return Ok(cert);
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<CertificateDto>> Issue(IssueCertificateRequest request, CancellationToken cancellationToken)
    {
        var staff = User.IsInRole("Admin") || User.IsInRole("Trainer");
        if (request.StudentUserId.HasValue && !staff) return Forbid();
        var studentUserId = staff ? request.StudentUserId ?? CurrentUserId : CurrentUserId;
        var cert = await certificates.IssueAsync(request.CourseId, studentUserId, staff, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, cert);
    }

    [AllowAnonymous]
    [HttpGet("verify/{verificationCode}")]
    public async Task<ActionResult<CertificateDto>> Verify(string verificationCode, CancellationToken cancellationToken)
    {
        var cert = await certificates.VerifyAsync(verificationCode, cancellationToken);
        return cert is null ? NotFound(new { success = false, message = "Certificate not found.", errors = Array.Empty<string>() }) : Ok(cert);
    }
}
