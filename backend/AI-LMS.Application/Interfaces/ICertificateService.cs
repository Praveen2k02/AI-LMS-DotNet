using AI_LMS.Application.DTOs;

namespace AI_LMS.Application.Interfaces;

public interface ICertificateService
{
    Task<IReadOnlyList<CertificateDto>> ListAsync(int? studentUserId, CancellationToken cancellationToken = default);
    Task<CertificateDto?> GetAsync(int id, CancellationToken cancellationToken = default);
    Task<CertificateDto> IssueAsync(int courseId, int studentUserId, bool staffOverride, CancellationToken cancellationToken = default);
    Task<CertificateDto?> VerifyAsync(string code, CancellationToken cancellationToken = default);
}
