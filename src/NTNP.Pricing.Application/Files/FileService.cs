using Microsoft.EntityFrameworkCore;
using NTNP.Pricing.Application.Common;
using NTNP.Pricing.Application.Exceptions;
using NTNP.Pricing.Contracts.Files;
using NTNP.Pricing.Domain.Entities;
using NTNP.Pricing.Domain.Enums;

namespace NTNP.Pricing.Application.Files;

public sealed class FileService : IFileService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public FileService(IApplicationDbContext db, ICurrentUserService currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<IReadOnlyList<StoredFileDto>> ListAsync(Guid? projectId, Guid? projectRevisionId, string? category, CancellationToken ct = default)
    {
        var q = _db.StoredFiles.AsNoTracking().AsQueryable();
        if (projectId is not null) q = q.Where(f => f.ProjectId == projectId);
        if (projectRevisionId is not null) q = q.Where(f => f.ProjectRevisionId == projectRevisionId);
        if (!string.IsNullOrWhiteSpace(category)) q = q.Where(f => f.Category.ToString() == category);

        var items = await q.OrderByDescending(f => f.CreatedAtUtc).ToListAsync(ct);
        return items.Select(ToDto).ToList();
    }

    public async Task<StoredFileDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var file = await _db.StoredFiles.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id, ct)
            ?? throw new NotFoundException(nameof(StoredFile), id);
        return ToDto(file);
    }

    public async Task<StoredFileDto> RegisterAsync(
        StoredFileResult savedFile, string fileName, string contentType, FileCategory category,
        Guid? projectId, Guid? projectRevisionId, CancellationToken ct = default)
    {
        var entity = new StoredFile
        {
            FileName = fileName,
            ContentType = contentType,
            Category = category,
            ProjectId = projectId,
            ProjectRevisionId = projectRevisionId,
            StoragePath = savedFile.StoragePath,
            SizeBytes = savedFile.SizeBytes,
            Sha256Hash = savedFile.Sha256Hash,
            CreatedByUserId = _currentUser.UserId,
            CreatedByUserName = _currentUser.UserName,
            CreatedAtUtc = _clock.UtcNow,
        };

        _db.StoredFiles.Add(entity);
        await _db.SaveChangesAsync(ct);

        return ToDto(entity);
    }

    public async Task<string> GetStoragePathAsync(Guid id, CancellationToken ct = default)
    {
        var path = await _db.StoredFiles.AsNoTracking().Where(f => f.Id == id).Select(f => f.StoragePath).FirstOrDefaultAsync(ct);
        return path ?? throw new NotFoundException(nameof(StoredFile), id);
    }

    private static StoredFileDto ToDto(StoredFile f) => new(
        f.Id, f.FileName, f.ContentType, f.Category.ToString(), f.ProjectId, f.ProjectRevisionId,
        f.SizeBytes, f.Sha256Hash, f.CreatedByUserName, f.CreatedAtUtc);
}
